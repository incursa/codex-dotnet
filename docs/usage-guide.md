# Usage Guide

`Incursa.OpenAI.Codex` is an async-only .NET client for the local Codex runtime. It launches the `codex` executable as a subprocess, so the machine running your app must already have Codex installed and authenticated.

Use this SDK when you want a C# wrapper around the local Codex CLI and its thread or turn APIs. Use the OpenAI SDK when you want direct API calls from .NET, ChatKit when you want a hosted chat UI surface, and the Agents SDK when you want higher-level agent orchestration.

It is built around three pieces:

- a root client such as [`CodexClient`](../src/Incursa.OpenAI.Codex/CodexClient.cs) for starting and managing Codex conversations
- stateful thread and turn handles such as [`CodexThread`](../src/Incursa.OpenAI.Codex/CodexClient.cs) and [`CodexTurn`](../src/Incursa.OpenAI.Codex/CodexClient.cs) for long-lived workflows
- typed inputs, events, results, and errors such as [`CodexInputItem`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexThreadEvent`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexThreadItem`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexRunResult`](../src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexThreadSnapshot`](../src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexRuntimeCapabilities`](../src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexRuntimeMetadata`](../src/Incursa.OpenAI.Codex/CoreTypes.cs), and [`CodexException`](../src/Incursa.OpenAI.Codex/Exceptions.cs) so callers do not need to parse raw runtime output

## Hello World

The smallest useful call starts a thread, sends one prompt, and prints the final response.

It uses [`CodexClient`](../src/Incursa.OpenAI.Codex/CodexClient.cs) to create the runtime and [`CodexThread`](../src/Incursa.OpenAI.Codex/CodexClient.cs) to run the prompt.

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

`CodexRunResult.FinalResponse` can be `null` when the turn completes without a final-answer or phase-less assistant message item.

If you want the thread to run in a specific location, set `CodexThreadOptions.WorkingDirectory`.

If you want a no-throw preflight for the local executable, call `await client.IsCodexAvailableAsync()` before `InitializeAsync()` or any turn operation.

## Backend Choice

The [`CodexClientOptions`](../src/Incursa.OpenAI.Codex/Options.cs) type controls which runtime backend is used through its `BackendSelection` property.

| Backend | Best for | Supports | Does not support |
| --- | --- | --- | --- |
| [`AppServer`](../src/Incursa.OpenAI.Codex/Enums.cs) | agents, UIs, and any workflow that needs persistent thread state | thread lifecycle, model listing, read/resume/fork/archive/unarchive, turn steering, turn interruption | N/A |
| [`Exec`](../src/Incursa.OpenAI.Codex/Enums.cs) | the smallest possible prompt-in, response-out integration | `RunAsync` and `RunStreamedAsync` style flows | thread management, model listing, turn steering, turn interruption |

The package currently defaults to [`AppServer`](../src/Incursa.OpenAI.Codex/Enums.cs).

Both backend modes still depend on the local `codex` executable.

At the transport level:

- [`AppServer`](../src/Incursa.OpenAI.Codex/Enums.cs) maps to `codex app-server --listen stdio://`
- [`Exec`](../src/Incursa.OpenAI.Codex/Enums.cs) maps to `codex exec --experimental-json`

## Common Use Cases

- One-shot answer: call [`CodexThread`](../src/Incursa.OpenAI.Codex/CodexClient.cs).`RunAsync(string)`
- Streaming UI: call [`CodexThread`](../src/Incursa.OpenAI.Codex/CodexClient.cs).`RunStreamedAsync(string)` or [`CodexTurn`](../src/Incursa.OpenAI.Codex/CodexClient.cs).`StreamAsync()`
- Structured output: pass [`CodexTurnOptions`](../src/Incursa.OpenAI.Codex/Options.cs).`OutputSchema`
- Multimodal prompts: add [`CodexImageInput`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs) or [`CodexLocalImageInput`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs)
- Long-lived agent sessions: use [`AppServer`](../src/Incursa.OpenAI.Codex/Enums.cs) plus [`CodexThread`](../src/Incursa.OpenAI.Codex/CodexClient.cs).`ReadAsync`, `SetNameAsync`, `CompactAsync`, [`CodexTurn`](../src/Incursa.OpenAI.Codex/CodexClient.cs).`SteerAsync`, and [`CodexTurn`](../src/Incursa.OpenAI.Codex/CodexClient.cs).`InterruptAsync`
- Hosted apps: register the client through [`Incursa.OpenAI.Codex.Extensions`](../src/Incursa.OpenAI.Codex.Extensions/README.md)

## Major API Surfaces

- [`CodexClient`](../src/Incursa.OpenAI.Codex/CodexClient.cs): root entry point, async-only, `IAsyncDisposable`, and `IsCodexAvailableAsync()` for an executable preflight
- [`CodexThread`](../src/Incursa.OpenAI.Codex/CodexClient.cs): stateful conversation handle
- [`CodexTurn`](../src/Incursa.OpenAI.Codex/CodexClient.cs): single-turn handle
- [`CodexClientOptions`](../src/Incursa.OpenAI.Codex/Options.cs): backend selection, executable path override, API key, configuration, environment, and approval handler
- [`CodexThreadOptions`](../src/Incursa.OpenAI.Codex/Options.cs): working directory, sandbox, approval, model, reasoning, web search, and thread-scoped settings
- [`CodexTurnOptions`](../src/Incursa.OpenAI.Codex/Options.cs): per-turn model, sandbox, approval, service tier, reasoning, and output schema settings
- [`CodexInputItem`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs) and derived types: [`CodexTextInput`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexImageInput`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexLocalImageInput`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexSkillInput`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs), and [`CodexMentionInput`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs)
- [`CodexThreadEvent`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs) and [`CodexThreadItem`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs) hierarchies for streamed runtime data
- [`CodexRunResult`](../src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexThreadSnapshot`](../src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexRuntimeCapabilities`](../src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexRuntimeMetadata`](../src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexThreadListResult`](../src/Incursa.OpenAI.Codex/CoreTypes.cs), and [`CodexModelListResult`](../src/Incursa.OpenAI.Codex/CoreTypes.cs) for result handling and discovery. `CodexRunResult.FinalResponse` stays nullable for commentary-only turns.
- [`CodexException`](../src/Incursa.OpenAI.Codex/Exceptions.cs) and related exception types for runtime, transport, capability, and retry failures

## DI Example

```csharp
using Incursa.OpenAI.Codex;
using Incursa.OpenAI.Codex.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCodex(options =>
{
    options.BackendSelection = CodexBackendSelection.AppServer;
});

var app = builder.Build();

app.MapGet("/hello", async (CodexClient client) =>
{
    CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
    {
        SkipGitRepoCheck = true,
    });

    CodexRunResult result = await thread.RunAsync("Say hello from Codex in one sentence.");
    return result.FinalResponse;
});

app.Run();
```

Use `AddCodex(IConfiguration)` when you want the client options to bind from configuration instead of an inline callback.

## What To Read Next

- [`docs/sample-modes.md`](sample-modes.md)
- [`samples/Incursa.OpenAI.Codex.Sample/README.md`](../samples/Incursa.OpenAI.Codex.Sample/README.md)
