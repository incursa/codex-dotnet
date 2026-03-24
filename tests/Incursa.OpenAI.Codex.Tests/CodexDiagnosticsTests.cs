using System.Collections.Generic;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexDiagnosticsTests
{
    public static IEnumerable<object[]> JsonRpcExceptionCases()
    {
        yield return new object[] { new Func<CodexJsonRpcException>(() => new CodexParseException()), -32700 };
        yield return new object[] { new Func<CodexJsonRpcException>(() => new CodexInvalidRequestException()), -32600 };
        yield return new object[] { new Func<CodexJsonRpcException>(() => new CodexMethodNotFoundException()), -32601 };
        yield return new object[] { new Func<CodexJsonRpcException>(() => new CodexInvalidParamsException()), -32602 };
        yield return new object[] { new Func<CodexJsonRpcException>(() => new CodexInternalRpcException()), -32603 };
        yield return new object[] { new Func<CodexJsonRpcException>(() => new CodexServerBusyException()), -32000 };
        yield return new object[] { new Func<CodexJsonRpcException>(() => new CodexRetryLimitExceededException()), -32001 };
    }

    [Theory]
    [MemberData(nameof(JsonRpcExceptionCases))]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0308")]
    public void JsonRpcDerivedExceptions_ExposeExpectedCodes(
        Func<CodexJsonRpcException> factory,
        int expectedCode)
    {
        CodexJsonRpcException exception = factory();

        Assert.Equal(expectedCode, exception.Code);
        Assert.Null(exception.ErrorData);
        Assert.True(exception is CodexException);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0308")]
    public void CapabilityNotSupportedException_CarriesOperationAndBackendSelection()
    {
        CodexCapabilityNotSupportedException exception = new(
            operation: nameof(CodexClient.ListThreadsAsync),
            backendSelection: CodexBackendSelection.Exec);

        Assert.Equal(nameof(CodexClient.ListThreadsAsync), exception.Operation);
        Assert.Equal(CodexBackendSelection.Exec, exception.BackendSelection);
        Assert.Contains(nameof(CodexClient.ListThreadsAsync), exception.Message);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0314")]
    public async Task ExecBackend_DisallowsThreadListing()
    {
        await using CodexClient client = new(new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.Exec,
        });

        CodexCapabilityNotSupportedException exception = await Assert.ThrowsAsync<CodexCapabilityNotSupportedException>(
            () => client.ListThreadsAsync());

        Assert.Equal(nameof(CodexClient.ListThreadsAsync), exception.Operation);
        Assert.Equal(CodexBackendSelection.Exec, exception.BackendSelection);
    }
}


