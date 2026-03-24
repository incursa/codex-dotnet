# Incursa.OpenAI.Codex

Core async-only Codex runtime for .NET.

This package is DI-agnostic and exposes the runtime-facing API:

- `CodexClient`
- `CodexThread`
- `CodexTurn`
- typed options, event, item, result, and exception models

The API supports both backend modes:

- `AppServer` (`codex app-server`) for full JSON-RPC surface and turn controls
- `Exec` (`codex exec --experimental-json`) for CLI-backed run flows

## Testing

- The repository suite validates the core package through `dotnet test Incursa.OpenAI.Codex.slnx -v minimal`.
- Live smoke tests are opt-in with `CODEX_LIVE_TESTS=1`.
- Public API drift is guarded by `tests/Incursa.OpenAI.Codex.Tests/PublicApiSnapshotTests.cs` and the shipped baseline files.

## Sample usage

Use the runnable sample for quickstart, streaming, structured output, image input, error handling, and turn controls:

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode quickstart --prompt "Summarize this repository."
```

The sample supports both `--backend app-server` and `--backend exec`.

## Runtime version note

The repository is currently validated against `codex-cli 0.116.0`:

```powershell
codex --version
```

## License

Apache 2.0. See the repository root `LICENSE`.
