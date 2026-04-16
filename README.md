# Incursa.OpenAI.Codex

`Incursa.OpenAI.Codex` is an async-only .NET client for the local Codex runtime. It launches the `codex` executable as a subprocess, so the machine running your app must already have Codex installed and authenticated.

It provides:

- [`CodexClient`](src/Incursa.OpenAI.Codex/CodexClient.cs) for starting and managing Codex conversations
- [`CodexThread`](src/Incursa.OpenAI.Codex/CodexClient.cs) and [`CodexTurn`](src/Incursa.OpenAI.Codex/CodexClient.cs) for stateful and turn-level control
- typed inputs, events, results, options, and exceptions such as [`CodexClientOptions`](src/Incursa.OpenAI.Codex/Options.cs), [`CodexThreadOptions`](src/Incursa.OpenAI.Codex/Options.cs), [`CodexTurnOptions`](src/Incursa.OpenAI.Codex/Options.cs), [`CodexInputItem`](src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexThreadEvent`](src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexThreadItem`](src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexRunResult`](src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexThreadSnapshot`](src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexRuntimeCapabilities`](src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexRuntimeMetadata`](src/Incursa.OpenAI.Codex/CoreTypes.cs), and [`CodexException`](src/Incursa.OpenAI.Codex/Exceptions.cs)
- an optional DI companion package, [`Incursa.OpenAI.Codex.Extensions`](src/Incursa.OpenAI.Codex.Extensions/README.md), for `IServiceCollection` registration

## When To Use This SDK

Use this SDK when you want a C# wrapper around the local Codex CLI and its thread or turn APIs.

- Good fit: repo-aware automation, server-side orchestration, and workflows that delegate work to a local Codex install.
- Use the OpenAI SDK when you want direct API calls from .NET.
- Use ChatKit when you want a hosted chat UI surface.
- Use the Agents SDK when you want higher-level agent orchestration.
- `CodexClientOptions.ApiKey` and `CodexClientOptions.BaseUrl` are forwarded to the Codex subprocess; they do not remove the local Codex installation requirement.
- If you want a no-throw preflight for the local executable, call `await client.IsCodexAvailableAsync()` before `InitializeAsync()` or any turn operation.

## Start Here

The smallest useful call is a thread plus a single prompt:

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

That example assumes the Codex runtime is installed locally and can authenticate through the environment or [`CodexClientOptions`](src/Incursa.OpenAI.Codex/Options.cs) forwarded to the subprocess.

If you need DI, install [`Incursa.OpenAI.Codex.Extensions`](src/Incursa.OpenAI.Codex.Extensions/README.md) and register [`CodexClient`](src/Incursa.OpenAI.Codex/CodexClient.cs) with [`AddCodex(...)`](src/Incursa.OpenAI.Codex.Extensions/CodexServiceCollectionExtensions.cs).

## Which Backend Should I Use?

The [`CodexClientOptions`](src/Incursa.OpenAI.Codex/Options.cs) type controls the runtime backend through its `BackendSelection` property.

| Backend | Use it when | Good for | Not available |
| --- | --- | --- | --- |
| [`AppServer`](src/Incursa.OpenAI.Codex/Enums.cs) | you need the richer conversation surface | thread lifecycle, model listing, thread read/resume/fork/archive/unarchive, turn steering, turn interruption | N/A |
| [`Exec`](src/Incursa.OpenAI.Codex/Enums.cs) | you only need the CLI-backed run/stream path | quick one-shot prompts and streaming responses | thread lifecycle management, model listing, turn steering, turn interruption |

The package currently defaults to [`AppServer`](src/Incursa.OpenAI.Codex/Enums.cs).

At the transport level:

- [`AppServer`](src/Incursa.OpenAI.Codex/Enums.cs) maps to `codex app-server --listen stdio://`
- [`Exec`](src/Incursa.OpenAI.Codex/Enums.cs) maps to `codex exec --experimental-json`

## What Is In The SDK?

The main public surfaces are:

- [`CodexClient`](src/Incursa.OpenAI.Codex/CodexClient.cs): root entry point, async-only, `IAsyncDisposable`, and `IsCodexAvailableAsync()` for an executable preflight
- [`CodexThread`](src/Incursa.OpenAI.Codex/CodexClient.cs): stateful conversation handle with `RunAsync`, `RunStreamedAsync`, `StartTurnAsync`, `ReadAsync`, `SetNameAsync`, and `CompactAsync`
- [`CodexTurn`](src/Incursa.OpenAI.Codex/CodexClient.cs): single-turn handle with `StreamAsync`, `RunAsync`, `SteerAsync`, and `InterruptAsync`
- [`CodexClientOptions`](src/Incursa.OpenAI.Codex/Options.cs), [`CodexThreadOptions`](src/Incursa.OpenAI.Codex/Options.cs), [`CodexTurnOptions`](src/Incursa.OpenAI.Codex/Options.cs): runtime, thread, and turn configuration
- [`CodexInputItem`](src/Incursa.OpenAI.Codex/ConversationTypes.cs) and derived types such as [`CodexTextInput`](src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexImageInput`](src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexLocalImageInput`](src/Incursa.OpenAI.Codex/ConversationTypes.cs), [`CodexSkillInput`](src/Incursa.OpenAI.Codex/ConversationTypes.cs), and [`CodexMentionInput`](src/Incursa.OpenAI.Codex/ConversationTypes.cs)
- [`CodexThreadEvent`](src/Incursa.OpenAI.Codex/ConversationTypes.cs) and [`CodexThreadItem`](src/Incursa.OpenAI.Codex/ConversationTypes.cs) hierarchies for streamed runtime data
- [`CodexRunResult`](src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexThreadSnapshot`](src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexRuntimeCapabilities`](src/Incursa.OpenAI.Codex/CoreTypes.cs), [`CodexRuntimeMetadata`](src/Incursa.OpenAI.Codex/CoreTypes.cs), and [`CodexException`](src/Incursa.OpenAI.Codex/Exceptions.cs) types for result handling and diagnostics

## Samples

The runnable sample in `samples/Incursa.OpenAI.Codex.Sample` demonstrates:

- `quickstart`
- `streaming`
- `structured-output`
- `image-input`
- `error-handling`
- `turn-controls`

See [`samples/Incursa.OpenAI.Codex.Sample/README.md`](samples/Incursa.OpenAI.Codex.Sample/README.md) for the sample overview and [`docs/sample-modes.md`](docs/sample-modes.md) for the mode-by-mode commands.

## Deeper Docs

- [`docs/usage-guide.md`](docs/usage-guide.md)
- [`docs/sample-modes.md`](docs/sample-modes.md)
- [`samples/Incursa.OpenAI.Codex.Sample/README.md`](samples/Incursa.OpenAI.Codex.Sample/README.md)
- [`src/Incursa.OpenAI.Codex/README.md`](src/Incursa.OpenAI.Codex/README.md)
- [`src/Incursa.OpenAI.Codex.Extensions/README.md`](src/Incursa.OpenAI.Codex.Extensions/README.md)
- [`tests/Incursa.OpenAI.Codex.Tests/README.md`](tests/Incursa.OpenAI.Codex.Tests/README.md)
- [`fuzz/README.md`](fuzz/README.md)
- [`benchmarks/README.md`](benchmarks/README.md)
- [`quality/testing-intent.yaml`](quality/testing-intent.yaml)

## License

This repository is licensed under Apache 2.0. See `LICENSE`.
