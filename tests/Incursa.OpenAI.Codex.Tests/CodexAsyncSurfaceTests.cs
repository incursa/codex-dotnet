using System.Reflection;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexAsyncSurfaceTests
{
    [Theory]
    [InlineData(typeof(CodexClient))]
    [InlineData(typeof(CodexThread))]
    [InlineData(typeof(CodexTurn))]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0301")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0311")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0312")]
    public void RuntimeClasses_ExposeOnlyAsyncPublicMethods(Type type)
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(type));

        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (MethodInfo method in methods)
        {
            if (method.IsSpecialName)
            {
                continue;
            }

            Assert.True(
                method.Name.EndsWith("Async", StringComparison.Ordinal),
                $"{type.Name}.{method.Name} should be async-only.");
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0312")]
    public void CodexClient_ImplementsIAsyncDisposable()
    {
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(CodexClient)));
    }

    public static IEnumerable<object[]> HelperEnumTypes =>
    [
        [typeof(CodexBackendSelection)],
        [typeof(CodexApprovalMode)],
        [typeof(CodexApprovalsReviewer)],
        [typeof(CodexCollabAgentStatus)],
        [typeof(CodexCollabAgentTool)],
        [typeof(CodexCollabAgentToolCallStatus)],
        [typeof(CodexCommandExecutionStatus)],
        [typeof(CodexDynamicToolCallStatus)],
        [typeof(CodexInputModality)],
        [typeof(CodexMcpToolCallStatus)],
        [typeof(CodexMessagePhase)],
        [typeof(CodexNetworkAccess)],
        [typeof(CodexPatchApplyStatus)],
        [typeof(CodexPatchChangeKind)],
        [typeof(CodexPersonality)],
        [typeof(CodexReasoningEffort)],
        [typeof(CodexReasoningSummary)],
        [typeof(CodexSandboxMode)],
        [typeof(CodexServiceTier)],
        [typeof(CodexSessionSourceKind)],
        [typeof(CodexSubAgentSourceKind)],
        [typeof(CodexThreadActiveFlag)],
        [typeof(CodexThreadSortKey)],
        [typeof(CodexThreadSourceKind)],
        [typeof(CodexTurnPlanStepStatus)],
        [typeof(CodexTurnStatus)],
        [typeof(CodexWebSearchContextSize)],
        [typeof(CodexWebSearchMode)],
    ];

    [Theory]
    [MemberData(nameof(HelperEnumTypes))]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0313")]
    public void HelperEnums_ArePublicAndStable(Type type)
    {
        Assert.True(type.IsEnum);
        Assert.NotEmpty(Enum.GetNames(type));
    }
}


