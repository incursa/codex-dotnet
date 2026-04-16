using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Incursa.OpenAI.Codex.Extensions;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexServiceCollectionExtensionsTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-API-0205")]
    [Trait("Requirement", "REQ-CODEX-SDK-DI-0263")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0309")]
    [CoverageType(RequirementCoverageType.Positive)]
    public async Task AddCodex_RegistersOptionsAndSingletonClient()
    {
        IServiceCollection services = new ServiceCollection();
        IServiceCollection returned = services.AddCodex(options =>
        {
            options.ClientName = "TraceTest";
            options.BackendSelection = CodexBackendSelection.Exec;
        });

        Assert.Same(services, returned);

        await using ServiceProvider provider = services.BuildServiceProvider();
        CodexClient client = provider.GetRequiredService<CodexClient>();
        CodexClientOptions options = provider.GetRequiredService<IOptions<CodexClientOptions>>().Value;

        Assert.Equal("TraceTest", options.ClientName);
        Assert.Equal(CodexBackendSelection.Exec, client.Options.BackendSelection);
        Assert.Equal("TraceTest", client.Options.ClientName);
        Assert.Same(client, provider.GetRequiredService<CodexClient>());
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-DI-0263")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0309")]
    [CoverageType(RequirementCoverageType.Positive)]
    public async Task AddCodex_BindsConfigurationIntoOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ClientName"] = "TraceConfig",
                ["BackendSelection"] = "Exec",
                ["ApiKey"] = "config-api-key",
            })
            .Build();

        IServiceCollection services = new ServiceCollection();
        services.AddCodex(configuration);

        await using ServiceProvider provider = services.BuildServiceProvider();
        CodexClient client = provider.GetRequiredService<CodexClient>();
        CodexClientOptions options = provider.GetRequiredService<IOptions<CodexClientOptions>>().Value;

        Assert.Equal("TraceConfig", options.ClientName);
        Assert.Equal(CodexBackendSelection.Exec, options.BackendSelection);
        Assert.Equal("config-api-key", options.ApiKey);
        Assert.Equal("TraceConfig", client.Options.ClientName);
        Assert.Equal("config-api-key", client.Options.ApiKey);
        Assert.Same(client, provider.GetRequiredService<CodexClient>());
    }
}
