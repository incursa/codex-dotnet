using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-TRANSPORT-0247, REQ-CODEX-SDK-TRANSPORT-0248, REQ-CODEX-SDK-CATALOG-0308.

internal static class CodexRetryPolicy
{
    private const string RetryLimitMessageToken = "retry limit";
    private const string TooManyAttemptsMessageToken = "too many failed attempts";
    private const string ServerOverloadedToken = "server_overloaded";

    public static CodexJsonRpcException MapJsonRpcError(JsonObject error)
    {
        int code = error.TryGetPropertyValue("code", out JsonNode? codeNode) && codeNode is JsonValue codeValue && codeValue.TryGetValue<int>(out int parsed)
            ? parsed
            : -32000;

        string message = error.TryGetPropertyValue("message", out JsonNode? messageNode)
            ? messageNode?.GetValue<string>() ?? "Unknown JSON-RPC error"
            : "Unknown JSON-RPC error";

        JsonNode? data = error.TryGetPropertyValue("data", out JsonNode? dataNode) ? dataNode : null;

        return code switch
        {
            -32700 => new CodexParseException(message, data),
            -32600 => new CodexInvalidRequestException(message, data),
            -32601 => new CodexMethodNotFoundException(message, data),
            -32602 => new CodexInvalidParamsException(message, data),
            -32603 => new CodexInternalRpcException(message, data),
            _ when code is >= -32099 and <= -32000 && IsRetryablePayload(message, data) => MapServerBusy(message, data),
            _ when code is >= -32099 and <= -32000 => new CodexServerBusyException(message, data),
            _ => new CodexInvalidRequestException(message, data),
        };
    }

    public static bool IsRetryable(Exception exception)
        => exception is CodexServerBusyException;

    public static async Task<TResult> RetryOnOverloadAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        int maxAttempts = 3,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        double jitterRatio = 0.2,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        Random? random = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "maxAttempts must be greater than or equal to 1.");
        }

        if (jitterRatio < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(jitterRatio), "jitterRatio must be greater than or equal to 0.");
        }

        TimeSpan currentDelay = initialDelay ?? TimeSpan.FromMilliseconds(250);
        TimeSpan ceilingDelay = maxDelay ?? TimeSpan.FromSeconds(2);
        Func<TimeSpan, CancellationToken, Task> sleepAsync = delayAsync ?? ((delay, token) => Task.Delay(delay, token));
        Random rng = random ?? Random.Shared;

        for (int attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (attempt < maxAttempts && IsRetryable(exception))
            {
                TimeSpan cappedDelay = currentDelay <= ceilingDelay ? currentDelay : ceilingDelay;
                double jitterWindow = cappedDelay.TotalMilliseconds * jitterRatio;
                double jitter = jitterWindow <= 0
                    ? 0
                    : (rng.NextDouble() * 2.0 - 1.0) * jitterWindow;
                double totalMilliseconds = Math.Max(0, cappedDelay.TotalMilliseconds + jitter);
                await sleepAsync(TimeSpan.FromMilliseconds(totalMilliseconds), cancellationToken).ConfigureAwait(false);
                currentDelay = currentDelay < ceilingDelay
                    ? TimeSpan.FromMilliseconds(Math.Min(ceilingDelay.TotalMilliseconds, currentDelay.TotalMilliseconds * 2))
                    : ceilingDelay;
            }
        }
    }

    public static Task RetryOnOverloadAsync(
        Func<CancellationToken, Task> operation,
        int maxAttempts = 3,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        double jitterRatio = 0.2,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        Random? random = null,
        CancellationToken cancellationToken = default)
        => RetryOnOverloadAsync(
            async token =>
            {
                await operation(token).ConfigureAwait(false);
                return true;
            },
            maxAttempts,
            initialDelay,
            maxDelay,
            jitterRatio,
            delayAsync,
            random,
            cancellationToken);

    private static CodexJsonRpcException MapServerBusy(string message, JsonNode? data)
        => ContainsRetryLimitText(message)
            ? new CodexRetryLimitExceededException(message, data)
            : new CodexServerBusyException(message, data);

    private static bool IsRetryablePayload(string message, JsonNode? data)
        => IsServerOverloaded(data) || ContainsRetryLimitText(message);

    private static bool ContainsRetryLimitText(string message)
        => message.Contains(RetryLimitMessageToken, StringComparison.OrdinalIgnoreCase)
           || message.Contains(TooManyAttemptsMessageToken, StringComparison.OrdinalIgnoreCase);

    private static bool IsServerOverloaded(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return false;
            case JsonValue value when value.TryGetValue<string>(out string? text):
                return string.Equals(text, ServerOverloadedToken, StringComparison.OrdinalIgnoreCase);
            case JsonArray array:
                foreach (JsonNode? item in array)
                {
                    if (IsServerOverloaded(item))
                    {
                        return true;
                    }
                }

                return false;
            case JsonObject @object:
                if (TryGetSpecialMarker(@object, "codex_error_info", out JsonNode? snakeMarker)
                    || TryGetSpecialMarker(@object, "codexErrorInfo", out snakeMarker)
                    || TryGetSpecialMarker(@object, "errorInfo", out snakeMarker))
                {
                    return IsServerOverloaded(snakeMarker);
                }

                foreach (KeyValuePair<string, JsonNode?> pair in @object)
                {
                    if (IsServerOverloaded(pair.Value))
                    {
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }

    private static bool TryGetSpecialMarker(JsonObject payload, string key, out JsonNode? marker)
    {
        if (payload.TryGetPropertyValue(key, out JsonNode? value) && value is not null)
        {
            marker = value;
            return true;
        }

        marker = null;
        return false;
    }
}


