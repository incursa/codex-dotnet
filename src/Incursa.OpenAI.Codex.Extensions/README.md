# Incursa.OpenAI.Codex.Extensions

Optional `IServiceCollection` registration for `Incursa.OpenAI.Codex`.

This package provides `CodexServiceCollectionExtensions`:

- `services.AddCodex()`
- `services.AddCodex(Action<CodexClientOptions>)`
- `services.AddCodex(IConfiguration)`

The core runtime remains usable without DI (`new CodexClient(...)`).

## Package boundary

- `Incursa.OpenAI.Codex`: runtime behavior and transport interaction
- `Incursa.OpenAI.Codex.Extensions`: registration/binding helpers only
- Public API drift is governed by the snapshot baseline files in the package directories.

## DI sample

The runnable sample can create `CodexClient` through DI:

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode quickstart --use-di
```

## License

Apache 2.0. See the repository root `LICENSE`.
