# Incursa.OpenAI.Codex.Sample

This console sample walks through the main SDK flows and assumes the machine running it has Codex installed and authenticated locally:

- `quickstart`
- `streaming`
- `structured-output`
- `image-input`
- `error-handling`
- `turn-controls`

For a short explanation of each mode and the exact commands, see [`docs/sample-modes.md`](../../docs/sample-modes.md).

If you are deciding where this fits, use this SDK for a local Codex subprocess workflow. Use the OpenAI SDK for direct API calls, ChatKit for a hosted chat UI, and the Agents SDK for higher-level orchestration.

## Common Commands

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode quickstart --prompt "Summarize this repository."
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode streaming --prompt "List three files likely to contain option models."
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode structured-output --prompt "Return JSON with answer and confidence."
```

## Backend options

- Default backend: `AppServer`
- CLI-backed mode: `--backend exec`
- DI example: `--use-di`
- Both backends still use the local `codex` executable
