using System.Text.Json.Nodes;
using Incursa.OpenAI.Codex;
using Incursa.OpenAI.Codex.Extensions;
using Microsoft.Extensions.DependencyInjection;

int exitCode = await ProgramEntry.RunAsync(args);
Environment.ExitCode = exitCode;

internal static class ProgramEntry
{
    public static async Task<int> RunAsync(string[] args)
    {
        SampleOptions options;
        try
        {
            options = SampleOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            PrintUsage();
            return 2;
        }

        if (options.ShowHelp)
        {
            PrintUsage();
            return 0;
        }

        try
        {
            await using CodexClient client = CreateClient(options);
            _ = await client.InitializeAsync().ConfigureAwait(false);

            switch (options.Mode)
            {
                case SampleMode.Quickstart:
                    await RunQuickstartAsync(client, options).ConfigureAwait(false);
                    break;
                case SampleMode.Streaming:
                    await RunStreamingAsync(client, options).ConfigureAwait(false);
                    break;
                case SampleMode.StructuredOutput:
                    await RunStructuredOutputAsync(client, options).ConfigureAwait(false);
                    break;
                case SampleMode.ImageInput:
                    await RunImageInputAsync(client, options).ConfigureAwait(false);
                    break;
                case SampleMode.ErrorHandling:
                    await RunErrorHandlingAsync(client, options).ConfigureAwait(false);
                    break;
                case SampleMode.TurnControls:
                    await RunTurnControlsAsync(client, options).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (CodexException ex)
        {
            PrintCodexException(ex);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unhandled error: {ex.GetType().Name}: {ex.Message}");
            return 1;
        }

        return 0;
    }

    private static CodexClient CreateClient(SampleOptions options)
    {
        if (!options.UseDependencyInjection)
        {
            return CreateDirectClient(options);
        }

        ServiceCollection services = new();
        services.AddCodex(clientOptions =>
        {
            ApplyClientOptions(clientOptions, options);
        });

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<CodexClient>();
    }

    private static CodexClient CreateDirectClient(SampleOptions options)
    {
        CodexClientOptions clientOptions = new();
        ApplyClientOptions(clientOptions, options);
        return new CodexClient(clientOptions);
    }

    private static void ApplyClientOptions(CodexClientOptions target, SampleOptions options)
    {
        target.BackendSelection = options.Backend;
        target.CodexPathOverride = options.CodexPathOverride;
        target.ApiKey = options.ApiKey;
    }

    private static async Task RunQuickstartAsync(CodexClient client, SampleOptions options)
    {
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = options.WorkingDirectory,
            SkipGitRepoCheck = true,
        }).ConfigureAwait(false);

        CodexRunResult result = await thread.RunAsync(options.Prompt).ConfigureAwait(false);
        Console.WriteLine("Final response:");
        Console.WriteLine(result.FinalResponse ?? "(no final response)");
    }

