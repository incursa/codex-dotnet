using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class JsonRpcConnectionTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void Constructor_RejectsNullArguments()
    {
        ScriptedCodexProcess process = new();

        Assert.Throws<ArgumentNullException>(() => new JsonRpcConnection(null!, (_, _) => new JsonObject()));
        Assert.Throws<ArgumentNullException>(() => new JsonRpcConnection(process, null!));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0243")]
    public async Task RequestAsync_HandlesServerInitiatedApprovalRequests()
    {
        ScriptedCodexProcess process = new();
        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() == "turn/start")
            {
                string id = request["id"]!.GetValue<string>();
                process.EnqueueStdout(TestJson.Message(
                    "item/commandExecution/requestApproval",
                    new JsonObject
                    {
                        ["reason"] = "needs approval",
                    },
                    "server-request-1"));
                process.EnqueueStdout(TestJson.Response(
                    id,
                    new JsonObject
                    {
                        ["ok"] = true,
                    }));
                process.Complete();
            }
        };

        JsonRpcConnection connection = new(process, (method, request) => new JsonObject
        {
            ["decision"] = "accept",
            ["method"] = method,
            ["seen"] = request?["reason"]?.GetValue<string>(),
        });

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
        Assert.Contains(process.StdIn.Lines, line => line.Contains("\"method\":\"item/commandExecution/requestApproval\"", StringComparison.Ordinal));
        Assert.Contains(process.StdIn.Lines, line => line.Contains("\"decision\":\"accept\"", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0243")]
    public async Task RequestAsync_HandlesServerInitiatedApprovalRequestsWithoutParams()
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
            process.EnqueueStdout(TestJson.Message(
                "item/fileChange/requestApproval",
                null,
                "server-request-1"));
            process.EnqueueStdout(TestJson.Response(
                id,
                new JsonObject
                {
                    ["ok"] = true,
                }));
            process.Complete();
        };

        JsonRpcConnection connection = new(process, (method, request) => new JsonObject
        {
            ["decision"] = "accept",
            ["method"] = method,
            ["hadRequest"] = request is not null,
        });

        await connection.InitializeStderrDrainAsync(CancellationToken.None);

        JsonNode? response = await connection.RequestAsync(
            "turn/start",
            new JsonObject
            {
                ["threadId"] = "thread-1",
            },
            CancellationToken.None);

        Assert.True(response!["ok"]!.GetValue<bool>());
        Assert.Contains(process.StdIn.Lines, line => line.Contains("\"method\":\"item/fileChange/requestApproval\"", StringComparison.Ordinal));
        Assert.Contains(process.StdIn.Lines, line => line.Contains("\"hadRequest\":false", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public async Task PublicOperations_RejectCanceledTokensBeforeStarting()
    {
        ScriptedCodexProcess process = new();
        JsonRpcConnection connection = new(process, (_, _) => new JsonObject());

        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            connection.InitializeStderrDrainAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            connection.RequestAsync("turn/start", new JsonObject(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            connection.NotifyAsync("initialized", new JsonObject(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            connection.NextNotificationAsync(cancellation.Token));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0241")]
    public async Task RequestAsync_QueuesUnknownNotificationsBeforeResponse()
    {
        ScriptedCodexProcess process = new();
        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() == "turn/start")
            {
                string id = request["id"]!.GetValue<string>();
                process.EnqueueStdout(TestJson.Notification(
                    "custom/runtime-event",
                    new JsonObject
                    {
                        ["threadId"] = "thread-1",
                        ["turnId"] = "turn-1",
                        ["item"] = new JsonObject
                        {
                            ["type"] = "reasoning",
                            ["id"] = "reason-1",
                            ["content"] = new JsonArray { "one" },
                            ["summary"] = new JsonArray { "two" },
                        },
                    }));
                process.EnqueueStdout(TestJson.Response(
                    id,
                    new JsonObject
                    {
                        ["ok"] = true,
                    }));
                process.Complete();
            }
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

        JsonObject notification = await connection.NextNotificationAsync(CancellationToken.None);
        Assert.Equal("custom/runtime-event", notification["method"]!.GetValue<string>());
        Assert.Equal("thread-1", notification["params"]!["threadId"]!.GetValue<string>());
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0308")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0246")]
    public async Task RequestAsync_MapsJsonRpcErrorResponses()
    {
        ScriptedCodexProcess process = new();
        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() == "turn/start")
            {
                string id = request["id"]!.GetValue<string>();
                process.EnqueueStdout(TestJson.ErrorResponse(
                    id,
                    -32602,
                    "bad params",
                    new JsonObject
                    {
                        ["field"] = "input",
                    }));
                process.Complete();
            }
        };

        JsonRpcConnection connection = new(process, (_, _) => new JsonObject());
        await connection.InitializeStderrDrainAsync(CancellationToken.None);

        CodexInvalidParamsException exception = await Assert.ThrowsAsync<CodexInvalidParamsException>(() =>
            connection.RequestAsync(
                "turn/start",
                new JsonObject
                {
                    ["threadId"] = "thread-1",
                },
                CancellationToken.None));

        Assert.Equal(-32602, exception.Code);
        Assert.Equal("bad params", exception.Message);
        Assert.Equal("input", exception.ErrorData!["field"]!.GetValue<string>());
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public async Task RequestAndNotifyAsync_UseEmptyParamsWhenParametersAreNull()
    {
        ScriptedCodexProcess process = new();
        string? notifyFrame = null;

        process.StdIn.LineWritten = line =>
        {
            JsonObject message = JsonNode.Parse(line)!.AsObject();
            string? method = message["method"]?.GetValue<string>();
            if (method == "initialized")
            {
                notifyFrame = line;
                return;
            }

            if (method != "turn/start")
            {
                return;
            }

            Assert.True(message["params"] is JsonObject paramsObject && paramsObject.Count == 0);
            string id = message["id"]!.GetValue<string>();
            process.EnqueueStdout(TestJson.Response(
                id,
                new JsonObject
                {
                    ["ok"] = true,
                }));
            process.Complete();
        };

        JsonRpcConnection connection = new(process, (_, _) => new JsonObject());
        await connection.NotifyAsync("initialized", null, CancellationToken.None);

        JsonNode? response = await connection.RequestAsync("turn/start", null, CancellationToken.None);

        Assert.True(response!["ok"]!.GetValue<bool>());
        Assert.NotNull(notifyFrame);
        Assert.Contains("\"method\":\"initialized\"", notifyFrame, StringComparison.Ordinal);
        Assert.Contains("\"params\":{}", notifyFrame, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0248")]
    public async Task RequestAsync_MapsStructuredRetryLimitErrors()
    {
        ScriptedCodexProcess process = new();
        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() == "turn/start")
            {
                string id = request["id"]!.GetValue<string>();
                process.EnqueueStdout(TestJson.ErrorResponse(
                    id,
                    -32001,
                    "too many failed attempts",
                    new JsonObject
                    {
                        ["codexErrorInfo"] = "server_overloaded",
                    }));
                process.Complete();
            }
        };

        JsonRpcConnection connection = new(process, (_, _) => new JsonObject());
        await connection.InitializeStderrDrainAsync(CancellationToken.None);

        CodexRetryLimitExceededException exception = await Assert.ThrowsAsync<CodexRetryLimitExceededException>(() =>
            connection.RequestAsync(
                "turn/start",
                new JsonObject
                {
                    ["threadId"] = "thread-1",
                },
                CancellationToken.None));

        Assert.Equal(-32001, exception.Code);
        Assert.Contains("failed attempts", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0241")]
    public async Task NotifyAsync_WritesNotificationFrame()
    {
        ScriptedCodexProcess process = new();
        JsonRpcConnection connection = new(process, (_, _) => new JsonObject());

        await connection.NotifyAsync(
            "initialized",
            new JsonObject
            {
                ["client"] = "trace",
            },
            CancellationToken.None);

        Assert.Single(process.StdIn.Lines);
        Assert.Contains("\"method\":\"initialized\"", process.StdIn.Lines[0], StringComparison.Ordinal);
        Assert.Contains("\"client\":\"trace\"", process.StdIn.Lines[0], StringComparison.Ordinal);
        Assert.DoesNotContain("\"id\":", process.StdIn.Lines[0], StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public async Task RequestAsync_RejectsMalformedResponses()
    {
        ScriptedCodexProcess process = new();
        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() == "turn/start")
            {
                process.EnqueueStdout("[]");
                process.Complete();
            }
        };

        JsonRpcConnection connection = new(process, (_, _) => new JsonObject());
        await connection.InitializeStderrDrainAsync(CancellationToken.None);

        CodexInvalidRequestException exception = await Assert.ThrowsAsync<CodexInvalidRequestException>(() =>
            connection.RequestAsync("turn/start", new JsonObject(), CancellationToken.None));

        Assert.Contains("Malformed JSON-RPC message", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public async Task DisposeAsync_CancelsPendingRequestsAndRejectsFurtherAccess()
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

        Task<JsonNode?> pendingRequest = connection.RequestAsync(
            "turn/start",
            new JsonObject(),
            CancellationToken.None);

        await requestWritten.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await connection.DisposeAsync();

        await Assert.ThrowsAsync<CodexTransportClosedException>(async () => await pendingRequest);
        await Assert.ThrowsAsync<CodexTransportClosedException>(async () =>
            await connection.RequestAsync("turn/start", new JsonObject(), CancellationToken.None));
        await Assert.ThrowsAsync<CodexTransportClosedException>(async () =>
            await connection.NotifyAsync("initialized", new JsonObject(), CancellationToken.None));
        await Assert.ThrowsAsync<CodexTransportClosedException>(async () =>
            await connection.NextNotificationAsync(CancellationToken.None));
    }
}
