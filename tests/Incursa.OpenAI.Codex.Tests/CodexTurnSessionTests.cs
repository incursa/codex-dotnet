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

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0291")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0296")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0300")]
    public void TurnSession_TracksLifecycleStateAndCompletionBranches()
    {
        CodexTurnSession session = new(
            "  ",
            "  ",
            [new CodexTextInput { Text = "seed" }],
            new CodexTurnOptions(),
            (_, _) => Task.CompletedTask,
            _ => Task.CompletedTask);

        session.BindThreadId("thread-0");
        session.BindTurnId("turn-0");
        session.BindThreadId(" ");
        session.BindTurnId("\t");

        Assert.Equal("thread-0", session.ThreadId);
        Assert.Equal("turn-0", session.Id);

        session.AppendEvent(new CodexThreadStartedEvent
        {
            Thread = new CodexThreadSummary
            {
                Id = "thread-1",
            },
        });

        session.AppendEvent(new CodexTurnStartedEvent
        {
            Turn = new CodexTurnRecord
            {
                Id = "turn-1",
                Status = CodexTurnStatus.InProgress,
            },
        });

        session.AppendEvent(new CodexItemCompletedEvent
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            Item = new CodexAgentMessageItem
            {
                Id = "message-1",
                Phase = CodexMessagePhase.Commentary,
                Text = "hello",
            },
        });

        session.AppendEvent(new CodexTurnCompletedEvent
        {
            Turn = new CodexTurnRecord
            {
                Id = "turn-1",
                Status = CodexTurnStatus.Completed,
                Items =
                [
                    new CodexPlanItem
                    {
                        Id = "plan-1",
                        Text = "plan",
                    },
                ],
                Usage = new CodexUsage
                {
                    Last = new CodexTokenUsageBreakdown
                    {
                        TotalTokens = 3,
                    },
                    ModelContextWindow = 128,
                    Total = new CodexTokenUsageBreakdown
                    {
                        TotalTokens = 7,
                    },
                },
            },
        });

        session.AppendEvent(new CodexThreadErrorEvent
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            Error = new CodexTurnError
            {
                Message = "thread failed",
            },
        });

        session.AppendEvent(new CodexTurnFailedEvent
        {
            Turn = new CodexTurnRecord
            {
                Id = "turn-2",
                Status = CodexTurnStatus.Failed,
                Items =
                [
                    new CodexErrorItem
                    {
                        Id = "error-1",
                        Message = "boom",
                    },
                ],
                Error = new CodexTurnError
                {
                    Message = "boom",
                    AdditionalDetails = "tail",
                },
                Usage = new CodexUsage
                {
                    Last = new CodexTokenUsageBreakdown
                    {
                        TotalTokens = 9,
                    },
                    Total = new CodexTokenUsageBreakdown
                    {
                        TotalTokens = 10,
                    },
                },
            },
        });

        session.InterruptFromTransport();

        CodexTurnRecord record = session.ToRecord();

        Assert.Equal("thread-1", session.ThreadId);
        Assert.Equal("turn-2", session.Id);
        Assert.Equal(CodexTurnStatus.Failed, session.Status);
        Assert.True(session.IsInterruptRequested);
        Assert.Equal("boom", session.Error!.Message);
        Assert.Equal("tail", session.Error.AdditionalDetails);
        Assert.Single(session.Items);
        Assert.IsType<CodexErrorItem>(session.Items[0]);
        Assert.Equal(9, session.Usage!.Last.TotalTokens);
        Assert.Equal(10, session.Usage.Total.TotalTokens);
        Assert.Null(record.Usage!.ModelContextWindow);
        Assert.Equal(CodexTurnStatus.Failed, record.Status);
        Assert.Equal("turn-2", record.Id);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0291")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0300")]
    public async Task TurnSession_CompleteMethods_UpdateStateAndRejectInactiveOperations()
    {
        CodexTurnSession session = new(
            "thread-1",
            "turn-1",
            [new CodexTextInput { Text = "seed" }],
            new CodexTurnOptions(),
            (_, _) => Task.CompletedTask,
            _ => Task.CompletedTask);

        session.CompleteSuccess(
            [
                new CodexPlanItem
                {
                    Id = "plan-1",
                    Text = "plan",
                },
            ],
            new CodexUsage
            {
                Last = new CodexTokenUsageBreakdown
                {
                    TotalTokens = 1,
                },
                Total = new CodexTokenUsageBreakdown
                {
                    TotalTokens = 2,
                },
            });

        Assert.Equal(CodexTurnStatus.Completed, session.Status);
        Assert.Single(session.Items);
        Assert.Equal("plan-1", session.Items[0].Id);
        Assert.Null(session.Error);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.SteerAsync([new CodexTextInput { Text = "more" }], CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.InterruptAsync(CancellationToken.None));

        session.CompleteFailure(
            new CodexTurnError
            {
                Message = "boom",
                AdditionalDetails = "tail",
            },
            CodexTurnStatus.Failed);

        session.InterruptFromTransport();

        Assert.Equal(CodexTurnStatus.Failed, session.Status);
        Assert.Equal("boom", session.Error!.Message);
        Assert.Equal("tail", session.Error.AdditionalDetails);
        Assert.True(session.IsInterruptRequested);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0290")]
    public async Task TurnSession_ReadEventsAsync_RejectsConcurrentConsumers()
    {
        CodexTurnSession session = new(
            "thread-1",
            "turn-1",
            [new CodexTextInput { Text = "seed" }],
            new CodexTurnOptions(),
            (_, _) => Task.CompletedTask,
            _ => Task.CompletedTask);

        session.AppendEvent(new CodexTurnStartedEvent
        {
            Turn = new CodexTurnRecord
            {
                Id = "turn-1",
                Status = CodexTurnStatus.InProgress,
            },
        });

        await using IAsyncEnumerator<CodexThreadEvent> firstConsumer = session.ReadEventsAsync().GetAsyncEnumerator();

        Assert.True(await firstConsumer.MoveNextAsync());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using IAsyncEnumerator<CodexThreadEvent> secondConsumer = session.ReadEventsAsync().GetAsyncEnumerator();
            await secondConsumer.MoveNextAsync();
        });

        Assert.Contains("already has an active consumer", exception.Message, StringComparison.Ordinal);

        await firstConsumer.DisposeAsync();
    }
}
