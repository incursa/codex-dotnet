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
            Path = "/work",
            Source = new CodexSessionSourceValue(CodexSessionSourceKind.AppServer),
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
            Path = summary.Path,
            Source = summary.Source,
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
        Assert.True(capabilities.SupportsTurnSteering);
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
    }
}


