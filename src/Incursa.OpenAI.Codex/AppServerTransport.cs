using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-CATALOG-0302, REQ-CODEX-SDK-CATALOG-0303, REQ-CODEX-SDK-CATALOG-0304, REQ-CODEX-SDK-HELPERS-0318, REQ-CODEX-SDK-HELPERS-0319.

internal sealed class CodexAppServerTransport : ICodexTransport
{
    private static readonly CodexRuntimeCapabilities SupportedCapabilities = new()
    {
        BackendSelection = CodexBackendSelection.AppServer,
        SupportsStartThread = true,
        SupportsResumeThread = true,
        SupportsThreadStreaming = true,
        SupportsTurnSteering = true,
        SupportsTurnInterruption = true,
        SupportsListModels = true,
        SupportsListThreads = true,
        SupportsReadThread = true,
        SupportsForkThread = true,
        SupportsArchiveThread = true,
        SupportsUnarchiveThread = true,
        SupportsSetThreadName = true,
        SupportsCompactThread = true,
        ExperimentalApi = true,
    };

    private readonly CodexClientOptions _options;
    private readonly CodexTurnConsumerGate _turnConsumerGate;
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private readonly object _sessionGate = new();
    private readonly List<CodexTurnSession> _activeSessions = [];
    private readonly Dictionary<string, CodexTurnSession> _sessionsByTurnId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CodexTurnSession> _sessionsByThreadId = new(StringComparer.Ordinal);
    private readonly List<PendingNotification> _pendingNotifications = [];
    private JsonRpcConnection? _connection;
    private ICodexProcess? _process;
    private bool _disposed;
    private bool _initialized;
    private CodexRuntimeMetadata? _metadata;
    private Task? _notificationDispatcherTask;

