# Incursa.OpenAI.Codex

`Incursa.OpenAI.Codex` is a .NET 10 Codex runtime client with:

- an async-only core package (`Incursa.OpenAI.Codex`)
- an optional DI package (`Incursa.OpenAI.Codex.Extensions`)
- a runnable sample app that demonstrates the major runtime flows

## Repository layout

- `src/Incursa.OpenAI.Codex`: core runtime client, thread, turn, transport, and type models
- `src/Incursa.OpenAI.Codex.Extensions`: `IServiceCollection` registration helpers
- `samples/Incursa.OpenAI.Codex.Sample`: one console sample with mode-based scenarios
- `tests/Incursa.OpenAI.Codex.Tests`: non-live and live-gated verification tests
- `NOTICE.md`: third-party package notice inventory for the current solution
- `LICENSE`: Apache 2.0 license text
- `specs/requirements/codex-sdk`: requirements and traceability matrix
- `specs/verification/codex-sdk`: verification artifacts

## Runtime prerequisites

- .NET SDK from `global.json` (`10.0.200`)
- local `codex` executable on `PATH` or `CodexPathOverride`
- `CODEX_API_KEY` in environment or `--api-key` in sample usage

Runtime version pin:

- The repository is currently validated against `codex-cli 0.116.0`.
- Check with:

```powershell
codex --version
```

## Quickstart

Build and run tests:

```powershell
dotnet restore
dotnet test Incursa.OpenAI.Codex.slnx -v minimal
```

Run the sample:

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode quickstart --prompt "Summarize this repository."
```

Sample modes:

- `quickstart`
- `streaming`
- `structured-output`
- `image-input`
- `error-handling`
- `turn-controls`

Example invocations:

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode streaming --prompt "List three files likely to contain option models."
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode structured-output --prompt "Return JSON with answer and confidence."
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode image-input --image C:\path\to\image.png --prompt "Describe the image."
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode turn-controls --prompt "Draft a short release note." --interrupt
```

The sample defaults to the `app-server` backend. Use `--backend exec` to exercise CLI-backed behavior. Add `--use-di` to build the client through `AddCodex(...)`.

## Live tests

- Default test runs skip the live Codex integration subset.
- Enable with:

```powershell
$Env:CODEX_LIVE_TESTS = "1"
dotnet test tests/Incursa.OpenAI.Codex.Tests/Incursa.OpenAI.Codex.Tests.csproj --filter FullyQualifiedName~CodexLiveIntegrationTests -v minimal
```

## Traceability and verification

- `specs/requirements/codex-sdk/TRACEABILITY.md`
- `specs/verification/codex-sdk/VER-CODEX-SDK-0001.md`
- `specs/verification/codex-sdk/VER-CODEX-SDK-0002.md`
- `specs/verification/codex-sdk/VER-CODEX-SDK-0003.md`

The public API baseline files are part of the release gate:

- `src/Incursa.OpenAI.Codex/PublicAPI.Shipped.txt`
- `src/Incursa.OpenAI.Codex.Extensions/PublicAPI.Shipped.txt`

## GitHub Actions

- `CI` runs on pushes, pull requests, and manual dispatches. It restores, builds, tests, and packs both NuGet packages.
- `Publish NuGet Packages` runs on version tags (`v*.*.*`) and manual dispatches. Manual dispatches require an explicit version input. The workflow packs the packages and pushes them to NuGet when `NUGET_API_KEY` is configured.

## License

This repository is licensed under Apache 2.0. See `LICENSE`.
