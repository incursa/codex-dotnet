# Incursa.OpenAI.Codex.Extensions

Optional `IServiceCollection` registration for `Incursa.OpenAI.Codex`.

This package provides [`CodexServiceCollectionExtensions`](CodexServiceCollectionExtensions.cs):

- [`services.AddCodex()`](CodexServiceCollectionExtensions.cs)
- [`services.AddCodex(Action<CodexClientOptions>)`](CodexServiceCollectionExtensions.cs)
- [`services.AddCodex(IConfiguration)`](CodexServiceCollectionExtensions.cs)

The core runtime remains usable without DI (`new CodexClient(...)`), via [`CodexClient`](../Incursa.OpenAI.Codex/CodexClient.cs).

## Minimal DI Setup

```csharp
using Incursa.OpenAI.Codex;
using Incursa.OpenAI.Codex.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCodex(options =>
{
    options.BackendSelection = CodexBackendSelection.AppServer;
});

var app = builder.Build();

app.MapGet("/hello", async (CodexClient client) =>
{
    CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
    {
        SkipGitRepoCheck = true,
    });

    CodexRunResult result = await thread.RunAsync("Say hello from Codex in one sentence.");
    return result.FinalResponse;
});

app.Run();
```

## Configuration Binding

If your app already has a [`CodexClientOptions`](../Incursa.OpenAI.Codex/Options.cs) section, use [`AddCodex(IConfiguration)`](CodexServiceCollectionExtensions.cs) to bind it directly.

## Package Boundary

- [`Incursa.OpenAI.Codex`](../Incursa.OpenAI.Codex/README.md): runtime behavior and transport interaction
- [`Incursa.OpenAI.Codex.Extensions`](README.md): registration and binding helpers only

## License

Apache 2.0. See the repository root `LICENSE`.
