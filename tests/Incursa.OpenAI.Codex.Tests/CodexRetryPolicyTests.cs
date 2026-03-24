using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexRetryPolicyTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void MapJsonRpcError_RecognizesStructuredRetryLimitConditions()
    {
        CodexJsonRpcException retryLimit = CodexRetryPolicy.MapJsonRpcError(new JsonObject
        {
            ["code"] = -32001,
            ["message"] = "too many failed attempts",
            ["data"] = new JsonObject
            {
                ["codexErrorInfo"] = "server_overloaded",
            },
        });

        Assert.IsType<CodexRetryLimitExceededException>(retryLimit);
        Assert.True(CodexRetryPolicy.IsRetryable(retryLimit));

        CodexJsonRpcException busy = CodexRetryPolicy.MapJsonRpcError(new JsonObject
        {
            ["code"] = -32002,
            ["message"] = "server busy",
            ["data"] = new JsonObject
            {
                ["errorInfo"] = "server_overloaded",
            },
        });

        Assert.IsType<CodexServerBusyException>(busy);
        Assert.True(CodexRetryPolicy.IsRetryable(busy));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0248")]
    public async Task RetryOnOverloadAsync_RetriesBusyErrorsWithExponentialBackoff()
    {
        List<TimeSpan> delays = [];
        int attempts = 0;

        int result = await CodexRetryPolicy.RetryOnOverloadAsync(
            async _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new CodexServerBusyException("busy");
                }

                await Task.Yield();
                return 42;
            },
            maxAttempts: 3,
            initialDelay: TimeSpan.FromMilliseconds(100),
            maxDelay: TimeSpan.FromMilliseconds(250),
            jitterRatio: 0,
            delayAsync: (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            },
            random: new Random(1),
            cancellationToken: CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(3, attempts);
        Assert.Equal([TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)], delays);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void RetryHelper_IsRetryableRecognizesBusyAndRetryLimitExceptions()
    {
        Assert.True(CodexRetryPolicy.IsRetryable(new CodexServerBusyException()));
        Assert.True(CodexRetryPolicy.IsRetryable(new CodexRetryLimitExceededException()));
        Assert.False(CodexRetryPolicy.IsRetryable(new CodexInvalidRequestException()));
    }
}


