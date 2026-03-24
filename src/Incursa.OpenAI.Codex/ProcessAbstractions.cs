using System.Diagnostics;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-TRANSPORT-0233, REQ-CODEX-SDK-TRANSPORT-0236, REQ-CODEX-SDK-HELPERS-0314.

internal sealed record CodexProcessStartInfo(
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment);

internal interface ICodexProcessLauncher
{
    Task<ICodexProcess> StartAsync(CodexProcessStartInfo startInfo, CancellationToken cancellationToken);
}

internal interface ICodexProcess : IAsyncDisposable
{
    TextReader StandardOutput { get; }

    TextReader StandardError { get; }

    TextWriter StandardInput { get; }

    int? ProcessId { get; }

    bool HasExited { get; }

    int ExitCode { get; }

    Task WaitForExitAsync(CancellationToken cancellationToken);

    void Kill();
}

internal sealed class ProcessCodexProcessLauncher : ICodexProcessLauncher
{
    public async Task<ICodexProcess> StartAsync(CodexProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        ProcessStartInfo processStartInfo = new()
        {
            FileName = startInfo.FileName,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (string argument in startInfo.Arguments)
        {
            processStartInfo.ArgumentList.Add(argument);
        }

        if (!string.IsNullOrWhiteSpace(startInfo.WorkingDirectory))
        {
            processStartInfo.WorkingDirectory = startInfo.WorkingDirectory;
        }

        foreach (KeyValuePair<string, string> pair in startInfo.Environment)
        {
            processStartInfo.Environment[pair.Key] = pair.Value;
        }

        Process process = new()
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true,
        };

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException($"Failed to start Codex process '{startInfo.FileName}'.");
        }

        await Task.Yield();
        return new ProcessCodexProcess(process);
    }
}

internal sealed class ProcessCodexProcess : ICodexProcess
{
    private readonly Process _process;

    public ProcessCodexProcess(Process process)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
        StandardOutput = process.StandardOutput;
        StandardError = process.StandardError;
        StandardInput = process.StandardInput;
    }

    public TextReader StandardOutput { get; }

    public TextReader StandardError { get; }

    public TextWriter StandardInput { get; }

    public int? ProcessId => _process.HasExited ? null : _process.Id;

    public bool HasExited => _process.HasExited;

    public int ExitCode => _process.ExitCode;

    public async Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        if (_process.HasExited)
        {
            return;
        }

        await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Kill()
    {
        if (_process.HasExited)
        {
            return;
        }

        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Ignore best-effort shutdown failures.
        }
    }

    public ValueTask DisposeAsync()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Ignore best-effort shutdown failures.
        }
        finally
        {
            _process.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}

internal static class CodexExecutableResolver
{
    private const string OriginatorMarker = "codex_sdk_dotnet";

    public static string Resolve(CodexClientOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CodexPathOverride))
        {
            return options.CodexPathOverride!;
        }

        string? fromPath = FindOnPath();
        if (fromPath is not null)
        {
            return fromPath;
        }

        throw new FileNotFoundException("Unable to locate the Codex executable. Set CodexClientOptions.CodexPathOverride or ensure `codex` is on PATH.");
    }

    public static IReadOnlyDictionary<string, string> BuildEnvironment(CodexClientOptions options)
    {
        Dictionary<string, string> env = options.Environment is null
            ? Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .Where(entry => entry.Key is string key && entry.Value is string)
                .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!, StringComparer.Ordinal)
            : new Dictionary<string, string>(options.Environment, StringComparer.Ordinal);

        env[EnvironmentVariables.OriginatorMarker] = OriginatorMarker;
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            env[EnvironmentVariables.ApiKey] = options.ApiKey!;
        }

        return env;
    }

    private static string? FindOnPath()
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string[] candidates = OperatingSystem.IsWindows()
            ? ["codex.exe", "codex.cmd", "codex.bat", "codex"]
            : ["codex"];

        foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (string candidate in candidates)
            {
                string fullPath = Path.Combine(directory, candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }

    private static class EnvironmentVariables
    {
        public const string ApiKey = "CODEX_API_KEY";

        public const string OriginatorMarker = "CODEX_INTERNAL_ORIGINATOR_OVERRIDE";
    }
}


