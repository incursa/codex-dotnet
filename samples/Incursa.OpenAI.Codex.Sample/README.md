# Incursa.OpenAI.Codex.Sample

This console sample demonstrates the main SDK flows:

- `quickstart`
- `streaming`
- `structured-output`
- `image-input`
- `error-handling`
- `turn-controls`

For a short explanation of each mode and the exact commands, see [`docs/sample-modes.md`](../../docs/sample-modes.md).

## Common Commands

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode quickstart --prompt "Summarize this repository."
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode streaming --prompt "List three files likely to contain option models."
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode structured-output --prompt "Return JSON with answer and confidence."
```

## Backend

- Default backend: `AppServer`
- CLI-backed mode: `--backend exec`
- DI registration example: `--use-di`
