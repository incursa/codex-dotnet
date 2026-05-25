using System.Runtime.CompilerServices;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-CATALOG-0301, REQ-CODEX-SDK-CATALOG-0302, REQ-CODEX-SDK-CATALOG-0303, REQ-CODEX-SDK-CATALOG-0304,
// REQ-CODEX-SDK-CATALOG-0307, REQ-CODEX-SDK-CATALOG-0308, REQ-CODEX-SDK-CATALOG-0309, REQ-CODEX-SDK-CATALOG-0311, REQ-CODEX-SDK-CATALOG-0312.

/// <summary>
/// Provides the high-level entry point for communicating with a Codex runtime.
/// </summary>
public sealed class CodexClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private readonly CodexTurnConsumerGate _turnConsumerGate = new();
    private readonly ICodexTransport _transport;
    private bool _disposed;
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodexClient"/> class with default options.
    /// </summary>
    public CodexClient()
        : this(new CodexClientOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodexClient"/> class.
    /// </summary>
    /// <param name="options">The client options to use, or <see langword="null"/> to use defaults.</param>
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

    /// <summary>
    /// Gets the options used by this client.
    /// </summary>
    public CodexClientOptions Options { get; }

    /// <summary>
    /// Gets runtime metadata after the client has been initialized.
    /// </summary>
    public CodexRuntimeMetadata? Metadata { get; private set; }

    /// <summary>
    /// Gets runtime capabilities after the client has been initialized.
    /// </summary>
    public CodexRuntimeCapabilities? Capabilities { get; private set; }

    /// <summary>
    /// Observes all runtime events received by this client after subscription.
    /// </summary>
    /// <returns>An observable stream of raw runtime events from the selected backend.</returns>
    public IObservable<CodexThreadEvent> ObserveEventsAsync()
    {
        ThrowIfDisposed();
        return _transport.ObserveEventsAsync();
    }

    /// <summary>
    /// Initializes the selected Codex backend and reads runtime metadata.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the initialization request.</param>
    /// <returns>The metadata returned by the Codex runtime.</returns>
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

    /// <summary>
    /// Checks whether the configured Codex executable is available.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the availability check.</param>
    /// <returns><see langword="true"/> when Codex can be resolved; otherwise, <see langword="false"/>.</returns>
    public Task<bool> IsCodexAvailableAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(CodexExecutableResolver.IsAvailable(Options));
    }

    /// <summary>
    /// Starts a new Codex thread.
    /// </summary>
    /// <param name="options">Thread defaults to apply when creating the thread.</param>
    /// <param name="cancellationToken">A token that cancels the start request.</param>
    /// <returns>A handle for the new Codex thread.</returns>
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

    /// <summary>
    /// Resumes an existing Codex thread.
    /// </summary>
    /// <param name="threadId">The identifier of the thread to resume.</param>
    /// <param name="options">Thread defaults to apply after resuming the thread.</param>
    /// <param name="cancellationToken">A token that cancels the resume request.</param>
    /// <returns>A handle for the resumed Codex thread.</returns>
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

    /// <summary>
    /// Forks an existing Codex thread.
    /// </summary>
    /// <param name="threadId">The identifier of the thread to fork.</param>
    /// <param name="options">Fork options to apply to the new thread.</param>
    /// <param name="cancellationToken">A token that cancels the fork request.</param>
    /// <returns>A handle for the forked Codex thread.</returns>
    public async Task<CodexThread> ForkThreadAsync(
        string threadId,
        CodexThreadForkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        CodexThreadHandleState handle = await ForkThreadHandleAsync(threadId, options, cancellationToken).ConfigureAwait(false);
        return new CodexThread(this, handle.Defaults ?? options, handle.Snapshot.Id, started: true);
    }

    /// <summary>
    /// Lists Codex threads visible to the selected backend.
    /// </summary>
    /// <param name="options">Filters and paging options for the list request.</param>
    /// <param name="cancellationToken">A token that cancels the list request.</param>
    /// <returns>The page of matching Codex threads.</returns>
    public async Task<CodexThreadListResult> ListThreadsAsync(
        CodexThreadListOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsListThreads == true, nameof(ListThreadsAsync));
        return await _transport.ListThreadsAsync(options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads a Codex thread snapshot.
    /// </summary>
    /// <param name="threadId">The identifier of the thread to read.</param>
    /// <param name="options">Options that control how much thread data is returned.</param>
    /// <param name="cancellationToken">A token that cancels the read request.</param>
    /// <returns>The requested thread snapshot.</returns>
    public async Task<CodexThreadSnapshot> ReadThreadAsync(
        string threadId,
        CodexThreadReadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await ReadThreadSnapshotAsync(threadId, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Archives a Codex thread.
    /// </summary>
    /// <param name="threadId">The identifier of the thread to archive.</param>
    /// <param name="cancellationToken">A token that cancels the archive request.</param>
    /// <returns>A task that completes when the archive request has finished.</returns>
    public async Task ArchiveThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsArchiveThread == true, nameof(ArchiveThreadAsync));
        await _transport.ArchiveThreadAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Restores an archived Codex thread.
    /// </summary>
    /// <param name="threadId">The identifier of the thread to unarchive.</param>
    /// <param name="cancellationToken">A token that cancels the unarchive request.</param>
    /// <returns>A handle for the unarchived Codex thread.</returns>
    public async Task<CodexThread> UnarchiveThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        CodexThreadHandleState handle = await UnarchiveThreadHandleAsync(threadId, cancellationToken).ConfigureAwait(false);
        return new CodexThread(this, handle.Defaults, handle.Snapshot.Id, started: true);
    }

    /// <summary>
    /// Lists models available to the selected Codex backend.
    /// </summary>
    /// <param name="options">Options that control the model list request.</param>
    /// <param name="cancellationToken">A token that cancels the list request.</param>
    /// <returns>The page of available models.</returns>
    public async Task<CodexModelListResult> ListModelsAsync(
        CodexModelListOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsListModels == true, nameof(ListModelsAsync));
        return await _transport.ListModelsAsync(options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the current Codex account rate-limit snapshot from the app-server backend.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the rate-limit request.</param>
    /// <returns>The current account-level rate-limit buckets and reset windows.</returns>
    public async Task<CodexAccountRateLimitsResult> GetAccountRateLimitsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsAccountRateLimits == true, nameof(GetAccountRateLimitsAsync));
        return await _transport.GetAccountRateLimitsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases resources held by the selected Codex transport.
    /// </summary>
    /// <returns>A task-like value that completes when disposal has finished.</returns>
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

    internal async Task<CodexThreadGoal?> GetThreadGoalAsync(string threadId, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsThreadGoals == true, nameof(CodexThread.GetGoalAsync));
        return await _transport.GetThreadGoalAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexThreadGoal> SetThreadGoalAsync(
        string threadId,
        string? objective,
        CodexThreadGoalStatus? status,
        long? tokenBudget,
        bool tokenBudgetSpecified,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsThreadGoals == true, nameof(CodexThread.SetGoalAsync));
        return await _transport.SetThreadGoalAsync(threadId, objective, status, tokenBudget, tokenBudgetSpecified, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<bool> ClearThreadGoalAsync(string threadId, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsThreadGoals == true, nameof(CodexThread.ClearGoalAsync));
        return await _transport.ClearThreadGoalAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexThreadSnapshot> RollbackThreadAsync(string threadId, int numTurns, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        return await _transport.RollbackThreadAsync(threadId, numTurns, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexThreadUnsubscribeStatus> UnsubscribeThreadAsync(string threadId, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        return await _transport.UnsubscribeThreadAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexThreadSnapshot> UpdateThreadMetadataAsync(string threadId, CodexGitInfo? gitInfo, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        return await _transport.UpdateThreadMetadataAsync(threadId, gitInfo, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexThreadSnapshot> UpdateThreadMetadataAsync(
        string threadId,
        CodexThreadMetadataGitInfoUpdate gitInfo,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        return await _transport.UpdateThreadMetadataAsync(threadId, gitInfo, cancellationToken).ConfigureAwait(false);
    }

    internal async Task ShellCommandThreadAsync(string threadId, string command, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await _transport.ShellCommandThreadAsync(threadId, command, cancellationToken).ConfigureAwait(false);
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

    internal async Task<CodexTurnSession> AttachTurnAsync(
        string threadId,
        string turnId,
        CodexThreadOptions? threadOptions,
        CodexTurnAttachOptions? options,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        EnsureCapability(Capabilities?.SupportsResumeThread == true, nameof(CodexThread.AttachTurnAsync));
        EnsureCapability(Capabilities?.SupportsThreadStreaming == true, nameof(CodexThread.AttachTurnAsync));
        return await _transport.AttachTurnAsync(threadId, turnId, threadOptions, options, cancellationToken).ConfigureAwait(false);
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

/// <summary>
/// Represents a Codex conversation thread that can run turns and read thread state.
/// </summary>
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

    /// <summary>
    /// Gets the thread identifier when it is known.
    /// </summary>
    public string? Id => _id;

    /// <summary>
    /// Runs a turn with a single text input and waits for completion.
    /// </summary>
    /// <param name="input">The text input to send to Codex.</param>
    /// <param name="options">Options that apply to this turn.</param>
    /// <param name="cancellationToken">A token that cancels the run.</param>
    /// <returns>The completed turn result.</returns>
    public async Task<CodexRunResult> RunAsync(
        string input,
        CodexTurnOptions? options = null,
        CancellationToken cancellationToken = default)
        => await RunAsync(NormalizeInput(input), options, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Runs a turn with structured input items and waits for completion.
    /// </summary>
    /// <param name="input">The input items to send to Codex.</param>
    /// <param name="options">Options that apply to this turn.</param>
    /// <param name="cancellationToken">A token that cancels the run.</param>
    /// <returns>The completed turn result.</returns>
    public async Task<CodexRunResult> RunAsync(
        IReadOnlyList<CodexInputItem> input,
        CodexTurnOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        CodexTurn turn = await StartTurnAsync(input, options, cancellationToken).ConfigureAwait(false);
        return await turn.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs a turn with a single text input and streams runtime events.
    /// </summary>
    /// <param name="input">The text input to send to Codex.</param>
    /// <param name="options">Options that apply to this turn.</param>
    /// <param name="cancellationToken">A token that cancels the stream.</param>
    /// <returns>An asynchronous stream of thread events emitted by the turn.</returns>
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

    /// <summary>
    /// Runs a turn with structured input items and streams runtime events.
    /// </summary>
    /// <param name="input">The input items to send to Codex.</param>
    /// <param name="options">Options that apply to this turn.</param>
    /// <param name="cancellationToken">A token that cancels the stream.</param>
    /// <returns>An asynchronous stream of thread events emitted by the turn.</returns>
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

    /// <summary>
    /// Starts a turn with a single text input without waiting for completion.
    /// </summary>
    /// <param name="input">The text input to send to Codex.</param>
    /// <param name="options">Options that apply to this turn.</param>
    /// <param name="cancellationToken">A token that cancels the start request.</param>
    /// <returns>A handle for the started Codex turn.</returns>
    public async Task<CodexTurn> StartTurnAsync(
        string input,
        CodexTurnOptions? options = null,
        CancellationToken cancellationToken = default)
        => await StartTurnAsync(NormalizeInput(input), options, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Starts a turn with structured input items without waiting for completion.
    /// </summary>
    /// <param name="input">The input items to send to Codex.</param>
    /// <param name="options">Options that apply to this turn.</param>
    /// <param name="cancellationToken">A token that cancels the start request.</param>
    /// <returns>A handle for the started Codex turn.</returns>
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

    /// <summary>
    /// Attaches a handle to an already-running turn on this thread.
    /// </summary>
    /// <param name="turnId">The identifier of the in-flight turn to attach.</param>
    /// <param name="options">Options that control the thread resume used for the attach operation.</param>
    /// <param name="cancellationToken">A token that cancels the attach request.</param>
    /// <returns>A handle that streams subsequent events for the active turn.</returns>
    public async Task<CodexTurn> AttachTurnAsync(
        string turnId,
        CodexTurnAttachOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(turnId))
        {
            throw new ArgumentException("Turn id must not be empty.", nameof(turnId));
        }

        if (string.IsNullOrWhiteSpace(_id))
        {
            throw new InvalidOperationException("Cannot attach a turn until the thread id is known.");
        }

        CodexThreadOptions? resumeOptions = options?.ResumeOptions ?? _defaults;
        CodexTurnSession session = await _client.AttachTurnAsync(_id!, turnId, resumeOptions, options, cancellationToken).ConfigureAwait(false);
        _started = true;
        if (!string.IsNullOrWhiteSpace(session.ThreadId))
        {
            _id = session.ThreadId;
        }

        return new CodexTurn(_client, session);
    }

    /// <summary>
    /// Reads the latest snapshot for this thread.
    /// </summary>
    /// <param name="includeTurns">Whether to include turn records in the snapshot.</param>
    /// <param name="cancellationToken">A token that cancels the read request.</param>
    /// <returns>The requested thread snapshot.</returns>
    public async Task<CodexThreadSnapshot> ReadAsync(
        bool includeTurns = false,
        CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.ReadThreadSnapshotAsync(threadId, new CodexThreadReadOptions { IncludeTurns = includeTurns }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the display name for this thread.
    /// </summary>
    /// <param name="name">The display name to assign.</param>
    /// <param name="cancellationToken">A token that cancels the rename request.</param>
    /// <returns>The updated thread snapshot.</returns>
    public async Task<CodexThreadSnapshot> SetNameAsync(string name, CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.SetThreadNameAsync(threadId, name, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Requests server-side compaction for this thread.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the compaction request.</param>
    /// <returns>A task that completes when the compaction request has finished.</returns>
    public async Task CompactAsync(CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        await _client.CompactThreadAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the current goal for this thread, if one is set.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the goal request.</param>
    /// <returns>The current goal, or <see langword="null"/> when no goal is set.</returns>
    public async Task<CodexThreadGoal?> GetGoalAsync(CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.GetThreadGoalAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets this thread's goal objective and marks it active.
    /// </summary>
    /// <param name="objective">The objective Codex should keep pursuing.</param>
    /// <param name="tokenBudget">Optional token budget to assign to the goal.</param>
    /// <param name="cancellationToken">A token that cancels the goal request.</param>
    /// <returns>The updated thread goal.</returns>
    public async Task<CodexThreadGoal> SetGoalAsync(
        string objective,
        long? tokenBudget = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objective))
        {
            throw new ArgumentException("Goal objective cannot be blank.", nameof(objective));
        }

        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.SetThreadGoalAsync(
            threadId,
            objective.Trim(),
            CodexThreadGoalStatus.Active,
            tokenBudget,
            tokenBudgetSpecified: tokenBudget.HasValue,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates this thread's current goal status.
    /// </summary>
    /// <param name="status">The new goal status.</param>
    /// <param name="cancellationToken">A token that cancels the goal request.</param>
    /// <returns>The updated thread goal.</returns>
    public async Task<CodexThreadGoal> SetGoalStatusAsync(
        CodexThreadGoalStatus status,
        CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.SetThreadGoalAsync(
            threadId,
            objective: null,
            status,
            tokenBudget: null,
            tokenBudgetSpecified: false,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears this thread's goal.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the goal request.</param>
    /// <returns><see langword="true"/> when a goal was cleared; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> ClearGoalAsync(CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.ClearThreadGoalAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Rolls back this thread by removing turns from the tail of its history.
    /// </summary>
    /// <param name="numTurns">The number of turns to drop from the end of the thread.</param>
    /// <param name="cancellationToken">A token that cancels the rollback request.</param>
    /// <returns>The updated thread snapshot.</returns>
    public async Task<CodexThreadSnapshot> RollbackAsync(int numTurns = 1, CancellationToken cancellationToken = default)
    {
        if (numTurns < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(numTurns));
        }

        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.RollbackThreadAsync(threadId, numTurns, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Unsubscribes this thread from live updates.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the unsubscribe request.</param>
    /// <returns>The unsubscribe status returned by the runtime.</returns>
    public async Task<CodexThreadUnsubscribeStatus> UnsubscribeAsync(CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.UnsubscribeThreadAsync(threadId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the stored Git metadata associated with this thread.
    /// </summary>
    /// <param name="gitInfo">The Git metadata to apply, or <see langword="null"/> to clear the stored metadata.</param>
    /// <param name="cancellationToken">A token that cancels the update request.</param>
    /// <returns>The updated thread snapshot.</returns>
    public async Task<CodexThreadSnapshot> UpdateMetadataAsync(CodexGitInfo? gitInfo, CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.UpdateThreadMetadataAsync(threadId, gitInfo, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the stored Git metadata associated with this thread using the patch wrapper shape.
    /// </summary>
    /// <param name="gitInfo">The Git metadata patch to apply.</param>
    /// <param name="cancellationToken">A token that cancels the update request.</param>
    /// <returns>The updated thread snapshot.</returns>
    public async Task<CodexThreadSnapshot> UpdateMetadataAsync(
        CodexThreadMetadataGitInfoUpdate gitInfo,
        CancellationToken cancellationToken = default)
    {
        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        return await _client.UpdateThreadMetadataAsync(threadId, gitInfo, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs a shell command attached to this thread.
    /// </summary>
    /// <param name="command">The shell command to execute.</param>
    /// <param name="cancellationToken">A token that cancels the command request.</param>
    /// <returns>A task that completes when the command request finishes.</returns>
    public async Task ShellCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command must not be empty.", nameof(command));
        }

        string threadId = await EnsureThreadIdAsync(cancellationToken).ConfigureAwait(false);
        await _client.ShellCommandThreadAsync(threadId, command, cancellationToken).ConfigureAwait(false);
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

/// <summary>
/// Represents an active Codex turn and exposes streaming, steering, and interruption operations.
/// </summary>
public sealed class CodexTurn
{
    private readonly CodexTurnSession _session;
    private readonly CodexReplayObservable<CodexThreadEvent> _events;
    private readonly CodexNormalizedTurnObservable _normalizedEvents;

    internal CodexTurn(CodexClient client, CodexTurnSession session)
    {
        Client = client;
        _session = session;
        _events = new CodexReplayObservable<CodexThreadEvent>(_session.ReadEventsAsync);
        _normalizedEvents = new CodexNormalizedTurnObservable(_events, () => CreateOutcomeBuilder());
    }

    /// <summary>
    /// Gets the client that owns this turn.
    /// </summary>
    public CodexClient Client { get; }

    /// <summary>
    /// Gets the identifier of the thread that owns this turn.
    /// </summary>
    public string ThreadId => _session.ThreadId;

    /// <summary>
    /// Gets the identifier of this turn.
    /// </summary>
    public string Id => _session.Id;

    /// <summary>
    /// Streams events emitted while this turn runs.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the event stream.</param>
    /// <returns>An asynchronous stream of thread events.</returns>
    public IAsyncEnumerable<CodexThreadEvent> StreamAsync(CancellationToken cancellationToken = default)
        => CodexObservableAdapters.ToAsyncEnumerable(ObserveEventsAsync(), cancellationToken);

    /// <summary>
    /// Observes events emitted while this turn runs.
    /// </summary>
    /// <returns>An observable stream of thread events.</returns>
    public IObservable<CodexThreadEvent> ObserveEventsAsync()
        => _events;

    /// <summary>
    /// Streams normalized turn events emitted while this turn runs.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the event stream.</param>
    /// <returns>An asynchronous stream of normalized turn events.</returns>
    public IAsyncEnumerable<CodexTurnEvent> StreamNormalizedAsync(CancellationToken cancellationToken = default)
        => CodexObservableAdapters.ToAsyncEnumerable(ObserveNormalizedEventsAsync(), cancellationToken);

    /// <summary>
    /// Observes normalized turn events emitted while this turn runs.
    /// </summary>
    /// <returns>An observable stream of normalized turn events.</returns>
    public IObservable<CodexTurnEvent> ObserveNormalizedEventsAsync()
        => _normalizedEvents;

    /// <summary>
    /// Runs this turn to stream closeout and returns detailed terminal-state diagnostics.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the run.</param>
    /// <returns>The detailed turn result.</returns>
    public async Task<CodexTurnResult> RunToResultAsync(CancellationToken cancellationToken = default)
    {
        CodexTurnOutcomeBuilder outcomeBuilder = CreateOutcomeBuilder();
        try
        {
            await foreach (CodexThreadEvent item in StreamAsync(cancellationToken).ConfigureAwait(false))
            {
                outcomeBuilder.Process(item);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            outcomeBuilder.RecordStreamException(exception);
        }

        outcomeBuilder.CompleteStream();
        return outcomeBuilder.ToResult();
    }

    /// <summary>
    /// Runs this turn to completion and returns the final result.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the run.</param>
    /// <returns>The completed turn result.</returns>
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

    /// <summary>
    /// Sends additional text input to a running turn.
    /// </summary>
    /// <param name="input">The text input used to steer the turn.</param>
    /// <param name="cancellationToken">A token that cancels the steer request.</param>
    /// <returns>A task that completes when the steer request has been sent.</returns>
    public async Task SteerAsync(string input, CancellationToken cancellationToken = default)
        => await SteerAsync([new CodexTextInput { Text = input }], cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Sends additional structured input to a running turn.
    /// </summary>
    /// <param name="input">The input items used to steer the turn.</param>
    /// <param name="cancellationToken">A token that cancels the steer request.</param>
    /// <returns>A task that completes when the steer request has been sent.</returns>
    public async Task SteerAsync(IReadOnlyList<CodexInputItem> input, CancellationToken cancellationToken = default)
    {
        await _session.SteerAsync(input, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Requests interruption of a running turn.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the interrupt request.</param>
    /// <returns>A task that completes when the interrupt request has been sent.</returns>
    public async Task InterruptAsync(CancellationToken cancellationToken = default)
    {
        await _session.InterruptAsync(cancellationToken).ConfigureAwait(false);
    }

    private CodexTurnOutcomeBuilder CreateOutcomeBuilder()
        => new(ThreadId, Id, _session.Options?.WorkingDirectory);
}
