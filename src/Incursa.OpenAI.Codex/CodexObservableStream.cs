using System.Threading.Channels;

namespace Incursa.OpenAI.Codex;

internal sealed class CodexReplayObservable<T> : IObservable<T>
{
    private readonly object _gate = new();
    private readonly Func<CancellationToken, IAsyncEnumerable<T>> _sourceFactory;
    private readonly List<T> _history = new();
    private readonly List<Subscription> _subscriptions = new();
    private CancellationTokenSource? _pumpCancellation;
    private Task? _pumpTask;
    private bool _completed;
    private Exception? _error;

    public CodexReplayObservable(Func<CancellationToken, IAsyncEnumerable<T>> sourceFactory)
    {
        _sourceFactory = sourceFactory;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        Subscription subscription = new(this, observer);
        T[] history;
        bool completed;
        Exception? error;

        lock (_gate)
        {
            history = _history.ToArray();
            completed = _completed;
            error = _error;

            if (!completed)
            {
                _subscriptions.Add(subscription);
            }

            foreach (T item in history)
            {
                subscription.TryWrite(item);
            }

            if (completed)
            {
                subscription.Complete(error);
            }
            else
            {
                EnsurePumpStarted();
            }
        }

        return subscription;
    }

    private void EnsurePumpStarted()
    {
        if (_pumpTask is { IsCompleted: false })
        {
            return;
        }

        _pumpCancellation = new CancellationTokenSource();
        CancellationTokenSource pumpCancellation = _pumpCancellation;
        _pumpTask = Task.Run(() => PumpAsync(pumpCancellation));
    }

    private async Task PumpAsync(CancellationTokenSource pumpCancellation)
    {
        bool canceled = false;
        Exception? error = null;

        try
        {
            await foreach (T item in _sourceFactory(pumpCancellation.Token).ConfigureAwait(false))
            {
                Publish(item);
            }
        }
        catch (OperationCanceledException) when (pumpCancellation.IsCancellationRequested)
        {
            canceled = true;
        }
        catch (Exception exception)
        {
            error = exception;
        }

        if (canceled)
        {
            ResetPump(pumpCancellation);
            return;
        }

        Complete(error);
    }

    private void Publish(T item)
    {
        Subscription[] subscriptions;
        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            _history.Add(item);
            subscriptions = _subscriptions.ToArray();
        }

        foreach (Subscription subscription in subscriptions)
        {
            subscription.TryWrite(item);
        }
    }

    private void Complete(Exception? error)
    {
        Subscription[] subscriptions;
        CancellationTokenSource? pumpCancellation;

        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            _error = error;
            subscriptions = _subscriptions.ToArray();
            _subscriptions.Clear();
            pumpCancellation = _pumpCancellation;
            _pumpCancellation = null;
            _pumpTask = null;
        }

        pumpCancellation?.Dispose();

        foreach (Subscription subscription in subscriptions)
        {
            subscription.Complete(error);
        }
    }

    private void ResetPump(CancellationTokenSource pumpCancellation)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_pumpCancellation, pumpCancellation))
            {
                _pumpCancellation = null;
                _pumpTask = null;
                if (!_completed && _subscriptions.Count > 0)
                {
                    EnsurePumpStarted();
                }
            }
        }

        pumpCancellation.Dispose();
    }

    private void Remove(Subscription subscription)
    {
        CancellationTokenSource? cancellationToRequest = null;

        lock (_gate)
        {
            _subscriptions.Remove(subscription);
            if (!_completed && _subscriptions.Count == 0)
            {
                cancellationToRequest = _pumpCancellation;
            }
        }

        cancellationToRequest?.Cancel();
    }

    private sealed class Subscription : IDisposable
    {
        private readonly CodexReplayObservable<T> _owner;
        private readonly IObserver<T> _observer;
        private readonly Channel<T> _channel;
        private int _disposed;
        private bool _suppressTerminalNotification;

        public Subscription(CodexReplayObservable<T> owner, IObserver<T> observer)
        {
            _owner = owner;
            _observer = observer;
            _channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });
            _ = DispatchAsync();
        }

        public bool TryWrite(T item)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return false;
            }

            return _channel.Writer.TryWrite(item);
        }

        public void Complete(Exception? error)
        {
            _channel.Writer.TryComplete(error);
        }

        public void Dispose()
        {
            Dispose(suppressTerminalNotification: true);
        }

        private void Dispose(bool suppressTerminalNotification)
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _suppressTerminalNotification = suppressTerminalNotification;
            _owner.Remove(this);
            _channel.Writer.TryComplete();
        }

        private async Task DispatchAsync()
        {
            try
            {
                await foreach (T item in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    try
                    {
                        _observer.OnNext(item);
                    }
                    catch (Exception exception)
                    {
                        NotifyError(exception);
                        Dispose(suppressTerminalNotification: true);
                        return;
                    }
                }

                if (!_suppressTerminalNotification)
                {
                    _observer.OnCompleted();
                }
            }
            catch (Exception exception)
            {
                if (!_suppressTerminalNotification)
                {
                    _observer.OnError(exception);
                }
            }
            finally
            {
                _owner.Remove(this);
            }
        }

        private void NotifyError(Exception exception)
        {
            try
            {
                _observer.OnError(exception);
            }
            catch
            {
                // Observer callbacks must not break the shared stream pump.
            }
        }
    }
}

