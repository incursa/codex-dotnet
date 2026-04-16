using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexContractTypeTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0314")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0315")]
    public void CoreHelperTypes_CarryRepresentativeValues()
    {
        CodexThreadSpawn spawn = new()
        {
            AgentNickname = "spawn-bot",
            AgentRole = "reviewer",
            Depth = 2,
            ParentThreadId = "thread-parent",
        };

        CodexThreadSummary summary = CreateThreadSummary("thread-1", "Trace thread") with
        {
            Status = new CodexActiveThreadStatus
            {
                ActiveFlags = [CodexThreadActiveFlag.WaitingOnApproval],
            },
            Source = new CodexSubAgentSessionSource(new CodexThreadSpawnSubAgentSource(spawn)),
            Path = "/work",
            AgentRole = "coding-agent",
            AgentNickname = "trace",
            GitInfo = new CodexGitInfo
            {
                Branch = "main",
                OriginUrl = "https://example.com/repo.git",
                Sha = "abc123",
            },
        };

        Assert.NotSame(summary, summary with { });
        Assert.Equal(summary, summary with { });
        Assert.NotEqual(summary, summary with { Name = "Other" });
        Assert.Equal("thread-1", summary.Id);
        Assert.Equal("trace", summary.AgentNickname);
        Assert.IsType<CodexActiveThreadStatus>(summary.Status);
        Assert.IsType<CodexSubAgentSessionSource>(summary.Source);
        Assert.Equal(
            "spawn-bot",
            ((CodexThreadSpawnSubAgentSource)((CodexSubAgentSessionSource)summary.Source).SubAgent).ThreadSpawn.AgentNickname);
        Assert.Equal("main", summary.GitInfo!.Branch);

        CodexTurnRecord turn = CreateTurnRecord();
        Assert.NotSame(turn, turn with { });
        Assert.Equal(turn, turn with { });
        Assert.Equal("turn-1", turn.Id);
        Assert.Equal(CodexTurnStatus.Completed, turn.Status);
        Assert.Equal("boom", turn.Error!.Message);
        Assert.Equal(20, turn.Usage!.Total.TotalTokens);

        CodexThreadSnapshot snapshot = CreateThreadSnapshot(summary, turn);
        Assert.NotSame(snapshot, snapshot with { });
        Assert.Equal(snapshot, snapshot with { });
        Assert.Equal("thread-1", snapshot.Id);
        Assert.Single(snapshot.Turns);
        Assert.Equal(turn, snapshot.Turns[0]);

        CodexRunResult runResult = new()
        {
            Items = [new CodexAgentMessageItem { Id = "message-1", Phase = CodexMessagePhase.FinalAnswer, Text = "done" }],
            FinalResponse = "done",
            Usage = turn.Usage,
        };
        Assert.NotSame(runResult, runResult with { });
        Assert.Equal(runResult, runResult with { });
        Assert.Equal("done", runResult.FinalResponse);
        Assert.Equal("message-1", ((CodexAgentMessageItem)runResult.Items[0]).Id);

        CodexModel model = CreateModel();
        Assert.NotSame(model, model with { });
        Assert.Equal(model, model with { });
        Assert.Equal("model-1", model.Id);
        Assert.Equal(CodexReasoningEffort.High, model.DefaultReasoningEffort);
        Assert.True(model.Hidden);
        Assert.True(model.SupportsPersonality);
        Assert.Equal("upgrade", model.Upgrade);
        Assert.Equal("Upgrade now", model.UpgradeInfo!.UpgradeCopy);

        CodexThreadListResult threadList = new()
        {
            Threads = [summary],
            NextCursor = "thread-cursor",
        };
        Assert.Equal(threadList, threadList with { });
        Assert.Equal("thread-cursor", threadList.NextCursor);
        Assert.Single(threadList.Threads);

        CodexModelListResult modelList = new()
        {
            Models = [model],
            NextCursor = "model-cursor",
        };
        Assert.Equal(modelList, modelList with { });
        Assert.Equal("model-cursor", modelList.NextCursor);
        Assert.Single(modelList.Models);

        CodexRuntimeMetadata metadata = new()
        {
            ServerInfo = new CodexServerInfo
            {
                Name = "codex-app-server",
                Version = "1.2.3",
            },
            PlatformFamily = "Unix",
            PlatformOs = "Linux",
            UserAgent = "codex-app-server/1.2.3",
        };
        Assert.NotSame(metadata, metadata with { });
        Assert.Equal(metadata, metadata with { });
        Assert.Equal("Linux", metadata.PlatformOs);
        Assert.Equal("codex-app-server", metadata.ServerInfo!.Name);

        CodexRuntimeCapabilities capabilities = new()
        {
            BackendSelection = CodexBackendSelection.AppServer,
            ExperimentalApi = true,
            SupportsArchiveThread = true,
            SupportsCompactThread = true,
            SupportsForkThread = true,
            SupportsListModels = true,
            SupportsListThreads = true,
            SupportsReadThread = true,
            SupportsResumeThread = true,
            SupportsSetThreadName = true,
            SupportsStartThread = true,
            SupportsThreadStreaming = true,
            SupportsTurnInterruption = true,
            SupportsTurnSteering = true,
            SupportsUnarchiveThread = true,
            OptOutNotificationMethods = ["turn.completed"],
        };
        Assert.NotSame(capabilities, capabilities with { });
        Assert.Equal(capabilities, capabilities with { });
        Assert.True(capabilities.SupportsTurnSteering);
        Assert.Contains("turn.completed", capabilities.OptOutNotificationMethods);

        CodexTurnError turnError = new()
        {
            Message = "boom",
            AdditionalDetails = "tail",
            CodexErrorInfo = new JsonObject
            {
                ["kind"] = "trace",
            },
        };
        Assert.Equal("boom", turnError.Message);
        Assert.Equal("tail", turnError.AdditionalDetails);

        CodexThreadError threadError = new()
        {
            Message = "thread boom",
        };
        Assert.Equal("thread boom", threadError.Message);

        CodexTokenUsageBreakdown breakdown = new()
        {
            CachedInputTokens = 1,
            InputTokens = 2,
            OutputTokens = 3,
            ReasoningOutputTokens = 4,
            TotalTokens = 10,
        };
        Assert.Equal(10, breakdown.TotalTokens);

        CodexUsage usage = new()
        {
            Last = breakdown,
            ModelContextWindow = 4096,
            Total = breakdown with
            {
                TotalTokens = 20,
            },
        };
        Assert.Equal(4096, usage.ModelContextWindow);
        Assert.Equal(20, usage.Total.TotalTokens);

        CodexModelAvailabilityNux nux = new()
        {
            Message = "upgrade available",
        };
        Assert.Equal("upgrade available", nux.Message);

        CodexModelUpgradeInfo upgradeInfo = new()
        {
            MigrationMarkdown = "docs",
            Model = "gpt-5",
            ModelLink = "https://example.com/model",
            UpgradeCopy = "Upgrade now",
        };
        Assert.Equal("gpt-5", upgradeInfo.Model);

        CodexReasoningEffortOption effortOption = new()
        {
            Description = "high throughput",
            ReasoningEffort = CodexReasoningEffort.High,
        };
        Assert.Equal(CodexReasoningEffort.High, effortOption.ReasoningEffort);

        CodexFileUpdateChange change = new()
        {
            Diff = "+hello",
            Kind = CodexPatchChangeKind.Add,
            Path = "/work/file.txt",
        };
        Assert.Equal(CodexPatchChangeKind.Add, change.Kind);

        CodexTodoItem todo = new()
        {
            Completed = true,
            Text = "ship it",
        };
        Assert.True(todo.Completed);

        CodexCollabAgentState collabState = new()
        {
            Message = "running",
            Status = CodexCollabAgentStatus.Running,
        };
        Assert.Equal(CodexCollabAgentStatus.Running, collabState.Status);

        CodexSessionSourceValue sessionSource = new(CodexSessionSourceKind.Exec);
        Assert.Equal(CodexSessionSourceKind.Exec, sessionSource.Value);

        CodexSubAgentSessionSource subAgentSource = new(new CodexThreadSpawnSubAgentSource(spawn));
        Assert.Equal("spawn-bot", ((CodexThreadSpawnSubAgentSource)subAgentSource.SubAgent).ThreadSpawn.AgentNickname);

        CodexOtherSubAgentSource otherSubAgentSource = new("custom");
        Assert.Equal("custom", otherSubAgentSource.Other);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0314")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0315")]
    public void CoreHelperTypes_DefaultValues_AreStable()
    {
        CodexGranularApprovalRules granularRules = new();
        Assert.False(granularRules.McpElicitations);
        Assert.False(granularRules.RequestPermissions);
        Assert.False(granularRules.Rules);
        Assert.False(granularRules.SandboxApproval);
        Assert.False(granularRules.SkillApproval);

        CodexRestrictedReadOnlyAccess restrictedReadOnly = new();
        Assert.True(restrictedReadOnly.IncludePlatformDefaults);
        Assert.Empty(restrictedReadOnly.ReadableRoots);

        CodexReadOnlySandboxPolicy readOnlySandbox = new();
        Assert.IsType<CodexFullAccessReadOnlyAccess>(readOnlySandbox.Access);
        Assert.False(readOnlySandbox.NetworkAccess);

        CodexExternalSandboxPolicy externalSandbox = new();
        Assert.Equal(CodexNetworkAccess.Restricted, externalSandbox.NetworkAccess);

        CodexWorkspaceWriteSandboxPolicy workspaceWriteSandbox = new();
        Assert.IsType<CodexFullAccessReadOnlyAccess>(workspaceWriteSandbox.ReadOnlyAccess);
        Assert.False(workspaceWriteSandbox.ExcludeSlashTmp);
        Assert.False(workspaceWriteSandbox.ExcludeTmpdirEnvVar);
        Assert.False(workspaceWriteSandbox.NetworkAccess);
        Assert.Empty(workspaceWriteSandbox.WritableRoots);

        CodexThreadSpawn threadSpawn = new();
        Assert.Null(threadSpawn.AgentNickname);
        Assert.Null(threadSpawn.AgentRole);
        Assert.Equal(0, threadSpawn.Depth);
        Assert.Equal(string.Empty, threadSpawn.ParentThreadId);

        CodexThreadSummary summary = new();
        Assert.Equal(string.Empty, summary.Id);
        Assert.Null(summary.Name);
        Assert.Equal(string.Empty, summary.Preview);
        Assert.IsType<CodexNotLoadedThreadStatus>(summary.Status);
        Assert.Equal(string.Empty, summary.ModelProvider);
        Assert.Equal(default(DateTimeOffset), summary.CreatedAt);
        Assert.Equal(default(DateTimeOffset), summary.UpdatedAt);
        Assert.False(summary.Ephemeral);
        Assert.Equal(string.Empty, summary.CliVersion);
        Assert.Null(summary.Path);
        Assert.IsType<CodexSessionSourceValue>(summary.Source);
        Assert.Equal(CodexSessionSourceKind.Unknown, ((CodexSessionSourceValue)summary.Source).Value);
        Assert.Null(summary.AgentRole);
        Assert.Null(summary.AgentNickname);
        Assert.Null(summary.GitInfo);

        CodexThreadSnapshot snapshot = new();
        Assert.Empty(snapshot.Turns);

        CodexTokenUsageBreakdown breakdown = new();
        Assert.Equal(0, breakdown.CachedInputTokens);
        Assert.Equal(0, breakdown.InputTokens);
        Assert.Equal(0, breakdown.OutputTokens);
        Assert.Equal(0, breakdown.ReasoningOutputTokens);
        Assert.Equal(0, breakdown.TotalTokens);

        CodexUsage usage = new();
        Assert.Equal(0, usage.Last.TotalTokens);
        Assert.Null(usage.ModelContextWindow);
        Assert.Equal(0, usage.Total.TotalTokens);

        CodexRunResult runResult = new();
        Assert.Empty(runResult.Items);
        Assert.Null(runResult.FinalResponse);
        Assert.Null(runResult.Usage);

        CodexTurnRecord turn = new();
        Assert.Equal(string.Empty, turn.Id);
        Assert.Equal(CodexTurnStatus.Completed, turn.Status);
        Assert.Empty(turn.Items);
        Assert.Null(turn.Error);
        Assert.Null(turn.Usage);

        CodexThreadListResult threadList = new();
        Assert.Empty(threadList.Threads);
        Assert.Null(threadList.NextCursor);

        CodexModelAvailabilityNux nux = new();
        Assert.Equal(string.Empty, nux.Message);

        CodexModelUpgradeInfo upgradeInfo = new();
        Assert.Null(upgradeInfo.MigrationMarkdown);
        Assert.Equal(string.Empty, upgradeInfo.Model);
        Assert.Null(upgradeInfo.ModelLink);
        Assert.Null(upgradeInfo.UpgradeCopy);

        CodexModel model = new();
        Assert.Null(model.AvailabilityNux);
        Assert.Equal(CodexReasoningEffort.None, model.DefaultReasoningEffort);
        Assert.Equal(string.Empty, model.Description);
        Assert.Equal(string.Empty, model.DisplayName);
        Assert.False(model.Hidden);
        Assert.Equal(string.Empty, model.Id);
        Assert.Null(model.InputModalities);
        Assert.False(model.IsDefault);
        Assert.Equal(string.Empty, model.Model);
        Assert.Empty(model.SupportedReasoningEfforts);
        Assert.Null(model.SupportsPersonality);
        Assert.Null(model.Upgrade);
        Assert.Null(model.UpgradeInfo);

        CodexModelListResult modelList = new();
        Assert.Empty(modelList.Models);
        Assert.Null(modelList.NextCursor);

        CodexServerInfo serverInfo = new();
        Assert.Null(serverInfo.Name);
        Assert.Null(serverInfo.Version);

        CodexRuntimeMetadata metadata = new();
        Assert.Null(metadata.ServerInfo);
        Assert.Null(metadata.PlatformFamily);
        Assert.Null(metadata.PlatformOs);
        Assert.Null(metadata.UserAgent);

        CodexRuntimeCapabilities capabilities = new();
        Assert.Equal(CodexBackendSelection.Exec, capabilities.BackendSelection);
        Assert.False(capabilities.ExperimentalApi);
        Assert.False(capabilities.SupportsArchiveThread);
        Assert.False(capabilities.SupportsCompactThread);
        Assert.False(capabilities.SupportsForkThread);
        Assert.False(capabilities.SupportsListModels);
        Assert.False(capabilities.SupportsListThreads);
        Assert.False(capabilities.SupportsReadThread);
        Assert.False(capabilities.SupportsResumeThread);
        Assert.False(capabilities.SupportsSetThreadName);
        Assert.False(capabilities.SupportsStartThread);
        Assert.False(capabilities.SupportsThreadStreaming);
        Assert.False(capabilities.SupportsTurnInterruption);
        Assert.False(capabilities.SupportsTurnSteering);
        Assert.False(capabilities.SupportsUnarchiveThread);
        Assert.Empty(capabilities.OptOutNotificationMethods);

        CodexThreadError threadError = new();
        Assert.Equal(string.Empty, threadError.Message);

        CodexTurnError turnError = new();
        Assert.Equal(string.Empty, turnError.Message);
        Assert.Null(turnError.AdditionalDetails);
        Assert.Null(turnError.CodexErrorInfo);

        CodexGitInfo gitInfo = new();
        Assert.Null(gitInfo.Branch);
        Assert.Null(gitInfo.OriginUrl);
        Assert.Null(gitInfo.Sha);

        CodexReasoningEffortOption effortOption = new();
        Assert.Equal(string.Empty, effortOption.Description);
        Assert.Equal(CodexReasoningEffort.None, effortOption.ReasoningEffort);

        CodexFileUpdateChange change = new();
        Assert.Equal(string.Empty, change.Diff);
        Assert.Equal(CodexPatchChangeKind.Add, change.Kind);
        Assert.Equal(string.Empty, change.Path);

        CodexTodoItem todo = new();
        Assert.False(todo.Completed);
        Assert.Equal(string.Empty, todo.Text);

        CodexCollabAgentState collabState = new();
        Assert.Equal(CodexCollabAgentStatus.PendingInit, collabState.Status);
        Assert.Null(collabState.Message);

        CodexSessionSourceValue sessionSource = new(CodexSessionSourceKind.Unknown);
        Assert.Equal(CodexSessionSourceKind.Unknown, sessionSource.Value);

        CodexSubAgentSessionSource subAgentSource = new(new CodexOtherSubAgentSource("other"));
        Assert.Equal("other", ((CodexOtherSubAgentSource)subAgentSource.SubAgent).Other);

        CodexOtherSubAgentSource otherSubAgentSource = new("other");
        Assert.Equal("other", otherSubAgentSource.Other);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0316")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0317")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0319")]
    public void ConversationHelperTypes_CarryRepresentativeValues()
    {
        CodexTextInput text = new()
        {
            Text = "hello",
        };

        CodexImageInput image = new()
        {
            Url = "https://example.com/image.png",
        };

        CodexLocalImageInput localImage = new()
        {
            Path = "/images/trace.png",
        };

        CodexSkillInput skill = new()
        {
            Name = "skill",
            Path = "/skills/skill.md",
        };

        CodexMentionInput mention = new()
        {
            Name = "mention",
            Path = "/mentions/mention.md",
        };

        Assert.Equal("text", text.Type);
        Assert.Equal("image", image.Type);
        Assert.Equal("localImage", localImage.Type);
        Assert.Equal("skill", skill.Type);
        Assert.Equal("mention", mention.Type);

        CodexReadCommandAction read = new()
        {
            Command = "cat",
            Name = "Read",
            Path = "/work/file.txt",
        };

        CodexListFilesCommandAction listFiles = new()
        {
            Command = "ls",
            Path = "/work",
        };

        CodexSearchCommandAction search = new()
        {
            Command = "grep",
            Path = "/work",
            Query = "needle",
        };

        CodexUnknownCommandAction unknownCommand = new()
        {
            Command = "custom",
        };

        Assert.Equal("read", read.Type);
        Assert.Equal("listFiles", listFiles.Type);
        Assert.Equal("search", search.Type);
        Assert.Equal("unknown", unknownCommand.Type);

        CodexSearchWebSearchAction webSearchAction = new()
        {
            Queries = ["one", "two"],
            Query = "one",
        };

        CodexOpenPageWebSearchAction openPageAction = new()
        {
            Url = "https://example.com",
        };

        CodexFindInPageWebSearchAction findInPageAction = new()
        {
            Pattern = "needle",
            Url = "https://example.com",
        };

        CodexOtherWebSearchAction otherWebSearchAction = new();

        Assert.Equal("search", webSearchAction.Type);
        Assert.Equal("openPage", openPageAction.Type);
        Assert.Equal("findInPage", findInPageAction.Type);
        Assert.Equal("other", otherWebSearchAction.Type);

        CodexInputTextDynamicToolCallOutputContentItem inputText = new()
        {
            Text = "hello",
        };

        CodexInputImageDynamicToolCallOutputContentItem inputImage = new()
        {
            ImageUrl = "https://example.com/image.png",
        };

        Assert.Equal("inputText", inputText.Type);
        Assert.Equal("inputImage", inputImage.Type);

        CodexMcpToolCallError mcpError = new()
        {
            Message = "boom",
        };

        CodexMcpToolCallResult mcpResult = new()
        {
            Content = [JsonValue.Create("content")!],
            StructuredContent = new JsonObject
            {
                ["kind"] = "trace",
            },
        };

        Assert.Equal("boom", mcpError.Message);
        Assert.Single(mcpResult.Content);
        Assert.Equal("trace", mcpResult.StructuredContent!["kind"]!.GetValue<string>());

        CodexUserMessageItem userMessage = new()
        {
            Id = "user-1",
            Content = [text, image, localImage, skill, mention],
        };

        CodexAgentMessageItem agentMessage = new()
        {
            Id = "agent-1",
            Phase = CodexMessagePhase.FinalAnswer,
            Text = "done",
        };

        CodexPlanItem plan = new()
        {
            Id = "plan-1",
            Text = "plan text",
        };

        CodexReasoningItem reasoning = new()
        {
            Id = "reasoning-1",
            Content = ["step one", "step two"],
            Summary = ["summary line"],
        };

        CodexCommandExecutionItem commandExecution = new()
        {
            Id = "command-1",
            AggregatedOutput = "output",
            Command = "dotnet test",
            CommandActions = [read, search],
            Cwd = "/work",
            DurationMs = 12,
            ExitCode = 0,
            ProcessId = "12345",
            Status = CodexCommandExecutionStatus.Completed,
        };

        CodexFileChangeItem fileChange = new()
        {
            Id = "file-1",
            Changes =
            [
                new CodexFileUpdateChange
                {
                    Diff = "+hello",
                    Kind = CodexPatchChangeKind.Add,
                    Path = "/work/file.txt",
                },
            ],
            Status = CodexPatchApplyStatus.Completed,
        };

        CodexMcpToolCallItem mcpToolCall = new()
        {
            Id = "mcp-1",
            Arguments = new JsonObject
            {
                ["kind"] = "trace",
            },
            DurationMs = 5,
            Error = mcpError,
            Server = "server",
            Result = mcpResult,
            Status = CodexMcpToolCallStatus.Completed,
            Tool = "tool",
        };

        CodexDynamicToolCallItem dynamicToolCall = new()
        {
            Id = "dynamic-1",
            Arguments = new JsonObject
            {
                ["kind"] = "dynamic",
            },
            ContentItems =
            [
                inputText,
                inputImage,
            ],
            DurationMs = 3,
            Status = CodexDynamicToolCallStatus.InProgress,
            Success = true,
            Tool = "dynamic-tool",
        };

        CodexCollabAgentToolCallItem collabToolCall = new()
        {
            Id = "collab-1",
            AgentsStates = new Dictionary<string, CodexCollabAgentState>(StringComparer.Ordinal)
            {
                ["agent-a"] = new CodexCollabAgentState
                {
                    Message = "running",
                    Status = CodexCollabAgentStatus.Running,
                },
            },
            Model = "gpt-5",
            Prompt = "help",
            ReasoningEffort = CodexReasoningEffort.High,
            ReceiverThreadIds = ["thread-a", "thread-b"],
            SenderThreadId = "thread-sender",
            Status = CodexCollabAgentToolCallStatus.Completed,
            Tool = CodexCollabAgentTool.SpawnAgent,
        };

        CodexWebSearchItem webSearch = new()
        {
            Id = "web-1",
            Action = webSearchAction,
            Query = "needle",
        };

        CodexImageViewItem imageView = new()
        {
            Id = "image-1",
            Path = "/images/trace.png",
        };

        CodexImageGenerationItem imageGeneration = new()
        {
            Id = "imagegen-1",
            Result = "image.png",
            RevisedPrompt = "more contrast",
            Status = "completed",
        };

        CodexEnteredReviewModeItem enteredReview = new()
        {
            Id = "review-1",
            Review = "enter review",
        };

        CodexExitedReviewModeItem exitedReview = new()
        {
            Id = "review-2",
            Review = "exit review",
        };

        CodexContextCompactionItem compaction = new()
        {
            Id = "compact-1",
        };

        CodexTodoListItem todoList = new()
        {
            Id = "todo-1",
            Items =
            [
                new CodexTodoItem
                {
                    Completed = false,
                    Text = "write tests",
                },
            ],
        };

        CodexErrorItem errorItem = new()
        {
            Id = "error-1",
            Message = "boom",
        };

        CodexUnknownThreadItem unknownItem = new("custom/runtime-event")
        {
            Id = "unknown-1",
            RawPayload = new JsonObject
            {
                ["note"] = "mystery",
            },
        };

        Assert.Equal("userMessage", userMessage.Type);
        Assert.Equal("agentMessage", agentMessage.Type);
        Assert.Equal("plan", plan.Type);
        Assert.Equal("reasoning", reasoning.Type);
        Assert.Equal("commandExecution", commandExecution.Type);
        Assert.Equal("fileChange", fileChange.Type);
        Assert.Equal("mcpToolCall", mcpToolCall.Type);
        Assert.Equal("dynamicToolCall", dynamicToolCall.Type);
        Assert.Equal("collabAgentToolCall", collabToolCall.Type);
        Assert.Equal("webSearch", webSearch.Type);
        Assert.Equal("imageView", imageView.Type);
        Assert.Equal("imageGeneration", imageGeneration.Type);
        Assert.Equal("enteredReviewMode", enteredReview.Type);
        Assert.Equal("exitedReviewMode", exitedReview.Type);
        Assert.Equal("contextCompaction", compaction.Type);
        Assert.Equal("todoList", todoList.Type);
        Assert.Equal("error", errorItem.Type);
        Assert.Equal("custom/runtime-event", unknownItem.UnknownType);
        Assert.Equal("mystery", unknownItem.RawPayload!["note"]!.GetValue<string>());

        Assert.Equal(userMessage, userMessage with { });
        Assert.Equal(agentMessage, agentMessage with { });
        Assert.Equal(plan, plan with { });
        Assert.Equal(reasoning, reasoning with { });
        Assert.Equal(commandExecution, commandExecution with { });
        Assert.Equal(fileChange, fileChange with { });
        Assert.Equal(mcpToolCall, mcpToolCall with { });
        Assert.Equal(dynamicToolCall, dynamicToolCall with { });
        Assert.Equal(collabToolCall, collabToolCall with { });
        Assert.Equal(webSearch, webSearch with { });
        Assert.Equal(imageView, imageView with { });
        Assert.Equal(imageGeneration, imageGeneration with { });
        Assert.Equal(enteredReview, enteredReview with { });
        Assert.Equal(exitedReview, exitedReview with { });
        Assert.Equal(todoList, todoList with { });
        Assert.Equal(errorItem, errorItem with { });
        Assert.Equal(unknownItem, unknownItem with { });

        CodexThreadStartedEvent threadStarted = new()
        {
            Thread = CreateThreadSummary("thread-1", "Trace thread"),
        };

        CodexTurnRecord turn = CreateTurnRecord();

        CodexTurnStartedEvent turnStarted = new()
        {
            Turn = turn,
        };

        CodexTurnCompletedEvent turnCompleted = new()
        {
            Turn = turn,
        };

        CodexTurnFailedEvent turnFailed = new()
        {
            Turn = turn with
            {
                Status = CodexTurnStatus.Failed,
            },
        };

        CodexItemStartedEvent itemStarted = new()
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            Item = agentMessage,
        };

        CodexItemUpdatedEvent itemUpdated = new()
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            Item = agentMessage,
        };

        CodexItemCompletedEvent itemCompleted = new()
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            Item = agentMessage,
        };

        CodexThreadErrorEvent threadError = new()
        {
            ThreadId = "thread-1",
            TurnId = "turn-1",
            WillRetry = true,
            Error = new CodexTurnError
            {
                Message = "boom",
            },
        };

        CodexUnknownThreadEvent unknownEvent = new("custom/runtime-event")
        {
            RawPayload = new JsonObject
            {
                ["note"] = "mystery",
            },
        };

        Assert.Equal("thread.started", threadStarted.Type);
        Assert.Equal("turn.started", turnStarted.Type);
        Assert.Equal("turn.completed", turnCompleted.Type);
        Assert.Equal("turn.failed", turnFailed.Type);
        Assert.Equal("item.started", itemStarted.Type);
        Assert.Equal("item.updated", itemUpdated.Type);
        Assert.Equal("item.completed", itemCompleted.Type);
        Assert.Equal("error", threadError.Type);
        Assert.Equal("custom/runtime-event", unknownEvent.UnknownType);
        Assert.Equal("mystery", unknownEvent.RawPayload!["note"]!.GetValue<string>());

        Assert.Equal(threadStarted, threadStarted with { });
        Assert.Equal(turnStarted, turnStarted with { });
        Assert.Equal(turnCompleted, turnCompleted with { });
        Assert.Equal(turnFailed, turnFailed with { });
        Assert.Equal(itemStarted, itemStarted with { });
        Assert.Equal(itemUpdated, itemUpdated with { });
        Assert.Equal(itemCompleted, itemCompleted with { });
        Assert.Equal(threadError, threadError with { });
        Assert.Equal(unknownEvent, unknownEvent with { });
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0316")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0317")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0319")]
    public void ConversationHelperTypes_DefaultValues_AreStable()
    {
        CodexTextInput text = new();
        Assert.Equal("text", text.Type);
        Assert.Equal(string.Empty, text.Text);

        CodexImageInput image = new();
        Assert.Equal("image", image.Type);
        Assert.Equal(string.Empty, image.Url);

        CodexLocalImageInput localImage = new();
        Assert.Equal("localImage", localImage.Type);
        Assert.Equal(string.Empty, localImage.Path);

        CodexSkillInput skill = new();
        Assert.Equal("skill", skill.Type);
        Assert.Equal(string.Empty, skill.Name);
        Assert.Equal(string.Empty, skill.Path);

        CodexMentionInput mention = new();
        Assert.Equal("mention", mention.Type);
        Assert.Equal(string.Empty, mention.Name);
        Assert.Equal(string.Empty, mention.Path);

        CodexReadCommandAction read = new();
        Assert.Equal("read", read.Type);
        Assert.Equal(string.Empty, read.Command);
        Assert.Equal(string.Empty, read.Name);
        Assert.Equal(string.Empty, read.Path);

        CodexListFilesCommandAction listFiles = new();
        Assert.Equal("listFiles", listFiles.Type);
        Assert.Equal(string.Empty, listFiles.Command);
        Assert.Null(listFiles.Path);

        CodexSearchCommandAction search = new();
        Assert.Equal("search", search.Type);
        Assert.Equal(string.Empty, search.Command);
        Assert.Null(search.Path);
        Assert.Null(search.Query);

        CodexUnknownCommandAction unknownCommand = new();
        Assert.Equal("unknown", unknownCommand.Type);
        Assert.Equal(string.Empty, unknownCommand.Command);

        CodexSearchWebSearchAction webSearchAction = new();
        Assert.Equal("search", webSearchAction.Type);
        Assert.Null(webSearchAction.Queries);
        Assert.Null(webSearchAction.Query);

        CodexOpenPageWebSearchAction openPageAction = new();
        Assert.Equal("openPage", openPageAction.Type);
        Assert.Null(openPageAction.Url);

        CodexFindInPageWebSearchAction findInPageAction = new();
        Assert.Equal("findInPage", findInPageAction.Type);
        Assert.Null(findInPageAction.Pattern);
        Assert.Null(findInPageAction.Url);

        CodexOtherWebSearchAction otherWebSearchAction = new();
        Assert.Equal("other", otherWebSearchAction.Type);

        CodexInputTextDynamicToolCallOutputContentItem inputText = new();
        Assert.Equal("inputText", inputText.Type);
        Assert.Equal(string.Empty, inputText.Text);

        CodexInputImageDynamicToolCallOutputContentItem inputImage = new();
        Assert.Equal("inputImage", inputImage.Type);
        Assert.Equal(string.Empty, inputImage.ImageUrl);

        CodexThreadStartedEvent threadStarted = new();
        Assert.Equal("thread.started", threadStarted.Type);
        Assert.Equal(string.Empty, threadStarted.Thread.Id);
        Assert.Equal(string.Empty, threadStarted.Thread.Preview);

        CodexTurnStartedEvent turnStarted = new();
        Assert.Equal("turn.started", turnStarted.Type);
        Assert.Equal(string.Empty, turnStarted.Turn.Id);
        Assert.Empty(turnStarted.Turn.Items);

        CodexTurnCompletedEvent turnCompleted = new();
        Assert.Equal("turn.completed", turnCompleted.Type);
        Assert.Equal(string.Empty, turnCompleted.Turn.Id);

        CodexTurnFailedEvent turnFailed = new();
        Assert.Equal("turn.failed", turnFailed.Type);
        Assert.Equal(string.Empty, turnFailed.Turn.Id);

        CodexItemStartedEvent itemStarted = new();
        Assert.Equal("item.started", itemStarted.Type);
        Assert.Equal(string.Empty, itemStarted.ThreadId);
        Assert.Equal(string.Empty, itemStarted.TurnId);
        Assert.IsType<CodexUnknownThreadItem>(itemStarted.Item);
        Assert.Equal("unknown", itemStarted.Item.Type);

        CodexItemUpdatedEvent itemUpdated = new();
        Assert.Equal("item.updated", itemUpdated.Type);
        Assert.Equal(string.Empty, itemUpdated.ThreadId);
        Assert.Equal(string.Empty, itemUpdated.TurnId);
        Assert.IsType<CodexUnknownThreadItem>(itemUpdated.Item);
        Assert.Equal("unknown", itemUpdated.Item.Type);

        CodexItemCompletedEvent itemCompleted = new();
        Assert.Equal("item.completed", itemCompleted.Type);
        Assert.Equal(string.Empty, itemCompleted.ThreadId);
        Assert.Equal(string.Empty, itemCompleted.TurnId);
        Assert.IsType<CodexUnknownThreadItem>(itemCompleted.Item);
        Assert.Equal("unknown", itemCompleted.Item.Type);

        CodexThreadErrorEvent threadError = new();
        Assert.Equal("error", threadError.Type);
        Assert.Equal(string.Empty, threadError.ThreadId);
        Assert.Null(threadError.TurnId);
        Assert.False(threadError.WillRetry);
        Assert.Equal(string.Empty, threadError.Error.Message);

        CodexUserMessageItem userMessage = new();
        Assert.Equal("userMessage", userMessage.Type);
        Assert.Empty(userMessage.Content);

        CodexAgentMessageItem agentMessage = new();
        Assert.Equal("agentMessage", agentMessage.Type);
        Assert.Null(agentMessage.Phase);
        Assert.Equal(string.Empty, agentMessage.Text);

        CodexPlanItem plan = new();
        Assert.Equal("plan", plan.Type);
        Assert.Equal(string.Empty, plan.Text);

        CodexReasoningItem reasoning = new();
        Assert.Equal("reasoning", reasoning.Type);
        Assert.Empty(reasoning.Content!);
        Assert.Empty(reasoning.Summary!);

        CodexCommandExecutionItem commandExecution = new();
        Assert.Equal("commandExecution", commandExecution.Type);
        Assert.Equal(string.Empty, commandExecution.AggregatedOutput);
        Assert.Equal(string.Empty, commandExecution.Command);
        Assert.Empty(commandExecution.CommandActions);
        Assert.Equal(string.Empty, commandExecution.Cwd);
        Assert.Null(commandExecution.DurationMs);
        Assert.Null(commandExecution.ExitCode);
        Assert.Null(commandExecution.ProcessId);
        Assert.Equal(CodexCommandExecutionStatus.InProgress, commandExecution.Status);

        CodexFileChangeItem fileChange = new();
        Assert.Equal("fileChange", fileChange.Type);
        Assert.Empty(fileChange.Changes);
        Assert.Equal(CodexPatchApplyStatus.InProgress, fileChange.Status);

        CodexMcpToolCallItem mcpToolCall = new();
        Assert.Equal("mcpToolCall", mcpToolCall.Type);
        Assert.Null(mcpToolCall.Arguments);
        Assert.Null(mcpToolCall.DurationMs);
        Assert.Null(mcpToolCall.Error);
        Assert.Equal(string.Empty, mcpToolCall.Server);
        Assert.Null(mcpToolCall.Result);
        Assert.Equal(CodexMcpToolCallStatus.InProgress, mcpToolCall.Status);
        Assert.Equal(string.Empty, mcpToolCall.Tool);

        CodexDynamicToolCallItem dynamicToolCall = new();
        Assert.Equal("dynamicToolCall", dynamicToolCall.Type);
        Assert.Null(dynamicToolCall.Arguments);
        Assert.Null(dynamicToolCall.ContentItems);
        Assert.Null(dynamicToolCall.DurationMs);
        Assert.Equal(CodexDynamicToolCallStatus.InProgress, dynamicToolCall.Status);
        Assert.Null(dynamicToolCall.Success);
        Assert.Equal(string.Empty, dynamicToolCall.Tool);

        CodexCollabAgentToolCallItem collabToolCall = new();
        Assert.Equal("collabAgentToolCall", collabToolCall.Type);
        Assert.Empty(collabToolCall.AgentsStates);
        Assert.Null(collabToolCall.Model);
        Assert.Null(collabToolCall.Prompt);
        Assert.Null(collabToolCall.ReasoningEffort);
        Assert.Empty(collabToolCall.ReceiverThreadIds);
        Assert.Equal(string.Empty, collabToolCall.SenderThreadId);
        Assert.Equal(CodexCollabAgentToolCallStatus.InProgress, collabToolCall.Status);
        Assert.Equal(CodexCollabAgentTool.SpawnAgent, collabToolCall.Tool);

        CodexWebSearchItem webSearch = new();
        Assert.Equal("webSearch", webSearch.Type);
        Assert.Null(webSearch.Action);
        Assert.Equal(string.Empty, webSearch.Query);

        CodexImageViewItem imageView = new();
        Assert.Equal("imageView", imageView.Type);
        Assert.Equal(string.Empty, imageView.Path);

        CodexImageGenerationItem imageGeneration = new();
        Assert.Equal("imageGeneration", imageGeneration.Type);
        Assert.Equal(string.Empty, imageGeneration.Result);
        Assert.Null(imageGeneration.RevisedPrompt);
        Assert.Equal(string.Empty, imageGeneration.Status);

        CodexEnteredReviewModeItem enteredReview = new();
        Assert.Equal("enteredReviewMode", enteredReview.Type);
        Assert.Equal(string.Empty, enteredReview.Review);

        CodexExitedReviewModeItem exitedReview = new();
        Assert.Equal("exitedReviewMode", exitedReview.Type);
        Assert.Equal(string.Empty, exitedReview.Review);

        CodexContextCompactionItem compaction = new();
        Assert.Equal("contextCompaction", compaction.Type);

        CodexTodoListItem todoList = new();
        Assert.Equal("todoList", todoList.Type);
        Assert.Empty(todoList.Items);

        CodexErrorItem errorItem = new();
        Assert.Equal("error", errorItem.Type);
        Assert.Equal(string.Empty, errorItem.Message);

        CodexUnknownThreadItem unknownItem = new("custom/runtime-event");
        Assert.Equal("custom/runtime-event", unknownItem.UnknownType);
        Assert.Null(unknownItem.RawPayload);
    }

    private static CodexThreadSummary CreateThreadSummary(string id, string? name = null)
        => new()
        {
            Id = id,
            Name = name,
            Preview = $"Preview for {id}",
            Status = new CodexIdleThreadStatus(),
            ModelProvider = "openai",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            Ephemeral = false,
            CliVersion = "1.2.3",
            Path = "/work",
            Source = new CodexSessionSourceValue(CodexSessionSourceKind.Exec),
            AgentRole = "coding-agent",
            AgentNickname = "trace",
            GitInfo = new CodexGitInfo
            {
                Branch = "main",
                OriginUrl = "https://example.com/repo.git",
                Sha = "abc123",
            },
        };

    private static CodexTurnRecord CreateTurnRecord(string id = "turn-1", CodexTurnStatus status = CodexTurnStatus.Completed)
        => new()
        {
            Id = id,
            Status = status,
            Items =
            [
                new CodexAgentMessageItem
                {
                    Id = "message-1",
                    Phase = CodexMessagePhase.FinalAnswer,
                    Text = "done",
                },
            ],
            Error = new CodexTurnError
            {
                Message = "boom",
                AdditionalDetails = "tail",
                CodexErrorInfo = new JsonObject
                {
                    ["kind"] = "trace",
                },
            },
            Usage = new CodexUsage
            {
                Last = new CodexTokenUsageBreakdown
                {
                    CachedInputTokens = 1,
                    InputTokens = 2,
                    OutputTokens = 3,
                    ReasoningOutputTokens = 4,
                    TotalTokens = 10,
                },
                ModelContextWindow = 4096,
                Total = new CodexTokenUsageBreakdown
                {
                    CachedInputTokens = 2,
                    InputTokens = 4,
                    OutputTokens = 6,
                    ReasoningOutputTokens = 8,
                    TotalTokens = 20,
                },
            },
        };

    private static CodexThreadSnapshot CreateThreadSnapshot(CodexThreadSummary summary, CodexTurnRecord turn)
        => new()
        {
            Id = summary.Id,
            Name = summary.Name,
            Preview = summary.Preview,
            Status = summary.Status,
            ModelProvider = summary.ModelProvider,
            CreatedAt = summary.CreatedAt,
            UpdatedAt = summary.UpdatedAt,
            Ephemeral = summary.Ephemeral,
            CliVersion = summary.CliVersion,
            Path = summary.Path,
            Source = summary.Source,
            AgentRole = summary.AgentRole,
            AgentNickname = summary.AgentNickname,
            GitInfo = summary.GitInfo,
            Turns = [turn],
        };

    private static CodexModel CreateModel(string id = "model-1", string model = "gpt-5")
        => new()
        {
            AvailabilityNux = new CodexModelAvailabilityNux
            {
                Message = "upgrade available",
            },
            DefaultReasoningEffort = CodexReasoningEffort.High,
            Description = "Trace model",
            DisplayName = model.ToUpperInvariant(),
            Hidden = true,
            Id = id,
            InputModalities = [CodexInputModality.Text, CodexInputModality.Image],
            IsDefault = true,
            Model = model,
            SupportedReasoningEfforts =
            [
                new CodexReasoningEffortOption
                {
                    Description = "balanced",
                    ReasoningEffort = CodexReasoningEffort.Medium,
                },
                new CodexReasoningEffortOption
                {
                    Description = "deep",
                    ReasoningEffort = CodexReasoningEffort.High,
                },
            ],
            SupportsPersonality = true,
            Upgrade = "upgrade",
            UpgradeInfo = new CodexModelUpgradeInfo
            {
                MigrationMarkdown = "docs",
                Model = model,
                ModelLink = "https://example.com/model",
                UpgradeCopy = "Upgrade now",
            },
        };
}
