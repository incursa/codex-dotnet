using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;

namespace Incursa.OpenAI.Codex.Benchmarks;

[MemoryDiagnoser]
public class CodexProtocolBenchmarks
{
    private CodexThreadOptions threadOptions = new();
    private CodexTurnOptions turnOptions = new();
    private CodexConfigObject config = new();
    private IReadOnlyList<CodexInputItem> turnInput = [];
    private JsonObject turnEvent = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        config = new CodexConfigObject
        {
            Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
            {
                ["service"] = new CodexConfigStringValue("codex"),
                ["sandbox"] = new CodexConfigObject
                {
                    Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                    {
                        ["workspace_write"] = new CodexConfigObject
                        {
                            Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                            {
                                ["network_access"] = new CodexConfigBooleanValue(true),
                            },
                        },
                    },
                },
            },
        };

        threadOptions = new CodexThreadOptions
        {
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
            ApprovalsReviewer = CodexApprovalsReviewer.GuardianSubAgent,
            BaseInstructions = "Base instructions",
            Config = config,
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
            WorkingDirectory = "/work",
            ServiceName = "codex-service",
            ModelReasoningEffort = CodexReasoningEffort.High,
            NetworkAccessEnabled = true,
            WebSearchMode = CodexWebSearchMode.Live,
            WebSearchEnabled = true,
            SkipGitRepoCheck = true,
            AdditionalDirectories = ["/extra/one", "/extra/two"],
        };

        turnOptions = new CodexTurnOptions
        {
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnFailure),
            ApprovalsReviewer = CodexApprovalsReviewer.GuardianSubAgent,
            Effort = CodexReasoningEffort.Medium,
            Model = "gpt-5-mini",
            OutputSchema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["summary"] = new JsonObject
                    {
                        ["type"] = "string",
                    },
                },
            },
            Personality = CodexPersonality.Friendly,
            SandboxPolicy = new CodexReadOnlySandboxPolicy
            {
                NetworkAccess = true,
            },
            ServiceTier = CodexServiceTier.Fast,
            Summary = CodexReasoningSummary.Concise,
            WorkingDirectory = "/turn",
        };

        turnInput =
        [
            new CodexTextInput { Text = "hello codex" },
            new CodexImageInput { Url = "https://example.com/image.png" },
            new CodexLocalImageInput { Path = "/images/trace.png" },
            new CodexSkillInput { Name = "skill", Path = "/skills/skill.md" },
            new CodexMentionInput { Name = "mention", Path = "/mentions/mention.md" },
        ];

        turnEvent = new JsonObject
        {
            ["type"] = "turn.completed",
            ["turn"] = new JsonObject
            {
                ["id"] = "turn-1",
                ["status"] = "completed",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "agentMessage",
                        ["id"] = "message-1",
                        ["phase"] = "finalAnswer",
                        ["text"] = "done",
                    },
                },
                ["usage"] = new JsonObject
                {
                    ["last"] = new JsonObject
                    {
                        ["totalTokens"] = 10,
                    },
                    ["total"] = new JsonObject
                    {
                        ["totalTokens"] = 10,
                    },
                },
            },
        };
    }

    [Benchmark]
    public JsonObject BuildThreadStartParams()
        => CodexProtocol.BuildThreadStartParams(threadOptions);

    [Benchmark]
    public JsonObject BuildTurnStartParams()
        => CodexProtocol.BuildTurnStartParams("thread-1", turnInput, turnOptions);

    [Benchmark]
    public CodexThreadEvent ParseTurnEvent()
        => CodexProtocol.ParseThreadEvent(turnEvent);

    [Benchmark]
    public IReadOnlyList<string> FlattenConfigOverrides()
        => CodexConfigSerialization.FlattenConfigOverrides(config);
}