internal sealed class CodexBroadcastObservable<T> : IObservable<T>
{
    private readonly object _gate = new();
    private readonly List<Subscription> _subscriptions = [];
    private bool _completed;
    private Exception? _error;

    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        Subscription subscription = new(this, observer);
        bool completed;
        Exception? error;

        lock (_gate)
        {
            completed = _completed;
            error = _error;

            if (completed)
            {
                subscription.Complete(error);
            }
            else
            {
                _subscriptions.Add(subscription);
            }
        }

        return subscription;
    }

    public void Publish(T item)
    {
        Subscription[] subscriptions;
        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            subscriptions = _subscriptions.ToArray();
        }

        foreach (Subscription subscription in subscriptions)
        {
            subscription.TryWrite(item);
        }
    }

    public void Complete(Exception? error = null)
    {
        Subscription[] subscriptions;

        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            _error = error;
            subscriptions = _subscriptions.ToArray();
            _subscriptions.Clear();
        }

        foreach (Subscription subscription in subscriptions)
        {
            subscription.Complete(error);
        }
    }

    private void Remove(Subscription subscription)
    {
        lock (_gate)
        {
            _subscriptions.Remove(subscription);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly CodexBroadcastObservable<T> _owner;
        private readonly IObserver<T> _observer;
        private readonly Channel<T> _channel;
        private int _disposed;
        private bool _suppressTerminalNotification;

        public Subscription(CodexBroadcastObservable<T> owner, IObserver<T> observer)
        {
            _owner = owner;
            _observer = observer;
            _channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });
            _ = DispatchAsync();
        }

        public bool TryWrite(T item)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return false;
            }

            return _channel.Writer.TryWrite(item);
        }

        public void Complete(Exception? error)
        {
            _channel.Writer.TryComplete(error);
        }

        public void Dispose()
        {
            Dispose(suppressTerminalNotification: true);
        }

        private void Dispose(bool suppressTerminalNotification)
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _suppressTerminalNotification = suppressTerminalNotification;
            _owner.Remove(this);
            _channel.Writer.TryComplete();
        }

        private async Task DispatchAsync()
        {
            try
            {
                await foreach (T item in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    try
                    {
                        _observer.OnNext(item);
                    }
                    catch (Exception exception)
                    {
                        NotifyError(exception);
                        Dispose(suppressTerminalNotification: true);
                        return;
                    }
                }

                if (!_suppressTerminalNotification)
                {
                    _observer.OnCompleted();
                }
            }
            catch (Exception exception)
            {
                if (!_suppressTerminalNotification)
                {
                    _observer.OnError(exception);
                }
            }
            finally
            {
                _owner.Remove(this);
            }
        }

        private void NotifyError(Exception exception)
        {
            try
            {
                _observer.OnError(exception);
            }
            catch
            {
                // Observer callbacks must not break the shared event stream.
            }
        }
    }
}

