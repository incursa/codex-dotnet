# Incursa.OpenAI.Codex

Async-only Codex runtime for .NET. It wraps the local `codex` executable and starts it as a subprocess, so the machine running your app must already have Codex installed and authenticated. Any `ApiKey` or `BaseUrl` settings are forwarded to that subprocess; they do not replace the local Codex installation requirement.

## Installation and prerequisites

The package targets .NET 10 (`net10.0`). Install it into an application that
already has the matching local Codex CLI and an authenticated Codex account:

```powershell
dotnet add package Incursa.OpenAI.Codex
```

The SDK starts `codex` locally. Install and authenticate the CLI separately, or
set [`CodexClientOptions.CodexPathOverride`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs) when the executable is
not on `PATH`. `ApiKey` and `BaseUrl` are passed to the local process; they do
not turn this package into a direct hosted API client. Call
`IsCodexAvailableAsync()` for a no-throw executable check before initialization.

This package is DI-agnostic and exposes the runtime API:

- [`CodexClient`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CodexClient.cs)
- [`CodexThread`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CodexClient.cs)
- [`CodexTurn`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CodexClient.cs)
- typed options, event, item, result, and exception models such as [`CodexClientOptions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs), [`CodexPlanModeOptions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs), [`CodexThreadOptions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs), [`CodexTurnOptions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs), [`CodexInputItem`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexThreadEvent`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexThreadItem`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexRunResult`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexThreadSnapshot`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexAccountReadResult`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexAccountRateLimitsResult`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexRuntimeCapabilities`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexRuntimeMetadata`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), and [`CodexException`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Exceptions.cs)

## When To Use This Package

Use this package when you want a .NET wrapper around the local Codex CLI for prompt/response flows, stateful threads, or turn-level control.

- Use the OpenAI SDK when you want direct API access from .NET.
- Use ChatKit when you want a hosted chat UI surface.
- Use the Agents SDK when you want higher-level agent orchestration.
- Use this package when you specifically want Codex-backed workflows driven from a local Codex install.
- If you want a no-throw preflight for the local executable, call `await client.IsCodexAvailableAsync()` before `InitializeAsync()` or any turn operation.

## Hello World

The smallest useful call starts a thread, sends one prompt, and prints the final response:

```csharp
using Incursa.OpenAI.Codex;

await using var client = new CodexClient();

CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
{
    SkipGitRepoCheck = true,
});

CodexRunResult result = await thread.RunAsync("Say hello from Codex in one sentence.");
Console.WriteLine(result.FinalResponse);
```

`CodexRunResult.FinalResponse` can be `null` when a turn completes with commentary only and never produces a final-answer or phase-less assistant message.

[`CodexClient`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CodexClient.cs) is async-only. Dispose it with `await using`.

