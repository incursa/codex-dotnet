using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-TRANSPORT-0247, REQ-CODEX-SDK-TRANSPORT-0248, REQ-CODEX-SDK-CATALOG-0308.

/// <summary>
/// Base exception for failures reported by the Codex SDK.
/// </summary>
public abstract class CodexException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodexException"/> class.
    /// </summary>
    protected CodexException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodexException"/> class with an error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected CodexException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodexException"/> class with an error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    protected CodexException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Indicates that an operation was attempted after the Codex transport was closed.
/// </summary>
public sealed class CodexTransportClosedException : CodexException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodexTransportClosedException"/> class.
    /// </summary>
    public CodexTransportClosedException()
        : base("The Codex transport has been closed.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodexTransportClosedException"/> class with an error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CodexTransportClosedException(string? message)
        : base(message)
    {
    }
}

/// <summary>
/// Base exception for JSON-RPC errors returned by a Codex runtime.
/// </summary>
public abstract class CodexJsonRpcException : CodexException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodexJsonRpcException"/> class.
    /// </summary>
    /// <param name="code">The JSON-RPC error code.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="data">Additional JSON-RPC error data.</param>
    protected CodexJsonRpcException(int code, string? message, JsonNode? data = null)
        : base(message)
    {
        Code = code;
        ErrorData = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodexJsonRpcException"/> class with an inner exception.
    /// </summary>
    /// <param name="code">The JSON-RPC error code.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="data">Additional JSON-RPC error data.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    protected CodexJsonRpcException(int code, string? message, JsonNode? data, Exception? innerException)
        : base(message, innerException)
    {
        Code = code;
        ErrorData = data;
    }

    /// <summary>
    /// Gets the JSON-RPC error code.
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// Gets additional JSON-RPC error data returned by the runtime.
    /// </summary>
    public JsonNode? ErrorData { get; }
}

/// <summary>
/// Indicates that the runtime returned a JSON-RPC parse error.
/// </summary>
public sealed class CodexParseException : CodexJsonRpcException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodexParseException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="data">Additional JSON-RPC error data.</param>
    public CodexParseException(string? message = null, JsonNode? data = null)
        : base(-32700, message ?? "Invalid JSON received from the Codex runtime.", data)
    {
    }
}

/// <summary>
/// Indicates that the runtime rejected a JSON-RPC request as invalid.
/// </summary>
public sealed class CodexInvalidRequestException : CodexJsonRpcException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodexInvalidRequestException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="data">Additional JSON-RPC error data.</param>
    public CodexInvalidRequestException(string? message = null, JsonNode? data = null)
        : base(-32600, message ?? "The Codex runtime rejected the request.", data)
    {
    }
}

/// <summary>
/// Indicates that the runtime does not expose a requested JSON-RPC method.
/// </summary>
public sealed class CodexMethodNotFoundException : CodexJsonRpcException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodexMethodNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="data">Additional JSON-RPC error data.</param>
    public CodexMethodNotFoundException(string? message = null, JsonNode? data = null)
        : base(-32601, message ?? "The Codex runtime does not expose the requested method.", data)
    {
    }
}

/// <summary>
/// Indicates that the runtime rejected JSON-RPC parameters.
/// </summary>
public sealed class CodexInvalidParamsException : CodexJsonRpcException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodexInvalidParamsException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="data">Additional JSON-RPC error data.</param>
    public CodexInvalidParamsException(string? message = null, JsonNode? data = null)
        : base(-32602, message ?? "The Codex runtime rejected the supplied parameters.", data)
    {
    }
}

/// <summary>
/// Indicates that the runtime reported an internal JSON-RPC failure.
/// </summary>
public sealed class CodexInternalRpcException : CodexJsonRpcException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodexInternalRpcException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="data">Additional JSON-RPC error data.</param>
    public CodexInternalRpcException(string? message = null, JsonNode? data = null)
        : base(-32603, message ?? "The Codex runtime reported an internal failure.", data)
    {
    }
}

/// <summary>
/// Indicates that the Codex runtime is temporarily unable to process a request.
/// </summary>
public class CodexServerBusyException : CodexJsonRpcException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodexServerBusyException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="data">Additional JSON-RPC error data.</param>
    public CodexServerBusyException(string? message = null, JsonNode? data = null)
        : base(-32000, message ?? "The Codex runtime is busy.", data)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodexServerBusyException"/> class with a custom JSON-RPC error code.
    /// </summary>
    /// <param name="code">The JSON-RPC error code.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="data">Additional JSON-RPC error data.</param>
    protected CodexServerBusyException(int code, string? message, JsonNode? data)
        : base(code, message ?? "The Codex runtime is busy.", data)
    {
    }
}

/// <summary>
/// Indicates that a retry policy exhausted its configured retry budget.
/// </summary>
public sealed class CodexRetryLimitExceededException : CodexServerBusyException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodexRetryLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="data">Additional JSON-RPC error data.</param>
    public CodexRetryLimitExceededException(string? message = null, JsonNode? data = null)
        : base(-32001, message ?? "The Codex runtime exceeded the configured retry budget.", data)
    {
    }
}

/// <summary>
/// Indicates that the selected Codex backend cannot perform a requested SDK operation.
/// </summary>
public sealed class CodexCapabilityNotSupportedException : CodexException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodexCapabilityNotSupportedException"/> class.
    /// </summary>
    /// <param name="operation">The SDK operation that required the unsupported capability.</param>
    /// <param name="backendSelection">The backend selected for the client.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
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

    /// <summary>
    /// Gets the SDK operation that required the unsupported capability.
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// Gets the backend selected for the client.
    /// </summary>
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

