# Sample Modes

The runnable sample in `samples/Incursa.OpenAI.Codex.Sample` is a single console app with mode-based scenarios.

## Quickstart

The default mode shows the shortest end-to-end prompt call:

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode quickstart --prompt "Summarize this repository."
```

Use this when you want the bare minimum from the SDK: create a client such as [`CodexClient`](../src/Incursa.OpenAI.Codex/CodexClient.cs), start a thread such as [`CodexThread`](../src/Incursa.OpenAI.Codex/CodexClient.cs), run one prompt, and print the final response.

## Streaming

Shows the event stream as it arrives:

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode streaming --prompt "List three files likely to contain option models."
```

Use this when you want to build a UI or log pipeline around incremental [`CodexThreadEvent`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs) data.

## Structured Output

Shows the [`CodexTurnOptions`](../src/Incursa.OpenAI.Codex/Options.cs).`OutputSchema` setting with a JSON schema:

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode structured-output --prompt "Return JSON with answer and confidence."
```

Use this when you need a predictable JSON-shaped result instead of free-form text.

## Image Input

Shows local and remote image inputs:

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode image-input --image C:\path\to\image.png --prompt "Describe the image."
```

Use `--image-url` instead of `--image` if you want to provide a remote image input such as [`CodexImageInput`](../src/Incursa.OpenAI.Codex/ConversationTypes.cs).

## Error Handling

Shows capability gating and typed exceptions:

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode error-handling --backend exec --prompt "Explain capability differences."
```

Use this when you want to see how the SDK behaves when a backend cannot support a request.

## Turn Controls

Shows explicit turn steering and interruption:

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode turn-controls --prompt "Draft a short release note." --interrupt
```

This mode is the clearest demonstration of why [`AppServer`](../src/Incursa.OpenAI.Codex/Enums.cs) exists: it gives you control after the turn has started with [`CodexTurn`](../src/Incursa.OpenAI.Codex/CodexClient.cs).

## DI Variant

Add `--use-di` to build the client through [`Incursa.OpenAI.Codex.Extensions`](../src/Incursa.OpenAI.Codex.Extensions/README.md):

```powershell
dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- --mode quickstart --use-di
```

## Backend Reminder

- [`AppServer`](../src/Incursa.OpenAI.Codex/Enums.cs) is the default in the current package and supports the full thread and turn-control surface
- [`Exec`](../src/Incursa.OpenAI.Codex/Enums.cs) is the CLI-backed path for simpler run and stream flows
