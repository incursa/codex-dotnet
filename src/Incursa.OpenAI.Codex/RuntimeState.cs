using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Incursa.OpenAI.Codex;

internal sealed class CodexTurnSession
{
    private readonly object _gate = new();
    private readonly List<CodexInputItem> _steeredInput = new();
    private readonly List<CodexThreadItem> _items = new();
    private readonly CodexTurnConsumerGate? _consumerGate;
    private readonly Func<IReadOnlyList<CodexInputItem>, CancellationToken, Task> _steerHandler;
    private readonly Func<CancellationToken, Task> _interruptHandler;
    private bool _consumerClaimed;
    private bool _interruptRequested;
    private string _threadId;
    private string _id;
    private CodexTurnStatus _status = CodexTurnStatus.InProgress;
    private CodexTurnError? _error;
    private CodexUsage? _usage;

    public CodexTurnSession(
        string threadId,
        string turnId,
        IReadOnlyList<CodexInputItem> input,
        CodexTurnOptions? options,
        Func<IReadOnlyList<CodexInputItem>, CancellationToken, Task> steerHandler,
        Func<CancellationToken, Task> interruptHandler,
        CodexTurnConsumerGate? consumerGate = null)
    {
        _threadId = string.IsNullOrWhiteSpace(threadId) ? string.Empty : threadId;
        _id = string.IsNullOrWhiteSpace(turnId) ? string.Empty : turnId;
        Input = input;
        Options = options;
        _consumerGate = consumerGate;
        _steerHandler = steerHandler;
        _interruptHandler = interruptHandler;
        Channel = System.Threading.Channels.Channel.CreateUnbounded<CodexThreadEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false,
        });
    }

    public string ThreadId
    {
        get
        {
            lock (_gate)
            {
                return _threadId;
            }
        }
    }

    public string Id
    {
        get
        {
            lock (_gate)
            {
                return _id;
            }
        }
    }

    public IReadOnlyList<CodexInputItem> Input { get; }

    public CodexTurnOptions? Options { get; }

    public Channel<CodexThreadEvent> Channel { get; }

    public ChannelWriter<CodexThreadEvent> Writer => Channel.Writer;

    public bool IsInterruptRequested
    {
        get
        {
            lock (_gate)
            {
                return _interruptRequested;
            }
        }
    }

    public IReadOnlyList<CodexInputItem> SteeredInput
    {
        get
        {
            lock (_gate)
            {
                return _steeredInput.ToArray();
            }
        }
    }

    public IReadOnlyList<CodexThreadItem> Items
    {
        get
        {
            lock (_gate)
            {
                return _items.ToArray();
            }
        }
    }

    public CodexTurnStatus Status
    {
        get
        {
            lock (_gate)
            {
                return _status;
            }
        }
    }

    public CodexTurnError? Error
    {
        get
        {
            lock (_gate)
            {
                return _error;
            }
        }
    }

    public CodexUsage? Usage
    {
        get
        {
            lock (_gate)
            {
                return _usage;
            }
        }
    }

    public async IAsyncEnumerable<CodexThreadEvent> ReadEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CodexTurnConsumerGate.Lease? globalLease = null;
        if (_consumerGate is not null)
        {
            globalLease = await _consumerGate.AcquireAsync(Id, cancellationToken).ConfigureAwait(false);
        }

        if (!TryBeginConsumer())
        {
            if (globalLease is not null)
            {
                await globalLease.DisposeAsync().ConfigureAwait(false);
            }
            throw new InvalidOperationException($"Turn '{Id}' already has an active consumer.");
        }

        try
        {
            await foreach (CodexThreadEvent evt in Channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return evt;
            }
        }
        finally
        {
            EndConsumer();
            if (globalLease is not null)
            {
                await globalLease.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task SteerAsync(IReadOnlyList<CodexInputItem> input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_status is not CodexTurnStatus.InProgress)
            {
                throw new InvalidOperationException($"Turn '{Id}' is no longer active.");
            }
        }

        await _steerHandler(input, cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            _steeredInput.AddRange(input);
        }
    }

    public async Task InterruptAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_status is not CodexTurnStatus.InProgress)
            {
                throw new InvalidOperationException($"Turn '{Id}' is no longer active.");
            }
        }

        await _interruptHandler(cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            _interruptRequested = true;
            _status = CodexTurnStatus.Interrupted;
        }
    }

    public void InterruptFromTransport()
    {
        lock (_gate)
        {
            _interruptRequested = true;
            if (_status is CodexTurnStatus.InProgress)
            {
                _status = CodexTurnStatus.Interrupted;
            }
        }
    }

    public void AppendEvent(CodexThreadEvent evt)
    {
        lock (_gate)
        {
            switch (evt)
            {
                case CodexThreadStartedEvent started when !string.IsNullOrWhiteSpace(started.Thread.Id):
                    _threadId = started.Thread.Id;
                    break;
                case CodexTurnStartedEvent startedTurn when !string.IsNullOrWhiteSpace(startedTurn.Turn.Id):
                    _id = startedTurn.Turn.Id;
                    _status = CodexTurnStatus.InProgress;
                    break;
                case CodexItemCompletedEvent completed:
                    _items.Add(completed.Item);
                    break;
                case CodexTurnCompletedEvent completedTurn:
                    if (!string.IsNullOrWhiteSpace(completedTurn.Turn.Id))
                    {
                        _id = completedTurn.Turn.Id;
                    }
                    _status = completedTurn.Turn.Status;
                    _usage = completedTurn.Turn.Usage;
                    _error = completedTurn.Turn.Error;
                    if (completedTurn.Turn.Items.Count > 0)
                    {
                        _items.Clear();
                        _items.AddRange(completedTurn.Turn.Items);
                    }
                    break;
                case CodexTurnFailedEvent failedTurn:
                    if (!string.IsNullOrWhiteSpace(failedTurn.Turn.Id))
                    {
                        _id = failedTurn.Turn.Id;
                    }
                    _status = failedTurn.Turn.Status;
                    _usage = failedTurn.Turn.Usage;
                    _error = failedTurn.Turn.Error;
                    if (failedTurn.Turn.Items.Count > 0)
                    {
                        _items.Clear();
                        _items.AddRange(failedTurn.Turn.Items);
                    }
                    break;
                case CodexThreadErrorEvent threadError:
                    _status = CodexTurnStatus.Failed;
                    _error = threadError.Error;
                    break;
            }
        }

        Channel.Writer.TryWrite(evt);
    }

    public void CompleteWriter()
    {
        Channel.Writer.TryComplete();
    }

    public CodexTurnRecord ToRecord()
    {
        lock (_gate)
        {
            return new CodexTurnRecord
            {
                Id = _id,
                Status = _status,
                Items = _items.ToArray(),
                Error = _error,
                Usage = _usage,
            };
        }
    }

    public void BindThreadId(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return;
        }

        lock (_gate)
        {
            _threadId = threadId;
        }
    }

    public void BindTurnId(string turnId)
    {
        if (string.IsNullOrWhiteSpace(turnId))
        {
            return;
        }

        lock (_gate)
        {
            _id = turnId;
        }
    }

    public void CompleteSuccess(IReadOnlyList<CodexThreadItem> items, CodexUsage usage)
    {
        lock (_gate)
        {
            _status = CodexTurnStatus.Completed;
            _items.Clear();
            _items.AddRange(items);
            _usage = usage;
            _error = null;
        }
    }

    public void CompleteFailure(CodexTurnError error, CodexTurnStatus status)
    {
        lock (_gate)
        {
            _status = status;
            _error = error;
        }
    }

    private bool TryBeginConsumer()
    {
        lock (_gate)
        {
            if (_consumerClaimed)
            {
                return false;
            }

            _consumerClaimed = true;
            return true;
        }
    }

    private void EndConsumer()
    {
        lock (_gate)
        {
            _consumerClaimed = false;
        }
    }
}

internal sealed class CodexTurnConsumerGate
{
    private readonly object _gate = new();
    private string? _activeTurnId;

    public ValueTask<Lease> AcquireAsync(string turnId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_activeTurnId is not null)
            {
                throw new InvalidOperationException(
                    $"Turn consumer '{turnId}' cannot start while '{_activeTurnId}' is still active.");
            }

            _activeTurnId = turnId;
            return ValueTask.FromResult(new Lease(this, turnId));
        }
    }

    private void Release(string turnId)
    {
        lock (_gate)
        {
            if (string.Equals(_activeTurnId, turnId, StringComparison.Ordinal))
            {
                _activeTurnId = null;
            }
        }
    }

    internal sealed class Lease : IAsyncDisposable
    {
        private CodexTurnConsumerGate? _owner;
        private readonly string _turnId;

        public Lease(CodexTurnConsumerGate owner, string turnId)
        {
            _owner = owner;
            _turnId = turnId;
        }

        public ValueTask DisposeAsync()
        {
            CodexTurnConsumerGate? owner = Interlocked.Exchange(ref _owner, null);
            owner?.Release(_turnId);
            return ValueTask.CompletedTask;
        }
    }
}
