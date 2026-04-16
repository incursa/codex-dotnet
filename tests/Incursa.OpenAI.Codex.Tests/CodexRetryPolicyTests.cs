using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexRetryPolicyTests
{
    [Theory]
    [InlineData(-32700, typeof(CodexParseException))]
    [InlineData(-32600, typeof(CodexInvalidRequestException))]
    [InlineData(-32601, typeof(CodexMethodNotFoundException))]
    [InlineData(-32602, typeof(CodexInvalidParamsException))]
    [InlineData(-32603, typeof(CodexInternalRpcException))]
    [InlineData(123, typeof(CodexInvalidRequestException))]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void MapJsonRpcError_MapsKnownAndUnexpectedProtocolCodes(int code, Type expectedType)
    {
        CodexJsonRpcException exception = CodexRetryPolicy.MapJsonRpcError(new JsonObject
        {
            ["code"] = code,
        });

        Assert.IsType(expectedType, exception);
    }

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
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void MapJsonRpcError_UsesDefaultsWhenThePayloadIsSparse()
    {
        CodexJsonRpcException exception = CodexRetryPolicy.MapJsonRpcError(new JsonObject());

        Assert.IsType<CodexServerBusyException>(exception);
        Assert.Equal(-32000, exception.Code);
        Assert.Equal("Unknown JSON-RPC error", exception.Message);
        Assert.Null(exception.ErrorData);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void MapJsonRpcError_DoesNotTreatOutOfRangeCodesAsRetryableEvenWithMarkers()
    {
        CodexJsonRpcException exception = CodexRetryPolicy.MapJsonRpcError(new JsonObject
        {
            ["code"] = -32100,
            ["message"] = "retry limit reached",
            ["data"] = new JsonObject
            {
                ["errorInfo"] = "server_overloaded",
            },
        });

        Assert.IsType<CodexInvalidRequestException>(exception);
        Assert.False(CodexRetryPolicy.IsRetryable(exception));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void MapJsonRpcError_RecognizesRetryLimitPhraseInMessage()
    {
        CodexJsonRpcException exception = CodexRetryPolicy.MapJsonRpcError(new JsonObject
        {
            ["code"] = -32010,
            ["message"] = "retry limit reached",
        });

        Assert.IsType<CodexRetryLimitExceededException>(exception);
        Assert.True(CodexRetryPolicy.IsRetryable(exception));
    }

    [Theory]
    [InlineData(-32099)]
    [InlineData(-32000)]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void MapJsonRpcError_TreatsBoundaryRetryCodesAsServerBusy(int code)
    {
        CodexJsonRpcException exception = CodexRetryPolicy.MapJsonRpcError(new JsonObject
        {
            ["code"] = code,
            ["message"] = "server busy",
        });

        Assert.IsType<CodexServerBusyException>(exception);
        Assert.True(CodexRetryPolicy.IsRetryable(exception));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void MapJsonRpcError_RecognizesSnakeCaseRetryMarkers()
    {
        CodexJsonRpcException exception = CodexRetryPolicy.MapJsonRpcError(new JsonObject
        {
            ["code"] = -32042,
            ["message"] = "server busy",
            ["data"] = new JsonObject
            {
                ["codex_error_info"] = new JsonObject
                {
                    ["status"] = "server_overloaded",
                },
            },
        });

        Assert.IsType<CodexServerBusyException>(exception);
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
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0248")]
    public async Task RetryOnOverloadAsync_AppliesDeterministicJitter()
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
                return 17;
            },
            maxAttempts: 3,
            initialDelay: TimeSpan.FromMilliseconds(100),
            maxDelay: TimeSpan.FromMilliseconds(150),
            jitterRatio: 0.5,
            delayAsync: (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            },
            random: new FixedRandom(0.75),
            cancellationToken: CancellationToken.None);

        Assert.Equal(17, result);
        Assert.Equal(3, attempts);
        Assert.Equal([TimeSpan.FromMilliseconds(125), TimeSpan.FromMilliseconds(187.5)], delays);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0248")]
    public async Task RetryOnOverloadAsync_CapsDelayAndStopsAfterTheConfiguredAttempts()
    {
        List<TimeSpan> delays = [];
        int attempts = 0;

        CodexServerBusyException exception = await Assert.ThrowsAsync<CodexServerBusyException>(async () =>
            await CodexRetryPolicy.RetryOnOverloadAsync(
                _ =>
                {
                    attempts++;
                    throw new CodexServerBusyException("still busy");
                },
                maxAttempts: 3,
                initialDelay: TimeSpan.FromMilliseconds(300),
                maxDelay: TimeSpan.FromMilliseconds(200),
                jitterRatio: 0,
                delayAsync: (delay, _) =>
                {
                    delays.Add(delay);
                    return Task.CompletedTask;
                },
                random: new Random(7),
                cancellationToken: CancellationToken.None));

        Assert.Equal("still busy", exception.Message);
        Assert.Equal(3, attempts);
        Assert.Equal([TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200)], delays);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0248")]
    public async Task RetryOnOverloadAsync_DoesNotRetryNonBusyFailures()
    {
        int attempts = 0;

        await Assert.ThrowsAsync<CodexInvalidRequestException>(async () =>
            await CodexRetryPolicy.RetryOnOverloadAsync(
                _ =>
                {
                    attempts++;
                    throw new CodexInvalidRequestException("no retry");
                },
                maxAttempts: 3,
                initialDelay: TimeSpan.FromMilliseconds(50),
                maxDelay: TimeSpan.FromMilliseconds(100),
                jitterRatio: 0,
                random: new Random(7),
                cancellationToken: CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0248")]
    public async Task RetryOnOverloadAsync_WithSingleAttemptDoesNotRetryBusyFailures()
    {
        int attempts = 0;

        await Assert.ThrowsAsync<CodexServerBusyException>(async () =>
            await CodexRetryPolicy.RetryOnOverloadAsync(
                _ =>
                {
                    attempts++;
                    throw new CodexServerBusyException("busy");
                },
                maxAttempts: 1,
                initialDelay: TimeSpan.FromMilliseconds(50),
                maxDelay: TimeSpan.FromMilliseconds(100),
                jitterRatio: 0,
                random: new Random(7),
                cancellationToken: CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0248")]
    public async Task RetryOnOverloadAsync_RejectsInvalidArguments()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CodexRetryPolicy.RetryOnOverloadAsync(
                _ => Task.FromResult(1),
                maxAttempts: 0));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CodexRetryPolicy.RetryOnOverloadAsync(
                _ => Task.FromResult(1),
                jitterRatio: -0.25));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void MapJsonRpcError_FollowsNestedRetryableMarkers()
    {
        CodexJsonRpcException busy = CodexRetryPolicy.MapJsonRpcError(new JsonObject
        {
            ["code"] = -32042,
            ["message"] = "server busy",
            ["data"] = new JsonObject
            {
                ["payload"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["errorInfo"] = new JsonObject
                        {
                            ["status"] = "server_overloaded",
                        },
                    },
                },
            },
        });

        Assert.IsType<CodexServerBusyException>(busy);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void MapJsonRpcError_IgnoresNullSpecialMarkersAndScansNestedPayload()
    {
        CodexJsonRpcException busy = CodexRetryPolicy.MapJsonRpcError(new JsonObject
        {
            ["code"] = -32042,
            ["message"] = "server busy",
            ["data"] = new JsonObject
            {
                ["errorInfo"] = null,
                ["payload"] = new JsonObject
                {
                    ["status"] = "server_overloaded",
                },
            },
        });

        Assert.IsType<CodexServerBusyException>(busy);
        Assert.True(CodexRetryPolicy.IsRetryable(busy));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0247")]
    public void RetryHelper_IsRetryableRecognizesBusyAndRetryLimitExceptions()
    {
        Assert.True(CodexRetryPolicy.IsRetryable(new CodexServerBusyException()));
        Assert.True(CodexRetryPolicy.IsRetryable(new CodexRetryLimitExceededException()));
        Assert.False(CodexRetryPolicy.IsRetryable(new CodexInvalidRequestException()));
    }

    private sealed class FixedRandom(double nextDouble) : Random
    {
        public override double NextDouble()
            => nextDouble;
    }
}
