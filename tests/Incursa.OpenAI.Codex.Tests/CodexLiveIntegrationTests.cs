using System.Text;

namespace Incursa.OpenAI.Codex.Tests;

// Traceability: REQ-CODEX-SDK-TRANSPORT-0233, REQ-CODEX-SDK-LIFECYCLE-0294, REQ-CODEX-SDK-LIFECYCLE-0295.

[Collection("Live Codex")]
public sealed class CodexLiveIntegrationTests
{
    [LiveCodexFact]
    [Trait("Category", "Integration")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0295")]
    public async Task CodexClient_RunAsync_ReadsTheTempFile()
    {
        string workDir = await CreateWorkspaceAsync();
        try
        {
            await using CodexClient client = new(LiveCodexIntegration.CreateClientOptions());
            CodexThread thread = await client.StartThreadAsync(LiveCodexIntegration.CreateThreadOptions(workDir));

            CodexRunResult result = await thread.RunAsync(
                "Read sample.txt in the current directory and reply with exactly VALUE=<token> for the token on line 1.");

            Assert.Equal("VALUE=alpha", result.FinalResponse);
        }
        finally
        {
            DeleteDirectory(workDir);
        }
    }

    [LiveCodexFact]
    [Trait("Category", "Integration")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0294")]
    public async Task CodexThread_RunStreamedAsync_ReadsTheTempFile()
    {
        string workDir = await CreateWorkspaceAsync();
        try
        {
            await using CodexClient client = new(LiveCodexIntegration.CreateClientOptions());
            CodexThread thread = await client.StartThreadAsync(LiveCodexIntegration.CreateThreadOptions(workDir));

            List<CodexThreadEvent> events = new();
            await foreach (CodexThreadEvent evt in thread.RunStreamedAsync(
                "Read sample.txt in the current directory and reply with exactly LINES=<count> for the total number of lines."))
            {
                events.Add(evt);
            }

            Assert.Contains(events, evt => evt is CodexTurnCompletedEvent);
            Assert.Contains(
                events.OfType<CodexItemCompletedEvent>().Select(evt => evt.Item).OfType<CodexAgentMessageItem>(),
                item => item.Text.Contains("LINES=2", StringComparison.Ordinal));
        }
        finally
        {
            DeleteDirectory(workDir);
        }
    }

    [LiveCodexFact]
    [Trait("Category", "Integration")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0295")]
    public async Task CodexTurn_RunAsync_ReadsTheTempFile()
    {
        string workDir = await CreateWorkspaceAsync();
        try
        {
            await using CodexClient client = new(LiveCodexIntegration.CreateClientOptions());
            CodexThread thread = await client.StartThreadAsync(LiveCodexIntegration.CreateThreadOptions(workDir));
            CodexTurn turn = await thread.StartTurnAsync(
                "Read sample.txt in the current directory and reply with exactly VALUE=<token> for the token on line 2.");

            CodexRunResult result = await turn.RunAsync();

            Assert.Equal("VALUE=beta", result.FinalResponse);
        }
        finally
        {
            DeleteDirectory(workDir);
        }
    }

    [LiveCodexFact]
    [Trait("Category", "Integration")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0302")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0303")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0295")]
    public async Task CodexAppServerClient_RunAsync_ReadsTheTempFileAndSupportsReadResume()
    {
        string workDir = await CreateWorkspaceAsync();
        try
        {
            await using CodexClient client = new(LiveCodexIntegration.CreateAppServerClientOptions());
            CodexRuntimeMetadata metadata = await client.InitializeAsync();

            Assert.Equal(CodexBackendSelection.AppServer, client.Capabilities!.BackendSelection);
            Assert.True(client.Capabilities.SupportsReadThread);
            Assert.True(client.Capabilities.SupportsResumeThread);
            Assert.True(client.Capabilities.SupportsTurnSteering);
            Assert.NotNull(metadata.ServerInfo);

            CodexThread thread = await client.StartThreadAsync(LiveCodexIntegration.CreateThreadOptions(workDir));
            CodexRunResult result = await thread.RunAsync(
                "Read sample.txt in the current directory and reply with exactly VALUE=<token> for the token on line 1.");

            Assert.Equal("VALUE=alpha", result.FinalResponse);

            CodexThreadSnapshot snapshot = await thread.ReadAsync(includeTurns: true);
            Assert.Equal(thread.Id, snapshot.Id);
            Assert.NotEmpty(snapshot.Turns);

            CodexThread resumedThread = await client.ResumeThreadAsync(thread.Id!);
            CodexThreadSnapshot resumedSnapshot = await resumedThread.ReadAsync(includeTurns: true);
            Assert.Equal(thread.Id, resumedSnapshot.Id);
        }
        finally
        {
            DeleteDirectory(workDir);
        }
    }

    private static async Task<string> CreateWorkspaceAsync()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(), "codex-live-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        string filePath = Path.Combine(directoryPath, "sample.txt");
        string content = string.Join(
            Environment.NewLine,
            "token: alpha",
            "token: beta");

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        return directoryPath;
    }

    private static void DeleteDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
