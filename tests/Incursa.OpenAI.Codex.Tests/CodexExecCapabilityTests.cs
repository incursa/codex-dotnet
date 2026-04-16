using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexExecCapabilityTests
{
    public static IEnumerable<object[]> UnsupportedClientOperations()
    {
        yield return [
            nameof(CodexClient.ArchiveThreadAsync),
            new Func<CodexClient, Task>(async client => await client.ArchiveThreadAsync("thread-1"))];
        yield return [
            nameof(CodexClient.ForkThreadAsync),
            new Func<CodexClient, Task>(async client => await client.ForkThreadAsync("thread-1"))];
        yield return [
            nameof(CodexClient.ListModelsAsync),
            new Func<CodexClient, Task>(async client => await client.ListModelsAsync())];
        yield return [
            nameof(CodexClient.ListThreadsAsync),
            new Func<CodexClient, Task>(async client => await client.ListThreadsAsync())];
        yield return [
            nameof(CodexClient.ReadThreadAsync),
            new Func<CodexClient, Task>(async client => await client.ReadThreadAsync("thread-1"))];
        yield return [
            nameof(CodexClient.UnarchiveThreadAsync),
            new Func<CodexClient, Task>(async client => await client.UnarchiveThreadAsync("thread-1"))];
    }

    public static IEnumerable<object[]> UnsupportedThreadOperations()
    {
        yield return [
            nameof(CodexThread.CompactAsync),
            new Func<CodexThread, Task>(async thread => await thread.CompactAsync())];
        yield return [
            nameof(CodexClient.ReadThreadAsync),
            new Func<CodexThread, Task>(async thread => await thread.ReadAsync())];
        yield return [
            nameof(CodexThread.SetNameAsync),
            new Func<CodexThread, Task>(async thread => await thread.SetNameAsync("renamed"))];
    }

    [Theory]
    [MemberData(nameof(UnsupportedClientOperations))]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0314")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0319")]
    [CoverageType(RequirementCoverageType.Negative)]
    public async Task ExecBackend_DisallowsUnsupportedClientOperations(
        string expectedOperation,
        Func<CodexClient, Task> invoke)
    {
        await using CodexClient client = CreateExecClient();

        CodexCapabilityNotSupportedException exception = await Assert.ThrowsAsync<CodexCapabilityNotSupportedException>(() => invoke(client));

        Assert.Equal(expectedOperation, exception.Operation);
        Assert.Equal(CodexBackendSelection.Exec, exception.BackendSelection);
    }

    [Theory]
    [MemberData(nameof(UnsupportedThreadOperations))]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0314")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0319")]
    [CoverageType(RequirementCoverageType.Negative)]
    public async Task ExecThread_DisallowsUnsupportedOperations(
        string expectedOperation,
        Func<CodexThread, Task> invoke)
    {
        await using CodexClient client = CreateExecClient();
        CodexThread thread = new(client, defaults: null, id: "thread-1", started: true);

        CodexCapabilityNotSupportedException exception = await Assert.ThrowsAsync<CodexCapabilityNotSupportedException>(() => invoke(thread));

        Assert.Equal(expectedOperation, exception.Operation);
        Assert.Equal(CodexBackendSelection.Exec, exception.BackendSelection);
    }

    private static CodexClient CreateExecClient()
        => new(new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = "codex",
        });
}
