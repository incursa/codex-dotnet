namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexClientLifecycleTests
{
    public static IEnumerable<object[]> DisposedClientOperations()
    {
        yield return [nameof(CodexClient.InitializeAsync), new Func<CodexClient, Task>(async client => await client.InitializeAsync())];
        yield return [nameof(CodexClient.IsCodexAvailableAsync), new Func<CodexClient, Task>(async client => _ = await client.IsCodexAvailableAsync())];
        yield return [nameof(CodexClient.StartThreadAsync), new Func<CodexClient, Task>(async client => await client.StartThreadAsync())];
        yield return [nameof(CodexClient.ResumeThreadAsync), new Func<CodexClient, Task>(async client => await client.ResumeThreadAsync("thread-1"))];
        yield return [nameof(CodexClient.ForkThreadAsync), new Func<CodexClient, Task>(async client => await client.ForkThreadAsync("thread-1"))];
        yield return [nameof(CodexClient.ListThreadsAsync), new Func<CodexClient, Task>(async client => await client.ListThreadsAsync())];
        yield return [nameof(CodexClient.ReadThreadAsync), new Func<CodexClient, Task>(async client => await client.ReadThreadAsync("thread-1"))];
        yield return [nameof(CodexClient.ArchiveThreadAsync), new Func<CodexClient, Task>(async client => await client.ArchiveThreadAsync("thread-1"))];
        yield return [nameof(CodexClient.UnarchiveThreadAsync), new Func<CodexClient, Task>(async client => await client.UnarchiveThreadAsync("thread-1"))];
        yield return [nameof(CodexClient.ListModelsAsync), new Func<CodexClient, Task>(async client => await client.ListModelsAsync())];
    }

    [Theory]
    [MemberData(nameof(DisposedClientOperations))]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0239")]
    [CoverageType(RequirementCoverageType.Negative)]
    public async Task DisposedClient_RejectsPublicOperations(
        string expectedOperation,
        Func<CodexClient, Task> invoke)
    {
        await using CodexClient client = new(new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = "codex",
        });

        await client.DisposeAsync();

        CodexTransportClosedException exception = await Assert.ThrowsAsync<CodexTransportClosedException>(() => invoke(client));

        Assert.NotNull(exception);
        Assert.Contains("closed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(expectedOperation);
    }
}
