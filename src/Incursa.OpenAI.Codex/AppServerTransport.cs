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
        SupportsAccountRateLimits = true,
        SupportsListThreads = true,
        SupportsReadThread = true,
        SupportsForkThread = true,
        SupportsArchiveThread = true,
        SupportsUnarchiveThread = true,
        SupportsSetThreadName = true,
        SupportsCompactThread = true,
        SupportsThreadGoals = true,
        ExperimentalApi = true,
    };

    private readonly CodexClientOptions _options;
    private readonly CodexTurnConsumerGate _turnConsumerGate;
    private readonly CodexBroadcastObservable<CodexThreadEvent> _events = new();
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private readonly object _sessionGate = new();
    private readonly List<CodexTurnSession> _activeSessions = [];
    private readonly Dictionary<string, CodexTurnSession> _sessionsByTurnId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CodexTurnSession> _sessionsByThreadId = new(StringComparer.Ordinal);
    private readonly List<PendingNotification> _pendingNotifications = [];
    private int _sessionRegistrationsInFlight;
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

    public IObservable<CodexThreadEvent> ObserveEventsAsync() => _events;

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

    public async Task<CodexAccountRateLimitsResult> GetAccountRateLimitsAsync(CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("account/rateLimits/read", new JsonObject(), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseAccountRateLimitsResult(payload);
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

    public async Task<CodexThreadGoal?> GetThreadGoalAsync(string threadId, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/goal/get", CodexProtocol.BuildThreadGoalGetParams(threadId), cancellationToken).ConfigureAwait(false);
        return payload.TryGetPropertyValue("goal", out JsonNode? goalNode) && goalNode is JsonObject goalObject
            ? CodexProtocol.ParseThreadGoal(goalObject)
            : null;
    }

    public async Task<CodexThreadGoal> SetThreadGoalAsync(
        string threadId,
        string? objective,
        CodexThreadGoalStatus? status,
        long? tokenBudget,
        bool tokenBudgetSpecified,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync(
            "thread/goal/set",
            CodexProtocol.BuildThreadGoalSetParams(threadId, objective, status, tokenBudget, tokenBudgetSpecified),
            cancellationToken).ConfigureAwait(false);

        return CodexProtocol.ParseThreadGoal(GetRequiredObject(payload, "goal"));
    }

    public async Task<bool> ClearThreadGoalAsync(string threadId, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/goal/clear", CodexProtocol.BuildThreadGoalClearParams(threadId), cancellationToken).ConfigureAwait(false);
        return payload.TryGetPropertyValue("cleared", out JsonNode? clearedNode)
            && clearedNode is JsonValue clearedValue
            && clearedValue.TryGetValue<bool>(out bool cleared)
            && cleared;
    }

    public async Task<CodexThreadSnapshot> RollbackThreadAsync(string threadId, int numTurns, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/rollback", CodexProtocol.BuildThreadRollbackParams(threadId, numTurns), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseThreadSnapshot(GetRequiredObject(payload, "thread"));
    }

    public async Task<CodexThreadUnsubscribeStatus> UnsubscribeThreadAsync(string threadId, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/unsubscribe", CodexProtocol.BuildThreadUnsubscribeParams(threadId), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseThreadUnsubscribeStatus(GetString(payload, "status"));
    }

    public async Task<CodexThreadSnapshot> UpdateThreadMetadataAsync(string threadId, CodexGitInfo? gitInfo, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/metadata/update", CodexProtocol.BuildThreadMetadataUpdateParams(threadId, gitInfo), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseThreadSnapshot(GetRequiredObject(payload, "thread"));
    }

    public async Task<CodexThreadSnapshot> UpdateThreadMetadataAsync(
        string threadId,
        CodexThreadMetadataGitInfoUpdate gitInfo,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        JsonObject payload = await RequestObjectAsync("thread/metadata/update", CodexProtocol.BuildThreadMetadataUpdateParams(threadId, gitInfo), cancellationToken).ConfigureAwait(false);
        return CodexProtocol.ParseThreadSnapshot(GetRequiredObject(payload, "thread"));
    }

    public async Task ShellCommandThreadAsync(string threadId, string command, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await RequestAsync("thread/shellCommand", CodexProtocol.BuildThreadShellCommandParams(threadId, command), cancellationToken).ConfigureAwait(false);
    }

    public async Task<CodexTurnSession> AttachTurnAsync(
        string threadId,
        string turnId,
        CodexThreadOptions? threadOptions,
        CodexTurnAttachOptions? options,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(threadId))
        {
            throw new ArgumentException("Thread id must not be empty.", nameof(threadId));
        }

        if (string.IsNullOrWhiteSpace(turnId))
        {
            throw new ArgumentException("Turn id must not be empty.", nameof(turnId));
        }

        CodexThreadOptions? resumeOptions = options?.ResumeOptions ?? threadOptions;
        BeginSessionRegistration();
        try
        {
            JsonObject payload = await RequestObjectAsync(
                "thread/resume",
                CodexProtocol.BuildThreadResumeParams(threadId, resumeOptions),
                cancellationToken).ConfigureAwait(false);

            CodexThreadHandleState handle = CodexProtocol.ParseThreadHandleState(payload, resumeOptions);
            CodexTurnRecord? turn = handle.Snapshot.Turns.FirstOrDefault(candidate => string.Equals(candidate.Id, turnId, StringComparison.Ordinal));
            if (turn is null)
            {
                throw new CodexInvalidRequestException($"Turn '{turnId}' was not found on thread '{threadId}'.");
            }

            if (turn.Status is not CodexTurnStatus.InProgress)
            {
                throw new InvalidOperationException($"Turn '{turnId}' on thread '{threadId}' is not active.");
            }

            string resolvedThreadId = string.IsNullOrWhiteSpace(handle.Snapshot.Id) ? threadId : handle.Snapshot.Id;
            CodexTurnSession session = new(
                resolvedThreadId,
                turn.Id,
                [],
                BuildAttachedTurnOptions(handle.Snapshot, resumeOptions),
                async (activeTurnId, steeredInput, token) =>
                {
                    await _connection!.RequestAsync(
                        "turn/steer",
                        CodexProtocol.BuildTurnSteerParams(resolvedThreadId, activeTurnId, steeredInput),
                        token).ConfigureAwait(false);
                },
                async (activeTurnId, token) =>
                {
                    await _connection!.RequestAsync(
                        "turn/interrupt",
                        CodexProtocol.BuildTurnInterruptParams(resolvedThreadId, activeTurnId),
                        token).ConfigureAwait(false);
                },
                _turnConsumerGate);

            session.BindThreadId(resolvedThreadId);
            session.BindTurnId(turn.Id);
            session.SeedTurnRecord(turn);
            RegisterTurnSession(session);
            return session;
        }
        finally
        {
            EndSessionRegistration();
        }
    }

    public async Task<CodexTurnSession> StartTurnAsync(
        string? threadId,
        IReadOnlyList<CodexInputItem> input,
        CodexThreadOptions? threadOptions,
        CodexTurnOptions? options,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        BeginSessionRegistration();
        try
        {
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
                async (activeTurnId, steeredInput, token) =>
                {
                    await _connection!.RequestAsync(
                        "turn/steer",
                        CodexProtocol.BuildTurnSteerParams(resolvedThreadId, activeTurnId, steeredInput),
                        token).ConfigureAwait(false);
                },
                async (activeTurnId, token) =>
                {
                    await _connection!.RequestAsync(
                        "turn/interrupt",
                        CodexProtocol.BuildTurnInterruptParams(resolvedThreadId, activeTurnId),
                        token).ConfigureAwait(false);
                },
                _turnConsumerGate);

            session.BindThreadId(resolvedThreadId);
            session.BindTurnId(turn.Id);
            RegisterTurnSession(session);
            return session;
        }
        finally
        {
            EndSessionRegistration();
        }
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
        _events.Complete();
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
                _events.Publish(evt);
                DispatchNotification(evt);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            FailActiveSessions(exception);
            _events.Complete(_disposed ? null : exception);
        }
    }

    private void DispatchNotification(CodexThreadEvent evt)
    {
        (string? turnId, string? threadId) = GetRoutingIdentifiers(evt);
        bool hasRoutingIdentifier = !string.IsNullOrWhiteSpace(turnId) || !string.IsNullOrWhiteSpace(threadId);

        lock (_sessionGate)
        {
            CodexTurnSession? session = ResolveSessionLocked(turnId, threadId, allowUnkeyedFallback: !hasRoutingIdentifier);
            if (session is null)
            {
                if (hasRoutingIdentifier || _sessionRegistrationsInFlight > 0)
                {
                    _pendingNotifications.Add(new PendingNotification(evt, turnId, threadId));
                }

                return;
            }

            DeliverNotification(session, evt);
        }
    }

    private void BeginSessionRegistration()
    {
        lock (_sessionGate)
        {
            _sessionRegistrationsInFlight++;
        }
    }

    private void EndSessionRegistration()
    {
        lock (_sessionGate)
        {
            if (_sessionRegistrationsInFlight > 0)
            {
                _sessionRegistrationsInFlight--;
            }

            if (_sessionRegistrationsInFlight == 0 && _activeSessions.Count == 0)
            {
                _pendingNotifications.RemoveAll(static pending =>
                    string.IsNullOrWhiteSpace(pending.TurnId)
                    && string.IsNullOrWhiteSpace(pending.ThreadId));
            }
        }
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
            DispatchBufferedNotificationsLocked();
        }
    }

    private void DispatchBufferedNotificationsLocked()
    {
        while (true)
        {
            PendingNotification? pending = null;
            CodexTurnSession? session = null;

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
        ReindexSessionLocked(session);

        if (evt is CodexTurnCompletedEvent or CodexTurnFailedEvent)
        {
            CompleteAndUnregisterSession(session);
        }
    }

    private void ReindexSessionLocked(CodexTurnSession session)
    {
        RemoveSessionMappingsLocked(_sessionsByTurnId, session, session.Id);
        RemoveSessionMappingsLocked(_sessionsByThreadId, session, session.ThreadId);

        if (!string.IsNullOrWhiteSpace(session.Id))
        {
            _sessionsByTurnId[session.Id] = session;
        }

        if (!string.IsNullOrWhiteSpace(session.ThreadId))
        {
            _sessionsByThreadId[session.ThreadId] = session;
        }
    }

    private void CompleteAndUnregisterSession(CodexTurnSession session)
    {
        lock (_sessionGate)
        {
            RemoveSessionMappingsLocked(_sessionsByTurnId, session, exceptKey: null);
            RemoveSessionMappingsLocked(_sessionsByThreadId, session, exceptKey: null);
            _activeSessions.Remove(session);
        }

        session.CompleteWriter();
    }

    private static void RemoveSessionMappingsLocked(
        Dictionary<string, CodexTurnSession> sessions,
        CodexTurnSession session,
        string? exceptKey)
    {
        foreach (KeyValuePair<string, CodexTurnSession> pair in sessions.ToArray())
        {
            if (ReferenceEquals(pair.Value, session)
                && !string.Equals(pair.Key, exceptKey, StringComparison.Ordinal))
            {
                sessions.Remove(pair.Key);
            }
        }
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
            CodexThreadEvent failedEvent = new CodexTurnFailedEvent
            {
                ThreadId = session.ThreadId,
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
            };
            _events.Publish(failedEvent);
            session.AppendEvent(failedEvent);
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

        foreach (string overrideValue in CodexConfigSerialization.FlattenPlanModeOverrides(_options.PlanMode))
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

    private static string? GetString(JsonObject? payload, string name)
        => payload is not null && payload.TryGetPropertyValue(name, out JsonNode? node) ? node?.GetValue<string>() : null;

    private static CodexTurnOptions BuildAttachedTurnOptions(CodexThreadSnapshot snapshot, CodexThreadOptions? threadOptions)
        => new()
        {
            ApprovalPolicy = threadOptions?.ApprovalPolicy,
            ApprovalsReviewer = threadOptions?.ApprovalsReviewer,
            Effort = threadOptions?.ModelReasoningEffort,
            Model = threadOptions?.Model,
            Personality = threadOptions?.Personality,
            SandboxPolicy = threadOptions?.Sandbox,
            ServiceTier = threadOptions?.ServiceTier,
            WorkingDirectory = string.IsNullOrWhiteSpace(snapshot.Cwd) ? threadOptions?.WorkingDirectory : snapshot.Cwd,
        };

    private static (string? TurnId, string? ThreadId) GetRoutingIdentifiers(CodexThreadEvent evt)
    {
        return evt switch
        {
            CodexTurnStartedEvent turnStarted => (turnStarted.Turn.Id, turnStarted.ThreadId),
            CodexTurnCompletedEvent turnCompleted => (turnCompleted.Turn.Id, turnCompleted.ThreadId),
            CodexTurnFailedEvent turnFailed => (turnFailed.Turn.Id, turnFailed.ThreadId),
            CodexItemStartedEvent itemStarted => (itemStarted.TurnId, itemStarted.ThreadId),
            CodexItemUpdatedEvent itemUpdated => (itemUpdated.TurnId, itemUpdated.ThreadId),
            CodexItemCompletedEvent itemCompleted => (itemCompleted.TurnId, itemCompleted.ThreadId),
            CodexItemAutoApprovalReviewStartedEvent autoApprovalStarted => (autoApprovalStarted.TurnId, autoApprovalStarted.ThreadId),
            CodexItemAutoApprovalReviewCompletedEvent autoApprovalCompleted => (autoApprovalCompleted.TurnId, autoApprovalCompleted.ThreadId),
            CodexThreadErrorEvent threadError => (threadError.TurnId, threadError.ThreadId),
            CodexThreadStartedEvent threadStarted => (null, threadStarted.Thread.Id),
            CodexHookStartedEvent hookStarted => (hookStarted.TurnId, hookStarted.ThreadId),
            CodexHookCompletedEvent hookCompleted => (hookCompleted.TurnId, hookCompleted.ThreadId),
            CodexWarningEvent warning => (null, warning.ThreadId),
            CodexGuardianWarningEvent guardianWarning => (null, guardianWarning.ThreadId),
            CodexServerRequestResolvedEvent serverRequestResolved => (null, serverRequestResolved.ThreadId),
            CodexThreadGoalUpdatedEvent goalUpdated => (goalUpdated.TurnId, goalUpdated.ThreadId),
            CodexThreadGoalClearedEvent goalCleared => (null, goalCleared.ThreadId),
            CodexThreadStatusChangedEvent statusChanged => (null, statusChanged.ThreadId),
            CodexThreadArchivedEvent archived => (null, archived.ThreadId),
            CodexThreadClosedEvent closed => (null, closed.ThreadId),
            CodexThreadCompactedEvent compacted => (compacted.TurnId, compacted.ThreadId),
            CodexThreadNameUpdatedEvent nameUpdated => (null, nameUpdated.ThreadId),
            CodexThreadTokenUsageUpdatedEvent tokenUsageUpdated => (tokenUsageUpdated.TurnId, tokenUsageUpdated.ThreadId),
            CodexThreadUnarchivedEvent unarchived => (null, unarchived.ThreadId),
            CodexThreadSettingsUpdatedEvent settingsUpdated => (null, settingsUpdated.ThreadId),
            CodexTurnDiffUpdatedEvent diffUpdated => (diffUpdated.TurnId, diffUpdated.ThreadId),
            CodexTurnPlanUpdatedEvent planUpdated => (planUpdated.TurnId, planUpdated.ThreadId),
            CodexPlanDeltaEvent planDelta => (planDelta.TurnId, planDelta.ThreadId),
            CodexAgentMessageDeltaEvent agentMessageDelta => (agentMessageDelta.TurnId, agentMessageDelta.ThreadId),
            CodexCommandExecutionOutputDeltaEvent commandExecutionDelta => (commandExecutionDelta.TurnId, commandExecutionDelta.ThreadId),
            CodexCommandExecutionTerminalInteractionEvent terminalInteraction => (terminalInteraction.TurnId, terminalInteraction.ThreadId),
            CodexFileChangeOutputDeltaEvent fileChangeOutputDelta => (fileChangeOutputDelta.TurnId, fileChangeOutputDelta.ThreadId),
            CodexFileChangePatchUpdatedEvent fileChangePatchUpdated => (fileChangePatchUpdated.TurnId, fileChangePatchUpdated.ThreadId),
            CodexMcpToolCallProgressEvent mcpToolCallProgress => (mcpToolCallProgress.TurnId, mcpToolCallProgress.ThreadId),
            CodexReasoningTextDeltaEvent reasoningTextDelta => (reasoningTextDelta.TurnId, reasoningTextDelta.ThreadId),
            CodexReasoningSummaryPartAddedEvent reasoningSummaryPartAdded => (reasoningSummaryPartAdded.TurnId, reasoningSummaryPartAdded.ThreadId),
            CodexReasoningSummaryTextDeltaEvent reasoningSummaryTextDelta => (reasoningSummaryTextDelta.TurnId, reasoningSummaryTextDelta.ThreadId),
            CodexModelReroutedEvent modelRerouted => (modelRerouted.TurnId, modelRerouted.ThreadId),
            CodexModelVerificationEvent modelVerification => (modelVerification.TurnId, modelVerification.ThreadId),
            CodexThreadRealtimeStartedEvent realtimeStarted => (null, realtimeStarted.ThreadId),
            CodexThreadRealtimeItemAddedEvent realtimeItemAdded => (null, realtimeItemAdded.ThreadId),
            CodexThreadRealtimeTranscriptDeltaEvent realtimeTranscriptDelta => (null, realtimeTranscriptDelta.ThreadId),
            CodexThreadRealtimeTranscriptDoneEvent realtimeTranscriptDone => (null, realtimeTranscriptDone.ThreadId),
            CodexThreadRealtimeOutputAudioDeltaEvent realtimeOutputAudioDelta => (null, realtimeOutputAudioDelta.ThreadId),
            CodexThreadRealtimeSdpEvent realtimeSdp => (null, realtimeSdp.ThreadId),
            CodexThreadRealtimeErrorEvent realtimeError => (null, realtimeError.ThreadId),
            CodexThreadRealtimeClosedEvent realtimeClosed => (null, realtimeClosed.ThreadId),
            CodexRawResponseItemCompletedEvent rawResponseItemCompleted => (rawResponseItemCompleted.TurnId, rawResponseItemCompleted.ThreadId),
            CodexUnknownThreadEvent unknown => (GetString(unknown.RawPayload, "turnId"), GetString(unknown.RawPayload, "threadId")),
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

        if (!string.IsNullOrWhiteSpace(turnId)
            && string.IsNullOrWhiteSpace(threadId)
            && _activeSessions.Count == 1)
        {
            return _activeSessions[0];
        }

        if (allowUnkeyedFallback && _activeSessions.Count > 0)
        {
            return _activeSessions[^1];
        }

        return null;
    }

    private sealed record PendingNotification(CodexThreadEvent Event, string? TurnId, string? ThreadId);
}
