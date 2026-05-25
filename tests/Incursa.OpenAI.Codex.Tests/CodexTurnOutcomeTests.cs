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

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0290")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0300")]
    public async Task ObserveNormalizedEventsAsync_FansOutTurnEventsToConcurrentSubscribers()
    {
        await using CodexClient client = new();
        CodexTurnSession session = CreateSession();
        CodexTurn turn = new(client, session);

        RecordingObserver<CodexTurnEvent> firstObserver = new();
        RecordingObserver<CodexTurnEvent> secondObserver = new();
        using IDisposable firstSubscription = turn.ObserveNormalizedEventsAsync().Subscribe(firstObserver);
        using IDisposable secondSubscription = turn.ObserveNormalizedEventsAsync().Subscribe(secondObserver);

        session.AppendEvent(CreateStartedEvent());
        session.AppendEvent(CreateCompletedEvent("Done."));
        session.CompleteWriter();

        await firstObserver.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        await secondObserver.Completion.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Done.", Assert.Single(firstObserver.Events, evt => evt.Kind == CodexTurnEventKind.FinalResponse).Text);
        Assert.Equal("Done.", Assert.Single(secondObserver.Events, evt => evt.Kind == CodexTurnEventKind.FinalResponse).Text);
        Assert.Equal(CodexTurnTerminalState.Completed, Assert.Single(firstObserver.Events, evt => evt.Kind == CodexTurnEventKind.Terminal).TerminalState);
        Assert.Equal(CodexTurnTerminalState.Completed, Assert.Single(secondObserver.Events, evt => evt.Kind == CodexTurnEventKind.Terminal).TerminalState);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0290")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0300")]
    public async Task ObserveNormalizedEventsAsync_CanShareTurnWithStreamNormalizedAsync()
    {
        await using CodexClient client = new();
        CodexTurnSession session = CreateSession();
        CodexTurn turn = new(client, session);
        RecordingObserver<CodexTurnEvent> observer = new();
        using IDisposable subscription = turn.ObserveNormalizedEventsAsync().Subscribe(observer);
        List<CodexTurnEvent> streamedEvents = [];

        Task streamTask = Task.Run(async () =>
        {
            await foreach (CodexTurnEvent evt in turn.StreamNormalizedAsync())
            {
                streamedEvents.Add(evt);
            }
        });

        session.AppendEvent(CreateStartedEvent());
        session.AppendEvent(CreateCompletedEvent("Shared."));
        session.CompleteWriter();

        await streamTask.WaitAsync(TimeSpan.FromSeconds(5));
        await observer.Completion.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Shared.", Assert.Single(streamedEvents, evt => evt.Kind == CodexTurnEventKind.FinalResponse).Text);
        Assert.Equal("Shared.", Assert.Single(observer.Events, evt => evt.Kind == CodexTurnEventKind.FinalResponse).Text);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0294")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    public async Task StreamNormalizedAsync_ProjectsTypedProgressEventsWithMetadata()
    {
        await using CodexClient client = new();
        CodexTurnSession session = CreateSession();
        session.AppendEvent(new CodexCommandExecutionOutputDeltaEvent
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            ItemId = "cmd-1",
            Delta = "stdout chunk",
        });
        session.AppendEvent(new CodexMcpToolCallProgressEvent
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            ItemId = "mcp-1",
            Message = "tool is running",
        });
        session.AppendEvent(new CodexThreadGoalUpdatedEvent
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            Goal = new CodexThreadGoal
            {
                ThreadId = "thread-1",
                Objective = "finish observable support",
                Status = CodexThreadGoalStatus.Active,
                TokenBudget = 1200,
                TokensUsed = 42,
            },
        });
        session.AppendEvent(new CodexThreadTokenUsageUpdatedEvent
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            TokenUsage = new CodexUsage
            {
                Last = new CodexTokenUsageBreakdown { TotalTokens = 7 },
                Total = new CodexTokenUsageBreakdown { TotalTokens = 99 },
            },
        });
        session.AppendEvent(new CodexHookStartedEvent
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            Run = new CodexHookRunSummary
            {
                Id = "hook-1",
                EventName = CodexHookEventName.PreToolUse,
                Status = CodexHookRunStatus.Running,
            },
        });
        session.AppendEvent(CreateCompletedEvent("Done."));
        session.CompleteWriter();

        List<CodexTurnEvent> events = [];
        await foreach (CodexTurnEvent evt in new CodexTurn(client, session).StreamNormalizedAsync())
        {
            events.Add(evt);
        }

        Assert.Contains(events, evt =>
            evt.RawEventType == "item.commandExecution.outputDelta"
            && evt.Kind == CodexTurnEventKind.Progress
            && evt.Text == "stdout chunk"
            && evt.Metadata["itemId"] == "cmd-1");
        Assert.Contains(events, evt =>
            evt.RawEventType == "item.mcpToolCall.progress"
            && evt.Text == "tool is running"
            && evt.Metadata["itemId"] == "mcp-1");
        Assert.Contains(events, evt =>
            evt.RawEventType == "thread.goal.updated"
            && evt.Text == "finish observable support"
            && evt.Metadata["tokensUsed"] == "42"
            && evt.IsUserVisibleByDefault);
        Assert.Contains(events, evt =>
            evt.RawEventType == "thread.tokenUsage.updated"
            && evt.Metadata["totalTokens"] == "99");
        Assert.Contains(events, evt =>
            evt.RawEventType == "hook.started"
            && evt.Metadata["hookRunId"] == "hook-1");
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

    private sealed class RecordingObserver<T> : IObserver<T>
    {
        private readonly object _gate = new();
        private readonly List<T> _events = [];
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Completion => _completion.Task;

        public IReadOnlyList<T> Events
        {
            get
            {
                lock (_gate)
                {
                    return _events.ToArray();
                }
            }
        }

        public void OnNext(T value)
        {
            lock (_gate)
            {
                _events.Add(value);
            }
        }

        public void OnError(Exception error)
        {
            _completion.TrySetException(error);
        }

        public void OnCompleted()
        {
            _completion.TrySetResult();
        }
    }
}
