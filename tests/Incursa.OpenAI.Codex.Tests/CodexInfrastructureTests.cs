using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexInfrastructureTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    public void ConfigSerialization_FlattensNestedOverrides()
    {
        CodexConfigObject config = new()
        {
            Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
            {
                ["sandbox"] = new CodexConfigObject
                {
                    Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                    {
                        ["workspace_write"] = new CodexConfigObject
                        {
                            Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                            {
                                ["network_access"] = new CodexConfigBooleanValue(false),
                            },
                        },
                    },
                },
                ["empty"] = new CodexConfigObject(),
                ["service"] = new CodexConfigStringValue("alpha"),
                ["count"] = new CodexConfigNumberValue(1.5),
            },
        };

        IReadOnlyList<string> overrides = CodexConfigSerialization.FlattenConfigOverrides(config);

        Assert.Contains("sandbox.workspace_write.network_access=false", overrides);
        Assert.Contains("empty={}", overrides);
        Assert.Contains("service=\"alpha\"", overrides);
        Assert.Contains("count=1.5", overrides);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    public void ConfigSerialization_RejectsNonFiniteNumbers()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            CodexConfigSerialization.ToTomlLiteral(new CodexConfigNumberValue(double.NaN), "root.value"));

        Assert.Contains("finite number", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0245")]
    public async Task OutputSchemaFile_CreatesAndDeletesTempDirectory()
    {
        JsonObject schema = new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["name"] = new JsonObject { ["type"] = "string" },
            },
        };

        string? directoryPath;
        string? filePath;

        await using (CodexOutputSchemaFile schemaFile = await CodexOutputSchemaFile.CreateAsync(schema, CancellationToken.None))
        {
            directoryPath = schemaFile.DirectoryPath;
            filePath = schemaFile.FilePath;

            Assert.False(string.IsNullOrWhiteSpace(directoryPath));
            Assert.False(string.IsNullOrWhiteSpace(filePath));
            Assert.True(Directory.Exists(directoryPath));
            Assert.True(File.Exists(filePath));
        }

        Assert.NotNull(directoryPath);
        Assert.NotNull(filePath);
        Assert.False(Directory.Exists(directoryPath));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0236")]
    public void ExecutableResolver_BuildEnvironment_UsesExplicitMapWithoutAmbientLeak()
    {
        CodexClientOptions options = new()
        {
            Environment = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["CUSTOM_VAR"] = "custom-value",
            },
            ApiKey = "secret-key",
        };

        IReadOnlyDictionary<string, string> env = CodexExecutableResolver.BuildEnvironment(options);

        Assert.Equal("custom-value", env["CUSTOM_VAR"]);
        Assert.Equal("secret-key", env["CODEX_API_KEY"]);
        Assert.Equal("codex_sdk_dotnet", env["CODEX_INTERNAL_ORIGINATOR_OVERRIDE"]);
        Assert.DoesNotContain("PATH", env.Keys);
    }
}