If you need DI registration, use [`Incursa.OpenAI.Codex.Extensions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex.Extensions/README.md) and call [`AddCodex(...)`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex.Extensions/CodexServiceCollectionExtensions.cs).

## Backend Modes

The API supports both backend modes:

- [`AppServer`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Enums.cs) (`codex app-server --listen stdio://`) for the full JSON-RPC surface, thread lifecycle operations, thread goals, model listing, account login/read/logout, account rate-limit reads, and turn steering or interruption
- [`Exec`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Enums.cs) (`codex exec --experimental-json`) for the CLI-backed run and stream flow

Use [`AppServer`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Enums.cs) when you need long-lived conversations, [`CodexThread`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CodexClient.cs) management, or turn control. Use [`Exec`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Enums.cs) when you only need prompt-in, response-out behavior.

The app-server v2 thread-start request uses the current wire shapes: a simple
approval mode is a string such as `"on-request"`, and a granular policy uses
snake_case keys such as `mcp_elicitations` and `request_permissions`. Thread
sandbox access is expressed as `"read-only"`, `"workspace-write"`, or
`"danger-full-access"`. Workspace network access, additional directories, and
writable roots are carried through the `config.sandbox_workspace_write`
configuration object. External sandbox policies remain turn-level only. Use
[`CodexTurnOptions.SandboxPolicy`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs) when a turn needs the richer
legacy policy shape.

For new app-server integrations, prefer `Never`, `OnRequest`, or `Untrusted`
through [`CodexApprovalModePolicy`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs). `OnFailure` remains in the
public enum for source compatibility, but current app-server v2 rejects it.
The exec backend can pass `on-failure` through its CLI configuration when the
installed Codex runtime accepts that value.

## Major API Surfaces

- [`CodexClient`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CodexClient.cs): the root entry point for runtime startup, thread management, model discovery, client-wide raw event observation, account login/read/logout, account rate-limit reads, and `IsCodexAvailableAsync()` for an executable preflight
- [`CodexThread`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CodexClient.cs): a stateful conversation handle with `RunAsync`, `RunStreamedAsync`, `StartTurnAsync`, `ReadAsync`, `SetNameAsync`, `CompactAsync`, `GetGoalAsync`, `SetGoalAsync`, `SetGoalStatusAsync`, `ClearGoalAsync`, `RollbackAsync`, `UnsubscribeAsync`, `UpdateMetadataAsync`, and `ShellCommandAsync`
- [`CodexTurn`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CodexClient.cs): a single-turn handle with `StreamAsync`, `StreamNormalizedAsync`, `ObserveEventsAsync`, `ObserveNormalizedEventsAsync`, `RunAsync`, `RunToResultAsync`, `SteerAsync`, and `InterruptAsync`
- [`CodexClientOptions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs): backend selection, executable path override, API key, configuration, plan-mode defaults, environment, and approval handler
- [`CodexThreadOptions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs), [`CodexThreadListOptions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs), and [`CodexTurnOptions`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs): working directory, thread origin metadata, sandbox, approval, model, Fast mode service tier, output schema, sort, and list-filter settings
- `CodexServiceTier.Fast` is the public name for the current `priority` wire value in both thread-level and per-turn service-tier fields.
- [`CodexInputItem`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/ConversationTypes.cs) and the typed input union for text, remote image, local image, skill, mention, and provenance-carrying external-message inputs
- [`CodexThreadEvent`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexThreadItem`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexRunResult`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexTurnEvent`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/TurnExecutionTypes.cs), [`CodexTurnResult`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/TurnExecutionTypes.cs), [`CodexThreadGoal`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexThreadSnapshot`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexAccountReadResult`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexAccountRateLimitsResult`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexTurnPlanUpdatedEvent`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexAccountRateLimitsUpdatedEvent`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexRuntimeCapabilities`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexRuntimeMetadata`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CoreTypes.cs), and [`CodexException`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Exceptions.cs) for streamed data, results, and diagnostics. `CodexRunResult.FinalResponse` stays nullable for commentary-only turns.

Use `CodexClient.ObserveEventsAsync()` as the exhaustive raw event channel across the client. Use `CodexTurn.StreamNormalizedAsync()`, `CodexTurn.ObserveNormalizedEventsAsync()`, or `CodexTurn.RunToResultAsync()` for UI clients that must distinguish Codex completion from transport or delivery behavior. `CodexTurn.ObserveEventsAsync()` and `CodexTurn.ObserveNormalizedEventsAsync()` expose turn-scoped observable streams that fan out from one underlying Codex reader and replay observed events to later subscribers. Consumers can add `System.Reactive` in their own app when they want Rx operators over these `IObservable<T>` surfaces; the core package stays dependency-free. The detailed result exposes `TerminalEventSeen`, `TerminalEventType`, `TerminalState`, `FinalResponseText`, `FinalResponseSource`, and assistant output character counts so callers do not need to infer completion from silence.

### External messages and current option coverage

Use [`CodexExternalMessageInput`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/ConversationTypes.cs) when relaying content
from another tool or application. It keeps the tool name, namespace, and content
as an explicit external-message item rather than flattening that content into a
user prompt:

```csharp
CodexRunResult result = await thread.RunAsync(
    [new CodexExternalMessageInput
    {
        ToolName = "work-tracker",
        Namespace = "incursa",
        Content = "Ticket INC-42 is ready for review.",
    }]);
```

`CodexConfigObject` supports nested configuration values and serializes them to
the CLI's dotted config overrides. Use `CodexClientOptions.RawConfigOverrides`
for raw CLI overrides. `CodexReasoningEffort.Max`, `Ultra`, and `Persistent`,
per-thread `ServiceTier`, `CodexTurnOptions.ServiceTierForTurn`,
`TurnTrigger`, `CyberAccessProgram`, `CodexThreadListOptions.SectionId`, and
`CodexThreadOptions.IncludeTurns` are available. The parity
review records current differences from the upstream Python and TypeScript
packages in [`quality/upstream-parity-gaps.md`](https://github.com/incursa/codex-dotnet/blob/main/quality/upstream-parity-gaps.md).

For the exec backend, configuration is applied in this order: client structured
config, client raw overrides, thread structured config, then typed options.
Later values win when they address the same key. Raw overrides are passed as
literal CLI `--config` values after structured client configuration.
The remaining limitation is the breadth of the generated low-level schema;
personality fields are retained as compatibility metadata and should not be
used to select model tone.

Features introduced by newer Codex runtimes are checked against the app-server
version when the SDK receives runtime metadata. Set
[`CodexClientOptions.RequireCompatibleRuntime`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/Options.cs) to `true` to turn an
unknown or older runtime into an exception; when it is `false`, inspect
[`CodexClient.RuntimeCompatibilityDiagnostic`](https://github.com/incursa/codex-dotnet/blob/main/src/Incursa.OpenAI.Codex/CodexClient.cs) after a request
that uses a gated feature.

## Sample

The runnable sample under `samples/Incursa.OpenAI.Codex.Sample` shows quickstart, streaming, structured output, image input, error handling, and turn controls.

## License

Apache 2.0. See the repository root `LICENSE`.
