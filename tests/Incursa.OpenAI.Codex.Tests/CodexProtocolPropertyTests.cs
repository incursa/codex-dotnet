using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Xunit;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexProtocolPropertyTests
{
    [Property]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0244")]
    [Trait("Category", "Property")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void BuildInputPayload_TextInput_PreservesText(NonEmptyString text)
    {
        JsonArray payload = CodexProtocol.BuildInputPayload([new CodexTextInput { Text = text.Get }]);

        Assert.Equal("text", payload[0]!["type"]!.GetValue<string>());
        Assert.Equal(text.Get, payload[0]!["text"]!.GetValue<string>());
    }

    [Property]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0244")]
    [Trait("Category", "Property")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void BuildInputPayload_SkillInput_PreservesNameAndPath(NonEmptyString name, NonEmptyString path)
    {
        JsonArray payload = CodexProtocol.BuildInputPayload([
            new CodexSkillInput
            {
                Name = name.Get,
                Path = path.Get,
            },
        ]);

        Assert.Equal("skill", payload[0]!["type"]!.GetValue<string>());
        Assert.Equal(name.Get, payload[0]!["name"]!.GetValue<string>());
        Assert.Equal(path.Get, payload[0]!["path"]!.GetValue<string>());
    }

    [Property]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    [Trait("Category", "Property")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void ConfigSerialization_StringValue_UsesJsonStringEncoding(NonEmptyString text)
    {
        string expected = JsonSerializer.Serialize(text.Get);

        string actual = CodexConfigSerialization.ToTomlLiteral(new CodexConfigStringValue(text.Get), "config.value");

        Assert.Equal(expected, actual);
    }

    [Property]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    [Trait("Category", "Property")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void ConfigSerialization_NumberValue_UsesInvariantCulture(int value)
    {
        string expected = value.ToString(CultureInfo.InvariantCulture);

        string actual = CodexConfigSerialization.ToTomlLiteral(new CodexConfigNumberValue(value), "config.value");

        Assert.Equal(expected, actual);
    }
}
