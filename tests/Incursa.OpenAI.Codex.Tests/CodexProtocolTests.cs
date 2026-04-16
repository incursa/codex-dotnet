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
            Limit = 50,
            ModelProviders = ["openai", "anthropic"],
            SearchTerm = "review",
            SortKey = CodexThreadSortKey.UpdatedAt,
            SourceKinds = [CodexThreadSourceKind.Exec, CodexThreadSourceKind.SubAgentThreadSpawn],
        });

        Assert.True(payload["archived"]!.GetValue<bool>());
        Assert.Equal("cursor-123", payload["cursor"]!.GetValue<string>());
        Assert.Equal("/work", payload["cwd"]!.GetValue<string>());
        Assert.Equal(50, payload["limit"]!.GetValue<int>());
        Assert.Equal("review", payload["searchTerm"]!.GetValue<string>());
        Assert.Equal("updated_at", payload["sortKey"]!.GetValue<string>());

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

        JsonObject fork = CodexProtocol.BuildThreadForkParams("thread-2", new CodexThreadForkOptions
        {
            WorkingDirectory = "/fork",
        });
        Assert.Equal("thread-2", fork["threadId"]!.GetValue<string>());
        Assert.Equal("/fork", fork["cwd"]!.GetValue<string>());

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
        JsonObject models = CodexProtocol.BuildModelListParams(new CodexModelListOptions
        {
            IncludeHidden = true,
        });
        JsonObject steer = CodexProtocol.BuildTurnSteerParams(
            "thread-8",
            "turn-9",
            [new CodexTextInput { Text = "steer" }]);
        JsonObject interrupt = CodexProtocol.BuildTurnInterruptParams("thread-10", "turn-11");

        Assert.Equal("thread-4", archive["threadId"]!.GetValue<string>());
        Assert.Equal("thread-5", unarchive["threadId"]!.GetValue<string>());
        Assert.Equal("thread-6", name["threadId"]!.GetValue<string>());
        Assert.Equal("renamed", name["name"]!.GetValue<string>());
        Assert.Equal("thread-7", compact["threadId"]!.GetValue<string>());
        Assert.True(models["includeHidden"]!.GetValue<bool>());
        Assert.Equal("thread-8", steer["threadId"]!.GetValue<string>());
        Assert.Equal("turn-9", steer["expectedTurnId"]!.GetValue<string>());
        Assert.Equal("thread-10", interrupt["threadId"]!.GetValue<string>());
        Assert.Equal("turn-11", interrupt["turnId"]!.GetValue<string>());
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
        Assert.Equal("fast", payload["serviceTier"]!.GetValue<string>());
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
                },
            },
        });

        Assert.Equal("cursor-2", result.NextCursor);
        Assert.Single(result.Models);
        Assert.Equal("model-1", result.Models[0].Id);
        Assert.Equal("gpt-5", result.Models[0].Model);
        Assert.Equal(CodexReasoningEffort.High, result.Models[0].DefaultReasoningEffort);
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
                },
            },
        });

        Assert.Equal("thread-cursor", result.NextCursor);
        Assert.Single(result.Threads);
        Assert.Equal("thread-1", result.Threads[0].Id);
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
}
