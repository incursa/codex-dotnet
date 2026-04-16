# Incursa.OpenAI.Codex.Fuzz

This project contains the SharpFuzz harness for the JSON-heavy Codex serialization and event parsing surface.

## Purpose

- Stress request serialization, config flattening, and thread-event parsing with randomized inputs.
- Fail fast on unexpected exceptions in serialization or parser paths.

## Build

```bash
dotnet build fuzz/Incursa.OpenAI.Codex.Fuzz.csproj -c Release
```

## Tooling

Run `dotnet tool restore` from the repository root to make the local `sharpfuzz` command available through the `SharpFuzz.CommandLine` tool package.
