# Incursa.OpenAI.Codex.Extensions

Optional `IServiceCollection` registration for `Incursa.OpenAI.Codex`.

This package provides `CodexServiceCollectionExtensions`:

- `services.AddCodex()`
- `services.AddCodex(Action<CodexClientOptions>)`
- `services.AddCodex(IConfiguration)`

The core runtime remains usable without DI (`new CodexClient(...)`).

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

If your app already has a `CodexClientOptions` section, use `AddCodex(IConfiguration)` to bind it directly.

## Package Boundary

- `Incursa.OpenAI.Codex`: runtime behavior and transport interaction
- `Incursa.OpenAI.Codex.Extensions`: registration and binding helpers only

## License

Apache 2.0. See the repository root `LICENSE`.