internal sealed class CodexNormalizedTurnObservable : IObservable<CodexTurnEvent>
{
    private readonly IObservable<CodexThreadEvent> _source;
    private readonly Func<CodexTurnOutcomeBuilder> _outcomeBuilderFactory;

    public CodexNormalizedTurnObservable(
        IObservable<CodexThreadEvent> source,
        Func<CodexTurnOutcomeBuilder> outcomeBuilderFactory)
    {
        _source = source;
        _outcomeBuilderFactory = outcomeBuilderFactory;
    }

    public IDisposable Subscribe(IObserver<CodexTurnEvent> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        CodexTurnOutcomeBuilder outcomeBuilder = _outcomeBuilderFactory();
        return _source.Subscribe(new NormalizingObserver(observer, outcomeBuilder));
    }

    private sealed class NormalizingObserver : IObserver<CodexThreadEvent>
    {
        private readonly IObserver<CodexTurnEvent> _observer;
        private readonly CodexTurnOutcomeBuilder _outcomeBuilder;

        public NormalizingObserver(IObserver<CodexTurnEvent> observer, CodexTurnOutcomeBuilder outcomeBuilder)
        {
            _observer = observer;
            _outcomeBuilder = outcomeBuilder;
        }

        public void OnNext(CodexThreadEvent value)
        {
            foreach (CodexTurnEvent normalized in _outcomeBuilder.Process(value))
            {
                _observer.OnNext(normalized);
            }
        }

        public void OnCompleted()
        {
            foreach (CodexTurnEvent normalized in _outcomeBuilder.CompleteStream())
            {
                _observer.OnNext(normalized);
            }

            _observer.OnCompleted();
        }

        public void OnError(Exception error)
        {
            if (error is OperationCanceledException)
            {
                _observer.OnError(error);
                return;
            }

            _outcomeBuilder.RecordStreamException(error);
            foreach (CodexTurnEvent normalized in _outcomeBuilder.CompleteStream())
            {
                _observer.OnNext(normalized);
            }

            _observer.OnCompleted();
        }
    }
}

internal static class CodexObservableAdapters
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        IObservable<T> observable,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observable);
        cancellationToken.ThrowIfCancellationRequested();

        Channel<T> channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        AsyncEnumerableObserver<T> channelObserver = new(channel.Writer, cancellationToken);
        using CancellationTokenRegistration registration = cancellationToken.Register(static state =>
        {
            AsyncEnumerableObserver<T> observer = (AsyncEnumerableObserver<T>)state!;
            observer.Cancel();
        }, channelObserver);
        using IDisposable subscription = observable.Subscribe(channelObserver);
        channelObserver.Attach(subscription);

        try
        {
            await foreach (T item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            subscription.Dispose();
        }
    }

    private sealed class AsyncEnumerableObserver<TItem> : IObserver<TItem>
    {
        private readonly ChannelWriter<TItem> _writer;
        private readonly CancellationToken _cancellationToken;
        private IDisposable? _subscription;

        public AsyncEnumerableObserver(
            ChannelWriter<TItem> writer,
            CancellationToken cancellationToken)
        {
            _writer = writer;
            _cancellationToken = cancellationToken;
        }

        public void Attach(IDisposable subscription)
        {
            _subscription = subscription;
        }

        public void OnNext(TItem value)
        {
            _writer.TryWrite(value);
        }

        public void OnError(Exception error)
        {
            _writer.TryComplete(error);
        }

        public void OnCompleted()
        {
            _writer.TryComplete();
        }

        public void Cancel()
        {
            _subscription?.Dispose();
            _writer.TryComplete(new OperationCanceledException(_cancellationToken));
        }
    }
}
