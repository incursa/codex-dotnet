using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexProtocolTests
{
    [Fact]
    public void CreateInitializeRequest_UsesDefaultClientIdentity()
    {
        JsonObject request = CodexProtocol.CreateInitializeRequest(new CodexClientOptions());

        JsonObject clientInfo = request["clientInfo"]!.AsObject();
        Assert.Equal("Incursa.OpenAI.Codex", clientInfo["name"]!.GetValue<string>());
        Assert.Equal("Incursa OpenAI Codex", clientInfo["title"]!.GetValue<string>());
        Assert.Equal(typeof(CodexClient).Assembly.GetName().Version?.ToString() ?? "0.0.0", clientInfo["version"]!.GetValue<string>());
        Assert.True(request["capabilities"]!["experimentalApi"]!.GetValue<bool>());
    }

    [Fact]
    public void CreateInitializeRequest_HonorsCustomClientIdentity()
    {
        JsonObject request = CodexProtocol.CreateInitializeRequest(new CodexClientOptions
        {
            ClientName = "TraceClient",
            ClientTitle = "Trace Title",
            ClientVersion = "9.9.9",
        });

        JsonObject clientInfo = request["clientInfo"]!.AsObject();
        Assert.Equal("TraceClient", clientInfo["name"]!.GetValue<string>());
        Assert.Equal("Trace Title", clientInfo["title"]!.GetValue<string>());
        Assert.Equal("9.9.9", clientInfo["version"]!.GetValue<string>());
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0244")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void BuildThreadStartParams_EmitsThreadOptionsAndPolicies()
    {
        JsonObject payload = CodexProtocol.BuildThreadStartParams(new CodexThreadOptions
        {
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnFailure),
            ApprovalsReviewer = CodexApprovalsReviewer.GuardianSubAgent,
            BaseInstructions = "Base instructions",
            DeveloperInstructions = "Developer instructions",
            Ephemeral = true,
            Model = "gpt-5",
            ModelProvider = "openai",
            Personality = CodexPersonality.Pragmatic,
            Sandbox = new CodexWorkspaceWriteSandboxPolicy
            {
                ExcludeSlashTmp = true,
                ExcludeTmpdirEnvVar = true,
                NetworkAccess = true,
            },
            ServiceTier = CodexServiceTier.Flex,
            WorkingDirectory = @"C:\work\codex",
            ServiceName = "trace-service",
            SessionStartSource = CodexThreadStartSource.Startup,
            ThreadSource = CodexThreadSource.Subagent,
            ModelReasoningEffort = CodexReasoningEffort.High,
            NetworkAccessEnabled = true,
            WebSearchMode = CodexWebSearchMode.Live,
            WebSearchEnabled = false,
            SkipGitRepoCheck = true,
            AdditionalDirectories = [@"C:\extra-one", @"C:\extra-two"],
        });

        Assert.Equal("Base instructions", payload["baseInstructions"]!.GetValue<string>());
        Assert.Equal("Developer instructions", payload["developerInstructions"]!.GetValue<string>());
        Assert.True(payload["ephemeral"]!.GetValue<bool>());
        Assert.Equal("gpt-5", payload["model"]!.GetValue<string>());
        Assert.Equal("openai", payload["modelProvider"]!.GetValue<string>());
        Assert.Equal("pragmatic", payload["personality"]!.GetValue<string>());
        Assert.Equal("workspaceWrite", payload["sandbox"]!["type"]!.GetValue<string>());
        Assert.True(payload["sandbox"]!["networkAccess"]!.GetValue<bool>());
        Assert.Equal("flex", payload["serviceTier"]!.GetValue<string>());
        Assert.Equal(@"C:\work\codex", payload["cwd"]!.GetValue<string>());
        Assert.Equal("trace-service", payload["serviceName"]!.GetValue<string>());
        Assert.Equal("startup", payload["sessionStartSource"]!.GetValue<string>());
        Assert.Equal("subagent", payload["threadSource"]!.GetValue<string>());
        Assert.Equal("high", payload["modelReasoningEffort"]!.GetValue<string>());
        Assert.True(payload["networkAccessEnabled"]!.GetValue<bool>());
        Assert.Equal("live", payload["webSearchMode"]!.GetValue<string>());
        Assert.False(payload["webSearchEnabled"]!.GetValue<bool>());
        Assert.True(payload["skipGitRepoCheck"]!.GetValue<bool>());
        Assert.Equal("guardian_subagent", payload["approvalsReviewer"]!.GetValue<string>());
        Assert.Equal("on-failure", payload["approvalPolicy"]!["value"]!.GetValue<string>());

        JsonArray additionalDirectories = payload["additionalDirectories"]!.AsArray();
        Assert.Equal(@"C:\extra-one", additionalDirectories[0]!.GetValue<string>());
        Assert.Equal(@"C:\extra-two", additionalDirectories[1]!.GetValue<string>());
    }

    [Fact]
    public void BuildThreadListParams_EmitsFiltersAndSourceKinds()
    {
        JsonObject payload = CodexProtocol.BuildThreadListParams(new CodexThreadListOptions
        {
            Archived = true,
            Cursor = "cursor-123",
            WorkingDirectory = @"/work",
            WorkingDirectories = ["/work", "/other-work"],
            Limit = 50,
            ModelProviders = ["openai", "anthropic"],
            SearchTerm = "review",
            SortKey = CodexThreadSortKey.UpdatedAt,
            SortDirection = CodexThreadSortDirection.Asc,
            SourceKinds = [CodexThreadSourceKind.Exec, CodexThreadSourceKind.SubAgentThreadSpawn],
            UseStateDbOnly = true,
        });

        Assert.True(payload["archived"]!.GetValue<bool>());
        Assert.Equal("cursor-123", payload["cursor"]!.GetValue<string>());
        JsonArray cwd = payload["cwd"]!.AsArray();
        Assert.Equal("/work", cwd[0]!.GetValue<string>());
        Assert.Equal("/other-work", cwd[1]!.GetValue<string>());
        Assert.Equal(50, payload["limit"]!.GetValue<int>());
        Assert.Equal("review", payload["searchTerm"]!.GetValue<string>());
        Assert.Equal("updated_at", payload["sortKey"]!.GetValue<string>());
        Assert.Equal("asc", payload["sortDirection"]!.GetValue<string>());
        Assert.True(payload["useStateDbOnly"]!.GetValue<bool>());

        JsonArray providers = payload["modelProviders"]!.AsArray();
        Assert.Equal("openai", providers[0]!.GetValue<string>());
        Assert.Equal("anthropic", providers[1]!.GetValue<string>());

        JsonArray sourceKinds = payload["sourceKinds"]!.AsArray();
        Assert.Equal("exec", sourceKinds[0]!.GetValue<string>());
        Assert.Equal("subAgentThreadSpawn", sourceKinds[1]!.GetValue<string>());
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0244")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void BuildThreadMutationParams_EmitExpectedWireNames()
    {
        JsonObject resume = CodexProtocol.BuildThreadResumeParams("thread-1", new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
        });
        Assert.Equal("thread-1", resume["threadId"]!.GetValue<string>());
        Assert.Equal("/work", resume["cwd"]!.GetValue<string>());
        Assert.Equal("gpt-5", resume["model"]!.GetValue<string>());
        Assert.False(resume.ContainsKey("sessionStartSource"));
        Assert.False(resume.ContainsKey("threadSource"));

        JsonObject fork = CodexProtocol.BuildThreadForkParams("thread-2", new CodexThreadForkOptions
        {
            WorkingDirectory = "/fork",
            SessionStartSource = CodexThreadStartSource.Clear,
            ThreadSource = CodexThreadSource.User,
        });
        Assert.Equal("thread-2", fork["threadId"]!.GetValue<string>());
        Assert.Equal("/fork", fork["cwd"]!.GetValue<string>());
        Assert.Equal("clear", fork["sessionStartSource"]!.GetValue<string>());
        Assert.Equal("user", fork["threadSource"]!.GetValue<string>());

        JsonObject read = CodexProtocol.BuildThreadReadParams("thread-3", new CodexThreadReadOptions
        {
            IncludeTurns = true,
        });
        Assert.Equal("thread-3", read["threadId"]!.GetValue<string>());
        Assert.True(read["includeTurns"]!.GetValue<bool>());

        JsonObject archive = CodexProtocol.BuildThreadArchiveParams("thread-4");
        JsonObject unarchive = CodexProtocol.BuildThreadUnarchiveParams("thread-5");
        JsonObject name = CodexProtocol.BuildThreadNameParams("thread-6", "renamed");
        JsonObject compact = CodexProtocol.BuildThreadCompactParams("thread-7");
        JsonObject goalGet = CodexProtocol.BuildThreadGoalGetParams("thread-8");
        JsonObject goalSet = CodexProtocol.BuildThreadGoalSetParams(
            "thread-9",
            "ship goal mode",
            CodexThreadGoalStatus.Active,
            1200,
            tokenBudgetSpecified: true);
        JsonObject goalStatus = CodexProtocol.BuildThreadGoalSetParams(
            "thread-10",
            objective: null,
            CodexThreadGoalStatus.Paused,
            tokenBudget: null,
            tokenBudgetSpecified: false);
        JsonObject goalClear = CodexProtocol.BuildThreadGoalClearParams("thread-11");
        JsonObject rollback = CodexProtocol.BuildThreadRollbackParams("thread-12", 3);
        JsonObject unsubscribe = CodexProtocol.BuildThreadUnsubscribeParams("thread-13");
        JsonObject metadataUpdate = CodexProtocol.BuildThreadMetadataUpdateParams(
            "thread-14",
            new CodexGitInfo
            {
                Branch = "main",
                OriginUrl = "https://example.com/repo.git",
            });
        JsonObject metadataPatch = CodexProtocol.BuildThreadMetadataUpdateParams(
            "thread-14b",
            new CodexThreadMetadataGitInfoUpdate
            {
                BranchSpecified = true,
                Branch = "dev",
                OriginUrlSpecified = true,
                OriginUrl = null,
                ShaSpecified = true,
                Sha = "abc123",
            });
        JsonObject shellCommand = CodexProtocol.BuildThreadShellCommandParams("thread-15", "ls -la");
        JsonObject models = CodexProtocol.BuildModelListParams(new CodexModelListOptions
        {
            IncludeHidden = true,
        });
        JsonObject steer = CodexProtocol.BuildTurnSteerParams(
            "thread-16",
            "turn-17",
            [new CodexTextInput { Text = "steer" }]);
        JsonObject interrupt = CodexProtocol.BuildTurnInterruptParams("thread-18", "turn-19");

        Assert.Equal("thread-4", archive["threadId"]!.GetValue<string>());
        Assert.Equal("thread-5", unarchive["threadId"]!.GetValue<string>());
        Assert.Equal("thread-6", name["threadId"]!.GetValue<string>());
        Assert.Equal("renamed", name["name"]!.GetValue<string>());
        Assert.Equal("thread-7", compact["threadId"]!.GetValue<string>());
        Assert.Equal("thread-8", goalGet["threadId"]!.GetValue<string>());
        Assert.Equal("thread-9", goalSet["threadId"]!.GetValue<string>());
        Assert.Equal("ship goal mode", goalSet["objective"]!.GetValue<string>());
        Assert.Equal("active", goalSet["status"]!.GetValue<string>());
        Assert.Equal(1200L, goalSet["tokenBudget"]!.GetValue<long>());
        Assert.Equal("thread-10", goalStatus["threadId"]!.GetValue<string>());
        Assert.Equal("paused", goalStatus["status"]!.GetValue<string>());
        Assert.False(goalStatus.ContainsKey("objective"));
        Assert.False(goalStatus.ContainsKey("tokenBudget"));
        Assert.Equal("thread-11", goalClear["threadId"]!.GetValue<string>());
        Assert.Equal("thread-12", rollback["threadId"]!.GetValue<string>());
        Assert.Equal(3, rollback["numTurns"]!.GetValue<int>());
        Assert.Equal("thread-13", unsubscribe["threadId"]!.GetValue<string>());
        Assert.Equal("thread-14", metadataUpdate["threadId"]!.GetValue<string>());
        Assert.Equal("main", metadataUpdate["gitInfo"]!["branch"]!.GetValue<string>());
        Assert.Equal("thread-14b", metadataPatch["threadId"]!.GetValue<string>());
        Assert.Equal("dev", metadataPatch["gitInfo"]!["branch"]!.GetValue<string>());
        JsonObject metadataPatchGitInfo = metadataPatch["gitInfo"]!.AsObject();
        Assert.True(metadataPatchGitInfo.ContainsKey("originUrl"));
        Assert.Null(metadataPatchGitInfo["originUrl"]);
        Assert.Equal("abc123", metadataPatchGitInfo["sha"]!.GetValue<string>());
        Assert.Equal("thread-15", shellCommand["threadId"]!.GetValue<string>());
        Assert.Equal("ls -la", shellCommand["command"]!.GetValue<string>());
        Assert.True(models["includeHidden"]!.GetValue<bool>());
        Assert.Equal("thread-16", steer["threadId"]!.GetValue<string>());
        Assert.Equal("turn-17", steer["expectedTurnId"]!.GetValue<string>());
        Assert.Equal("thread-18", interrupt["threadId"]!.GetValue<string>());
        Assert.Equal("turn-19", interrupt["turnId"]!.GetValue<string>());
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0244")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void BuildTurnStartParams_EmitsThreadIdInputAndTurnOptions()
    {
        JsonObject outputSchema = new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["answer"] = new JsonObject { ["type"] = "string" },
            },
        };

        JsonObject payload = CodexProtocol.BuildTurnStartParams(
            "thread-1",
            [
                new CodexTextInput { Text = "hello" },
                new CodexImageInput { Url = "https://example.com/image.png" },
                new CodexLocalImageInput { Path = @"C:\images\trace.png" },
                new CodexSkillInput { Name = "skill", Path = @"C:\skills\skill.md" },
                new CodexMentionInput { Name = "mention", Path = @"C:\mentions\mention.md" },
            ],
            new CodexTurnOptions
            {
                ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
                Effort = CodexReasoningEffort.Low,
                Model = "gpt-5-mini",
                OutputSchema = outputSchema,
                Personality = CodexPersonality.Friendly,
                SandboxPolicy = new CodexReadOnlySandboxPolicy
                {
                    NetworkAccess = true,
                },
                ServiceTier = CodexServiceTier.Fast,
                Summary = CodexReasoningSummary.Concise,
                WorkingDirectory = @"/tmp/work",
            });

        Assert.Equal("thread-1", payload["threadId"]!.GetValue<string>());
        Assert.Equal("on-request", payload["approvalPolicy"]!["value"]!.GetValue<string>());
        Assert.Equal("low", payload["effort"]!.GetValue<string>());
        Assert.Equal("gpt-5-mini", payload["model"]!.GetValue<string>());
        Assert.Equal("friendly", payload["personality"]!.GetValue<string>());
        Assert.Equal("readOnly", payload["sandboxPolicy"]!["type"]!.GetValue<string>());
        Assert.True(payload["sandboxPolicy"]!["networkAccess"]!.GetValue<bool>());
        Assert.Equal("priority", payload["serviceTier"]!.GetValue<string>());
        Assert.Equal("concise", payload["summary"]!.GetValue<string>());
        Assert.Equal("/tmp/work", payload["workingDirectory"]!.GetValue<string>());
        Assert.True(JsonNode.DeepEquals(outputSchema, payload["outputSchema"]));

        JsonArray input = payload["input"]!.AsArray();
        Assert.Equal("text", input[0]!["type"]!.GetValue<string>());
        Assert.Equal("hello", input[0]!["text"]!.GetValue<string>());
        Assert.Equal("image", input[1]!["type"]!.GetValue<string>());
        Assert.Equal("https://example.com/image.png", input[1]!["url"]!.GetValue<string>());
        Assert.Equal("localImage", input[2]!["type"]!.GetValue<string>());
        Assert.Equal(@"C:\images\trace.png", input[2]!["path"]!.GetValue<string>());
        Assert.Equal("skill", input[3]!["type"]!.GetValue<string>());
        Assert.Equal("skill", input[3]!["name"]!.GetValue<string>());
        Assert.Equal(@"C:\skills\skill.md", input[3]!["path"]!.GetValue<string>());
        Assert.Equal("mention", input[4]!["type"]!.GetValue<string>());
        Assert.Equal("mention", input[4]!["name"]!.GetValue<string>());
        Assert.Equal(@"C:\mentions\mention.md", input[4]!["path"]!.GetValue<string>());
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0244")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void BuildTurnStartParams_DeepClonesOutputSchema()
    {
        JsonObject outputSchema = new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["summary"] = new JsonObject
                {
                    ["type"] = "string",
                },
            },
        };

        JsonObject payload = CodexProtocol.BuildTurnStartParams(
            "thread-1",
            [new CodexTextInput { Text = "hello" }],
            new CodexTurnOptions
            {
                OutputSchema = outputSchema,
            });

        outputSchema["properties"]!["summary"]!["type"] = "number";

        Assert.Equal("string", payload["outputSchema"]!["properties"]!["summary"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void BuildConfigPayload_EncodesNestedObjectsAndArrays()
    {
        JsonObject payload = CodexProtocol.BuildConfigPayload(new CodexConfigObject
        {
            Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
            {
                ["alpha"] = new CodexConfigStringValue("value"),
                ["nested"] = new CodexConfigObject
                {
                    Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                    {
                        ["count"] = new CodexConfigNumberValue(2.5),
                        ["flags"] = new CodexConfigArrayValue
                        {
                            Items =
                            [
                                new CodexConfigBooleanValue(true),
                                new CodexConfigBooleanValue(false),
                            ],
                        },
                    },
                },
            },
        });

        Assert.Equal("value", payload["alpha"]!.GetValue<string>());
        Assert.Equal(2.5, payload["nested"]!["count"]!.GetValue<double>());
        JsonArray flags = payload["nested"]!["flags"]!.AsArray();
        Assert.True(flags[0]!.GetValue<bool>());
        Assert.False(flags[1]!.GetValue<bool>());
    }

    [Fact]
    public void ParseThreadEvent_MapsLifecycleAndUnknownEvents()
    {
        CodexThreadEvent started = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread.started",
            new JsonObject
            {
                ["thread"] = new JsonObject
                {
                    ["id"] = "thread-1",
                    ["preview"] = "Preview",
                    ["status"] = new JsonObject { ["type"] = "idle" },
                    ["modelProvider"] = "openai",
                    ["createdAt"] = 1,
                    ["updatedAt"] = 2,
                    ["ephemeral"] = false,
                    ["cliVersion"] = "1.2.3",
                },
            }));

        Assert.IsType<CodexThreadStartedEvent>(started);
        Assert.Equal("thread-1", ((CodexThreadStartedEvent)started).Thread.Id);

        CodexThreadEvent statusChanged = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread.status.changed",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["status"] = new JsonObject { ["type"] = "active" },
            }));

        Assert.IsType<CodexThreadStatusChangedEvent>(statusChanged);
        Assert.Equal("thread-1", ((CodexThreadStatusChangedEvent)statusChanged).ThreadId);
        Assert.IsType<CodexActiveThreadStatus>(((CodexThreadStatusChangedEvent)statusChanged).Status);

        CodexThreadEvent archived = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread.archived",
            new JsonObject { ["threadId"] = "thread-1" }));

        Assert.IsType<CodexThreadArchivedEvent>(archived);
        Assert.Equal("thread-1", ((CodexThreadArchivedEvent)archived).ThreadId);

        CodexThreadEvent unarchived = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread.unarchived",
            new JsonObject { ["threadId"] = "thread-1" }));

        Assert.IsType<CodexThreadUnarchivedEvent>(unarchived);
        Assert.Equal("thread-1", ((CodexThreadUnarchivedEvent)unarchived).ThreadId);

        CodexThreadEvent closed = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread.closed",
            new JsonObject { ["threadId"] = "thread-1" }));

        Assert.IsType<CodexThreadClosedEvent>(closed);
        Assert.Equal("thread-1", ((CodexThreadClosedEvent)closed).ThreadId);

        CodexThreadEvent compacted = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread.compacted",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
            }));

        Assert.IsType<CodexThreadCompactedEvent>(compacted);
        Assert.Equal("turn-1", ((CodexThreadCompactedEvent)compacted).TurnId);

        CodexThreadEvent renamed = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread.name.updated",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["threadName"] = "Renamed thread",
            }));

        Assert.IsType<CodexThreadNameUpdatedEvent>(renamed);
        Assert.Equal("Renamed thread", ((CodexThreadNameUpdatedEvent)renamed).ThreadName);

        CodexThreadEvent usageUpdated = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread.tokenUsage.updated",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["tokenUsage"] = new JsonObject
                {
                    ["last"] = new JsonObject
                    {
                        ["inputTokens"] = 11,
                        ["cachedInputTokens"] = 2,
                        ["outputTokens"] = 3,
                        ["reasoningOutputTokens"] = 4,
                        ["totalTokens"] = 20,
                    },
                    ["modelContextWindow"] = 8192,
                    ["total"] = new JsonObject
                    {
                        ["inputTokens"] = 101,
                        ["cachedInputTokens"] = 5,
                        ["outputTokens"] = 6,
                        ["reasoningOutputTokens"] = 7,
                        ["totalTokens"] = 119,
                    },
                },
            }));

        Assert.IsType<CodexThreadTokenUsageUpdatedEvent>(usageUpdated);
        Assert.Equal(8192, ((CodexThreadTokenUsageUpdatedEvent)usageUpdated).TokenUsage.ModelContextWindow);
        Assert.Equal(119, ((CodexThreadTokenUsageUpdatedEvent)usageUpdated).TokenUsage.Total.TotalTokens);

        CodexThreadEvent diffUpdated = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "turn.diff.updated",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["diff"] = "diff text",
            }));

        Assert.IsType<CodexTurnDiffUpdatedEvent>(diffUpdated);
        Assert.Equal("diff text", ((CodexTurnDiffUpdatedEvent)diffUpdated).Diff);

        CodexThreadEvent agentDelta = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "item.agentMessage.delta",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["itemId"] = "item-1",
                ["delta"] = "hello",
            }));

        Assert.IsType<CodexAgentMessageDeltaEvent>(agentDelta);
        Assert.Equal("hello", ((CodexAgentMessageDeltaEvent)agentDelta).Delta);

        CodexThreadEvent commandDelta = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "item.commandExecution.outputDelta",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["itemId"] = "item-2",
                ["delta"] = "output",
            }));

        Assert.IsType<CodexCommandExecutionOutputDeltaEvent>(commandDelta);
        Assert.Equal("output", ((CodexCommandExecutionOutputDeltaEvent)commandDelta).Delta);

        CodexThreadEvent fileChangeDelta = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "item.fileChange.outputDelta",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["itemId"] = "item-3",
                ["delta"] = "patch chunk",
            }));

        Assert.IsType<CodexFileChangeOutputDeltaEvent>(fileChangeDelta);
        Assert.Equal("patch chunk", ((CodexFileChangeOutputDeltaEvent)fileChangeDelta).Delta);

        CodexThreadEvent fileChangePatchUpdated = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "item.fileChange.patchUpdated",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["itemId"] = "item-4",
                ["changes"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["path"] = "src/Program.cs",
                        ["kind"] = new JsonObject { ["type"] = "add" },
                        ["diff"] = "+line",
                    },
                },
            }));

        CodexFileChangePatchUpdatedEvent patchUpdated = Assert.IsType<CodexFileChangePatchUpdatedEvent>(fileChangePatchUpdated);
        Assert.Single(patchUpdated.Changes);
        Assert.Equal(CodexPatchChangeKind.Add, patchUpdated.Changes[0].Kind);

        CodexThreadEvent mcpProgress = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "item.mcpToolCall.progress",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["itemId"] = "item-5",
                ["message"] = "running",
            }));

        Assert.IsType<CodexMcpToolCallProgressEvent>(mcpProgress);
        Assert.Equal("running", ((CodexMcpToolCallProgressEvent)mcpProgress).Message);

        CodexThreadEvent reasoningTextDelta = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "item.reasoning.textDelta",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["itemId"] = "item-6",
                ["contentIndex"] = 2,
                ["delta"] = "reasoning",
            }));

        Assert.IsType<CodexReasoningTextDeltaEvent>(reasoningTextDelta);
        Assert.Equal(2, ((CodexReasoningTextDeltaEvent)reasoningTextDelta).ContentIndex);

        CodexThreadEvent summaryPartAdded = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "item.reasoning.summaryPartAdded",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["itemId"] = "item-7",
                ["summaryIndex"] = 1,
            }));

        Assert.IsType<CodexReasoningSummaryPartAddedEvent>(summaryPartAdded);
        Assert.Equal(1, ((CodexReasoningSummaryPartAddedEvent)summaryPartAdded).SummaryIndex);

        CodexThreadEvent summaryTextDelta = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "item.reasoning.summaryTextDelta",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["itemId"] = "item-8",
                ["summaryIndex"] = 1,
                ["delta"] = "summary",
            }));

        Assert.IsType<CodexReasoningSummaryTextDeltaEvent>(summaryTextDelta);
        Assert.Equal("summary", ((CodexReasoningSummaryTextDeltaEvent)summaryTextDelta).Delta);

        CodexThreadEvent completed = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "turn.completed",
            new JsonObject
            {
                ["turn"] = new JsonObject
                {
                    ["id"] = "turn-1",
                    ["status"] = "completed",
                    ["items"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "agentMessage",
                            ["id"] = "msg-1",
                            ["phase"] = "finalAnswer",
                            ["text"] = "done",
                        },
                    },
                },
            }));

        Assert.IsType<CodexTurnCompletedEvent>(completed);
        Assert.Equal("turn-1", ((CodexTurnCompletedEvent)completed).Turn.Id);
        Assert.Single(((CodexTurnCompletedEvent)completed).Turn.Items);

        CodexThreadEvent reviewStarted = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "item/autoApprovalReview/started",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["reviewId"] = "review-1",
                ["startedAtMs"] = 1234L,
                ["targetItemId"] = "item-1",
                ["action"] = new JsonObject { ["kind"] = "review" },
                ["review"] = new JsonObject { ["kind"] = "guardian" },
            }));

        Assert.IsType<CodexItemAutoApprovalReviewStartedEvent>(reviewStarted);
        Assert.Equal("review-1", ((CodexItemAutoApprovalReviewStartedEvent)reviewStarted).ReviewId);
        Assert.Equal(1234L, ((CodexItemAutoApprovalReviewStartedEvent)reviewStarted).StartedAtMs);

        CodexThreadEvent reviewCompleted = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "item/autoApprovalReview/completed",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["reviewId"] = "review-1",
                ["startedAtMs"] = 1234L,
                ["completedAtMs"] = 2345L,
                ["decisionSource"] = "agent",
                ["targetItemId"] = "item-1",
                ["action"] = new JsonObject { ["kind"] = "review" },
                ["review"] = new JsonObject { ["kind"] = "guardian" },
            }));

        Assert.IsType<CodexItemAutoApprovalReviewCompletedEvent>(reviewCompleted);
        Assert.Equal("agent", ((CodexItemAutoApprovalReviewCompletedEvent)reviewCompleted).DecisionSource);
        Assert.Equal(2345L, ((CodexItemAutoApprovalReviewCompletedEvent)reviewCompleted).CompletedAtMs);

        CodexThreadEvent unknown = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "custom/runtime-event",
            new JsonObject
            {
                ["note"] = "mystery",
            }));

        Assert.IsType<CodexUnknownThreadEvent>(unknown);
        Assert.Equal("custom.runtime-event", ((CodexUnknownThreadEvent)unknown).UnknownType);
        Assert.Equal("mystery", ((CodexUnknownThreadEvent)unknown).RawPayload!["note"]!.GetValue<string>());
    }

    [Fact]
    public void ParseThreadItem_MapsExpandedThreadItems()
    {
        CodexUserMessageItem userMessage = Assert.IsType<CodexUserMessageItem>(CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "userMessage",
            ["id"] = "user-1",
            ["content"] = new JsonArray
            {
                new JsonObject { ["type"] = "text", ["text"] = "hello" },
                new JsonObject { ["type"] = "image", ["url"] = "https://example.com/image.png" },
                new JsonObject { ["type"] = "localImage", ["path"] = @"C:\\image.png" },
                new JsonObject { ["type"] = "skill", ["name"] = "skill", ["path"] = @"C:\\skill.md" },
                new JsonObject { ["type"] = "mention", ["name"] = "mention", ["path"] = @"C:\\mention.md" },
            },
        }));
        Assert.Equal(5, userMessage.Content.Count);
        Assert.IsType<CodexTextInput>(userMessage.Content[0]);

        CodexHookPromptItem hookPrompt = Assert.IsType<CodexHookPromptItem>(CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "hookPrompt",
            ["id"] = "hook-1",
            ["fragments"] = new JsonArray
            {
                new JsonObject { ["hookRunId"] = "hook-run-1", ["text"] = "prompt text" },
            },
        }));
        Assert.Single(hookPrompt.Fragments);
        Assert.Equal("prompt text", hookPrompt.Fragments[0].Text);

        CodexAgentMessageItem agentMessage = Assert.IsType<CodexAgentMessageItem>(CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "agentMessage",
            ["id"] = "agent-1",
            ["phase"] = "finalAnswer",
            ["text"] = "done",
            ["memoryCitation"] = new JsonObject
            {
                ["entries"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["lineStart"] = 1,
                        ["lineEnd"] = 3,
                        ["note"] = "cite",
                        ["path"] = "README.md",
                    },
                },
                ["threadIds"] = new JsonArray { "thread-1" },
            },
        }));
        Assert.Equal("done", agentMessage.Text);
        Assert.Equal("README.md", agentMessage.MemoryCitation!.Entries[0].Path);

        CodexCommandExecutionItem commandExecution = Assert.IsType<CodexCommandExecutionItem>(CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "commandExecution",
            ["id"] = "cmd-1",
            ["aggregatedOutput"] = "output",
            ["command"] = "dotnet test",
            ["commandActions"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "read",
                    ["command"] = "cat",
                    ["name"] = "Read file",
                    ["path"] = "README.md",
                },
            },
            ["cwd"] = @"C:\\work",
            ["durationMs"] = 123,
            ["exitCode"] = 0,
            ["processId"] = "proc-1",
            ["source"] = "userShell",
            ["status"] = "completed",
        }));
        Assert.Equal(CodexCommandExecutionSource.UserShell, commandExecution.Source);
        Assert.Single(commandExecution.CommandActions);

        CodexFileChangeItem fileChange = Assert.IsType<CodexFileChangeItem>(CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "fileChange",
            ["id"] = "file-1",
            ["changes"] = new JsonArray
            {
                new JsonObject
                {
                    ["path"] = "src/Program.cs",
                    ["kind"] = new JsonObject { ["type"] = "update" },
                    ["diff"] = "diff",
                },
            },
            ["status"] = "completed",
        }));
        Assert.Equal(CodexPatchChangeKind.Update, fileChange.Changes[0].Kind);

        CodexMcpToolCallItem mcpToolCall = Assert.IsType<CodexMcpToolCallItem>(CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "mcpToolCall",
            ["id"] = "mcp-1",
            ["arguments"] = new JsonObject { ["alpha"] = 1 },
            ["durationMs"] = 22,
            ["error"] = new JsonObject { ["message"] = "failed" },
            ["mcpAppResourceUri"] = "app://resource",
            ["result"] = new JsonObject
            {
                ["content"] = new JsonArray { new JsonObject { ["type"] = "text", ["text"] = "ok" } },
                ["structuredContent"] = new JsonObject { ["beta"] = true },
            },
            ["server"] = "server",
            ["status"] = "failed",
            ["tool"] = "tool",
        }));
        Assert.Equal("app://resource", mcpToolCall.McpAppResourceUri);
        Assert.Single(mcpToolCall.Result!.Content);

        CodexDynamicToolCallItem dynamicToolCall = Assert.IsType<CodexDynamicToolCallItem>(CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "dynamicToolCall",
            ["id"] = "dyn-1",
            ["arguments"] = new JsonObject { ["alpha"] = 1 },
            ["contentItems"] = new JsonArray
            {
                new JsonObject { ["type"] = "inputText", ["text"] = "value" },
            },
            ["durationMs"] = 44,
            ["namespace"] = "ns",
            ["status"] = "completed",
            ["success"] = true,
            ["tool"] = "tool",
        }));
        Assert.Equal("ns", dynamicToolCall.Namespace);
        Assert.Single(dynamicToolCall.ContentItems!);

        CodexCollabAgentToolCallItem collabAgentToolCall = Assert.IsType<CodexCollabAgentToolCallItem>(CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "collabAgentToolCall",
            ["id"] = "collab-1",
            ["agentsStates"] = new JsonObject
            {
                ["agent-1"] = new JsonObject { ["status"] = "running", ["message"] = "ok" },
            },
            ["model"] = "gpt-5",
            ["prompt"] = "prompt",
            ["reasoningEffort"] = "high",
            ["receiverThreadIds"] = new JsonArray { "thread-2" },
            ["senderThreadId"] = "thread-1",
            ["status"] = "completed",
            ["tool"] = "spawnAgent",
        }));
        Assert.Equal(CodexCollabAgentTool.SpawnAgent, collabAgentToolCall.Tool);
        Assert.Equal(CodexCollabAgentStatus.Running, collabAgentToolCall.AgentsStates["agent-1"].Status);

        CodexWebSearchItem webSearch = Assert.IsType<CodexWebSearchItem>(CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "webSearch",
            ["id"] = "web-1",
            ["action"] = new JsonObject { ["type"] = "search", ["query"] = "codex" },
            ["query"] = "codex",
        }));
        Assert.IsType<CodexSearchWebSearchAction>(webSearch.Action);

        CodexImageGenerationItem imageGeneration = Assert.IsType<CodexImageGenerationItem>(CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "imageGeneration",
            ["id"] = "img-1",
            ["result"] = "result",
            ["revisedPrompt"] = "revised",
            ["savedPath"] = @"C:\\result.png",
            ["status"] = "completed",
        }));
        Assert.Equal(@"C:\\result.png", imageGeneration.SavedPath);

        CodexTodoListItem todoList = Assert.IsType<CodexTodoListItem>(CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "todoList",
            ["id"] = "todo-1",
            ["items"] = new JsonArray
            {
                new JsonObject { ["text"] = "task", ["completed"] = true },
            },
        }));
        Assert.True(todoList.Items[0].Completed);
    }

    [Fact]
    public void ParseThreadEvent_MapsAccountRateLimitsUpdatedNotification()
    {
        CodexThreadEvent evt = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "account/rateLimits/updated",
            new JsonObject
            {
                ["rateLimits"] = new JsonObject
                {
                    ["limitId"] = "codex",
                    ["planType"] = "plus",
                    ["primary"] = new JsonObject
                    {
                        ["usedPercent"] = 25,
                        ["windowDurationMins"] = 300,
                        ["resetsAt"] = 1778076000L,
                    },
                },
            }));

        CodexAccountRateLimitsUpdatedEvent updated = Assert.IsType<CodexAccountRateLimitsUpdatedEvent>(evt);
        Assert.Equal("account.rateLimits.updated", updated.Type);
        Assert.Equal("codex", updated.RateLimits.LimitId);
        Assert.Equal(CodexPlanType.Plus, updated.RateLimits.PlanType);
        Assert.Equal(25, updated.RateLimits.Primary!.UsedPercent);
        Assert.Equal(300, updated.RateLimits.Primary.WindowDurationMinutes);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1778076000L), updated.RateLimits.Primary.ResetsAt);
    }

    [Fact]
    public void ParseThreadEvent_MapsThreadGoalNotifications()
    {
        CodexThreadEvent updatedEvent = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread/goal/updated",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["goal"] = CreateThreadGoal("thread-1", "ship goal mode", "budgetLimited"),
            }));

        CodexThreadGoalUpdatedEvent updated = Assert.IsType<CodexThreadGoalUpdatedEvent>(updatedEvent);
        Assert.Equal("thread.goal.updated", updated.Type);
        Assert.Equal("thread-1", updated.ThreadId);
        Assert.Equal("turn-1", updated.TurnId);
        Assert.Equal("ship goal mode", updated.Goal.Objective);
        Assert.Equal(CodexThreadGoalStatus.BudgetLimited, updated.Goal.Status);
        Assert.Equal(1200, updated.Goal.TokenBudget);
        Assert.Equal(42, updated.Goal.TokensUsed);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1778076000L), updated.Goal.CreatedAt);

        CodexThreadEvent clearedEvent = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread/goal/cleared",
            new JsonObject
            {
                ["threadId"] = "thread-1",
            }));

        CodexThreadGoalClearedEvent cleared = Assert.IsType<CodexThreadGoalClearedEvent>(clearedEvent);
        Assert.Equal("thread.goal.cleared", cleared.Type);
        Assert.Equal("thread-1", cleared.ThreadId);
    }

    [Fact]
    public void ParseThreadEvent_MapsStructuredPlanNotifications()
    {
        CodexThreadEvent updatedEvent = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "turn/plan/updated",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["explanation"] = "Plan changed",
                ["plan"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["step"] = "Inspect upstream",
                        ["status"] = "completed",
                    },
                    new JsonObject
                    {
                        ["step"] = "Patch SDK",
                        ["status"] = "inProgress",
                    },
                    new JsonObject
                    {
                        ["step"] = "Verify",
                        ["status"] = "pending",
                    },
                },
            }));

        CodexTurnPlanUpdatedEvent updated = Assert.IsType<CodexTurnPlanUpdatedEvent>(updatedEvent);
        Assert.Equal("turn.plan.updated", updated.Type);
        Assert.Equal("thread-1", updated.ThreadId);
        Assert.Equal("turn-1", updated.TurnId);
        Assert.Equal("Plan changed", updated.Explanation);
        Assert.Equal(3, updated.Plan.Count);
        Assert.Equal("Inspect upstream", updated.Plan[0].Step);
        Assert.Equal(CodexTurnPlanStepStatus.Completed, updated.Plan[0].Status);
        Assert.Equal(CodexTurnPlanStepStatus.InProgress, updated.Plan[1].Status);
        Assert.Equal(CodexTurnPlanStepStatus.Pending, updated.Plan[2].Status);

        CodexThreadEvent deltaEvent = CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "item/plan/delta",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["itemId"] = "plan-1",
                ["delta"] = "Inspect",
            }));

        CodexPlanDeltaEvent delta = Assert.IsType<CodexPlanDeltaEvent>(deltaEvent);
        Assert.Equal("item.plan.delta", delta.Type);
        Assert.Equal("thread-1", delta.ThreadId);
        Assert.Equal("turn-1", delta.TurnId);
        Assert.Equal("plan-1", delta.ItemId);
        Assert.Equal("Inspect", delta.Delta);
    }

    [Fact]
    public void ParseThreadEvent_MapsOperationalNotifications()
    {
        CodexHookStartedEvent hookStarted = Assert.IsType<CodexHookStartedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "hook/started",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["run"] = new JsonObject
                {
                    ["id"] = "run-1",
                    ["displayOrder"] = 2,
                    ["durationMs"] = 11,
                    ["entries"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["kind"] = "warning",
                            ["text"] = "watch this",
                        },
                    },
                    ["eventName"] = "sessionStart",
                    ["executionMode"] = "sync",
                    ["handlerType"] = "command",
                    ["scope"] = "thread",
                    ["source"] = "plugin",
                    ["sourcePath"] = "/hooks/start",
                    ["startedAt"] = 1778076000L,
                    ["status"] = "running",
                },
            })));
        Assert.Equal("thread-1", hookStarted.ThreadId);
        Assert.Equal("turn-1", hookStarted.TurnId);
        Assert.Equal("run-1", hookStarted.Run.Id);
        Assert.Equal(CodexHookEventName.SessionStart, hookStarted.Run.EventName);
        Assert.Equal(CodexHookExecutionMode.Sync, hookStarted.Run.ExecutionMode);
        Assert.Equal(CodexHookHandlerType.Command, hookStarted.Run.HandlerType);
        Assert.Equal(CodexHookScope.Thread, hookStarted.Run.Scope);
        Assert.Equal(CodexHookSourceKind.Plugin, hookStarted.Run.Source);
        Assert.Equal("watch this", hookStarted.Run.Entries[0].Text);
        Assert.Equal(CodexHookOutputEntryKind.Warning, hookStarted.Run.Entries[0].Kind);

        CodexHookCompletedEvent hookCompleted = Assert.IsType<CodexHookCompletedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "hook/completed",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["run"] = new JsonObject
                {
                    ["id"] = "run-2",
                    ["displayOrder"] = 3,
                    ["eventName"] = "stop",
                    ["executionMode"] = "async",
                    ["handlerType"] = "agent",
                    ["scope"] = "turn",
                    ["source"] = "system",
                    ["sourcePath"] = "/hooks/complete",
                    ["startedAt"] = 1778076001L,
                    ["completedAt"] = 1778076002L,
                    ["status"] = "completed",
                    ["statusMessage"] = "done",
                },
            })));
        Assert.Equal(CodexHookRunStatus.Completed, hookCompleted.Run.Status);
        Assert.Equal(CodexHookHandlerType.Agent, hookCompleted.Run.HandlerType);
        Assert.Equal("done", hookCompleted.Run.StatusMessage);

        CodexProcessOutputDeltaEvent processDelta = Assert.IsType<CodexProcessOutputDeltaEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "process/outputDelta",
            new JsonObject
            {
                ["processHandle"] = "proc-1",
                ["stream"] = "stderr",
                ["deltaBase64"] = "aGVsbG8=",
                ["capReached"] = true,
            })));
        Assert.Equal("proc-1", processDelta.ProcessHandle);
        Assert.Equal(CodexProcessOutputStream.Stderr, processDelta.Stream);
        Assert.True(processDelta.CapReached);

        CodexProcessExitedEvent processExited = Assert.IsType<CodexProcessExitedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "process/exited",
            new JsonObject
            {
                ["processHandle"] = "proc-2",
                ["exitCode"] = 7,
                ["stderr"] = "oops",
                ["stderrCapReached"] = false,
                ["stdout"] = "ok",
                ["stdoutCapReached"] = true,
            })));
        Assert.Equal(7, processExited.ExitCode);
        Assert.Equal("ok", processExited.Stdout);
        Assert.True(processExited.StdoutCapReached);

        CodexWarningEvent warning = Assert.IsType<CodexWarningEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "warning",
            new JsonObject
            {
                ["message"] = "be careful",
            })));
        Assert.Equal("be careful", warning.Message);
        Assert.Null(warning.ThreadId);

        CodexConfigWarningEvent configWarning = Assert.IsType<CodexConfigWarningEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "configWarning",
            new JsonObject
            {
                ["summary"] = "bad config",
                ["details"] = "extra detail",
                ["path"] = "codex.toml",
                ["range"] = new JsonObject
                {
                    ["start"] = new JsonObject
                    {
                        ["line"] = 2,
                        ["column"] = 4,
                    },
                    ["end"] = new JsonObject
                    {
                        ["line"] = 2,
                        ["column"] = 12,
                    },
                },
            })));
        Assert.Equal("bad config", configWarning.Summary);
        Assert.Equal(2, configWarning.Range!.Start.Line);
        Assert.Equal(12, configWarning.Range.End.Column);

        CodexAccountUpdatedEvent accountUpdated = Assert.IsType<CodexAccountUpdatedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "account/updated",
            new JsonObject
            {
                ["authMode"] = "chatgptAuthTokens",
                ["planType"] = "plus",
            })));
        Assert.Equal(CodexAuthMode.ChatgptAuthTokens, accountUpdated.AuthMode);
        Assert.Equal(CodexPlanType.Plus, accountUpdated.PlanType);

        CodexAccountLoginCompletedEvent loginCompleted = Assert.IsType<CodexAccountLoginCompletedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "account/login/completed",
            new JsonObject
            {
                ["loginId"] = "login-1",
                ["success"] = true,
        })));
        Assert.True(loginCompleted.Success);
        Assert.Equal("login-1", loginCompleted.LoginId);

        CodexServerRequestResolvedEvent serverRequestResolved = Assert.IsType<CodexServerRequestResolvedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "serverRequest/resolved",
            new JsonObject
            {
                ["requestId"] = "request-1",
                ["threadId"] = "thread-1",
            })));
        Assert.Equal("request-1", serverRequestResolved.RequestId);
        Assert.Equal("thread-1", serverRequestResolved.ThreadId);

        CodexAppListUpdatedEvent appListUpdated = Assert.IsType<CodexAppListUpdatedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "app/list/updated",
            new JsonObject
            {
                ["data"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["id"] = "app-1",
                        ["name"] = "Trace App",
                    },
                },
            })));
        Assert.Single(appListUpdated.Data);
        Assert.Equal("Trace App", appListUpdated.Data[0]["name"]!.GetValue<string>());

        Assert.IsType<CodexSkillsChangedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification("skills/changed", new JsonObject())));

        CodexFsChangedEvent fsChanged = Assert.IsType<CodexFsChangedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "fs/changed",
            new JsonObject
            {
                ["changedPaths"] = new JsonArray { "/work/a.txt", "/work/b.txt" },
                ["watchId"] = "watch-1",
            })));
        Assert.Equal("watch-1", fsChanged.WatchId);
        Assert.Equal("/work/a.txt", fsChanged.ChangedPaths[0]);

        CodexMcpServerOauthLoginCompletedEvent oauthLoginCompleted = Assert.IsType<CodexMcpServerOauthLoginCompletedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "mcpServer/oauthLogin/completed",
            new JsonObject
            {
                ["name"] = "server-1",
                ["success"] = true,
            })));
        Assert.True(oauthLoginCompleted.Success);
        Assert.Equal("server-1", oauthLoginCompleted.Name);

        CodexMcpServerStartupStatusUpdatedEvent startupStatus = Assert.IsType<CodexMcpServerStartupStatusUpdatedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "mcpServer/startupStatus/updated",
            new JsonObject
            {
                ["name"] = "server-2",
                ["status"] = "ready",
            })));
        Assert.Equal(CodexMcpServerStartupState.Ready, startupStatus.Status);

        CodexRemoteControlStatusChangedEvent remoteStatus = Assert.IsType<CodexRemoteControlStatusChangedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "remoteControl/status/changed",
            new JsonObject
            {
                ["installationId"] = "install-1",
                ["status"] = "connected",
            })));
        Assert.Equal(CodexRemoteControlConnectionStatus.Connected, remoteStatus.Status);

        CodexWindowsSandboxSetupCompletedEvent sandboxCompleted = Assert.IsType<CodexWindowsSandboxSetupCompletedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "windowsSandbox/setupCompleted",
            new JsonObject
            {
                ["mode"] = "elevated",
                ["success"] = true,
            })));
        Assert.Equal(CodexWindowsSandboxSetupMode.Elevated, sandboxCompleted.Mode);
        Assert.True(sandboxCompleted.Success);

        CodexWindowsWorldWritableWarningEvent worldWritable = Assert.IsType<CodexWindowsWorldWritableWarningEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "windows/worldWritableWarning",
            new JsonObject
            {
                ["extraCount"] = 3,
                ["failedScan"] = false,
                ["samplePaths"] = new JsonArray { "C:\\tmp\\one", "C:\\tmp\\two" },
            })));
        Assert.Equal(3, worldWritable.ExtraCount);
        Assert.Equal("C:\\tmp\\one", worldWritable.SamplePaths[0]);
    }

    [Fact]
    public void ParseThreadEvent_MapsRealtimeAndModelNotifications()
    {
        CodexModelReroutedEvent rerouted = Assert.IsType<CodexModelReroutedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "model/rerouted",
            new JsonObject
            {
                ["fromModel"] = "gpt-5",
                ["reason"] = "highRiskCyberActivity",
                ["threadId"] = "thread-1",
                ["toModel"] = "gpt-5.1",
                ["turnId"] = "turn-1",
            })));
        Assert.Equal(CodexModelRerouteReason.HighRiskCyberActivity, rerouted.Reason);

        CodexModelVerificationEvent verification = Assert.IsType<CodexModelVerificationEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "model/verification",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["turnId"] = "turn-1",
                ["verifications"] = new JsonArray { "trustedAccessForCyber" },
            })));
        Assert.Equal(CodexModelVerificationValue.TrustedAccessForCyber, verification.Verifications[0]);

        CodexFuzzyFileSearchSessionUpdatedEvent fuzzyUpdated = Assert.IsType<CodexFuzzyFileSearchSessionUpdatedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "fuzzyFileSearch/sessionUpdated",
            new JsonObject
            {
                ["sessionId"] = "search-1",
                ["query"] = "trace",
                ["files"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["fileName"] = "trace.cs",
                        ["indices"] = new JsonArray { 1, 5 },
                        ["matchType"] = "directory",
                        ["path"] = "/work/src/trace.cs",
                        ["root"] = "/work",
                        ["score"] = 91,
                    },
                },
        })));
        Assert.Equal("trace", fuzzyUpdated.Query);
        Assert.Equal(1, fuzzyUpdated.Files[0].Indices![0]);
        Assert.Equal(5, fuzzyUpdated.Files[0].Indices![1]);

        CodexThreadRealtimeStartedEvent realtimeStarted = Assert.IsType<CodexThreadRealtimeStartedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread/realtime/started",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["realtimeSessionId"] = "rt-1",
                ["version"] = "v2",
            })));
        Assert.Equal("rt-1", realtimeStarted.RealtimeSessionId);
        Assert.Equal(CodexRealtimeConversationVersion.V2, realtimeStarted.Version);

        CodexThreadRealtimeItemAddedEvent realtimeItemAdded = Assert.IsType<CodexThreadRealtimeItemAddedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread/realtime/itemAdded",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["item"] = new JsonObject
                {
                    ["type"] = "agentMessage",
                    ["id"] = "item-1",
                    ["text"] = "hello",
                },
            })));
        Assert.Equal("item-1", realtimeItemAdded.Item!["id"]!.GetValue<string>());

        CodexThreadRealtimeTranscriptDeltaEvent transcriptDelta = Assert.IsType<CodexThreadRealtimeTranscriptDeltaEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread/realtime/transcript/delta",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["role"] = "assistant",
                ["delta"] = "he",
            })));
        Assert.Equal("he", transcriptDelta.Delta);

        CodexThreadRealtimeTranscriptDoneEvent transcriptDone = Assert.IsType<CodexThreadRealtimeTranscriptDoneEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread/realtime/transcript/done",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["role"] = "assistant",
                ["text"] = "hello",
            })));
        Assert.Equal("hello", transcriptDone.Text);

        CodexThreadRealtimeOutputAudioDeltaEvent audioDelta = Assert.IsType<CodexThreadRealtimeOutputAudioDeltaEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread/realtime/outputAudio/delta",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["audio"] = new JsonObject
                {
                    ["data"] = "aGVsbG8=",
                    ["numChannels"] = 1,
                    ["sampleRate"] = 24000,
                    ["samplesPerChannel"] = 3,
                },
            })));
        Assert.Equal(1, audioDelta.Audio.NumChannels);
        Assert.Equal(24000, audioDelta.Audio.SampleRate);

        CodexThreadRealtimeSdpEvent sdp = Assert.IsType<CodexThreadRealtimeSdpEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread/realtime/sdp",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["sdp"] = "offer",
            })));
        Assert.Equal("offer", sdp.Sdp);

        CodexThreadRealtimeErrorEvent realtimeError = Assert.IsType<CodexThreadRealtimeErrorEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread/realtime/error",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["message"] = "boom",
            })));
        Assert.Equal("boom", realtimeError.Message);

        CodexThreadRealtimeClosedEvent realtimeClosed = Assert.IsType<CodexThreadRealtimeClosedEvent>(CodexProtocol.ParseThreadEvent(TestJson.Notification(
            "thread/realtime/closed",
            new JsonObject
            {
                ["threadId"] = "thread-1",
                ["reason"] = "done",
            })));
        Assert.Equal("done", realtimeClosed.Reason);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0241")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void ParseThreadEvent_UnwrapsParamsAndParsesFallbackBranches()
    {
        CodexThreadEvent evt = CodexProtocol.ParseThreadEvent(new JsonObject
        {
            ["method"] = "turn.failed",
            ["params"] = new JsonObject
            {
                ["turn"] = new JsonObject
                {
                    ["id"] = "turn-2",
                    ["status"] = "unexpected",
                    ["items"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "agentMessage",
                            ["id"] = "msg-2",
                            ["phase"] = "final_answer",
                            ["text"] = "done",
                        },
                    },
                },
            },
        });

        CodexTurnFailedEvent failed = Assert.IsType<CodexTurnFailedEvent>(evt);
        Assert.Equal("turn-2", failed.Turn.Id);
        Assert.Equal(CodexTurnStatus.InProgress, failed.Turn.Status);

        CodexAgentMessageItem message = Assert.IsType<CodexAgentMessageItem>(failed.Turn.Items.Single());
        Assert.Equal("msg-2", message.Id);
        Assert.Equal(CodexMessagePhase.FinalAnswer, message.Phase);
        Assert.Equal("done", message.Text);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0317")]
    public void ParseThreadItem_MapsCommonItemVariants()
    {
        CodexThreadItem agentMessage = CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "agentMessage",
            ["id"] = "message-1",
            ["phase"] = "commentary",
            ["text"] = "hello",
        });

        CodexThreadItem plan = CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "plan",
            ["id"] = "plan-1",
            ["text"] = "plan text",
        });

        CodexThreadItem reasoning = CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "reasoning",
            ["id"] = "reasoning-1",
            ["content"] = new JsonArray { "step one", "step two" },
            ["summary"] = new JsonArray { "summary line" },
        });

        CodexThreadItem compaction = CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "contextCompaction",
            ["id"] = "compaction-1",
        });

        CodexThreadItem error = CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "error",
            ["id"] = "error-1",
            ["message"] = "boom",
        });

        CodexThreadItem unknown = CodexProtocol.ParseThreadItem(new JsonObject
        {
            ["type"] = "custom-item",
            ["id"] = "unknown-1",
            ["extra"] = "value",
        });

        Assert.IsType<CodexAgentMessageItem>(agentMessage);
        Assert.IsType<CodexPlanItem>(plan);
        Assert.IsType<CodexReasoningItem>(reasoning);
        Assert.IsType<CodexContextCompactionItem>(compaction);
        Assert.IsType<CodexErrorItem>(error);
        Assert.IsType<CodexUnknownThreadItem>(unknown);
        Assert.Equal("custom-item", ((CodexUnknownThreadItem)unknown).UnknownType);
    }

    [Fact]
    public void ParseTurnRecord_ParsesUsageAndError()
    {
        CodexTurnRecord turn = CodexProtocol.ParseTurnRecord(new JsonObject
        {
            ["id"] = "turn-1",
            ["status"] = "failed",
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "reasoning",
                    ["id"] = "reasoning-1",
                    ["content"] = new JsonArray { "step one", "step two" },
                    ["summary"] = new JsonArray { "summary line" },
                },
            },
            ["error"] = new JsonObject
            {
                ["message"] = "boom",
                ["additionalDetails"] = "tail",
            },
            ["usage"] = new JsonObject
            {
                ["last"] = new JsonObject
                {
                    ["inputTokens"] = 3,
                    ["outputTokens"] = 4,
                    ["reasoningOutputTokens"] = 5,
                    ["totalTokens"] = 12,
                },
                ["modelContextWindow"] = 128,
                ["total"] = new JsonObject
                {
                    ["inputTokens"] = 7,
                    ["outputTokens"] = 8,
                    ["reasoningOutputTokens"] = 9,
                    ["totalTokens"] = 24,
                },
            },
        });

        Assert.Equal("turn-1", turn.Id);
        Assert.Equal(CodexTurnStatus.Failed, turn.Status);
        Assert.Single(turn.Items);
        Assert.Equal("boom", turn.Error!.Message);
        Assert.Equal("tail", turn.Error.AdditionalDetails);
        Assert.Equal(128, turn.Usage!.ModelContextWindow);
        Assert.Equal(24, turn.Usage.Total.TotalTokens);
    }

    [Fact]
    public void ParseAccountRateLimitsResult_ParsesRateLimitsByLimitId()
    {
        CodexAccountRateLimitsResult result = CodexProtocol.ParseAccountRateLimitsResult(new JsonObject
        {
            ["rateLimitsByLimitId"] = new JsonObject
            {
                ["codex"] = new JsonObject
                {
                    ["limitName"] = "Codex",
                    ["planType"] = "plus",
                    ["credits"] = new JsonObject
                    {
                        ["balance"] = 12.5,
                        ["hasCredits"] = true,
                        ["unlimited"] = false,
                    },
                    ["primary"] = new JsonObject
                    {
                        ["usedPercent"] = 10,
                        ["resetsAt"] = 1778076000L,
                        ["windowDurationMins"] = 300,
                    },
                    ["secondary"] = new JsonObject
                    {
                        ["usedPercent"] = 61,
                        ["resetsAt"] = 1778421600L,
                        ["windowDurationMins"] = 10080,
                    },
                },
            },
        });

        CodexRateLimitSnapshot bucket = Assert.Single(result.RateLimits);
        Assert.Equal("codex", bucket.LimitId);
        Assert.Equal("Codex", bucket.LimitName);
        Assert.Equal(CodexPlanType.Plus, bucket.PlanType);
        Assert.Equal(12.5, bucket.Credits!.Balance);
        Assert.True(bucket.Credits.HasCredits);
        Assert.False(bucket.Credits.Unlimited);
        Assert.Equal(10, bucket.Primary!.UsedPercent);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1778076000L), bucket.Primary.ResetsAt);
        Assert.Equal(300, bucket.Primary.WindowDurationMinutes);
        Assert.Equal(61, bucket.Secondary!.UsedPercent);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1778421600L), bucket.Secondary.ResetsAt);
        Assert.Equal(10080, bucket.Secondary.WindowDurationMinutes);
        Assert.Same(bucket, result.RateLimitsByLimitId["codex"]);
    }

    [Fact]
    public void ParseAccountRateLimitsResult_ParsesLegacySnakeCasePayload()
    {
        CodexAccountRateLimitsResult result = CodexProtocol.ParseAccountRateLimitsResult(new JsonObject
        {
            ["rate_limits"] = new JsonObject
            {
                ["limit_id"] = "codex",
                ["limit_name"] = "Codex",
                ["plan_type"] = "pro",
                ["rate_limit_reached_type"] = "rate_limit_reached",
                ["primary"] = new JsonObject
                {
                    ["used_percent"] = 101,
                    ["resets_at"] = "1778076000",
                    ["window_duration_mins"] = "300",
                },
            },
        });

        CodexRateLimitSnapshot bucket = Assert.Single(result.RateLimits);
        Assert.Equal("codex", bucket.LimitId);
        Assert.Equal(CodexPlanType.Pro, bucket.PlanType);
        Assert.Equal("rate_limit_reached", bucket.RateLimitReachedType);
        Assert.Equal(100, bucket.Primary!.UsedPercent);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1778076000L), bucket.Primary.ResetsAt);
        Assert.Equal(300, bucket.Primary.WindowDurationMinutes);
        Assert.Same(bucket, result.RateLimitsByLimitId["codex"]);
    }

    [Fact]
    public void ParseModelListResult_ParsesModelsAndCursor()
    {
        CodexModelListResult result = CodexProtocol.ParseModelListResult(new JsonObject
        {
            ["nextCursor"] = "cursor-2",
            ["models"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "model-1",
                    ["model"] = "gpt-5",
                    ["displayName"] = "GPT-5",
                    ["description"] = "Trace model",
                    ["hidden"] = false,
                    ["isDefault"] = true,
                    ["defaultReasoningEffort"] = "high",
                    ["availabilityNux"] = new JsonObject { ["message"] = "Upgrade required" },
                    ["inputModalities"] = new JsonArray { "text", "image" },
                    ["additionalSpeedTiers"] = new JsonArray { "fast" },
                    ["supportedReasoningEfforts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["description"] = "Balanced",
                            ["reasoningEffort"] = "medium",
                        },
                    },
                    ["supportsPersonality"] = true,
                    ["upgrade"] = "upgrade",
                    ["upgradeInfo"] = new JsonObject
                    {
                        ["migrationMarkdown"] = "migrate",
                        ["model"] = "gpt-6",
                        ["modelLink"] = "https://example.com/model",
                        ["upgradeCopy"] = "Upgrade now",
                    },
                    ["serviceTiers"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["id"] = "priority",
                            ["name"] = "Fast",
                            ["description"] = "Fastest inference with increased plan usage",
                        },
                    },
                },
            },
        });

        Assert.Equal("cursor-2", result.NextCursor);
        Assert.Single(result.Models);
        Assert.Equal("model-1", result.Models[0].Id);
        Assert.Equal("gpt-5", result.Models[0].Model);
        Assert.Equal(CodexReasoningEffort.High, result.Models[0].DefaultReasoningEffort);
        Assert.Equal("Upgrade required", result.Models[0].AvailabilityNux!.Message);
        Assert.Equal(CodexInputModality.Text, result.Models[0].InputModalities![0]);
        Assert.Equal(CodexInputModality.Image, result.Models[0].InputModalities![1]);
        Assert.Equal("fast", Assert.Single(result.Models[0].AdditionalSpeedTiers));
        CodexReasoningEffortOption reasoningEffort = Assert.Single(result.Models[0].SupportedReasoningEfforts);
        Assert.Equal("Balanced", reasoningEffort.Description);
        Assert.Equal(CodexReasoningEffort.Medium, reasoningEffort.ReasoningEffort);
        Assert.True(result.Models[0].SupportsPersonality);
        Assert.Equal("upgrade", result.Models[0].Upgrade);
        Assert.Equal("Upgrade now", result.Models[0].UpgradeInfo!.UpgradeCopy);
        CodexModelServiceTier serviceTier = Assert.Single(result.Models[0].ServiceTiers);
        Assert.Equal("priority", serviceTier.Id);
        Assert.Equal("Fast", serviceTier.Name);
        Assert.Equal("Fastest inference with increased plan usage", serviceTier.Description);
    }

    [Fact]
    public void ParseModelListResult_ParsesDataAndCursor()
    {
        CodexModelListResult result = CodexProtocol.ParseModelListResult(new JsonObject
        {
            ["nextCursor"] = "cursor-3",
            ["data"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "model-2",
                    ["model"] = "gpt-5-mini",
                    ["displayName"] = "GPT-5 Mini",
                    ["description"] = "Trace model",
                    ["hidden"] = true,
                    ["isDefault"] = false,
                    ["defaultReasoningEffort"] = "unexpected",
                },
            },
        });

        Assert.Equal("cursor-3", result.NextCursor);
        Assert.Single(result.Models);
        Assert.Equal("model-2", result.Models[0].Id);
        Assert.Equal(CodexReasoningEffort.Medium, result.Models[0].DefaultReasoningEffort);
    }

    [Fact]
    public void ParseThreadListResult_ParsesThreadsAndCursor()
    {
        CodexThreadListResult result = CodexProtocol.ParseThreadListResult(new JsonObject
        {
            ["backwardsCursor"] = "previous-thread-cursor",
            ["nextCursor"] = "thread-cursor",
            ["threads"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "thread-1",
                    ["preview"] = "Preview",
                    ["status"] = new JsonObject { ["type"] = "unexpected" },
                    ["modelProvider"] = "openai",
                    ["createdAt"] = 1,
                    ["updatedAt"] = 2,
                    ["ephemeral"] = false,
                    ["cliVersion"] = "1.2.3",
                    ["cwd"] = "/work",
                    ["sessionId"] = "session-1",
                    ["source"] = new JsonObject
                    {
                        ["custom"] = "integration-test",
                    },
                    ["threadSource"] = "subagent",
                    ["sessionStartSource"] = "startup",
                },
            },
        });

        Assert.Equal("previous-thread-cursor", result.BackwardsCursor);
        Assert.Equal("thread-cursor", result.NextCursor);
        Assert.Single(result.Threads);
        Assert.Equal("thread-1", result.Threads[0].Id);
        Assert.Equal("/work", result.Threads[0].Cwd);
        Assert.Equal("session-1", result.Threads[0].SessionId);
        Assert.IsType<CodexCustomSessionSource>(result.Threads[0].Source);
        Assert.Equal(CodexThreadSource.Subagent, result.Threads[0].ThreadSource);
        Assert.Equal(CodexThreadStartSource.Startup, result.Threads[0].SessionStartSource);
        Assert.IsType<CodexNotLoadedThreadStatus>(result.Threads[0].Status);
    }

    [Fact]
    public void ParseThreadHandleState_UsesPayloadWhenThreadObjectMissing()
    {
        CodexThreadHandleState handle = CodexProtocol.ParseThreadHandleState(new JsonObject
        {
            ["id"] = "thread-2",
            ["name"] = "fallback thread",
            ["preview"] = "Preview",
            ["status"] = new JsonObject { ["type"] = "unexpected" },
            ["modelProvider"] = "openai",
            ["createdAt"] = 1,
            ["updatedAt"] = 2,
            ["ephemeral"] = false,
            ["cliVersion"] = "1.2.3",
        }, defaults: null);

        Assert.Equal("thread-2", handle.Snapshot.Id);
        Assert.Equal("fallback thread", handle.Snapshot.Name);
        Assert.IsType<CodexNotLoadedThreadStatus>(handle.Snapshot.Status);
        Assert.Null(handle.Defaults);
    }

    [Fact]
    public void NormalizeMetadata_InfersServerIdentityFromUserAgent()
    {
        CodexRuntimeMetadata normalized = CodexProtocol.NormalizeMetadata(new CodexRuntimeMetadata
        {
            UserAgent = "codex-app-server/1.2.3",
            PlatformFamily = "Unix",
            PlatformOs = "Linux",
        });

        Assert.Equal("codex-app-server", normalized.ServerInfo!.Name);
        Assert.Equal("1.2.3", normalized.ServerInfo.Version);
    }

    private static JsonObject CreateThreadGoal(string threadId, string objective, string status = "active")
        => new()
        {
            ["threadId"] = threadId,
            ["objective"] = objective,
            ["status"] = status,
            ["tokenBudget"] = 1200L,
            ["tokensUsed"] = 42L,
            ["timeUsedSeconds"] = 60L,
            ["createdAt"] = 1778076000L,
            ["updatedAt"] = 1778076060L,
        };
}
