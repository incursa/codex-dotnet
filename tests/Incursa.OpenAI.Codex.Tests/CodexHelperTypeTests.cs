using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexHelperTypeTests
{
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
        [typeof(CodexPlanType)],
        [typeof(CodexPersonality)],
        [typeof(CodexAuthMode)],
        [typeof(CodexProcessOutputStream)],
        [typeof(CodexHookEventName)],
        [typeof(CodexHookExecutionMode)],
        [typeof(CodexHookHandlerType)],
        [typeof(CodexHookOutputEntryKind)],
        [typeof(CodexHookRunStatus)],
        [typeof(CodexHookScope)],
        [typeof(CodexHookSourceKind)],
        [typeof(CodexRealtimeConversationVersion)],
        [typeof(CodexFuzzyFileSearchMatchType)],
        [typeof(CodexMcpServerStartupState)],
        [typeof(CodexRemoteControlConnectionStatus)],
        [typeof(CodexWindowsSandboxSetupMode)],
        [typeof(CodexModelRerouteReason)],
        [typeof(CodexModelVerificationValue)],
        [typeof(CodexReasoningEffort)],
        [typeof(CodexReasoningSummary)],
        [typeof(CodexSandboxMode)],
        [typeof(CodexServiceTier)],
        [typeof(CodexSessionSourceKind)],
        [typeof(CodexSubAgentSourceKind)],
        [typeof(CodexThreadActiveFlag)],
        [typeof(CodexThreadSortDirection)],
        [typeof(CodexThreadSortKey)],
        [typeof(CodexThreadSource)],
        [typeof(CodexThreadSourceKind)],
        [typeof(CodexThreadStartSource)],
        [typeof(CodexThreadUnsubscribeStatus)],
        [typeof(CodexFinalResponseSource)],
        [typeof(CodexTurnEventImportance)],
        [typeof(CodexTurnEventKind)],
        [typeof(CodexTurnPlanStepStatus)],
        [typeof(CodexTurnTerminalState)],
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

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0314")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0315")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0319")]
    public void HelperRecords_CarryRepresentativeValues()
    {
        CodexApprovalModePolicy approval = new(CodexApprovalMode.OnRequest);
        Assert.Equal(CodexApprovalMode.OnRequest, approval.Mode);

        CodexGranularApprovalPolicy granular = new(new CodexGranularApprovalRules
        {
            McpElicitations = true,
            RequestPermissions = true,
            Rules = false,
            SandboxApproval = true,
            SkillApproval = false,
        });
        Assert.True(granular.Granular.SandboxApproval);

        CodexReadOnlySandboxPolicy readOnly = new()
        {
            Access = new CodexRestrictedReadOnlyAccess
            {
                IncludePlatformDefaults = false,
                ReadableRoots = ["/repo"],
            },
            NetworkAccess = true,
        };
        Assert.True(readOnly.NetworkAccess);
        Assert.Single(((CodexRestrictedReadOnlyAccess)readOnly.Access).ReadableRoots);

        CodexWorkspaceWriteSandboxPolicy workspace = new()
        {
            ExcludeSlashTmp = true,
            ExcludeTmpdirEnvVar = true,
            NetworkAccess = false,
            WritableRoots = ["/work"],
        };
        Assert.True(workspace.ExcludeSlashTmp);
        Assert.Single(workspace.WritableRoots);

        CodexThreadSummary summary = new()
        {
            Id = "thread-1",
            Name = "Trace thread",
            Preview = "Preview",
            Status = new CodexIdleThreadStatus(),
            ModelProvider = "openai",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch.AddHours(1),
            Ephemeral = true,
            CliVersion = "1.2.3",
            Cwd = "/work",
            Path = "/work",
            SessionId = "session-1",
            ForkedFromId = "thread-parent",
            Source = new CodexSubAgentSessionSource(new CodexSubAgentSourceValue(CodexSubAgentSourceKind.Review)),
            ThreadSource = CodexThreadSource.Subagent,
            SessionStartSource = CodexThreadStartSource.Startup,
            AgentRole = "coding-agent",
            AgentNickname = "trace",
            GitInfo = new CodexGitInfo
            {
                Branch = "main",
                OriginUrl = "https://example.com/repo.git",
                Sha = "abc123",
            },
        };
        Assert.Equal("thread-1", summary.Id);
        Assert.Equal("main", summary.GitInfo!.Branch);

        CodexThreadSnapshot snapshot = new()
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
            Cwd = summary.Cwd,
            Path = summary.Path,
            SessionId = summary.SessionId,
            ForkedFromId = summary.ForkedFromId,
            Source = summary.Source,
            ThreadSource = summary.ThreadSource,
            SessionStartSource = summary.SessionStartSource,
            AgentRole = summary.AgentRole,
            AgentNickname = summary.AgentNickname,
            GitInfo = summary.GitInfo,
            Turns =
            [
                new CodexTurnRecord
                {
                    Id = "turn-1",
                    Status = CodexTurnStatus.Completed,
                },
            ],
        };
        Assert.Single(snapshot.Turns);

        CodexRunResult runResult = new()
        {
            Items =
            [
                new CodexAgentMessageItem
                {
                    Id = "message-1",
                    Phase = CodexMessagePhase.FinalAnswer,
                    Text = "done",
                },
            ],
            FinalResponse = "done",
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
        };
        Assert.Equal("done", runResult.FinalResponse);
        Assert.Equal(1, runResult.Usage!.Total.TotalTokens);

        CodexTurnEvent normalizedEvent = new()
        {
            SequenceNumber = 1,
            ProjectId = "project-1",
            WorkingDirectory = "/work",
            ThreadId = "thread-1",
            TurnId = "turn-1",
            RawEventType = "turn.completed",
            Kind = CodexTurnEventKind.Terminal,
            Importance = CodexTurnEventImportance.High,
            Timestamp = DateTimeOffset.UnixEpoch.AddMinutes(1),
            Title = "Turn completed",
            Text = "done",
            Metadata = new Dictionary<string, string?>
            {
                ["status"] = "Completed",
            },
            IsTerminal = true,
            TerminalState = CodexTurnTerminalState.Completed,
            ContributesToFinalOutput = false,
            IsUserVisibleByDefault = true,
        };
        Assert.Equal(CodexTurnTerminalState.Completed, normalizedEvent.TerminalState);

        CodexTurnResult turnResult = new()
        {
            ProjectId = "project-1",
            WorkingDirectory = "/work",
            ThreadId = "thread-1",
            TurnId = "turn-1",
            TerminalState = CodexTurnTerminalState.Completed,
            TurnStatus = CodexTurnStatus.Completed,
            TerminalEventSeen = true,
            TerminalEventType = "turn.completed",
            FinalResponseText = "done",
            FinalResponseSource = CodexFinalResponseSource.TerminalEvent,
            FinalResponseComplete = true,
            StartedUtc = DateTimeOffset.UnixEpoch,
            CompletedUtc = DateTimeOffset.UnixEpoch.AddMinutes(1),
            RawEventCount = 2,
            NormalizedEventCount = 3,
            AssistantOutputCharCount = 4,
            FinalResponseCharCount = 4,
            Artifacts =
            [
                new CodexTurnArtifactSummary
                {
                    Id = "image-1",
                    Type = "imageView",
                    Path = "/work/image.png",
                    Status = "ready",
                },
            ],
            Items = runResult.Items,
            Usage = runResult.Usage,
            DiagnosticsTraceId = "trace-1",
        };
        Assert.True(turnResult.TerminalEventSeen);
        Assert.Single(turnResult.Artifacts);

        CodexRuntimeMetadata metadata = new()
        {
            UserAgent = "codex-app-server/1.2.3",
            PlatformFamily = "Unix",
            PlatformOs = "Linux",
            ServerInfo = new CodexServerInfo
            {
                Name = "codex-app-server",
                Version = "1.2.3",
            },
        };
        Assert.Equal("codex-app-server", metadata.ServerInfo!.Name);

        CodexRuntimeCapabilities capabilities = new()
        {
            BackendSelection = CodexBackendSelection.AppServer,
            ExperimentalApi = true,
            SupportsAccountRateLimits = true,
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
        };
        Assert.True(capabilities.SupportsAccountRateLimits);
        Assert.True(capabilities.SupportsTurnSteering);

        CodexAccountRateLimitsResult rateLimits = new()
        {
            RateLimits =
            [
                new CodexRateLimitSnapshot
                {
                    LimitId = "codex",
                    PlanType = CodexPlanType.Plus,
                    Primary = new CodexRateLimitWindow
                    {
                        UsedPercent = 30,
                        WindowDurationMinutes = 300,
                    },
                },
            ],
        };
        Assert.Equal("codex", rateLimits.RateLimits[0].LimitId);

        CodexThreadMetadataGitInfoUpdate metadataUpdate = new()
        {
            BranchSpecified = true,
            Branch = "main",
            OriginUrlSpecified = true,
            OriginUrl = null,
            ShaSpecified = true,
            Sha = "abc123",
        };
        Assert.True(metadataUpdate.BranchSpecified);
        Assert.True(metadataUpdate.OriginUrlSpecified);
        Assert.True(metadataUpdate.ShaSpecified);

        CodexTextPosition position = new()
        {
            Line = 2,
            Column = 4,
        };
        CodexTextRange range = new()
        {
            Start = position,
            End = position with { Column = 12 },
        };
        Assert.Equal(2, range.Start.Line);

        CodexHookOutputEntry hookOutput = new()
        {
            Kind = CodexHookOutputEntryKind.Warning,
            Text = "watch this",
        };
        CodexHookRunSummary hookRun = new()
        {
            DisplayOrder = 1,
            Entries = [hookOutput],
            EventName = CodexHookEventName.SessionStart,
            ExecutionMode = CodexHookExecutionMode.Sync,
            HandlerType = CodexHookHandlerType.Command,
            Id = "run-1",
            Scope = CodexHookScope.Thread,
            Source = CodexHookSourceKind.Plugin,
            SourcePath = "/hooks/start",
            StartedAt = 1778076000L,
            Status = CodexHookRunStatus.Running,
        };
        Assert.Equal("run-1", hookRun.Id);

        CodexThreadRealtimeAudioChunk audioChunk = new()
        {
            Data = "aGVsbG8=",
            ItemId = "item-1",
            NumChannels = 1,
            SampleRate = 24000,
            SamplesPerChannel = 3,
        };
        Assert.Equal(24000, audioChunk.SampleRate);

        CodexFuzzyFileSearchResult fuzzyResult = new()
        {
            FileName = "trace.cs",
            Indices = [1, 5],
            MatchType = CodexFuzzyFileSearchMatchType.File,
            Path = "/work/src/trace.cs",
            Root = "/work",
            Score = 91,
        };
        Assert.Equal(5, fuzzyResult.Indices![1]);

        CodexModelServiceTier serviceTier = new()
        {
            Id = "priority",
            Name = "Fast",
            Description = "Fastest inference with increased plan usage",
        };
        Assert.Equal("priority", serviceTier.Id);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0316")]
    public void HelperActionTypes_CarryRepresentativeValues()
    {
        CodexReadCommandAction read = new()
        {
            Command = "cat",
            Name = "Read",
            Path = "/work/file.txt",
        };
        Assert.Equal("read", read.Type);

        CodexListFilesCommandAction listFiles = new()
        {
            Command = "ls",
            Path = "/work",
        };
        Assert.Equal("listFiles", listFiles.Type);

        CodexSearchCommandAction search = new()
        {
            Command = "grep",
            Path = "/work",
            Query = "needle",
        };
        Assert.Equal("search", search.Type);

        CodexUnknownCommandAction unknownCommand = new()
        {
            Command = "custom",
        };
        Assert.Equal("unknown", unknownCommand.Type);

        CodexSearchWebSearchAction webSearch = new()
        {
            Queries = ["one", "two"],
            Query = "one",
        };
        Assert.Equal("search", webSearch.Type);

        CodexOpenPageWebSearchAction openPage = new()
        {
            Url = "https://example.com",
        };
        Assert.Equal("openPage", openPage.Type);

        CodexFindInPageWebSearchAction findInPage = new()
        {
            Pattern = "needle",
            Url = "https://example.com",
        };
        Assert.Equal("findInPage", findInPage.Type);

        CodexOtherWebSearchAction other = new();
        Assert.Equal("other", other.Type);

        CodexInputTextDynamicToolCallOutputContentItem text = new()
        {
            Text = "hello",
        };
        CodexInputImageDynamicToolCallOutputContentItem image = new()
        {
            ImageUrl = "https://example.com/image.png",
        };
        Assert.Equal("inputText", text.Type);
        Assert.Equal("inputImage", image.Type);

        CodexMcpToolCallError error = new()
        {
            Message = "boom",
        };
        CodexMcpToolCallResult result = new()
        {
            Content = [JsonValue.Create("content")!],
            StructuredContent = new JsonObject
            {
                ["kind"] = "trace",
            },
        };
        Assert.Equal("boom", error.Message);
        Assert.Single(result.Content);
        Assert.Equal("trace", result.StructuredContent!["kind"]!.GetValue<string>());

        CodexTurnPlanStep planStep = new()
        {
            Step = "Patch SDK",
            Status = CodexTurnPlanStepStatus.Completed,
        };
        Assert.Equal("Patch SDK", planStep.Step);
        Assert.Equal(CodexTurnPlanStepStatus.Completed, planStep.Status);
    }
}
