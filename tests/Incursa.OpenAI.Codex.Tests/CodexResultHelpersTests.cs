namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexResultHelpersTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0317")]
    public void SelectFinalResponse_PrefersTheLastFinalAnswerWithText()
    {
        IReadOnlyList<CodexThreadItem> items =
        [
            new CodexAgentMessageItem
            {
                Id = "message-1",
                Phase = CodexMessagePhase.Commentary,
                Text = "thinking",
            },
            new CodexAgentMessageItem
            {
                Id = "message-2",
                Phase = CodexMessagePhase.FinalAnswer,
                Text = " ",
            },
            new CodexAgentMessageItem
            {
                Id = "message-3",
                Phase = CodexMessagePhase.FinalAnswer,
                Text = "done",
            },
            new CodexPlanItem
            {
                Id = "plan-1",
                Text = "plan",
            },
        ];

        Assert.Equal("done", CodexResultHelpers.SelectFinalResponse(items));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0317")]
    public void SelectFinalResponse_FallsBackToTheLastPhaseLessAgentMessage()
    {
        IReadOnlyList<CodexThreadItem> items =
        [
            new CodexAgentMessageItem
            {
                Id = "message-1",
                Phase = CodexMessagePhase.Commentary,
                Text = "thinking",
            },
            new CodexAgentMessageItem
            {
                Id = "message-2",
                Text = "fallback",
            },
            new CodexAgentMessageItem
            {
                Id = "message-3",
                Phase = CodexMessagePhase.FinalAnswer,
                Text = " ",
            },
        ];

        Assert.Equal("fallback", CodexResultHelpers.SelectFinalResponse(items));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0317")]
    public void SelectFinalResponse_ReturnsNullWhenNoUsableTextExists()
    {
        IReadOnlyList<CodexThreadItem> items =
        [
            new CodexAgentMessageItem
            {
                Id = "message-1",
                Phase = CodexMessagePhase.FinalAnswer,
                Text = " ",
            },
            new CodexPlanItem
            {
                Id = "plan-1",
                Text = "plan",
            },
        ];

        Assert.Null(CodexResultHelpers.SelectFinalResponse(items));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0237")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0317")]
    public void ToException_MapsTurnFailuresConsistently()
    {
        CodexInvalidRequestException missingError = Assert.IsType<CodexInvalidRequestException>(
            CodexResultHelpers.ToException(new CodexTurnRecord
            {
                Id = "turn-1",
                Status = CodexTurnStatus.Failed,
            }));

        Assert.Contains("failed without a populated error object", missingError.Message, StringComparison.Ordinal);

        CodexInvalidRequestException populatedError = Assert.IsType<CodexInvalidRequestException>(
            CodexResultHelpers.ToException(new CodexTurnRecord
            {
                Id = "turn-2",
                Status = CodexTurnStatus.Failed,
                Error = new CodexTurnError
                {
                    Message = "boom",
                },
            }));

        Assert.Equal("boom", populatedError.Message);
    }
}
