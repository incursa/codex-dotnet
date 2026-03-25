# Incursa.OpenAI.Codex

Core async-only Codex runtime for .NET.

This package is DI-agnostic and exposes the runtime-facing API:

- `CodexClient`
- `CodexThread`
- `CodexTurn`
- typed options, event, item, result, and exception models

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

`CodexClient` is async-only. Dispose it with `await using`.

If you want DI registration, use `Incursa.OpenAI.Codex.Extensions` and call `AddCodex(...)`.

## Backend Modes

The API supports both backend modes:

- `AppServer` (`codex app-server --listen stdio://`) for the full JSON-RPC surface, thread lifecycle operations, model listing, and turn steering or interruption
- `Exec` (`codex exec --experimental-json`) for the CLI-backed run and stream flow

Use `AppServer` when you need long-lived conversations, `CodexThread` management, or turn control. Use `Exec` when you only need prompt-in, response-out behavior.

## Major API Surfaces

- `CodexClient`: the root entry point for runtime startup, thread management, and model discovery
- `CodexThread`: a stateful conversation handle with `RunAsync`, `RunStreamedAsync`, `StartTurnAsync`, `ReadAsync`, `SetNameAsync`, and `CompactAsync`
- `CodexTurn`: a single-turn handle with `StreamAsync`, `RunAsync`, `SteerAsync`, and `InterruptAsync`
- `CodexClientOptions`: backend selection, executable path override, API key, configuration, environment, and approval handler
- `CodexThreadOptions` and `CodexTurnOptions`: working directory, sandbox, approval, model, and output schema settings
- `CodexInputItem` and the typed input union for text, remote image, local image, skill, and mention inputs
- `CodexThreadEvent`, `CodexThreadItem`, `CodexRunResult`, `CodexThreadSnapshot`, `CodexRuntimeCapabilities`, `CodexRuntimeMetadata`, and `CodexException` for streamed data, results, and diagnostics

## Sample

The runnable sample under `samples/Incursa.OpenAI.Codex.Sample` shows quickstart, streaming, structured output, image input, error handling, and turn controls.

## License

Apache 2.0. See the repository root `LICENSE`.
