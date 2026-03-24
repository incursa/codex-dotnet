using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace Incursa.OpenAI.Codex.Tests;

internal sealed class ScriptedCodexProcessLauncher : ICodexProcessLauncher
{
    public List<CodexProcessStartInfo> StartInfos { get; } = [];

    public ScriptedCodexProcess? LastProcess { get; private set; }

    public Func<CodexProcessStartInfo, ScriptedCodexProcess>? Factory { get; set; }

    public Task<ICodexProcess> StartAsync(CodexProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        StartInfos.Add(startInfo);
        LastProcess = Factory?.Invoke(startInfo) ?? new ScriptedCodexProcess();
        return Task.FromResult<ICodexProcess>(LastProcess);
    }
}

internal sealed class ScriptedCodexProcess : ICodexProcess
{
    private readonly TaskCompletionSource<int> _exitSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public QueueTextReader StdOut { get; } = new();

    public QueueTextReader StdErr { get; } = new();

    public CapturingTextWriter StdIn { get; } = new();

    public bool KillCalled { get; private set; }

    public int? ProcessId { get; set; } = 12345;

    public bool HasExited { get; private set; }

    public int ExitCode { get; private set; }

    public TextReader StandardOutput => StdOut;

    public TextReader StandardError => StdErr;

    public TextWriter StandardInput => StdIn;

    public Task WaitForExitAsync(CancellationToken cancellationToken)
        => _exitSignal.Task.WaitAsync(cancellationToken);

    public void Kill()
    {
        KillCalled = true;
        Complete(137);
    }

    public void Complete(int exitCode = 0)
    {
        if (HasExited)
        {
            return;
        }

        HasExited = true;
        ExitCode = exitCode;
        StdOut.Complete();
        StdErr.Complete();
        _exitSignal.TrySetResult(exitCode);
    }

    public void EnqueueStdout(string line) => StdOut.Enqueue(line);

    public void EnqueueStdout(JsonObject line) => EnqueueStdout(line.ToJsonString());

    public void EnqueueStderr(string line) => StdErr.Enqueue(line);

    public void EnqueueStderr(JsonObject line) => EnqueueStderr(line.ToJsonString());

    public ValueTask DisposeAsync()
    {
        Complete(0);
        StdIn.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed class QueueTextReader : TextReader
{
    private readonly Channel<string> _lines = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false,
    });
    private readonly TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task ReadStarted => _readStarted.Task;

    public void Enqueue(string line)
        => _lines.Writer.TryWrite(line);

    public void Complete()
        => _lines.Writer.TryComplete();

    public override async Task<string?> ReadLineAsync()
    {
        _readStarted.TrySetResult();
        try
        {
            return await _lines.Reader.ReadAsync().ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }
}

internal sealed class CapturingTextWriter : TextWriter
{
    private readonly StringBuilder _buffer = new();

    public List<string> Lines { get; } = [];

    public Action<string>? LineWritten { get; set; }

    public Action<string>? TextWritten { get; set; }

    public string Text => _buffer.ToString();

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(string? value)
    {
        if (value is null)
        {
            return;
        }

        _buffer.Append(value);
        TextWritten?.Invoke(value);
    }

    public override void Write(char value)
    {
        _buffer.Append(value);
        TextWritten?.Invoke(value.ToString());
    }

    public override void Write(char[] buffer, int index, int count)
    {
        string text = new(buffer, index, count);
        _buffer.Append(text);
        TextWritten?.Invoke(text);
    }

    public override void WriteLine(string? value)
    {
        string text = value ?? string.Empty;
        Lines.Add(text);
        _buffer.AppendLine(text);
        LineWritten?.Invoke(text);
    }

    public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
    {
        string text = new(buffer.Span);
        _buffer.Append(text);
        TextWritten?.Invoke(text);
        return Task.CompletedTask;
    }
}

internal static class TestJson
{
    public static JsonObject Message(string method, JsonObject? parameters = null, string? id = null)
    {
        JsonObject message = new()
        {
            ["method"] = method,
        };

        if (parameters is not null)
        {
            message["params"] = parameters;
        }

        if (!string.IsNullOrWhiteSpace(id))
        {
            message["id"] = id;
        }

        return message;
    }

    public static JsonObject Notification(string method, JsonObject? parameters = null)
        => Message(method, parameters);

    public static JsonObject Response(string id, JsonObject result)
        => new()
        {
            ["id"] = id,
            ["result"] = result,
        };

    public static JsonObject ErrorResponse(string id, int code, string message, JsonNode? data = null)
        => new()
        {
            ["id"] = id,
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message,
                ["data"] = data,
            },
        };
}
