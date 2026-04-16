namespace Incursa.OpenAI.Codex.Tests;

internal static class LiveCodexIntegration
{
    private const string EnableVariable = "CODEX_LIVE_TESTS";

    public static string? GetSkipReason()
    {
        if (IsCiEnvironment())
        {
            return "Live Codex integration tests are skipped in CI/GitHub Actions.";
        }

        if (!IsEnabled())
        {
            return $"Set {EnableVariable}=1 to run live Codex integration tests locally.";
        }

        try
        {
            _ = CodexExecutableResolver.Resolve(new CodexClientOptions());
        }
        catch (FileNotFoundException exception)
        {
            return exception.Message;
        }

        return null;
    }

    public static CodexClientOptions CreateClientOptions()
    {
        return new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = CodexExecutableResolver.Resolve(new CodexClientOptions()),
            ProcessLauncher = new BypassSandboxCodexProcessLauncher(),
        };
    }

    public static CodexClientOptions CreateAppServerClientOptions()
    {
        return new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.AppServer,
            CodexPathOverride = CodexExecutableResolver.Resolve(new CodexClientOptions()),
            ProcessLauncher = new BypassSandboxCodexProcessLauncher(),
        };
    }

    public static CodexThreadOptions CreateThreadOptions(string workingDirectory)
    {
        return new CodexThreadOptions
        {
            WorkingDirectory = workingDirectory,
            SkipGitRepoCheck = true,
        };
    }

    private static bool IsEnabled()
    {
        string? value = Environment.GetEnvironmentVariable(EnableVariable);
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCiEnvironment()
    {
        string? ci = Environment.GetEnvironmentVariable("CI");
        string? githubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");

        return string.Equals(ci, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(githubActions, "true", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class LiveCodexFactAttribute : FactAttribute
{
    public LiveCodexFactAttribute()
    {
        Skip = LiveCodexIntegration.GetSkipReason();
    }
}

internal sealed class BypassSandboxCodexProcessLauncher : ICodexProcessLauncher
{
    private readonly ProcessCodexProcessLauncher _inner = new();

    public Task<ICodexProcess> StartAsync(CodexProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        List<string> arguments = ["--dangerously-bypass-approvals-and-sandbox"];
        arguments.AddRange(startInfo.Arguments);

        return _inner.StartAsync(
            startInfo with
            {
                Arguments = arguments,
            },
            cancellationToken);
    }
}
