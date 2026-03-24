using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Incursa.OpenAI.Codex.Extensions;

// Traceability: REQ-CODEX-SDK-CATALOG-0309.

public static class CodexServiceCollectionExtensions
{
    public static IServiceCollection AddCodex(this IServiceCollection services)
        => AddCodex(services, configure: null);

    public static IServiceCollection AddCodex(
        this IServiceCollection services,
        Action<CodexClientOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<CodexClientOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton(sp => new CodexClient(sp.GetRequiredService<IOptions<CodexClientOptions>>().Value));
        return services;
    }

    public static IServiceCollection AddCodex(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CodexClientOptions>().Bind(configuration);
        services.TryAddSingleton(sp => new CodexClient(sp.GetRequiredService<IOptions<CodexClientOptions>>().Value));
        return services;
    }
}


