using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Incursa.OpenAI.Codex.Extensions;

// Traceability: REQ-CODEX-SDK-CATALOG-0309.

/// <summary>
/// Provides dependency-injection registration helpers for the Codex SDK.
/// </summary>
public static class CodexServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="CodexClient"/> with default options.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCodex(this IServiceCollection services)
        => AddCodex(services, configure: null);

    /// <summary>
    /// Registers <see cref="CodexClient"/> and applies programmatic option configuration.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configure">The callback used to configure <see cref="CodexClientOptions"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
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

    /// <summary>
    /// Registers <see cref="CodexClient"/> and binds options from configuration.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">The configuration section to bind to <see cref="CodexClientOptions"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
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

