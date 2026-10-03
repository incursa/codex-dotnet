using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexPublicWireParityTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0306")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void ExternalStructuredContentIsSentIntactAndCanBeReused()
    {
        JsonArray content = new(new JsonObject
        {
            ["type"] = "input_text",
            ["text"] = "External application result",
        });
        CodexExternalMessageInput input = new()
        {
            ToolName = "application",
            Namespace = "integration",
            Content = "legacy fallback must not replace structured content",
            StructuredContent = content,
        };

        JsonObject first = CodexProtocol.BuildTurnStartParams("thread-1", [input], null);
        JsonObject second = CodexProtocol.BuildTurnStartParams("thread-1", [input], null);

        Assert.Empty(first["input"]!.AsArray());
        Assert.Equal("application", first["toolOutput"]!["name"]!.GetValue<string>());
        Assert.Equal("integration", first["toolOutput"]!["namespace"]!.GetValue<string>());
        Assert.True(JsonNode.DeepEquals(content, first["toolOutput"]!["output"]));
        Assert.True(JsonNode.DeepEquals(content, second["toolOutput"]!["output"]));
        Assert.Null(content.Parent);
        first["toolOutput"]!["output"]![0]!["text"] = "Modified payload";
        Assert.Equal("External application result", content[0]!["text"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(CodexServiceTier.Fast, "priority")]
    [InlineData(CodexServiceTier.Flex, "flex")]
    [InlineData(CodexServiceTier.Default, "default")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0303")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void ThreadAndPerTurnTiersUseCanonicalRuntimeIdentifiers(CodexServiceTier tier, string wireValue)
    {
        JsonObject thread = CodexProtocol.BuildThreadStartParams(new CodexThreadOptions { ServiceTier = tier });
        JsonObject turn = CodexProtocol.BuildTurnStartParams("thread-1", [new CodexTextInput { Text = "hello" }],
            new CodexTurnOptions { ServiceTier = tier, ServiceTierForTurn = tier });

        Assert.Equal(wireValue, thread["serviceTier"]!.GetValue<string>());
        Assert.Equal(wireValue, turn["serviceTier"]!.GetValue<string>());
        Assert.Equal(wireValue, turn["serviceTierForTurn"]!.GetValue<string>());
    }
}