    public CodexAppServerTransport(CodexClientOptions options, CodexTurnConsumerGate turnConsumerGate)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _turnConsumerGate = turnConsumerGate ?? throw new ArgumentNullException(nameof(turnConsumerGate));
    }

    public CodexRuntimeCapabilities Capabilities => SupportedCapabilities;

    public async Task<CodexRuntimeMetadata> InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        if (_initialized && _metadata is not null)
        {
            return _metadata;
        }

        await _initializeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized && _metadata is not null)
            {
                return _metadata;
            }

            if (_connection is null)
            {
                _process = await StartProcessAsync(cancellationToken).ConfigureAwait(false);
                _connection = new JsonRpcConnection(_process, HandleApprovalRequest);
                await _connection.InitializeStderrDrainAsync(cancellationToken).ConfigureAwait(false);
            }

            JsonObject initializeResponse = await RequestObjectAsync(
                "initialize",
                CodexProtocol.CreateInitializeRequest(_options),
                cancellationToken).ConfigureAwait(false);

            await _connection.NotifyAsync("initialized", new JsonObject(), cancellationToken).ConfigureAwait(false);
            EnsureNotificationDispatcherStarted();

            _metadata = CodexProtocol.NormalizeMetadata(ParseRuntimeMetadata(initializeResponse));
            _initialized = true;
            return _metadata;
        }
        catch
        {
            await DisposeConnectionAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            _initializeGate.Release();
        }
    }

    public async Task<CodexThreadHandleState> StartThreadAsync(CodexThreadOptions? options, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/start", CodexProtocol.BuildThreadStartParams(options), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseThreadHandleState(payload, options);
    }

    public async Task<CodexThreadHandleState> ResumeThreadAsync(string threadId, CodexThreadOptions? options, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/resume", CodexProtocol.BuildThreadResumeParams(threadId, options), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseThreadHandleState(payload, options);
    }

    public async Task<CodexThreadHandleState> ForkThreadAsync(string threadId, CodexThreadForkOptions? options, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/fork", CodexProtocol.BuildThreadForkParams(threadId, options), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseThreadHandleState(payload, options);
    }

    public async Task<CodexThreadListResult> ListThreadsAsync(CodexThreadListOptions? options, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonNode? payload = await RequestAsync("thread/list", CodexProtocol.BuildThreadListParams(options), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseThreadListResult(payload);
    }

    public async Task<CodexThreadSnapshot> ReadThreadAsync(string threadId, CodexThreadReadOptions? options, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/read", CodexProtocol.BuildThreadReadParams(threadId, options), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseThreadSnapshot(GetRequiredObject(payload, "thread"));
    }

    public async Task ArchiveThreadAsync(string threadId, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await RequestAsync("thread/archive", CodexProtocol.BuildThreadArchiveParams(threadId), cancellationToken).ConfigureAwait(false);
    }

    public async Task<CodexThreadHandleState> UnarchiveThreadAsync(string threadId, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/unarchive", CodexProtocol.BuildThreadUnarchiveParams(threadId), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseThreadHandleState(payload, defaults: null);
    }

    public async Task<CodexModelListResult> ListModelsAsync(CodexModelListOptions? options, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonNode? payload = await RequestAsync("model/list", CodexProtocol.BuildModelListParams(options), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseModelListResult(payload);
    }

    public async Task<CodexThreadSnapshot> SetThreadNameAsync(string threadId, string name, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/name/set", CodexProtocol.BuildThreadNameParams(threadId, name), cancellationToken).ConfigureAwait(false);
        return payload.TryGetPropertyValue("thread", out JsonNode? threadNode) && threadNode is JsonObject threadObject
            ? CodexProtocol.ParseThreadSnapshot(threadObject)
            : new CodexThreadSnapshot { Id = threadId, Name = name };
    }

    public async Task CompactThreadAsync(string threadId, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await RequestAsync("thread/compact/start", CodexProtocol.BuildThreadCompactParams(threadId), cancellationToken).ConfigureAwait(false);
    }

    public async Task<CodexTurnSession> StartTurnAsync(
        string? threadId,
        IReadOnlyList<CodexInputItem> input,
        CodexThreadOptions? threadOptions,
        CodexTurnOptions? options,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        JsonObject payload = await RequestObjectAsync(
            "turn/start",
            CodexProtocol.BuildTurnStartParams(threadId, input, options),
            cancellationToken).ConfigureAwait(false);

        JsonObject turnObject = GetRequiredObject(payload, "turn");
        CodexTurnRecord turn = CodexProtocol.ParseTurnRecord(turnObject);
        string resolvedThreadId = threadId ?? GetString(payload, "threadId") ?? string.Empty;
        CodexTurnSession session = new(
            resolvedThreadId,
            turn.Id,
            input,
            options,
            async (steeredInput, token) =>
            {
                await _connection!.RequestAsync(
                    "turn/steer",
                    CodexProtocol.BuildTurnSteerParams(resolvedThreadId, turn.Id, steeredInput),
                    token).ConfigureAwait(false);
            },
            async token =>
            {
                await _connection!.RequestAsync(
                    "turn/interrupt",
                    CodexProtocol.BuildTurnInterruptParams(resolvedThreadId, turn.Id),
                    token).ConfigureAwait(false);
            },
            _turnConsumerGate);

        session.BindThreadId(resolvedThreadId);
        session.BindTurnId(turn.Id);
        RegisterTurnSession(session);
        return session;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _initializeGate.Dispose();
        await DisposeConnectionAsync().ConfigureAwait(false);
    }

    private void EnsureNotificationDispatcherStarted()
    {
        if (_notificationDispatcherTask is not null)
        {
            return;
        }

        lock (_sessionGate)
        {
            _notificationDispatcherTask ??= RunNotificationDispatcherAsync();
        }
    }

    private async Task RunNotificationDispatcherAsync()
    {
        JsonRpcConnection connection = _connection ?? throw new CodexTransportClosedException();

        try
        {
            while (true)
            {
                JsonObject message = await connection.NextNotificationAsync(CancellationToken.None).ConfigureAwait(false);
                CodexThreadEvent evt = CodexProtocol.ParseThreadEvent(message);
                DispatchNotification(evt);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            FailActiveSessions(exception);
        }
    }

    private void DispatchNotification(CodexThreadEvent evt)
    {
        (string? turnId, string? threadId) = GetRoutingIdentifiers(evt);
        CodexTurnSession? session;

        lock (_sessionGate)
        {
            session = ResolveSessionLocked(turnId, threadId, allowUnkeyedFallback: string.IsNullOrWhiteSpace(turnId) && string.IsNullOrWhiteSpace(threadId));
            if (session is null)
            {
                _pendingNotifications.Add(new PendingNotification(evt, turnId, threadId));
                return;
            }
        }

        DeliverNotification(session, evt);
    }

    private void RegisterTurnSession(CodexTurnSession session)
    {
        lock (_sessionGate)
        {
            if (!string.IsNullOrWhiteSpace(session.Id))
            {
                _sessionsByTurnId[session.Id] = session;
            }

            if (!string.IsNullOrWhiteSpace(session.ThreadId))
            {
                _sessionsByThreadId[session.ThreadId] = session;
            }

            _activeSessions.Add(session);
        }

        DispatchBufferedNotifications();
    }

    private void DispatchBufferedNotifications()
    {
        while (true)
        {
            PendingNotification? pending = null;
            CodexTurnSession? session = null;

            lock (_sessionGate)
            {
                for (int i = 0; i < _pendingNotifications.Count; i++)
                {
                    PendingNotification candidate = _pendingNotifications[i];
                    session = ResolveSessionLocked(candidate.TurnId, candidate.ThreadId, allowUnkeyedFallback: string.IsNullOrWhiteSpace(candidate.TurnId) && string.IsNullOrWhiteSpace(candidate.ThreadId));
                    if (session is null)
                    {
                        continue;
                    }

                    pending = candidate;
                    _pendingNotifications.RemoveAt(i);
                    break;
                }
            }

            if (pending is null || session is null)
            {
                return;
            }

            DeliverNotification(session, pending.Event);
        }
    }

    private void DeliverNotification(CodexTurnSession session, CodexThreadEvent evt)
    {
        session.AppendEvent(evt);

        if (evt is CodexTurnCompletedEvent or CodexTurnFailedEvent)
        {
            CompleteAndUnregisterSession(session);
        }
    }

    private void CompleteAndUnregisterSession(CodexTurnSession session)
    {
        lock (_sessionGate)
        {
            if (!string.IsNullOrWhiteSpace(session.Id))
            {
                _sessionsByTurnId.Remove(session.Id);
            }

            if (!string.IsNullOrWhiteSpace(session.ThreadId))
            {
                _sessionsByThreadId.Remove(session.ThreadId);
            }

            _activeSessions.Remove(session);
        }

        session.CompleteWriter();
    }

    private void FailActiveSessions(Exception exception)
    {
        List<CodexTurnSession> sessions;
        lock (_sessionGate)
        {
            sessions = _activeSessions.ToList();
            _activeSessions.Clear();
            _sessionsByTurnId.Clear();
            _sessionsByThreadId.Clear();
            _pendingNotifications.Clear();
        }

        foreach (CodexTurnSession session in sessions)
        {
            session.AppendEvent(new CodexTurnFailedEvent
            {
                Turn = new CodexTurnRecord
                {
                    Id = session.Id,
                    Status = CodexTurnStatus.Failed,
                    Items = session.Items,
                    Error = new CodexTurnError
                    {
                        Message = exception.Message,
                        AdditionalDetails = exception.InnerException?.Message,
                    },
                },
            });
            session.CompleteWriter();
        }
    }

    private CodexRuntimeMetadata ParseRuntimeMetadata(JsonObject response)
    {
        CodexServerInfo? serverInfo = null;
        if (response.TryGetPropertyValue("serverInfo", out JsonNode? serverNode) && serverNode is JsonObject serverObject)
        {
            serverInfo = new CodexServerInfo
            {
                Name = GetString(serverObject, "name"),
                Version = GetString(serverObject, "version"),
            };
        }

        return new CodexRuntimeMetadata
        {
            ServerInfo = serverInfo,
            UserAgent = GetString(response, "userAgent"),
            PlatformFamily = GetString(response, "platformFamily"),
            PlatformOs = GetString(response, "platformOs"),
        };
    }

    private async Task<JsonNode?> RequestAsync(string method, JsonObject parameters, CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            throw new CodexTransportClosedException();
        }

        return await _connection.RequestAsync(method, parameters, cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonObject> RequestObjectAsync(string method, JsonObject parameters, CancellationToken cancellationToken)
    {
        JsonNode? payload = await RequestAsync(method, parameters, cancellationToken).ConfigureAwait(false);
        if (payload is not JsonObject obj)
        {
            throw new CodexInvalidRequestException($"{method} response must be a JSON object.");
        }

        return obj;
    }

    private async Task<CodexRuntimeMetadata> EnsureInitializedAsync(CancellationToken cancellationToken)
        => await InitializeAsync(cancellationToken).ConfigureAwait(false);

    private async Task<ICodexProcess> StartProcessAsync(CancellationToken cancellationToken)
    {
        string executablePath = CodexExecutableResolver.Resolve(_options);
        List<string> args = BuildLaunchArguments();
        ICodexProcessLauncher launcher = _options.ProcessLauncher ?? new ProcessCodexProcessLauncher();
        return await launcher.StartAsync(
            new CodexProcessStartInfo(
                executablePath,
                args,
                WorkingDirectory: null,
                CodexExecutableResolver.BuildEnvironment(_options)),
            cancellationToken).ConfigureAwait(false);
    }

    private List<string> BuildLaunchArguments()
    {
        List<string> args = [];
        foreach (string overrideValue in CodexConfigSerialization.FlattenConfigOverrides(_options.Config))
        {
            args.Add("--config");
            args.Add(overrideValue);
        }

        args.Add("app-server");
        args.Add("--listen");
        args.Add("stdio://");
        return args;
    }

    private JsonObject HandleApprovalRequest(string method, JsonObject? request)
    {
        if (_options.ApprovalHandler is not null)
        {
            JsonObject? handled = _options.ApprovalHandler(method, request);
            return handled ?? new JsonObject();
        }

        return method switch
        {
            "item/commandExecution/requestApproval" => new JsonObject { ["decision"] = "accept" },
            "item/fileChange/requestApproval" => new JsonObject { ["decision"] = "accept" },
            _ => new JsonObject(),
        };
    }

    private async Task DisposeConnectionAsync()
    {
        if (_connection is not null)
        {
            try
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Best-effort shutdown.
            }
            finally
            {
                _connection = null;
                _process = null;
            }
        }
        else if (_process is not null)
        {
            try
            {
                await _process.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Best-effort shutdown.
            }
            finally
            {
                _process = null;
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new CodexTransportClosedException();
        }
    }

    private static JsonObject GetRequiredObject(JsonObject payload, string name)
    {
        if (payload.TryGetPropertyValue(name, out JsonNode? node) && node is JsonObject obj)
        {
            return obj;
        }

        throw new CodexInvalidRequestException($"Expected '{name}' to be a JSON object.");
    }

    private static string? GetString(JsonObject payload, string name)
        => payload.TryGetPropertyValue(name, out JsonNode? node) ? node?.GetValue<string>() : null;

    private static (string? TurnId, string? ThreadId) GetRoutingIdentifiers(CodexThreadEvent evt)
    {
        return evt switch
        {
            CodexTurnStartedEvent turnStarted => (turnStarted.Turn.Id, null),
            CodexTurnCompletedEvent turnCompleted => (turnCompleted.Turn.Id, null),
            CodexTurnFailedEvent turnFailed => (turnFailed.Turn.Id, null),
            CodexItemStartedEvent itemStarted => (itemStarted.TurnId, itemStarted.ThreadId),
            CodexItemUpdatedEvent itemUpdated => (itemUpdated.TurnId, itemUpdated.ThreadId),
            CodexItemCompletedEvent itemCompleted => (itemCompleted.TurnId, itemCompleted.ThreadId),
            CodexThreadErrorEvent threadError => (threadError.TurnId, threadError.ThreadId),
            CodexThreadStartedEvent threadStarted => (null, threadStarted.Thread.Id),
            _ => (null, null),
        };
    }

    private CodexTurnSession? ResolveSessionLocked(string? turnId, string? threadId, bool allowUnkeyedFallback)
    {
        if (!string.IsNullOrWhiteSpace(turnId) && _sessionsByTurnId.TryGetValue(turnId, out CodexTurnSession? byTurn))
        {
            return byTurn;
        }

        if (!string.IsNullOrWhiteSpace(threadId) && _sessionsByThreadId.TryGetValue(threadId, out CodexTurnSession? byThread))
        {
            return byThread;
        }

        if (allowUnkeyedFallback && _activeSessions.Count > 0)
        {
            return _activeSessions[^1];
        }

        return null;
    }

    private sealed record PendingNotification(CodexThreadEvent Event, string? TurnId, string? ThreadId);
}


