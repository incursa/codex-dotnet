using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-TRANSPORT-0247, REQ-CODEX-SDK-TRANSPORT-0248, REQ-CODEX-SDK-CATALOG-0308.

public abstract class CodexException : Exception
{
    protected CodexException()
    {
    }

    protected CodexException(string? message)
        : base(message)
    {
    }

    protected CodexException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

public sealed class CodexTransportClosedException : CodexException
{
    public CodexTransportClosedException()
        : base("The Codex transport has been closed.")
    {
    }

    public CodexTransportClosedException(string? message)
        : base(message)
    {
    }
}

public abstract class CodexJsonRpcException : CodexException
{
    protected CodexJsonRpcException(int code, string? message, JsonNode? data = null)
        : base(message)
    {
        Code = code;
        ErrorData = data;
    }

    protected CodexJsonRpcException(int code, string? message, JsonNode? data, Exception? innerException)
        : base(message, innerException)
    {
        Code = code;
        ErrorData = data;
    }

    public int Code { get; }

    public JsonNode? ErrorData { get; }
}

public sealed class CodexParseException : CodexJsonRpcException
{
    public CodexParseException(string? message = null, JsonNode? data = null)
        : base(-32700, message ?? "Invalid JSON received from the Codex runtime.", data)
    {
    }
}

public sealed class CodexInvalidRequestException : CodexJsonRpcException
{
    public CodexInvalidRequestException(string? message = null, JsonNode? data = null)
        : base(-32600, message ?? "The Codex runtime rejected the request.", data)
    {
    }
}

public sealed class CodexMethodNotFoundException : CodexJsonRpcException
{
    public CodexMethodNotFoundException(string? message = null, JsonNode? data = null)
        : base(-32601, message ?? "The Codex runtime does not expose the requested method.", data)
    {
    }
}

public sealed class CodexInvalidParamsException : CodexJsonRpcException
{
    public CodexInvalidParamsException(string? message = null, JsonNode? data = null)
        : base(-32602, message ?? "The Codex runtime rejected the supplied parameters.", data)
    {
    }
}

public sealed class CodexInternalRpcException : CodexJsonRpcException
{
    public CodexInternalRpcException(string? message = null, JsonNode? data = null)
        : base(-32603, message ?? "The Codex runtime reported an internal failure.", data)
    {
    }
}

public class CodexServerBusyException : CodexJsonRpcException
{
    public CodexServerBusyException(string? message = null, JsonNode? data = null)
        : base(-32000, message ?? "The Codex runtime is busy.", data)
    {
    }

    protected CodexServerBusyException(int code, string? message, JsonNode? data)
        : base(code, message ?? "The Codex runtime is busy.", data)
    {
    }
}

public sealed class CodexRetryLimitExceededException : CodexServerBusyException
{
    public CodexRetryLimitExceededException(string? message = null, JsonNode? data = null)
        : base(-32001, message ?? "The Codex runtime exceeded the configured retry budget.", data)
    {
    }
}

public sealed class CodexCapabilityNotSupportedException : CodexException
{
    public CodexCapabilityNotSupportedException(
        string? operation = null,
        CodexBackendSelection backendSelection = CodexBackendSelection.AppServer,
        string? message = null,
        Exception? innerException = null)
        : base(message ?? BuildMessage(operation, backendSelection), innerException)
    {
        Operation = operation;
        BackendSelection = backendSelection;
    }

    public string? Operation { get; }

    public CodexBackendSelection BackendSelection { get; }

    private static string BuildMessage(string? operation, CodexBackendSelection backendSelection)
    {
        if (string.IsNullOrWhiteSpace(operation))
        {
            return $"The selected Codex backend ({backendSelection}) does not support this operation.";
        }

        return $"The selected Codex backend ({backendSelection}) does not support '{operation}'.";
    }
}


