using System.Runtime.CompilerServices;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-CATALOG-0301, REQ-CODEX-SDK-CATALOG-0302, REQ-CODEX-SDK-CATALOG-0303, REQ-CODEX-SDK-CATALOG-0304,
// REQ-CODEX-SDK-CATALOG-0307, REQ-CODEX-SDK-CATALOG-0308, REQ-CODEX-SDK-CATALOG-0309, REQ-CODEX-SDK-CATALOG-0311, REQ-CODEX-SDK-CATALOG-0312.

public sealed class CodexClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private readonly CodexTurnConsumerGate _turnConsumerGate = new();
    private readonly ICodexTransport _transport;
    private bool _disposed;
    private bool _initialized;

    public CodexClient()
        : this(new CodexClientOptions())
    {
    }

    public CodexClient(CodexClientOptions? options)
    {
        Options = options ?? new CodexClientOptions();
        Options.ProcessLauncher ??= new ProcessCodexProcessLauncher();
        _transport = Options.BackendSelection switch
        {
            CodexBackendSelection.Exec => new CodexExecTransport(Options, _turnConsumerGate),
            _ => new CodexAppServerTransport(Options, _turnConsumerGate),
        };
    }

    public CodexClientOptions Options { get; }

    public CodexRuntimeMetadata? Metadata { get; private set; }

    public CodexRuntimeCapabilities? Capabilities { get; private set; }

    public async Task<CodexRuntimeMetadata> InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_initialized)
        {
            return Metadata ?? throw new InvalidOperationException("CodexClient initialization completed without metadata.");
        }

        await _initializationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return Metadata ?? throw new InvalidOperationException("CodexClient initialization completed without metadata.");
            }

            Metadata = await _transport.InitializeAsync(cancellationToken).ConfigureAwait(false);
            Capabilities = _transport.Capabilities;
            _initialized = true;
            return Metadata;
        }
        finally
        {
            _initializationGate.Release();
        }
    }

    public Task<bool> IsCodexAvailableAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(CodexExecutableResolver.IsAvailable(Options));
    }

    public async Task<CodexThread> StartThreadAsync(
        CodexThreadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsStartThread == true, nameof(StartThreadAsync));

        if (Options.BackendSelection == CodexBackendSelection.Exec)
        {
            return new CodexThread(this, options, started: false);
        }

        CodexThreadHandleState handle = await StartThreadHandleAsync(options, cancellationToken).ConfigureAwait(false);
        return new CodexThread(this, handle.Defaults ?? options, handle.Snapshot.Id, started: true);
    }

    public async Task<CodexThread> ResumeThreadAsync(
        string threadId,
        CodexThreadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsResumeThread == true, nameof(ResumeThreadAsync));

        if (Options.BackendSelection == CodexBackendSelection.Exec)
        {
            return new CodexThread(this, options, threadId, started: true);
        }

        CodexThreadHandleState handle = await ResumeThreadHandleAsync(threadId, options, cancellationToken).ConfigureAwait(false);
        return new CodexThread(this, handle.Defaults ?? options, handle.Snapshot.Id, started: true);
    }

    public async Task<CodexThread> ForkThreadAsync(
        string threadId,
        CodexThreadForkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        CodexThreadHandleState handle = await ForkThreadHandleAsync(threadId, options, cancellationToken).ConfigureAwait(false);
        return new CodexThread(this, handle.Defaults ?? options, handle.Snapshot.Id, started: true);
    }

    public async Task<CodexThreadListResult> ListThreadsAsync(
        CodexThreadListOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsListThreads == true, nameof(ListThreadsAsync));
        return await _transport.ListThreadsAsync(options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CodexThreadSnapshot> ReadThreadAsync(
        string threadId,
        CodexThreadReadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await ReadThreadSnapshotAsync(threadId, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task ArchiveThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsArchiveThread == true, nameof(ArchiveThreadAsync));
        await _transport.ArchiveThreadAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CodexThread> UnarchiveThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        CodexThreadHandleState handle = await UnarchiveThreadHandleAsync(threadId, cancellationToken).ConfigureAwait(false);
        return new CodexThread(this, handle.Defaults, handle.Snapshot.Id, started: true);
    }

    public async Task<CodexModelListResult> ListModelsAsync(
        CodexModelListOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsListModels == true, nameof(ListModelsAsync));
        return await _transport.ListModelsAsync(options, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _initializationGate.Dispose();
        await _transport.DisposeAsync().ConfigureAwait(false);
    }

    internal async Task<CodexThreadHandleState> StartThreadHandleAsync(
        CodexThreadOptions? options,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsStartThread == true, nameof(StartThreadAsync));
        return await _transport.StartThreadAsync(options, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexThreadHandleState> ResumeThreadHandleAsync(
        string threadId,
        CodexThreadOptions? options,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsResumeThread == true, nameof(ResumeThreadAsync));
        return await _transport.ResumeThreadAsync(threadId, options, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexThreadHandleState> ForkThreadHandleAsync(
        string threadId,
        CodexThreadForkOptions? options,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsForkThread == true, nameof(ForkThreadAsync));
        return await _transport.ForkThreadAsync(threadId, options, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexThreadHandleState> UnarchiveThreadHandleAsync(
        string threadId,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsUnarchiveThread == true, nameof(UnarchiveThreadAsync));
        return await _transport.UnarchiveThreadAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexThreadSnapshot> ReadThreadSnapshotAsync(
        string threadId,
        CodexThreadReadOptions? options,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsReadThread == true, nameof(ReadThreadAsync));
        return await _transport.ReadThreadAsync(threadId, options, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexThreadSnapshot> SetThreadNameAsync(
        string threadId,
        string name,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsSetThreadName == true, nameof(CodexThread.SetNameAsync));
        return await _transport.SetThreadNameAsync(threadId, name, cancellationToken).ConfigureAwait(false);
    }

    internal async Task CompactThreadAsync(string threadId, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsCompactThread == true, nameof(CodexThread.CompactAsync));
        await _transport.CompactThreadAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexTurnSession> StartTurnAsync(
        string? threadId,
        IReadOnlyList<CodexInputItem> input,
        CodexThreadOptions? threadOptions,
        CodexTurnOptions? options,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsThreadStreaming == true, nameof(CodexThread.StartTurnAsync));
        return await _transport.StartTurnAsync(threadId, input, threadOptions, options, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_initialized)
        {
            return;
        }

        await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    private void EnsureCapability(bool supported, string operation)
    {
        if (supported)
        {
            return;
        }

        throw new CodexCapabilityNotSupportedException(operation, Options.BackendSelection);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new CodexTransportClosedException();
        }
    }
}

public sealed class CodexThread
{
    private readonly CodexClient _client;
    private readonly CodexThreadOptions? _defaults;
    private readonly SemaphoreSlim _idGate = new(1, 1);
    private string? _id;
    private bool _started;

    internal CodexThread(CodexClient client, CodexThreadOptions? defaults, string? id = null, bool started = false)
    {
        _client = client;
        _defaults = defaults;
        _id = string.IsNullOrWhiteSpace(id) ? null : id;
        _started = started || _id is not null;
    }

    public string? Id => _id;

    public async Task<CodexRunResult> RunAsync(
        string input,
        CodexTurnOptions? options = null,
        CancellationToken cancellationToken = default)
        => await RunAsync(NormalizeInput(input), options, cancellationToken).ConfigureAwait(false);

    public async Task<CodexRunResult> RunAsync(
        IReadOnlyList<CodexInputItem> input,
        CodexTurnOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        CodexTurn turn = await StartTurnAsync(input, options, cancellationToken).ConfigureAwait(false);
        return await turn.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<CodexThreadEvent> RunStreamedAsync(
        string input,
        CodexTurnOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (CodexThreadEvent item in RunStreamedAsync(NormalizeInput(input), options, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<CodexThreadEvent> RunStreamedAsync(
        IReadOnlyList<CodexInputItem> input,
        CodexTurnOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CodexTurn turn = await StartTurnAsync(input, options, cancellationToken).ConfigureAwait(false);
        await foreach (CodexThreadEvent item in turn.StreamAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async Task<CodexTurn> StartTurnAsync(
        string input,
        CodexTurnOptions? options = null,
        CancellationToken cancellationToken = default)
        => await StartTurnAsync(NormalizeInput(input), options, cancellationToken).ConfigureAwait(false);

    public async Task<CodexTurn> StartTurnAsync(
        IReadOnlyList<CodexInputItem> input,
        CodexTurnOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        string? threadId = _id;
        if (_client.Options.BackendSelection == CodexBackendSelection.AppServer && string.IsNullOrWhiteSpace(threadId))
        {
            threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        }

        CodexTurnSession session = await _client.StartTurnAsync(threadId, input, _defaults, MergeTurnOptions(options), cancellationToken).ConfigureAwait(false);
        _started = true;
        if (!string.IsNullOrWhiteSpace(session.ThreadId))
        {
            _id = session.ThreadId;
        }
        return new CodexTurn(_client, session);
    }

    public async Task<CodexThreadSnapshot> ReadAsync(
        bool includeTurns = false,
        CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.ReadThreadSnapshotAsync(threadId, new CodexThreadReadOptions { IncludeTurns = includeTurns }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CodexThreadSnapshot> SetNameAsync(string name, CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.SetThreadNameAsync(threadId, name, cancellationToken).ConfigureAwait(false);
    }

    public async Task CompactAsync(CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        await _client.CompactThreadAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<CodexInputItem> NormalizeInput(string input)
        => [new CodexTextInput { Text = input }];

    private CodexTurnOptions MergeTurnOptions(CodexTurnOptions? options)
    {
        CodexThreadOptions defaults = _defaults ?? new CodexThreadOptions();
        return new CodexTurnOptions
        {
            ApprovalPolicy = options?.ApprovalPolicy ?? defaults.ApprovalPolicy,
            ApprovalsReviewer = options?.ApprovalsReviewer ?? defaults.ApprovalsReviewer,
            Effort = options?.Effort ?? defaults.ModelReasoningEffort,
            Model = options?.Model ?? defaults.Model,
            OutputSchema = options?.OutputSchema,
            Personality = options?.Personality ?? defaults.Personality,
            SandboxPolicy = options?.SandboxPolicy ?? defaults.Sandbox,
            ServiceTier = options?.ServiceTier ?? defaults.ServiceTier,
            Summary = options?.Summary,
            WorkingDirectory = options?.WorkingDirectory ?? defaults.WorkingDirectory,
        };
    }

    private async Task<string> EnsureThreadIdAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_id))
        {
            return _id!;
        }

        if (_started)
        {
            throw new InvalidOperationException("The thread id is not available until the first turn starts.");
        }

        await _idGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrWhiteSpace(_id))
            {
                return _id!;
            }

            CodexThreadHandleState handle = await _client.StartThreadHandleAsync(_defaults, cancellationToken).ConfigureAwait(false);
            _id = handle.Snapshot.Id;
            _started = true;
            return _id!;
        }
        finally
        {
            _idGate.Release();
        }
    }
}

public sealed class CodexTurn
{
    private readonly CodexTurnSession _session;

    internal CodexTurn(CodexClient client, CodexTurnSession session)
    {
        Client = client;
        _session = session;
        ThreadId = session.ThreadId;
        Id = session.Id;
    }

    public CodexClient Client { get; }

    public string ThreadId { get; }

    public string Id { get; }

    public async IAsyncEnumerable<CodexThreadEvent> StreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (CodexThreadEvent item in _session.ReadEventsAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async Task<CodexRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        CodexRunResult? completedResult = null;

        await foreach (CodexThreadEvent evt in StreamAsync(cancellationToken).ConfigureAwait(false))
        {
            switch (evt)
            {
                case CodexTurnCompletedEvent completed:
                    completedResult = new CodexRunResult
                    {
                        Items = _session.Items,
                        Usage = completed.Turn.Usage ?? _session.Usage,
                        FinalResponse = CodexResultHelpers.SelectFinalResponse(_session.Items),
                    };
                    break;
                case CodexTurnFailedEvent failed:
                    throw CodexResultHelpers.ToException(failed.Turn);
            }
        }

        return completedResult
            ?? throw new CodexInvalidRequestException($"Turn '{Id}' on thread '{ThreadId}' did not complete successfully.");
    }

    public async Task SteerAsync(string input, CancellationToken cancellationToken = default)
        => await SteerAsync([new CodexTextInput { Text = input }], cancellationToken).ConfigureAwait(false);

    public async Task SteerAsync(IReadOnlyList<CodexInputItem> input, CancellationToken cancellationToken = default)
    {
        await _session.SteerAsync(input, cancellationToken).ConfigureAwait(false);
    }

    public async Task InterruptAsync(CancellationToken cancellationToken = default)
    {
        await _session.InterruptAsync(cancellationToken).ConfigureAwait(false);
    }
}

