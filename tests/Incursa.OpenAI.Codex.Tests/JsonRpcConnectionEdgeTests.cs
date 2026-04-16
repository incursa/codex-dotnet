using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class JsonRpcConnectionEdgeTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    [CoverageType(RequirementCoverageType.Edge)]
    public async Task RequestAsync_IgnoresUnmatchedResponsesUntilTheCorrectIdArrives()
    {
        ScriptedCodexProcess process = new();
        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() != "turn/start")
            {
                return;
            }

            string id = request["id"]!.GetValue<string>();
            process.EnqueueStdout(TestJson.Response(
                "unknown-request",
                new JsonObject
                {
                    ["ignored"] = true,
                }));
            process.EnqueueStdout(TestJson.Response(
                id,
                new JsonObject
                {
                    ["ok"] = true,
                }));
            process.Complete();
        };

        JsonRpcConnection connection = new(process, (_, _) => new JsonObject());
        await connection.InitializeStderrDrainAsync(CancellationToken.None);

        JsonNode? response = await connection.RequestAsync(
            "turn/start",
            new JsonObject
            {
                ["threadId"] = "thread-1",
            },
            CancellationToken.None);

        Assert.True(response!["ok"]!.GetValue<bool>());
        Assert.Contains(process.StdIn.Lines, line => line.Contains("\"method\":\"turn/start\"", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    [CoverageType(RequirementCoverageType.Negative)]
    public async Task RequestAsync_FailsWhenStdoutClosesBeforeResponseAndIncludesStderrTail()
    {
        ScriptedCodexProcess process = new();
        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() != "turn/start")
            {
                return;
            }

            process.EnqueueStderr("stderr-one");
            process.EnqueueStderr("stderr-two");
            process.Complete();
        };

        JsonRpcConnection connection = new(process, (_, _) => new JsonObject());
        await connection.InitializeStderrDrainAsync(CancellationToken.None);

        CodexTransportClosedException exception = await Assert.ThrowsAsync<CodexTransportClosedException>(() =>
            connection.RequestAsync(
                "turn/start",
                new JsonObject
                {
                    ["threadId"] = "thread-1",
                },
                CancellationToken.None));

        Assert.Contains("stderr-one", exception.Message, StringComparison.Ordinal);
        Assert.Contains("stderr-two", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    [CoverageType(RequirementCoverageType.Edge)]
    public async Task RequestAsync_ReturnsNullWhenTheResponseHasNoResultPayload()
    {
        ScriptedCodexProcess process = new();
        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() != "turn/start")
            {
                return;
            }

            string id = request["id"]!.GetValue<string>();
            process.EnqueueStdout(new JsonObject
            {
                ["id"] = id,
            });
            process.Complete();
        };

        JsonRpcConnection connection = new(process, (_, _) => new JsonObject());
        await connection.InitializeStderrDrainAsync(CancellationToken.None);

        JsonNode? response = await connection.RequestAsync(
            "turn/start",
            new JsonObject
            {
                ["threadId"] = "thread-1",
            },
            CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    [CoverageType(RequirementCoverageType.Negative)]
    public async Task RequestAsync_CanBeCanceledBeforeAResponseArrives()
    {
        ScriptedCodexProcess process = new();
        TaskCompletionSource requestWritten = new(TaskCreationOptions.RunContinuationsAsynchronously);

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() == "turn/start")
            {
                requestWritten.TrySetResult();
            }
        };

        JsonRpcConnection connection = new(process, (_, _) => new JsonObject());
        await connection.InitializeStderrDrainAsync(CancellationToken.None);

        using CancellationTokenSource cancellation = new();
        Task<JsonNode?> pending = connection.RequestAsync(
            "turn/start",
            new JsonObject
            {
                ["threadId"] = "thread-1",
            },
            cancellation.Token);

        await requestWritten.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);

        process.Complete();
        await connection.DisposeAsync();
    }
}
