namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexTurnSessionTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0242")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0290")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0291")]
    public async Task TurnConsumerGate_RejectsConcurrentConsumers()
    {
        CodexTurnConsumerGate gate = new();

        await using (await gate.AcquireAsync("turn-1", CancellationToken.None))
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                gate.AcquireAsync("turn-2", CancellationToken.None).AsTask());

            Assert.Contains("turn-1", exception.Message, StringComparison.Ordinal);
        }

        await using CodexTurnConsumerGate.Lease second = await gate.AcquireAsync("turn-2", CancellationToken.None);
        Assert.NotNull(second);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0291")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0296")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0300")]
    public async Task TurnSession_TracksSteerInterruptAndCompletionState()
    {
        CodexTurnSession session = new(
            "thread-1",
            "turn-1",
            [new CodexTextInput { Text = "hello" }],
            new CodexTurnOptions(),
            (_, _) => Task.CompletedTask,
            _ => Task.CompletedTask,
            new CodexTurnConsumerGate());

        session.AppendEvent(new CodexTurnStartedEvent
        {
            Turn = new CodexTurnRecord
            {
                Id = "turn-1",
                Status = CodexTurnStatus.InProgress,
            },
        });

        await session.SteerAsync(
            [new CodexTextInput { Text = "more context" }],
            CancellationToken.None);

        await session.InterruptAsync(CancellationToken.None);

        session.AppendEvent(new CodexTurnCompletedEvent
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
                        Text = "done",
                    },
                ],
                Usage = new CodexUsage
                {
                    Last = new CodexTokenUsageBreakdown
                    {
                        TotalTokens = 1,
                    },
                    Total = new CodexTokenUsageBreakdown
                    {
                        TotalTokens = 1,
                    },
                },
            },
        });

        Assert.True(session.IsInterruptRequested);
        Assert.Contains(session.SteeredInput, item => item is CodexTextInput text && text.Text == "more context");
        Assert.Equal(CodexTurnStatus.Completed, session.Status);
        Assert.Single(session.Items);
        Assert.Equal("done", ((CodexAgentMessageItem)session.Items[0]).Text);
        Assert.Equal("turn-1", session.ToRecord().Id);
    }
}


