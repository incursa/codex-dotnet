# Incursa.OpenAI.Codex

Core async-only Codex runtime for .NET.

This package is DI-agnostic and exposes the runtime-facing API:

- [`CodexClient`](CodexClient.cs)
- [`CodexThread`](CodexClient.cs)
- [`CodexTurn`](CodexClient.cs)
- typed options, event, item, result, and exception models such as [`CodexClientOptions`](Options.cs), [`CodexThreadOptions`](Options.cs), [`CodexTurnOptions`](Options.cs), [`CodexInputItem`](ConversationTypes.cs), [`CodexThreadEvent`](ConversationTypes.cs), [`CodexThreadItem`](ConversationTypes.cs), [`CodexRunResult`](CoreTypes.cs), [`CodexThreadSnapshot`](CoreTypes.cs), [`CodexRuntimeCapabilities`](CoreTypes.cs), [`CodexRuntimeMetadata`](CoreTypes.cs), and [`CodexException`](Exceptions.cs)

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

[`CodexClient`](CodexClient.cs) is async-only. Dispose it with `await using`.

If you want DI registration, use [`Incursa.OpenAI.Codex.Extensions`](../Incursa.OpenAI.Codex.Extensions/README.md) and call [`AddCodex(...)`](../Incursa.OpenAI.Codex.Extensions/CodexServiceCollectionExtensions.cs).

## Backend Modes

The API supports both backend modes:

- [`AppServer`](Enums.cs) (`codex app-server --listen stdio://`) for the full JSON-RPC surface, thread lifecycle operations, model listing, and turn steering or interruption
- [`Exec`](Enums.cs) (`codex exec --experimental-json`) for the CLI-backed run and stream flow

Use [`AppServer`](Enums.cs) when you need long-lived conversations, [`CodexThread`](CodexClient.cs) management, or turn control. Use [`Exec`](Enums.cs) when you only need prompt-in, response-out behavior.

## Major API Surfaces

- [`CodexClient`](CodexClient.cs): the root entry point for runtime startup, thread management, and model discovery
- [`CodexThread`](CodexClient.cs): a stateful conversation handle with `RunAsync`, `RunStreamedAsync`, `StartTurnAsync`, `ReadAsync`, `SetNameAsync`, and `CompactAsync`
- [`CodexTurn`](CodexClient.cs): a single-turn handle with `StreamAsync`, `RunAsync`, `SteerAsync`, and `InterruptAsync`
- [`CodexClientOptions`](Options.cs): backend selection, executable path override, API key, configuration, environment, and approval handler
- [`CodexThreadOptions`](Options.cs) and [`CodexTurnOptions`](Options.cs): working directory, sandbox, approval, model, and output schema settings
- [`CodexInputItem`](ConversationTypes.cs) and the typed input union for text, remote image, local image, skill, and mention inputs
- [`CodexThreadEvent`](ConversationTypes.cs), [`CodexThreadItem`](ConversationTypes.cs), [`CodexRunResult`](CoreTypes.cs), [`CodexThreadSnapshot`](CoreTypes.cs), [`CodexRuntimeCapabilities`](CoreTypes.cs), [`CodexRuntimeMetadata`](CoreTypes.cs), and [`CodexException`](Exceptions.cs) for streamed data, results, and diagnostics

## Sample

The runnable sample under `samples/Incursa.OpenAI.Codex.Sample` shows quickstart, streaming, structured output, image input, error handling, and turn controls.

## License

Apache 2.0. See the repository root `LICENSE`.
