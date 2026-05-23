namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexTurnOutcomeTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0290")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0300")]
    public async Task StreamNormalizedAsync_EmitsFinalResponseAndTerminalFromCompletedTurn()
    {
        await using CodexClient client = new();
        CodexTurnSession session = CreateSession();
        session.AppendEvent(CreateStartedEvent());
        session.AppendEvent(CreateCompletedEvent("Done."));
        session.CompleteWriter();

        CodexTurn turn = new(client, session);
        List<CodexTurnEvent> events = [];
        await foreach (CodexTurnEvent evt in turn.StreamNormalizedAsync())
        {
            events.Add(evt);
        }

        CodexTurnEvent finalResponse = Assert.Single(events, evt => evt.Kind == CodexTurnEventKind.FinalResponse);
        Assert.Equal("Done.", finalResponse.Text);
        Assert.True(finalResponse.ContributesToFinalOutput);

        CodexTurnEvent terminal = Assert.Single(events, evt => evt.Kind == CodexTurnEventKind.Terminal);
        Assert.True(terminal.IsTerminal);
        Assert.Equal(CodexTurnTerminalState.Completed, terminal.TerminalState);
        Assert.Equal("turn.completed", terminal.RawEventType);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0290")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0300")]
    public async Task RunToResultAsync_ReportsTerminalStateAndFinalResponseSource()
    {
        await using CodexClient client = new();
        CodexTurnSession session = CreateSession();
        session.AppendEvent(CreateStartedEvent());
        session.AppendEvent(CreateCompletedEvent("Final answer."));
        session.CompleteWriter();

        CodexTurnResult result = await new CodexTurn(client, session).RunToResultAsync();

        Assert.Equal("thread-1", result.ThreadId);
        Assert.Equal("turn-1", result.TurnId);
        Assert.Equal(CodexTurnTerminalState.Completed, result.TerminalState);
        Assert.Equal(CodexTurnStatus.Completed, result.TurnStatus);
        Assert.True(result.TerminalEventSeen);
        Assert.Equal("turn.completed", result.TerminalEventType);
        Assert.Equal("Final answer.", result.FinalResponseText);
        Assert.Equal(CodexFinalResponseSource.TerminalEvent, result.FinalResponseSource);
        Assert.True(result.FinalResponseComplete);
        Assert.Equal("C:\\work", result.WorkingDirectory);
        Assert.Equal(2, result.RawEventCount);
        Assert.True(result.NormalizedEventCount >= 3);
        Assert.Equal("Final answer.".Length, result.FinalResponseCharCount);
        Assert.Single(result.Items);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0290")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0300")]
    public async Task RunToResultAsync_UsesAssistantDeltasAndMarksIncompleteWhenTerminalIsMissing()
    {
        await using CodexClient client = new();
        CodexTurnSession session = CreateSession();
        session.AppendEvent(new CodexAgentMessageDeltaEvent
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            ItemId = "message-1",
            Delta = "partial answer",
        });
        session.CompleteWriter();

        CodexTurnResult result = await new CodexTurn(client, session).RunToResultAsync();

        Assert.Equal(CodexTurnTerminalState.Incomplete, result.TerminalState);
        Assert.False(result.TerminalEventSeen);
        Assert.Equal("partial answer", result.FinalResponseText);
        Assert.Equal(CodexFinalResponseSource.AssistantDelta, result.FinalResponseSource);
        Assert.False(result.FinalResponseComplete);
        Assert.Equal("partial answer".Length, result.AssistantOutputCharCount);
        Assert.True(result.NormalizedEventCount >= 2);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0290")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0300")]
    public async Task RunToResultAsync_ReportsFailedTerminalEventAndErrorMessage()
    {
        await using CodexClient client = new();
        CodexTurnSession session = CreateSession();
        session.AppendEvent(new CodexTurnFailedEvent
        {
            Turn = new CodexTurnRecord
            {
                Id = "turn-1",
                Status = CodexTurnStatus.Failed,
                Error = new CodexTurnError
                {
                    Message = "boom",
                },
            },
        });
        session.CompleteWriter();

        CodexTurnResult result = await new CodexTurn(client, session).RunToResultAsync();

        Assert.Equal(CodexTurnTerminalState.Failed, result.TerminalState);
        Assert.True(result.TerminalEventSeen);
        Assert.Equal("turn.failed", result.TerminalEventType);
        Assert.Equal("boom", result.ErrorMessage);
        Assert.Null(result.FinalResponseText);
        Assert.Equal(CodexFinalResponseSource.None, result.FinalResponseSource);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0290")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0300")]
    public async Task RunToResultAsync_ReportsStreamFailureWithoutClaimingTerminalEvent()
    {
        await using CodexClient client = new();
        CodexTurnSession session = CreateSession();
        session.AppendEvent(CreateStartedEvent());
        session.Writer.TryComplete(new InvalidOperationException("pipe broke"));

        CodexTurnResult result = await new CodexTurn(client, session).RunToResultAsync();

        Assert.Equal(CodexTurnTerminalState.Incomplete, result.TerminalState);
        Assert.False(result.TerminalEventSeen);
        Assert.Equal("turn.stream.failed", result.TerminalEventType);
        Assert.Equal("pipe broke", result.ErrorMessage);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0290")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0300")]
    public async Task StreamNormalizedAsync_EmitsIncompleteTerminalWhenStreamFails()
    {
        await using CodexClient client = new();
        CodexTurnSession session = CreateSession();
        session.Writer.TryComplete(new InvalidOperationException("pipe broke"));

        List<CodexTurnEvent> events = [];
        await foreach (CodexTurnEvent evt in new CodexTurn(client, session).StreamNormalizedAsync())
        {
            events.Add(evt);
        }

        CodexTurnEvent terminal = Assert.Single(events, evt => evt.Kind == CodexTurnEventKind.Terminal);
        Assert.Equal(CodexTurnTerminalState.Incomplete, terminal.TerminalState);
        Assert.DoesNotContain(events, evt => evt.RawEventType == "turn.completed" || evt.RawEventType == "turn.failed");
    }

    private static CodexTurnSession CreateSession()
        => new(
            "thread-1",
            "turn-1",
            [new CodexTextInput { Text = "hello" }],
            new CodexTurnOptions
            {
                WorkingDirectory = "C:\\work",
            },
            (_, _, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask);

    private static CodexTurnStartedEvent CreateStartedEvent()
        => new()
        {
            Turn = new CodexTurnRecord
            {
                Id = "turn-1",
                Status = CodexTurnStatus.InProgress,
            },
        };

    private static CodexTurnCompletedEvent CreateCompletedEvent(string finalResponse)
        => new()
        {
            Turn = new CodexTurnRecord
            {
                Id = "turn-1",
                Status = CodexTurnStatus.Completed,
                Items =
                [
                    new CodexAgentMessageItem
                    {
                        Id = "message-1",
                        Phase = CodexMessagePhase.FinalAnswer,
                        Text = finalResponse,
                    },
                ],
            },
        };
}
