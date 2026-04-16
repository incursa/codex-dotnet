namespace Incursa.OpenAI.Codex.Tests;

// Traceability: REQ-CODEX-SDK-0103, REQ-CODEX-SDK-0106.

public sealed class SampleProgramTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-0103")]
    [Trait("Requirement", "REQ-CODEX-SDK-0106")]
    public void SampleOptions_Parse_UsesTheDocumentedDefaults()
    {
        global::SampleOptions options = global::SampleOptions.Parse([]);

        Assert.Equal(global::SampleMode.Quickstart, options.Mode);
        Assert.Equal(CodexBackendSelection.AppServer, options.Backend);
        Assert.Equal("Summarize the current repository state in three bullet points.", options.Prompt);
        Assert.Equal(Environment.CurrentDirectory, options.WorkingDirectory);
        Assert.Null(options.LocalImagePath);
        Assert.Null(options.RemoteImageUrl);
        Assert.Null(options.CodexPathOverride);
        Assert.Null(options.ApiKey);
        Assert.False(options.UseDependencyInjection);
        Assert.False(options.InterruptTurn);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-0103")]
    [Trait("Requirement", "REQ-CODEX-SDK-0106")]
    public void SampleOptions_Parse_SupportsAllDocumentedFlags()
    {
        global::SampleOptions options = global::SampleOptions.Parse(
            [
                "--mode",
                "turn-controls",
                "--backend",
                "exec",
                "--prompt",
                "Draft a release note.",
                "--cwd",
                @"C:\work",
                "--image",
                @"C:\images\trace.png",
                "--image-url",
                "https://example.com/image.png",
                "--codex-path",
                @"C:\codex.exe",
                "--api-key",
                "secret",
                "--use-di",
                "--interrupt",
            ]);

        Assert.Equal(global::SampleMode.TurnControls, options.Mode);
        Assert.Equal(CodexBackendSelection.Exec, options.Backend);
        Assert.Equal("Draft a release note.", options.Prompt);
        Assert.Equal(@"C:\work", options.WorkingDirectory);
        Assert.Equal(@"C:\images\trace.png", options.LocalImagePath);
        Assert.Equal("https://example.com/image.png", options.RemoteImageUrl);
        Assert.Equal(@"C:\codex.exe", options.CodexPathOverride);
        Assert.Equal("secret", options.ApiKey);
        Assert.True(options.UseDependencyInjection);
        Assert.True(options.InterruptTurn);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-0103")]
    public async Task RunAsync_HelpReturnsZeroAndPrintsUsage()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = await global::ProgramEntry.RunAsync(["--help"], output, error);

        Assert.Equal(0, exitCode);
        Assert.Contains("--mode <quickstart|streaming|structured-output|image-input|error-handling|turn-controls>", output.ToString());
        Assert.Contains("--use-di", output.ToString());
        Assert.Empty(error.ToString());
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-0103")]
    public async Task RunAsync_InvalidArgumentsReturnTwoAndPrintUsage()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = await global::ProgramEntry.RunAsync(["--unknown"], output, error);

        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown argument: --unknown", error.ToString());
        Assert.Contains("--prompt <text>", output.ToString());
    }
}
