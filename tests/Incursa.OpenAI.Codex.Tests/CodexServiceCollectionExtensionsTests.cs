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
}


