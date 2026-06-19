---
title: "Maintainer Readiness"
---

# Maintainer Readiness

This guide is for maintainers and operators validating `codex-dotnet` from a local checkout. It does not rely on GitHub Actions as proof.

## Repository Purpose

`codex-dotnet` provides .NET packages for driving a local Codex runtime from .NET applications. The SDK owns typed .NET contracts, transport orchestration, result handling, and package integration around the local `codex` executable. It does not replace the OpenAI SDK for direct API calls, ChatKit for hosted chat UI, or the Agents SDK for higher-level agent orchestration.

The machine running an application that uses this SDK must have Codex installed and authenticated separately.

## Package and Service Boundaries

- `src/Incursa.OpenAI.Codex`: core package. It exposes `CodexClient`, `CodexThread`, `CodexTurn`, typed options, input records, event records, result records, runtime metadata, capability models, and Codex-specific exceptions.
- `src/Incursa.OpenAI.Codex.Extensions`: optional package. It contains `IServiceCollection` registration and configuration binding helpers only.
- `samples/Incursa.OpenAI.Codex.Sample`: runnable local sample app for direct client creation, DI creation, streaming, structured output, image input, error handling, and turn controls.
- `tests/Incursa.OpenAI.Codex.Tests`: xUnit suite for protocol mapping, transports, lifecycle behavior, public API snapshots, DI registration, diagnostics, sample behavior, property tests, and opt-in live Codex smoke tests.
- `fuzz`: SharpFuzz harness for JSON-heavy serialization and event parsing paths.
- `benchmarks`: BenchmarkDotNet suites for permanent protocol benchmarks.
- `specs`: repository-native requirements, architecture records, verification records, and traceability.
- `quality`: local quality intent plus upstream Python and TypeScript Codex SDK parity state.

The SDK is a local subprocess/client library. It does not provide a hosted service and does not own Codex account provisioning, Codex authentication, OpenAI billing, or remote runtime deployment.

## Architecture and Runtime Flow

The root runtime object is `CodexClient`. It owns backend selection, process or connection lifetime, runtime metadata, capability discovery, and disposal. Callers create or resume `CodexThread` instances from the client, then run work through thread-level helpers or explicit `CodexTurn` handles.

`AppServer` is the default backend. It launches `codex app-server --listen stdio://` and talks JSON-RPC over stdio. Use it for stateful thread lifecycle operations, model listing, account rate limits, turn steering, interruption, rollback, metadata updates, shell commands, goals, and realtime notifications.

`Exec` is the compatibility backend. It launches `codex exec --experimental-json` for run and streamed run flows. It does not support the full thread lifecycle or app-server-only operations. Capability checks should fail with typed SDK exceptions instead of leaking transport internals.

The high-level flow is:

1. Construct `CodexClientOptions`.
2. Create `CodexClient`.
3. Optionally call `InitializeAsync()` to force runtime startup and capability discovery.
4. Start or resume a `CodexThread`.
5. Run a prompt with `RunAsync`, consume raw events with `RunStreamedAsync`, or create a `CodexTurn` for streaming, observation, steering, interruption, or detailed turn results.
6. Dispose the client with `await using` or `DisposeAsync()` so the subprocess or connection is closed.

Architecture details are recorded in `specs/architecture/codex-sdk/ARC-CODEX-SDK-0001.md` and `specs/architecture/codex-sdk/ARC-CODEX-SDK-0002.md`.

## Observable and Event Surfaces

Use `CodexClient.ObserveEventsAsync()` when a client needs the exhaustive raw runtime event stream for the underlying Codex process or connection.

Use turn-scoped APIs when a caller needs one turn:

- `CodexTurn.StreamAsync()` yields raw `CodexThreadEvent` values for one active turn.
- `CodexTurn.StreamNormalizedAsync()` yields normalized `CodexTurnEvent` values for UI and operator display.
- `CodexTurn.ObserveEventsAsync()` exposes replayed turn-scoped raw events for multiple subscribers.
- `CodexTurn.ObserveNormalizedEventsAsync()` exposes replayed turn-scoped normalized events for multiple subscribers.
- `CodexTurn.RunToResultAsync()` returns `CodexTurnResult`, including terminal-state diagnostics.

The core package intentionally exposes `IObservable<T>` without taking a `System.Reactive` dependency. Applications can add Rx operators in their own projects.

When interpreting completion, prefer `CodexTurnResult.TerminalEventSeen`, `TerminalEventType`, `TerminalState`, `FinalResponseText`, and `FinalResponseSource` over silence or stream closure. `FinalResponseSource` records whether the final response came from a terminal event, a completed item, or accumulated assistant deltas.

## Minimal Usage

Install the core package in an application that already has a local Codex runtime available:

```powershell
dotnet add package Incursa.OpenAI.Codex
```

Minimal direct-client flow:

```csharp
await using var client = new CodexClient();
CodexThread thread = await client.StartThreadAsync();
CodexRunResult result = await thread.RunAsync("Summarize this repository in three bullets.");

Console.WriteLine(result.FinalResponse);
```

