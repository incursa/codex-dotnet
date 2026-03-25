# Usage Guide

`Incursa.OpenAI.Codex` is an async-only .NET client for the Codex runtime.

It is designed around three things:

- a root client that starts and manages Codex conversations
- stateful thread and turn handles for long-lived workflows
- typed inputs, events, results, and errors so callers do not need to parse raw runtime output

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

If you want the thread to work in a specific location, set `CodexThreadOptions.WorkingDirectory`.

## Backend Choice

`CodexClientOptions.BackendSelection` controls which runtime backend is used.

| Backend | Best for | Supports | Does not support |
| --- | --- | --- | --- |
| `AppServer` | agents, UIs, and any workflow that needs persistent thread state | thread lifecycle, model listing, read/resume/fork/archive/unarchive, turn steering, turn interruption | N/A |
| `Exec` | the smallest possible prompt-in, response-out integration | `RunAsync` and `RunStreamedAsync` style flows | thread management, model listing, turn steering, turn interruption |

The current package behavior defaults to `AppServer`.

Under the hood:

- `AppServer` maps to `codex app-server --listen stdio://`
- `Exec` maps to `codex exec --experimental-json`

## Common Use Cases

- One-shot answer: call `CodexThread.RunAsync(string)`
- Streaming UI: call `CodexThread.RunStreamedAsync(string)` or `CodexTurn.StreamAsync()`
- Structured output: pass `CodexTurnOptions.OutputSchema`
- Multimodal prompts: add `CodexImageInput` or `CodexLocalImageInput`
- Long-lived agent sessions: use `AppServer` plus `CodexThread.ReadAsync`, `SetNameAsync`, `CompactAsync`, `CodexTurn.SteerAsync`, and `CodexTurn.InterruptAsync`
- Hosted apps: register the client with `Incursa.OpenAI.Codex.Extensions`

## Major API Surfaces

- `CodexClient`: root entry point, async-only, `IAsyncDisposable`
- `CodexThread`: stateful conversation handle
- `CodexTurn`: single-turn handle
- `CodexClientOptions`: backend selection, executable path override, API key, configuration, environment, and approval handler
- `CodexThreadOptions`: working directory, sandbox, approval, model, reasoning, web search, and thread-scoped settings
- `CodexTurnOptions`: per-turn model, sandbox, approval, service tier, reasoning, and output schema settings
- `CodexInputItem` and derived types: `CodexTextInput`, `CodexImageInput`, `CodexLocalImageInput`, `CodexSkillInput`, and `CodexMentionInput`
- `CodexThreadEvent` and `CodexThreadItem` hierarchies for streamed runtime data
- `CodexRunResult`, `CodexThreadSnapshot`, `CodexRuntimeCapabilities`, `CodexRuntimeMetadata`, `CodexThreadListResult`, and `CodexModelListResult` for result handling and discovery
- `CodexException` and related exception types for runtime, transport, capability, and retry failures

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
