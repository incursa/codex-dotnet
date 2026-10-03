using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexSdkParitySurfaceTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0303")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void ServiceTierAdditionsPreservePublishedNumericValues()
    {
        Assert.Equal(0, (int)CodexServiceTier.Fast);
        Assert.Equal(1, (int)CodexServiceTier.Flex);
        Assert.NotEqual(0, (int)CodexServiceTier.Default);
        Assert.NotEqual(1, (int)CodexServiceTier.Default);
    }

    [Theory]
    [InlineData("0.150.0", false)]
    [InlineData("0.151.0", true)]
    [InlineData("0.160.0", true)]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0303")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void RuntimeCompatibilityRecognizesLatestTurnAndThreadFieldFloor(string version, bool compatible)
    {
        string? diagnostic = CodexClient.ValidateRuntimeCompatibility(
            version,
            ["per-turn service tier", "thread list section filter", "includeTurns"],
            requireCompatibleRuntime: false);

        if (compatible)
        {
            Assert.Null(diagnostic);
        }
        else
        {
            Assert.Contains("0.151.0", diagnostic, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0303")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void LatestExecAndTurnOptionsPreserveParityValues()
    {
        CodexClientOptions clientOptions = new()
        {
            RawConfigOverrides = ["custom.raw={\"enabled\"=true}"],
        };
        CodexThreadOptions threadOptions = new()
        {
            ThreadSource = CodexThreadSource.MemoryConsolidation,
        };
        CodexTurnOptions turnOptions = new()
        {
            CyberAccessProgram = CodexCyberAccessProgram.DaybreakRed,
            Effort = CodexReasoningEffort.Persistent,
            ServiceTierForTurn = CodexServiceTier.Default,
            TurnTrigger = "automation",
        };
        CodexThreadListOptions listOptions = new() { SectionId = "section-1" };

        Assert.Equal("custom.raw={\"enabled\"=true}", Assert.Single(clientOptions.RawConfigOverrides!));
        Assert.Equal(CodexThreadSource.MemoryConsolidation, threadOptions.ThreadSource);
        Assert.Equal(CodexCyberAccessProgram.DaybreakRed, turnOptions.CyberAccessProgram);
        Assert.Equal(CodexReasoningEffort.Persistent, turnOptions.Effort);
        Assert.Equal(CodexServiceTier.Default, turnOptions.ServiceTierForTurn);
        Assert.Equal("automation", turnOptions.TurnTrigger);
        Assert.Equal("section-1", listOptions.SectionId);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0306")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void ExternalMessageSupportsResponsesStructuredOutput()
    {
        CodexExternalMessageInput input = new()
        {
            ToolName = "notifications",
            Namespace = "slack",
            StructuredContent = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "input_text",
                    ["text"] = "message body",
                },
            },
        };

        Assert.Equal("notifications", input.ToolName);
        Assert.Equal("slack", input.Namespace);
        Assert.Equal("input_text", input.StructuredContent![0]!.AsObject()["type"]!.GetValue<string>());
    }
}
