# Incursa.OpenAI.Codex

Async-only Codex runtime for .NET. It wraps the local `codex` executable and starts it as a subprocess, so the machine running your app must already have Codex installed and authenticated. Any `ApiKey` or `BaseUrl` settings are forwarded to that subprocess; they do not replace the local Codex installation requirement.

This package is DI-agnostic and exposes the runtime API:

- [`CodexClient`](CodexClient.cs)
- [`CodexThread`](CodexClient.cs)
- [`CodexTurn`](CodexClient.cs)
- typed options, event, item, result, and exception models such as [`CodexClientOptions`](Options.cs), [`CodexThreadOptions`](Options.cs), [`CodexTurnOptions`](Options.cs), [`CodexInputItem`](ConversationTypes.cs), [`CodexThreadEvent`](ConversationTypes.cs), [`CodexThreadItem`](ConversationTypes.cs), [`CodexRunResult`](CoreTypes.cs), [`CodexThreadSnapshot`](CoreTypes.cs), [`CodexAccountRateLimitsResult`](CoreTypes.cs), [`CodexRuntimeCapabilities`](CoreTypes.cs), [`CodexRuntimeMetadata`](CoreTypes.cs), and [`CodexException`](Exceptions.cs)

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

[`CodexClient`](CodexClient.cs) is async-only. Dispose it with `await using`.

If you need DI registration, use [`Incursa.OpenAI.Codex.Extensions`](../Incursa.OpenAI.Codex.Extensions/README.md) and call [`AddCodex(...)`](../Incursa.OpenAI.Codex.Extensions/CodexServiceCollectionExtensions.cs).

## Backend Modes

The API supports both backend modes:

- [`AppServer`](Enums.cs) (`codex app-server --listen stdio://`) for the full JSON-RPC surface, thread lifecycle operations, thread goals, model listing, account rate-limit reads, and turn steering or interruption
- [`Exec`](Enums.cs) (`codex exec --experimental-json`) for the CLI-backed run and stream flow

Use [`AppServer`](Enums.cs) when you need long-lived conversations, [`CodexThread`](CodexClient.cs) management, or turn control. Use [`Exec`](Enums.cs) when you only need prompt-in, response-out behavior.

## Major API Surfaces

- [`CodexClient`](CodexClient.cs): the root entry point for runtime startup, thread management, model discovery, account rate-limit reads, and `IsCodexAvailableAsync()` for an executable preflight
- [`CodexThread`](CodexClient.cs): a stateful conversation handle with `RunAsync`, `RunStreamedAsync`, `StartTurnAsync`, `ReadAsync`, `SetNameAsync`, `CompactAsync`, `GetGoalAsync`, `SetGoalAsync`, `SetGoalStatusAsync`, `ClearGoalAsync`, `RollbackAsync`, `UnsubscribeAsync`, `UpdateMetadataAsync`, and `ShellCommandAsync`
- [`CodexTurn`](CodexClient.cs): a single-turn handle with `StreamAsync`, `StreamNormalizedAsync`, `RunAsync`, `RunToResultAsync`, `SteerAsync`, and `InterruptAsync`
- [`CodexClientOptions`](Options.cs): backend selection, executable path override, API key, configuration, environment, and approval handler
- [`CodexThreadOptions`](Options.cs), [`CodexThreadListOptions`](Options.cs), and [`CodexTurnOptions`](Options.cs): working directory, thread origin metadata, sandbox, approval, model, Fast mode service tier, output schema, sort, and list-filter settings
- [`CodexInputItem`](ConversationTypes.cs) and the typed input union for text, remote image, local image, skill, and mention inputs
- [`CodexThreadEvent`](ConversationTypes.cs), [`CodexThreadItem`](ConversationTypes.cs), [`CodexRunResult`](CoreTypes.cs), [`CodexTurnEvent`](TurnExecutionTypes.cs), [`CodexTurnResult`](TurnExecutionTypes.cs), [`CodexThreadGoal`](CoreTypes.cs), [`CodexThreadSnapshot`](CoreTypes.cs), [`CodexAccountRateLimitsResult`](CoreTypes.cs), [`CodexTurnPlanUpdatedEvent`](ConversationTypes.cs), [`CodexAccountRateLimitsUpdatedEvent`](ConversationTypes.cs), [`CodexRuntimeCapabilities`](CoreTypes.cs), [`CodexRuntimeMetadata`](CoreTypes.cs), and [`CodexException`](Exceptions.cs) for streamed data, results, and diagnostics. `CodexRunResult.FinalResponse` stays nullable for commentary-only turns.

Use `CodexTurn.StreamNormalizedAsync()` or `CodexTurn.RunToResultAsync()` for UI clients that must distinguish Codex completion from transport or delivery behavior. The detailed result exposes `TerminalEventSeen`, `TerminalEventType`, `TerminalState`, `FinalResponseText`, `FinalResponseSource`, and assistant output character counts so callers do not need to infer completion from silence.

## Sample

The runnable sample under `samples/Incursa.OpenAI.Codex.Sample` shows quickstart, streaming, structured output, image input, error handling, and turn controls.

## License

Apache 2.0. See the repository root `LICENSE`.
