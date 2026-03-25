# Incursa.OpenAI.Codex

`Incursa.OpenAI.Codex` is an async-only .NET client for the Codex runtime.

It gives you:

- `CodexClient` for starting and managing Codex conversations
- `CodexThread` and `CodexTurn` for stateful and turn-level control
- typed inputs, events, results, options, and exceptions
- an optional DI package, `Incursa.OpenAI.Codex.Extensions`, for `IServiceCollection` registration

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

That example assumes the Codex runtime is available and can authenticate through the environment or `CodexClientOptions`.

If you prefer DI, install `Incursa.OpenAI.Codex.Extensions` and register `CodexClient` with `AddCodex(...)`.

## Which Backend Should I Use?

`CodexClientOptions.BackendSelection` controls the runtime backend.

| Backend | Use it when | Good for | Not available |
| --- | --- | --- | --- |
| `AppServer` | you need the richer conversation surface | thread lifecycle, model listing, thread read/resume/fork/archive/unarchive, turn steering, turn interruption | N/A |
| `Exec` | you only need the CLI-backed run/stream path | quick one-shot prompts and streaming responses | thread lifecycle management, model listing, turn steering, turn interruption |

Current package behavior defaults to `AppServer`.

Under the hood:

- `AppServer` maps to `codex app-server --listen stdio://`
- `Exec` maps to `codex exec --experimental-json`

## What Is In The SDK?

The major public surfaces are:

- `CodexClient`: root entry point, async-only, `IAsyncDisposable`
- `CodexThread`: stateful conversation handle with `RunAsync`, `RunStreamedAsync`, `StartTurnAsync`, `ReadAsync`, `SetNameAsync`, and `CompactAsync`
- `CodexTurn`: single-turn handle with `StreamAsync`, `RunAsync`, `SteerAsync`, and `InterruptAsync`
- `CodexClientOptions`, `CodexThreadOptions`, `CodexTurnOptions`: runtime, thread, and turn configuration
- `CodexInputItem` and derived types such as `CodexTextInput`, `CodexImageInput`, `CodexLocalImageInput`, `CodexSkillInput`, and `CodexMentionInput`
- `CodexThreadEvent` and `CodexThreadItem` hierarchies for streamed runtime data
- `CodexRunResult`, `CodexThreadSnapshot`, `CodexRuntimeCapabilities`, `CodexRuntimeMetadata`, and `CodexException` types for result handling and diagnostics

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

## License

This repository is licensed under Apache 2.0. See `LICENSE`.