    private static async Task RunStreamingAsync(CodexClient client, SampleOptions options)
    {
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = options.WorkingDirectory,
            SkipGitRepoCheck = true,
        }).ConfigureAwait(false);

        await foreach (CodexThreadEvent evt in thread.RunStreamedAsync(options.Prompt).ConfigureAwait(false))
        {
            Console.WriteLine(FormatEvent(evt));
        }
    }

    private static async Task RunStructuredOutputAsync(CodexClient client, SampleOptions options)
    {
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = options.WorkingDirectory,
            SkipGitRepoCheck = true,
        }).ConfigureAwait(false);

        JsonObject schema = new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["answer"] = new JsonObject { ["type"] = "string" },
                ["confidence"] = new JsonObject { ["type"] = "number" },
            },
            ["required"] = new JsonArray("answer", "confidence"),
            ["additionalProperties"] = false,
        };

        CodexRunResult result = await thread.RunAsync(
            options.Prompt,
            new CodexTurnOptions
            {
                OutputSchema = schema,
            }).ConfigureAwait(false);

        Console.WriteLine("Structured output response:");
        Console.WriteLine(result.FinalResponse ?? "(no final response)");
    }

    private static async Task RunImageInputAsync(CodexClient client, SampleOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.LocalImagePath) && string.IsNullOrWhiteSpace(options.RemoteImageUrl))
        {
            throw new ArgumentException("Image mode requires --image <local-path> or --image-url <https-url>.");
        }

        List<CodexInputItem> input =
        [
            new CodexTextInput { Text = options.Prompt },
        ];

        if (!string.IsNullOrWhiteSpace(options.LocalImagePath))
        {
            input.Add(new CodexLocalImageInput
            {
                Path = Path.GetFullPath(options.LocalImagePath),
            });
        }

        if (!string.IsNullOrWhiteSpace(options.RemoteImageUrl))
        {
            input.Add(new CodexImageInput
            {
                Url = options.RemoteImageUrl,
            });
        }

        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = options.WorkingDirectory,
            SkipGitRepoCheck = true,
        }).ConfigureAwait(false);

        CodexRunResult result = await thread.RunAsync(input).ConfigureAwait(false);
        Console.WriteLine("Image flow response:");
        Console.WriteLine(result.FinalResponse ?? "(no final response)");
    }

    private static async Task RunErrorHandlingAsync(CodexClient client, SampleOptions options)
    {
        try
        {
            _ = await client.ListThreadsAsync(new CodexThreadListOptions
            {
                Limit = 5,
            }).ConfigureAwait(false);
            Console.WriteLine("ListThreadsAsync succeeded.");
            return;
        }
        catch (CodexCapabilityNotSupportedException ex) when (client.Options.BackendSelection == CodexBackendSelection.Exec)
        {
            Console.WriteLine($"Capability gating worked as expected for exec backend: {ex.Message}");
        }

        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = options.WorkingDirectory,
            SkipGitRepoCheck = true,
        }).ConfigureAwait(false);

        try
        {
            _ = await thread.RunAsync(options.Prompt).ConfigureAwait(false);
            Console.WriteLine("Request completed without runtime exception.");
        }
        catch (CodexException ex)
        {
            PrintCodexException(ex);
            throw;
        }
    }

    private static async Task RunTurnControlsAsync(CodexClient client, SampleOptions options)
    {
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = options.WorkingDirectory,
            SkipGitRepoCheck = true,
        }).ConfigureAwait(false);

        CodexTurn turn = await thread.StartTurnAsync(options.Prompt).ConfigureAwait(false);

        Task streamTask = Task.Run(async () =>
        {
            await foreach (CodexThreadEvent evt in turn.StreamAsync().ConfigureAwait(false))
            {
                Console.WriteLine(FormatEvent(evt));
            }
        });

        await turn.SteerAsync("Also include a concise bullet summary.").ConfigureAwait(false);

        if (options.InterruptTurn)
        {
            await turn.InterruptAsync().ConfigureAwait(false);
        }

        await streamTask.ConfigureAwait(false);
    }

    private static string FormatEvent(CodexThreadEvent evt)
    {
        return evt switch
        {
            CodexThreadStartedEvent started => $"thread.started thread={started.Thread.Id}",
            CodexTurnStartedEvent turnStarted => $"turn.started turn={turnStarted.Turn.Id}",
            CodexItemCompletedEvent itemCompleted => $"item.completed type={itemCompleted.Item.Type}",
            CodexTurnCompletedEvent completed => $"turn.completed turn={completed.Turn.Id}",
            CodexTurnFailedEvent failed => $"turn.failed turn={failed.Turn.Id} error={failed.Turn.Error?.Message}",
            CodexThreadErrorEvent threadError => $"error message={threadError.Error.Message}",
            _ => evt.Type,
        };
    }

    private static void PrintCodexException(CodexException ex)
    {
        Console.Error.WriteLine($"Codex error: {ex.GetType().Name}");
        Console.Error.WriteLine(ex.Message);

        if (ex is CodexJsonRpcException rpcException && rpcException.ErrorData is not null)
        {
            Console.Error.WriteLine("Error data:");
            Console.Error.WriteLine(rpcException.ErrorData.ToJsonString());
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
Codex .NET sample

Usage:
  dotnet run --project samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj -- [options]

Options:
  --mode <quickstart|streaming|structured-output|image-input|error-handling|turn-controls>
  --backend <app-server|exec>                 Default: app-server
  --prompt <text>                             Prompt for run-based modes
  --cwd <path>                                Working directory forwarded to Codex
  --image <path>                              Local image path for image-input mode
  --image-url <https-url>                     Remote image URL for image-input mode
  --codex-path <path>                         Explicit path to codex executable
  --api-key <key>                             Optional API key override (otherwise env)
  --use-di                                    Build CodexClient through AddCodex DI extensions
  --interrupt                                 Interrupt turn in turn-controls mode
  --help                                      Show this message
""");
    }
}

internal enum SampleMode
{
    Quickstart,
    Streaming,
    StructuredOutput,
    ImageInput,
    ErrorHandling,
    TurnControls,
}

internal sealed record SampleOptions(
    SampleMode Mode,
    CodexBackendSelection Backend,
    string Prompt,
    string? WorkingDirectory,
    string? LocalImagePath,
    string? RemoteImageUrl,
    string? CodexPathOverride,
    string? ApiKey,
    bool UseDependencyInjection,
    bool InterruptTurn,
    bool ShowHelp)
{
    public static SampleOptions Parse(string[] args)
    {
        SampleMode mode = SampleMode.Quickstart;
        CodexBackendSelection backend = CodexBackendSelection.AppServer;
        string prompt = "Summarize the current repository state in three bullet points.";
        string? cwd = Environment.CurrentDirectory;
        string? localImagePath = null;
        string? remoteImageUrl = null;
        string? codexPath = null;
        string? apiKey = null;
        bool useDi = false;
        bool interruptTurn = false;
        bool showHelp = false;

        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            switch (arg)
            {
                case "--mode":
                    mode = ParseMode(ReadValue(args, ref index, arg));
                    break;
                case "--backend":
                    backend = ParseBackend(ReadValue(args, ref index, arg));
                    break;
                case "--prompt":
                    prompt = ReadValue(args, ref index, arg);
                    break;
                case "--cwd":
                    cwd = ReadValue(args, ref index, arg);
                    break;
                case "--image":
                    localImagePath = ReadValue(args, ref index, arg);
                    break;
                case "--image-url":
                    remoteImageUrl = ReadValue(args, ref index, arg);
                    break;
                case "--codex-path":
                    codexPath = ReadValue(args, ref index, arg);
                    break;
                case "--api-key":
                    apiKey = ReadValue(args, ref index, arg);
                    break;
                case "--use-di":
                    useDi = true;
                    break;
                case "--interrupt":
                    interruptTurn = true;
                    break;
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        return new SampleOptions(
            mode,
            backend,
            prompt,
            cwd,
            localImagePath,
            remoteImageUrl,
            codexPath,
            apiKey,
            useDi,
            interruptTurn,
            showHelp);
    }

    private static string ReadValue(string[] args, ref int index, string switchName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {switchName}.");
        }

        index++;
        return args[index];
    }

    private static SampleMode ParseMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "quickstart" => SampleMode.Quickstart,
            "streaming" => SampleMode.Streaming,
            "structured-output" => SampleMode.StructuredOutput,
            "image-input" => SampleMode.ImageInput,
            "error-handling" => SampleMode.ErrorHandling,
            "turn-controls" => SampleMode.TurnControls,
            _ => throw new ArgumentException($"Unsupported mode: {value}"),
        };
    }

    private static CodexBackendSelection ParseBackend(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "app-server" => CodexBackendSelection.AppServer,
            "exec" => CodexBackendSelection.Exec,
            _ => throw new ArgumentException($"Unsupported backend: {value}"),
        };
    }
}
