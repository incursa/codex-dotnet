# Incursa.OpenAI.Codex.Extensions

Optional `IServiceCollection` registration for `Incursa.OpenAI.Codex`. The runtime package still launches the local `codex` executable as a subprocess, so the machine running your app must already have Codex installed and authenticated.

This package targets .NET 10 (`net10.0`) and is installed alongside the core
runtime package:

```powershell
dotnet add package Incursa.OpenAI.Codex.Extensions
dotnet add package Incursa.OpenAI.Codex
```

Install and authenticate the local Codex CLI separately. The DI package only
binds options and registers the client; it does not provide a hosted runtime.

This package exposes [`CodexServiceCollectionExtensions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex.Extensions/CodexServiceCollectionExtensions.cs):

- [`services.AddCodex()`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex.Extensions/CodexServiceCollectionExtensions.cs)
- [`services.AddCodex(Action<CodexClientOptions>)`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex.Extensions/CodexServiceCollectionExtensions.cs)
- [`services.AddCodex(IConfiguration)`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex.Extensions/CodexServiceCollectionExtensions.cs)

The core runtime still works without DI (`new CodexClient(...)`), via [`CodexClient`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CodexClient.cs).

If you want a no-throw preflight for the local executable, call `await client.IsCodexAvailableAsync()` before `InitializeAsync()` or any turn operation.

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

If your app already has a [`CodexClientOptions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs) configuration section, use [`AddCodex(IConfiguration)`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex.Extensions/CodexServiceCollectionExtensions.cs) to bind it directly.

## Package Boundary

- [`Incursa.OpenAI.Codex`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/README.md): runtime behavior and transport interaction
- [`Incursa.OpenAI.Codex.Extensions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex.Extensions/README.md): registration and binding helpers only

## License

Apache 2.0. See the repository root `LICENSE`.
