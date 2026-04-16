using System.Text.Json.Nodes;
using SharpFuzz;

namespace Incursa.OpenAI.Codex.Fuzz;

public static class Program
{
    public static void Main(string[] args)
    {
        Fuzzer.OutOfProcess.Run(ConsumeInput);
    }

    private static void ConsumeInput(Stream stream)
    {
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        ReadOnlySpan<byte> packet = buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length));
        if (packet.IsEmpty)
        {
            return;
        }

        Random random = new(GetSeed(packet));

        for (int iteration = 0; iteration < 64; iteration++)
        {
            CodexThreadOptions threadOptions = BuildThreadOptions(random);
            CodexTurnOptions turnOptions = BuildTurnOptions(random);
            CodexConfigObject config = BuildConfig(random);
            IReadOnlyList<CodexInputItem> input = BuildInput(random);

            _ = CodexProtocol.BuildThreadStartParams(threadOptions);
            _ = CodexProtocol.BuildTurnStartParams(RandomThreadId(random), input, turnOptions);
            _ = CodexProtocol.BuildConfigPayload(config);
            _ = CodexConfigSerialization.FlattenConfigOverrides(config);
            _ = CodexProtocol.ParseThreadEvent(BuildThreadEvent(random));
        }
    }

    private static CodexThreadOptions BuildThreadOptions(Random random)
    {
        return new CodexThreadOptions
        {
            ApprovalPolicy = new CodexApprovalModePolicy(random.Next(2) == 0 ? CodexApprovalMode.OnRequest : CodexApprovalMode.OnFailure),
            ApprovalsReviewer = random.Next(2) == 0 ? CodexApprovalsReviewer.GuardianSubAgent : CodexApprovalsReviewer.User,
            BaseInstructions = RandomText(random, "base"),
            Config = BuildConfig(random),
            DeveloperInstructions = RandomText(random, "developer"),
            Ephemeral = random.Next(2) == 0,
            Model = RandomText(random, "model"),
            ModelProvider = RandomText(random, "provider"),
            Personality = random.Next(2) == 0 ? CodexPersonality.Friendly : CodexPersonality.Pragmatic,
            Sandbox = BuildSandbox(random),
            ServiceTier = random.Next(2) == 0 ? CodexServiceTier.Fast : CodexServiceTier.Flex,
            WorkingDirectory = RandomText(random, "workdir"),
            ServiceName = RandomText(random, "service"),
            ModelReasoningEffort = random.Next(2) == 0 ? CodexReasoningEffort.Medium : CodexReasoningEffort.High,
            NetworkAccessEnabled = random.Next(2) == 0,
            WebSearchMode = random.Next(3) switch
            {
                0 => CodexWebSearchMode.Disabled,
                1 => CodexWebSearchMode.Live,
                _ => CodexWebSearchMode.Cached,
            },
            WebSearchEnabled = random.Next(2) == 0,
            SkipGitRepoCheck = random.Next(2) == 0,
            AdditionalDirectories = [RandomText(random, "extra-1"), RandomText(random, "extra-2")],
        };
    }

    private static CodexTurnOptions BuildTurnOptions(Random random)
    {
        return new CodexTurnOptions
        {
            ApprovalPolicy = new CodexApprovalModePolicy(random.Next(2) == 0 ? CodexApprovalMode.OnRequest : CodexApprovalMode.OnFailure),
            ApprovalsReviewer = random.Next(2) == 0 ? CodexApprovalsReviewer.GuardianSubAgent : CodexApprovalsReviewer.User,
            Effort = random.Next(2) == 0 ? CodexReasoningEffort.Low : CodexReasoningEffort.High,
            Model = RandomText(random, "turn-model"),
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
            Personality = random.Next(2) == 0 ? CodexPersonality.Friendly : CodexPersonality.Pragmatic,
            SandboxPolicy = BuildSandbox(random),
            ServiceTier = random.Next(2) == 0 ? CodexServiceTier.Fast : CodexServiceTier.Flex,
            Summary = random.Next(2) == 0 ? CodexReasoningSummary.Concise : CodexReasoningSummary.Detailed,
            WorkingDirectory = RandomText(random, "turn-workdir"),
        };
    }

    private static CodexSandboxPolicy BuildSandbox(Random random)
    {
        return random.Next(3) switch
        {
            0 => new CodexDangerFullAccessSandboxPolicy(),
            1 => new CodexReadOnlySandboxPolicy
            {
                NetworkAccess = random.Next(2) == 0,
            },
            _ => new CodexWorkspaceWriteSandboxPolicy
            {
                ExcludeSlashTmp = random.Next(2) == 0,
                ExcludeTmpdirEnvVar = random.Next(2) == 0,
                NetworkAccess = random.Next(2) == 0,
            },
        };
    }

    private static IReadOnlyList<CodexInputItem> BuildInput(Random random)
    {
        return
        [
            new CodexTextInput { Text = RandomText(random, "text") },
            new CodexImageInput { Url = $"https://example.com/{RandomText(random, "image")}.png" },
            new CodexLocalImageInput { Path = RandomText(random, "path") },
            new CodexSkillInput { Name = RandomText(random, "skill"), Path = RandomText(random, "skill-path") },
            new CodexMentionInput { Name = RandomText(random, "mention"), Path = RandomText(random, "mention-path") },
        ];
    }

    private static CodexConfigObject BuildConfig(Random random)
    {
        return new CodexConfigObject
        {
            Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
            {
                ["service"] = new CodexConfigStringValue(RandomText(random, "value")),
                ["count"] = new CodexConfigNumberValue(random.Next(0, 1000)),
                ["enabled"] = new CodexConfigBooleanValue(random.Next(2) == 0),
                ["nested"] = new CodexConfigObject
                {
                    Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                    {
                        ["leaf"] = new CodexConfigArrayValue
                        {
                            Items =
                            [
                                new CodexConfigStringValue(RandomText(random, "item")),
                                new CodexConfigBooleanValue(random.Next(2) == 0),
                                new CodexConfigNumberValue(random.Next(0, 10)),
                            ],
                        },
                    },
                },
            },
        };
    }

    private static JsonObject BuildThreadEvent(Random random)
    {
        JsonObject payload = random.Next(5) switch
        {
            0 => new JsonObject
            {
                ["type"] = "thread.started",
                ["thread"] = BuildThreadSummary(random),
            },
            1 => new JsonObject
            {
                ["type"] = "turn.completed",
                ["turn"] = BuildTurnRecord(random, CodexTurnStatus.Completed),
            },
            2 => new JsonObject
            {
                ["type"] = "turn.failed",
                ["turn"] = BuildTurnRecord(random, CodexTurnStatus.Failed),
            },
            3 => new JsonObject
            {
                ["type"] = "item.completed",
                ["threadId"] = RandomText(random, "thread"),
                ["turnId"] = RandomText(random, "turn"),
                ["item"] = BuildThreadItem(random),
            },
            4 => new JsonObject
            {
                ["type"] = "error",
                ["threadId"] = RandomText(random, "thread"),
                ["turnId"] = RandomText(random, "turn"),
                ["error"] = new JsonObject
                {
                    ["message"] = RandomText(random, "error"),
                    ["additionalDetails"] = RandomText(random, "details"),
                },
            },
            _ => new JsonObject
            {
                ["type"] = "custom/runtime-event",
                ["note"] = RandomText(random, "custom"),
            },
        };

        if (random.Next(2) == 0)
        {
            return payload;
        }

        return new JsonObject
        {
            ["method"] = payload["type"]!.GetValue<string>(),
            ["params"] = payload,
        };
    }

    private static JsonObject BuildThreadSummary(Random random)
    {
        return new JsonObject
        {
            ["id"] = RandomText(random, "thread"),
            ["preview"] = RandomText(random, "preview"),
            ["status"] = new JsonObject
            {
                ["type"] = random.Next(3) switch
                {
                    0 => "idle",
                    1 => "active",
                    _ => "systemError",
                },
            },
            ["modelProvider"] = "openai",
            ["createdAt"] = 1,
            ["updatedAt"] = 2,
            ["ephemeral"] = random.Next(2) == 0,
            ["cliVersion"] = "1.2.3",
        };
    }

    private static JsonObject BuildTurnRecord(Random random, CodexTurnStatus status)
    {
        string statusValue = status switch
        {
            CodexTurnStatus.Completed => "completed",
            CodexTurnStatus.Interrupted => "interrupted",
            CodexTurnStatus.Failed => "failed",
            _ => "inProgress",
        };

        return new JsonObject
        {
            ["id"] = RandomText(random, "turn"),
            ["status"] = statusValue,
            ["items"] = new JsonArray
            {
                BuildThreadItem(random),
            },
            ["usage"] = new JsonObject
            {
                ["last"] = new JsonObject
                {
                    ["totalTokens"] = random.Next(1, 25),
                },
                ["total"] = new JsonObject
                {
                    ["totalTokens"] = random.Next(1, 25),
                },
            },
        };
    }

    private static JsonObject BuildThreadItem(Random random)
    {
        return random.Next(4) switch
        {
            0 => new JsonObject
            {
                ["type"] = "agentMessage",
                ["id"] = RandomText(random, "message"),
                ["phase"] = random.Next(2) == 0 ? "commentary" : "finalAnswer",
                ["text"] = RandomText(random, "message-text"),
            },
            1 => new JsonObject
            {
                ["type"] = "plan",
                ["id"] = RandomText(random, "plan"),
                ["text"] = RandomText(random, "plan-text"),
            },
            2 => new JsonObject
            {
                ["type"] = "reasoning",
                ["id"] = RandomText(random, "reasoning"),
                ["content"] = new JsonArray
                {
                    RandomText(random, "step-1"),
                    RandomText(random, "step-2"),
                },
                ["summary"] = new JsonArray
                {
                    RandomText(random, "summary"),
                },
            },
            _ => new JsonObject
            {
                ["type"] = "error",
                ["id"] = RandomText(random, "error"),
                ["message"] = RandomText(random, "message"),
            },
        };
    }

    private static string RandomThreadId(Random random)
        => random.Next(4) == 0 ? string.Empty : RandomText(random, "thread");

    private static string RandomText(Random random, string prefix)
        => $"{prefix}-{random.Next(1, 1000):D3}-{random.NextInt64():x}";

    private static int GetSeed(ReadOnlySpan<byte> data)
    {
        unchecked
        {
            int seed = 17;
            foreach (byte value in data)
            {
                seed = (seed * 31) + value;
            }

            return seed;
        }
    }
}
