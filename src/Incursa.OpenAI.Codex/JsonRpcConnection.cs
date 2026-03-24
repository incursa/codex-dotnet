using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-TRANSPORT-0247, REQ-CODEX-SDK-TRANSPORT-0248, REQ-CODEX-SDK-CATALOG-0308, REQ-CODEX-SDK-HELPERS-0314, REQ-CODEX-SDK-HELPERS-0318, REQ-CODEX-SDK-HELPERS-0319.

internal sealed class JsonRpcConnection : IAsyncDisposable
{
    private readonly ICodexProcess _process;
    private readonly Func<string, JsonObject?, JsonObject?> _approvalHandler;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonNode?>> _pendingRequests = new(StringComparer.Ordinal);
    private readonly Channel<JsonObject> _notificationChannel = Channel.CreateUnbounded<JsonObject>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true,
        AllowSynchronousContinuations = false,
    });
    private readonly List<string> _stderrLines = new();
    private readonly object _startGate = new();
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _stdoutTask;
    private Task? _stderrTask;
    private bool _disposed;
    private bool _started;

    public JsonRpcConnection(ICodexProcess process, Func<string, JsonObject?, JsonObject?> approvalHandler)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
        _approvalHandler = approvalHandler ?? throw new ArgumentNullException(nameof(approvalHandler));
    }

    public Task InitializeStderrDrainAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        EnsureStarted();
        return Task.CompletedTask;
    }

    public async Task<JsonNode?> RequestAsync(string method, JsonObject? parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        EnsureStarted();

        string id = Guid.NewGuid().ToString("N");
        TaskCompletionSource<JsonNode?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRequests.TryAdd(id, completion))
        {
            throw new InvalidOperationException($"A pending request already exists for id '{id}'.");
        }

        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(() =>
            CancelPendingRequest(id, completion, cancellationToken));

        try
        {
            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                WriteMessage(new JsonObject
                {
                    ["id"] = id,
                    ["method"] = method,
                    ["params"] = parameters ?? new JsonObject(),
                });
            }
            finally
            {
                _writeGate.Release();
            }

            return await completion.Task.ConfigureAwait(false);
        }
        finally
        {
            _pendingRequests.TryRemove(id, out _);
        }
    }

    public async Task NotifyAsync(string method, JsonObject? parameters, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        EnsureStarted();

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            WriteMessage(new JsonObject
            {
                ["method"] = method,
                ["params"] = parameters ?? new JsonObject(),
            });
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<JsonObject> NextNotificationAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        EnsureStarted();

        return await _notificationChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    public string GetStderrTail(int limit = 40)
    {
        lock (_stderrLines)
        {
            int start = Math.Max(0, _stderrLines.Count - limit);
            return string.Join(Environment.NewLine, _stderrLines.Skip(start));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _shutdown.Cancel();
        FailPendingRequests(new CodexTransportClosedException());
        _notificationChannel.Writer.TryComplete(new CodexTransportClosedException());

        try
        {
            await _process.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            await WaitForBackgroundTasksAsync().ConfigureAwait(false);
            _writeGate.Dispose();
            _shutdown.Dispose();
        }
    }

    private void EnsureStarted()
    {
        if (_started)
        {
            return;
        }

        lock (_startGate)
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _stdoutTask = ReadLoopAsync(_shutdown.Token);
            _stderrTask = DrainStderrAsync(_shutdown.Token);
        }
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                string? line = await _process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                {
                    throw new CodexTransportClosedException($"The Codex app-server closed stdout. stderr_tail={GetStderrTail()}");
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                JsonObject message = ParseMessage(line);
                if (TryHandleServerRequest(message, out string requestId, out JsonObject? response))
                {
                    await WriteMessageAsync(new JsonObject
                    {
                        ["id"] = requestId,
                        ["result"] = response ?? new JsonObject(),
                    }, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (TryHandleResponse(message))
                {
                    continue;
                }

                if (IsNotification(message))
                {
                    _notificationChannel.Writer.TryWrite(message);
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            FailPendingRequests(exception);
            _notificationChannel.Writer.TryComplete(exception);
            return;
        }
        finally
        {
            _notificationChannel.Writer.TryComplete();
        }
    }

    private async Task DrainStderrAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                string? line = await _process.StandardError.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                lock (_stderrLines)
                {
                    _stderrLines.Add(line);
                    if (_stderrLines.Count > 400)
                    {
                        _stderrLines.RemoveAt(0);
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            lock (_stderrLines)
            {
                _stderrLines.Add($"[stderr reader failed] {exception.Message}");
            }
        }
    }

    private async Task WaitForBackgroundTasksAsync()
    {
        Task? stdoutTask = _stdoutTask;
        Task? stderrTask = _stderrTask;

        if (stdoutTask is not null)
        {
            try
            {
                await stdoutTask.ConfigureAwait(false);
            }
            catch
            {
                // Best-effort shutdown: the reader may terminate because the process exited.
            }
        }

        if (stderrTask is not null)
        {
            try
            {
                await stderrTask.ConfigureAwait(false);
            }
            catch
            {
                // Best-effort shutdown: the reader may terminate because the process exited.
            }
        }
    }

    private async Task WriteMessageAsync(JsonObject message, CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            WriteMessage(message);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private void WriteMessage(JsonObject message)
    {
        if (_process.StandardInput is null)
        {
            throw new CodexTransportClosedException();
        }

        _process.StandardInput.WriteLine(message.ToJsonString());
        _process.StandardInput.Flush();
    }

    private bool TryHandleServerRequest(JsonObject message, out string requestId, out JsonObject? response)
    {
        requestId = string.Empty;
        response = null;

        if (!message.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
        {
            return false;
        }

        if (!message.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
        {
            return false;
        }

        requestId = idNode.GetValue<string>();
        string? method = methodValue.GetValue<string>();
        JsonObject? parameters = message.TryGetPropertyValue("params", out JsonNode? paramsNode) && paramsNode is JsonObject paramsObject
            ? paramsObject
            : null;

        response = _approvalHandler(method ?? string.Empty, parameters);
        return true;
    }

    private bool TryHandleResponse(JsonObject message)
    {
        if (!message.TryGetPropertyValue("id", out JsonNode? responseIdNode) || responseIdNode is null)
        {
            return false;
        }

        if (message.TryGetPropertyValue("method", out _))
        {
            return false;
        }

        string responseId = responseIdNode.GetValue<string>();
        if (!_pendingRequests.TryRemove(responseId, out TaskCompletionSource<JsonNode?>? completion))
        {
            return false;
        }

        if (message.TryGetPropertyValue("error", out JsonNode? errorNode) && errorNode is not null)
        {
            completion.TrySetException(CodexRetryPolicy.MapJsonRpcError(errorNode.AsObject()));
            return true;
        }

        JsonNode? result = message.TryGetPropertyValue("result", out JsonNode? resultNode) ? resultNode : null;
        completion.TrySetResult(result);
        return true;
    }

    private void FailPendingRequests(Exception exception)
    {
        foreach (KeyValuePair<string, TaskCompletionSource<JsonNode?>> pair in _pendingRequests)
        {
            if (_pendingRequests.TryRemove(pair.Key, out TaskCompletionSource<JsonNode?>? completion))
            {
                completion.TrySetException(exception);
            }
        }
    }

    private void CancelPendingRequest(string id, TaskCompletionSource<JsonNode?> completion, CancellationToken cancellationToken)
    {
        if (_pendingRequests.TryRemove(id, out TaskCompletionSource<JsonNode?>? pending))
        {
            pending.TrySetCanceled(cancellationToken);
        }
        else
        {
            completion.TrySetCanceled(cancellationToken);
        }
    }

    private static JsonObject ParseMessage(string line)
    {
        JsonNode? node = JsonNode.Parse(line);
        if (node is not JsonObject message)
        {
            throw new CodexInvalidRequestException("Malformed JSON-RPC message.");
        }

        return message;
    }

    private static bool IsNotification(JsonObject message)
        => message.TryGetPropertyValue("method", out _) && !message.ContainsKey("id");

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new CodexTransportClosedException();
        }
    }
}