Run the sample app from this repository:

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --help
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode quickstart --prompt "Summarize this repository."
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode streaming --prompt "List the major package surfaces."
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode error-handling --backend exec --prompt "Explain capability differences."
```

See `docs/sample-modes.md` for every sample mode.

## Local Validation

Run these commands from the repository root:

```powershell
dotnet restore Incursa.OpenAI.Codex.slnx
dotnet build Incursa.OpenAI.Codex.slnx -c Release --no-restore
dotnet test Incursa.OpenAI.Codex.slnx -c Release --no-build -v minimal
dotnet pack src/Incursa.OpenAI.Codex/Incursa.OpenAI.Codex.csproj -c Release --no-build --output artifacts/packages /p:ContinuousIntegrationBuild=true
dotnet pack src/Incursa.OpenAI.Codex.Extensions/Incursa.OpenAI.Codex.Extensions.csproj -c Release --no-build --output artifacts/packages /p:ContinuousIntegrationBuild=true
git diff --check
```

Run focused tests while iterating:

```powershell
dotnet test tests/Incursa.OpenAI.Codex.Tests/Incursa.OpenAI.Codex.Tests.csproj -c Release --no-build --filter FullyQualifiedName~CodexTurnOutcomeTests
dotnet test tests/Incursa.OpenAI.Codex.Tests/Incursa.OpenAI.Codex.Tests.csproj -c Release --no-build --filter FullyQualifiedName~CodexAppServerTransportTests
dotnet test tests/Incursa.OpenAI.Codex.Tests/Incursa.OpenAI.Codex.Tests.csproj -c Release --no-build --filter FullyQualifiedName~PublicApiSnapshotTests
```

Live Codex tests are opt-in because they require the local Codex executable and local authentication:

```powershell
$env:CODEX_LIVE_TESTS = '1'
dotnet test tests/Incursa.OpenAI.Codex.Tests/Incursa.OpenAI.Codex.Tests.csproj -c Release --filter FullyQualifiedName~CodexLiveIntegrationTests
Remove-Item Env:CODEX_LIVE_TESTS
```

Use local tools for optional fuzzing or mutation work:

```powershell
dotnet tool restore
dotnet build fuzz/Incursa.OpenAI.Codex.Fuzz.csproj -c Release
dotnet run -c Release --project benchmarks/Incursa.OpenAI.Codex.Benchmarks.csproj -- --job Dry
```

## Release and Versioning

The package version is stored in `Directory.Build.props`. Releases should be cut with `scripts/release.ps1` instead of hand-editing the version or tag.

The release script:

1. Finds the latest `v*.*.*` tag.
2. Compares `PublicAPI.Shipped.txt` baselines against that tag.
3. Requires `PublicAPI.Unshipped.txt` files to be empty.
4. Chooses a major, minor, or patch bump from public API changes.
5. Updates `Directory.Build.props`.
6. Runs the Release test suite.
7. Commits, tags, and optionally pushes.

If public API changes are intentional, update the public API baselines first:

```powershell
$env:UPDATE_PUBLIC_API_BASELINES = '1'
dotnet test tests/Incursa.OpenAI.Codex.Tests/Incursa.OpenAI.Codex.Tests.csproj -c Release --filter FullyQualifiedName~PublicApiSnapshotTests
Remove-Item Env:UPDATE_PUBLIC_API_BASELINES
```

Package publishing is handled from tags by `.github/workflows/publish-nuget-packages.yml`, but local readiness should be proven with build, test, and pack commands before relying on any workflow.

## Security and Credentials

Treat the host application as the access boundary. The SDK can pass local prompts, repository paths, command output, environment-derived settings, and streamed Codex events through application code.

Required practices:

- Install and authenticate Codex for the account and permission scope intended for the host application.
- Use the narrowest practical working directory for automated runs.
- Review sandbox and approval settings before running against sensitive repositories.
- Keep `CodexClientOptions.ApiKey`, environment variables, local Codex auth state, private transcripts, local repository paths, and sample output out of source control.
- Do not log raw events, prompts, patches, command output, screenshots, or transcripts from private repositories unless the destination is approved for that data.
- Use `SECURITY.md` for vulnerability reporting instructions.

## Traceability and Parity

Requirements live under `specs/requirements/codex-sdk`. Architecture records live under `specs/architecture/codex-sdk`. Verification records live under `specs/verification/codex-sdk`. The primary requirement-to-code-and-test map is `specs/requirements/codex-sdk/TRACEABILITY.md`.

Upstream Python and TypeScript Codex SDK parity state is recorded in `quality/upstream-parity.json` and summarized in `quality/upstream-parity-gaps.md`. Rerun `scripts/Invoke-UpstreamParityReview.ps1` when upstream parity is the work item; do not treat an old parity file as proof that the current upstream head has no gaps.

## Readiness State

Stable surfaces:

- Core async client, thread, turn, typed input, typed event, result, metadata, capability, and exception models.
- App-server backend for full thread and turn-control flows.
- Exec backend for simpler run and streamed run flows.
- DI registration through the extensions package.
- Public API baseline governance and package metadata for NuGet packing.
- Repository-native requirements, architecture records, verification records, and traceability.

Known gaps and maintenance risks:

- Live behavior depends on the installed `codex` executable, local Codex authentication, local sandbox settings, and upstream runtime behavior.
- Live smoke tests are skipped unless `CODEX_LIVE_TESTS=1` is explicitly set.
- Upstream Codex SDK parity must be refreshed when Python or TypeScript SDK behavior changes.
- `Exec` intentionally lacks app-server-only lifecycle and control capabilities.
- `dotnet format Incursa.OpenAI.Codex.slnx --verify-no-changes --no-restore` reports broad existing analyzer and formatting findings across source and tests; treat formatter cleanup as a separate code-maintenance change, not as part of a documentation-only pass.
- Issue templates and a top-level `specs/README.md` are still missing from the broader open-source repository hygiene inventory.

Before a release candidate, maintainers should have a clean local build, test, pack, and `git diff --check` result, plus explicit notes about any skipped live Codex checks.
