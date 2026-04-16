# Benchmarks

This directory contains permanent BenchmarkDotNet suites for `Incursa.OpenAI.Codex`.

## Included Suites

- `CodexProtocolBenchmarks`

## Run

```bash
dotnet run -c Release --project benchmarks/Incursa.OpenAI.Codex.Benchmarks.csproj -- --job Dry
```

Use `--filter` to narrow to a subset of benchmarks when iterating locally.
