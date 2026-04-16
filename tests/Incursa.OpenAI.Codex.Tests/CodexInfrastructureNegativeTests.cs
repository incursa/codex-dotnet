using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexInfrastructureNegativeTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    [CoverageType(RequirementCoverageType.Negative)]
    public void ConfigSerialization_RejectsBlankOverrideKeys()
    {
        CodexConfigObject config = new()
        {
            Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
            {
                [""] = new CodexConfigStringValue("trace"),
            },
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            CodexConfigSerialization.FlattenConfigOverrides(config));

        Assert.Contains("non-empty strings", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    [CoverageType(RequirementCoverageType.Negative)]
    public void ConfigSerialization_RejectsNullJsonNodeValues()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            CodexConfigSerialization.ToTomlLiteral((JsonNode?)null, "root.value"));

        Assert.Contains("cannot be null", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [CoverageType(RequirementCoverageType.Negative)]
    public async Task ProcessLauncher_RejectsNullStartInfo()
    {
        ProcessCodexProcessLauncher launcher = new();

        await Assert.ThrowsAsync<ArgumentNullException>(() => launcher.StartAsync(null!, CancellationToken.None));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [CoverageType(RequirementCoverageType.Negative)]
    public void ProcessCodexProcess_RejectsNullProcess()
    {
        Assert.Throws<ArgumentNullException>(() => new ProcessCodexProcess(null!));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    [CoverageType(RequirementCoverageType.Negative)]
    public void ConfigSerialization_RejectsBlankJsonObjectKeys()
    {
        JsonObject value = new()
        {
            [""] = true,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            CodexConfigSerialization.ToTomlLiteral(value, "root.value"));

        Assert.Contains("non-empty strings", exception.Message, StringComparison.Ordinal);
    }
}
