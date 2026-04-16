using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-CATALOG-0302, REQ-CODEX-SDK-CATALOG-0304, REQ-CODEX-SDK-HELPERS-0314, REQ-CODEX-SDK-HELPERS-0319.

internal sealed class CodexExecTransport : ICodexTransport
{
    private static readonly CodexRuntimeCapabilities SupportedCapabilities = new()
    {
        BackendSelection = CodexBackendSelection.Exec,
        SupportsStartThread = true,
        SupportsResumeThread = true,
        SupportsThreadStreaming = true,
        SupportsTurnSteering = false,
        SupportsTurnInterruption = false,
        SupportsListModels = false,
        SupportsListThreads = false,
        SupportsReadThread = false,
        SupportsForkThread = false,
        SupportsArchiveThread = false,
        SupportsUnarchiveThread = false,
        SupportsSetThreadName = false,
        SupportsCompactThread = false,
        ExperimentalApi = true,
    };

    private readonly CodexClientOptions _options;
    private readonly CodexTurnConsumerGate _turnConsumerGate;
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private string? _executablePath;
    private bool _disposed;
    private CodexRuntimeMetadata? _metadata;

    public CodexExecTransport(CodexClientOptions options, CodexTurnConsumerGate turnConsumerGate)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _turnConsumerGate = turnConsumerGate ?? throw new ArgumentNullException(nameof(turnConsumerGate));
    }

    public CodexRuntimeCapabilities Capabilities => SupportedCapabilities;

    public async Task<CodexRuntimeMetadata> InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        if (_metadata is not null)
        {
            return _metadata;
        }

        await _initializeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_metadata is not null)
            {
                return _metadata;
            }

            _executablePath ??= CodexExecutableResolver.Resolve(_options);
            string version = string.IsNullOrWhiteSpace(_options.ClientVersion)
                ? typeof(CodexClient).Assembly.GetName().Version?.ToString() ?? "0.0.0"
                : _options.ClientVersion!;

            _metadata = new CodexRuntimeMetadata
            {
                UserAgent = $"{(string.IsNullOrWhiteSpace(_options.ClientName) ? "Incursa.OpenAI.Codex" : _options.ClientName)}/{version}",
                PlatformFamily = Environment.OSVersion.Platform.ToString(),
                PlatformOs = RuntimeInformation.OSDescription,
                ServerInfo = new CodexServerInfo
                {
                    Name = "codex-exec",
                    Version = version,
                },
            };

            return _metadata;
        }
        finally
        {
            _initializeGate.Release();
        }
    }

    public Task<CodexThreadHandleState> StartThreadAsync(CodexThreadOptions? options, CancellationToken cancellationToken)
        => Task.FromException<CodexThreadHandleState>(new CodexCapabilityNotSupportedException(nameof(CodexClient.StartThreadAsync), CodexBackendSelection.Exec));

    public Task<CodexThreadHandleState> ResumeThreadAsync(string threadId, CodexThreadOptions? options, CancellationToken cancellationToken)
        => Task.FromException<CodexThreadHandleState>(new CodexCapabilityNotSupportedException(nameof(CodexClient.ResumeThreadAsync), CodexBackendSelection.Exec));

    public Task<CodexThreadHandleState> ForkThreadAsync(string threadId, CodexThreadForkOptions? options, CancellationToken cancellationToken)
        => Task.FromException<CodexThreadHandleState>(new CodexCapabilityNotSupportedException(nameof(CodexClient.ForkThreadAsync), CodexBackendSelection.Exec));

    public Task<CodexThreadListResult> ListThreadsAsync(CodexThreadListOptions? options, CancellationToken cancellationToken)
        => Task.FromException<CodexThreadListResult>(new CodexCapabilityNotSupportedException(nameof(CodexClient.ListThreadsAsync), CodexBackendSelection.Exec));

    public Task<CodexThreadSnapshot> ReadThreadAsync(string threadId, CodexThreadReadOptions? options, CancellationToken cancellationToken)
        => Task.FromException<CodexThreadSnapshot>(new CodexCapabilityNotSupportedException(nameof(CodexClient.ReadThreadAsync), CodexBackendSelection.Exec));

    public Task ArchiveThreadAsync(string threadId, CancellationToken cancellationToken)
        => Task.FromException(new CodexCapabilityNotSupportedException(nameof(CodexClient.ArchiveThreadAsync), CodexBackendSelection.Exec));

    public Task<CodexThreadHandleState> UnarchiveThreadAsync(string threadId, CancellationToken cancellationToken)
        => Task.FromException<CodexThreadHandleState>(new CodexCapabilityNotSupportedException(nameof(CodexClient.UnarchiveThreadAsync), CodexBackendSelection.Exec));

    public Task<CodexModelListResult> ListModelsAsync(CodexModelListOptions? options, CancellationToken cancellationToken)
        => Task.FromException<CodexModelListResult>(new CodexCapabilityNotSupportedException(nameof(CodexClient.ListModelsAsync), CodexBackendSelection.Exec));

    public Task<CodexThreadSnapshot> SetThreadNameAsync(string threadId, string name, CancellationToken cancellationToken)
        => Task.FromException<CodexThreadSnapshot>(new CodexCapabilityNotSupportedException(nameof(CodexThread.SetNameAsync), CodexBackendSelection.Exec));

    public Task CompactThreadAsync(string threadId, CancellationToken cancellationToken)
        => Task.FromException(new CodexCapabilityNotSupportedException(nameof(CodexThread.CompactAsync), CodexBackendSelection.Exec));

    public Task<CodexTurnSession> StartTurnAsync(
        string? threadId,
        IReadOnlyList<CodexInputItem> input,
        CodexThreadOptions? threadOptions,
        CodexTurnOptions? options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        _executablePath ??= CodexExecutableResolver.Resolve(_options);

        return StartTurnCoreAsync(threadId, input, threadOptions, options, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        _initializeGate.Dispose();
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new CodexTransportClosedException();
        }
    }

    private async Task<CodexTurnSession> StartTurnCoreAsync(
        string? threadId,
        IReadOnlyList<CodexInputItem> input,
        CodexThreadOptions? threadOptions,
        CodexTurnOptions? options,
        CancellationToken cancellationToken)
    {
        string prompt = NormalizeInput(input, out IReadOnlyList<string> images);
        string turnId = CreateTurnId();
        CodexOutputSchemaFile schemaFile = await CodexOutputSchemaFile.CreateAsync(options?.OutputSchema, cancellationToken).ConfigureAwait(false);
        ICodexProcess process;

        try
        {
            List<string> args = BuildArguments(threadId, threadOptions, options, images, schemaFile.FilePath);
            process = await _options.ProcessLauncher!.StartAsync(
                new CodexProcessStartInfo(
                    _executablePath!,
                    args,
                    options?.WorkingDirectory ?? threadOptions?.WorkingDirectory,
                    CodexExecutableResolver.BuildEnvironment(_options)),
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await schemaFile.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        CodexTurnSession session = new(
            threadId ?? string.Empty,
            turnId,
            input,
            options,
            (_, _) => throw new CodexCapabilityNotSupportedException(nameof(CodexTurn.SteerAsync), CodexBackendSelection.Exec),
            _ => throw new CodexCapabilityNotSupportedException(nameof(CodexTurn.InterruptAsync), CodexBackendSelection.Exec),
            _turnConsumerGate);

        _ = PumpAsync(process, schemaFile, session, prompt, cancellationToken);
        return session;
    }

    private async Task PumpAsync(
        ICodexProcess process,
        CodexOutputSchemaFile schemaFile,
        CodexTurnSession session,
        string prompt,
        CancellationToken cancellationToken)
    {
        List<string> stderrLines = new();
        bool terminalSeen = false;

        try
        {
            Task stderrTask = DrainStderrAsync(process, stderrLines);

            using (CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(() => process.Kill()))
            {
                await WritePromptAsync(process, prompt, cancellationToken).ConfigureAwait(false);
            }

            while (true)
            {
                string? line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                CodexThreadEvent evt = ParseEvent(line);
                if (evt is CodexTurnCompletedEvent or CodexTurnFailedEvent)
                {
                    terminalSeen = true;
                }

                session.AppendEvent(evt);
            }

            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            await stderrTask.ConfigureAwait(false);

            if (!terminalSeen)
            {
                string tail = BuildStderrTail(stderrLines);
                session.AppendEvent(new CodexTurnFailedEvent
                {
                    Turn = new CodexTurnRecord
                    {
                        Id = session.Id,
                        Status = CodexTurnStatus.Failed,
                        Items = session.Items,
                        Error = new CodexTurnError
                        {
                            Message = $"Codex exec exited unexpectedly with code {process.ExitCode}.",
                            AdditionalDetails = string.IsNullOrWhiteSpace(tail) ? null : tail,
                        },
                    },
                });
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            session.AppendEvent(new CodexTurnFailedEvent
            {
                Turn = new CodexTurnRecord
                {
                    Id = session.Id,
                    Status = CodexTurnStatus.Failed,
                    Items = session.Items,
                    Error = new CodexTurnError
                    {
                        Message = exception.Message,
                        AdditionalDetails = exception.InnerException?.Message,
                    },
                },
            });
        }
        finally
        {
            try
            {
                await schemaFile.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
            }

            try
            {
                await process.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
            }

            session.CompleteWriter();
        }
    }

    private static CodexThreadEvent ParseEvent(string line)
    {
        JsonNode? node = JsonNode.Parse(line);
        if (node is not JsonObject message)
        {
            throw new InvalidOperationException("Codex exec output must be a JSON object.");
        }

        return CodexProtocol.ParseThreadEvent(message);
    }

    private async Task DrainStderrAsync(ICodexProcess process, List<string> stderrLines)
    {
        while (true)
        {
            string? line = await process.StandardError.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                return;
            }

            lock (stderrLines)
            {
                stderrLines.Add(line);
                if (stderrLines.Count > 400)
                {
                    stderrLines.RemoveAt(0);
                }
            }
        }
    }

    private async Task WritePromptAsync(ICodexProcess process, string prompt, CancellationToken cancellationToken)
    {
        await process.StandardInput.WriteAsync(prompt.AsMemory(), cancellationToken).ConfigureAwait(false);
        await process.StandardInput.FlushAsync().ConfigureAwait(false);
        process.StandardInput.Close();
    }

    private static string BuildStderrTail(List<string> stderrLines)
    {
        lock (stderrLines)
        {
            int start = Math.Max(0, stderrLines.Count - 40);
            return string.Join(Environment.NewLine, stderrLines.Skip(start));
        }
    }

    private List<string> BuildArguments(
        string? threadId,
        CodexThreadOptions? threadOptions,
        CodexTurnOptions? turnOptions,
        IReadOnlyList<string> images,
        string? outputSchemaFile)
    {
        List<string> args = ["exec", "--experimental-json"];
        CodexTurnOptions effectiveTurnOptions = turnOptions ?? new CodexTurnOptions();
        AddConfigOverrides(args, _options.Config);
        AddConfigOverrides(args, threadOptions?.Config);

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            args.Add("--config");
            args.Add($"openai_base_url={JsonSerializer.Serialize(_options.BaseUrl)}");
        }

        string? effectiveModel = !string.IsNullOrWhiteSpace(effectiveTurnOptions.Model)
            ? effectiveTurnOptions.Model
            : threadOptions?.Model;
        if (!string.IsNullOrWhiteSpace(effectiveModel))
        {
            args.Add("--model");
            args.Add(effectiveModel!);
        }

        if (effectiveTurnOptions.SandboxPolicy is not null || threadOptions?.Sandbox is not null)
        {
            args.Add("--sandbox");
            args.Add(MapSandbox(effectiveTurnOptions.SandboxPolicy ?? threadOptions!.Sandbox!));
        }

        string? workingDirectory = effectiveTurnOptions.WorkingDirectory ?? threadOptions?.WorkingDirectory;
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            args.Add("--cd");
            args.Add(workingDirectory!);
        }

        if (threadOptions?.AdditionalDirectories is { Count: > 0 } additionalDirectories)
        {
            foreach (string directory in additionalDirectories)
            {
                args.Add("--add-dir");
                args.Add(directory);
            }
        }

        if (threadOptions?.SkipGitRepoCheck is true)
        {
            args.Add("--skip-git-repo-check");
        }

        if (!string.IsNullOrWhiteSpace(outputSchemaFile))
        {
            args.Add("--output-schema");
            args.Add(outputSchemaFile!);
        }

        CodexReasoningEffort? effectiveReasoningEffort = effectiveTurnOptions.Effort ?? threadOptions?.ModelReasoningEffort;
        if (effectiveReasoningEffort is not null)
        {
            args.Add("--config");
            args.Add($"model_reasoning_effort={JsonSerializer.Serialize(MapReasoningEffort(effectiveReasoningEffort.Value))}");
        }

        if (threadOptions?.NetworkAccessEnabled is not null)
        {
            args.Add("--config");
            args.Add($"sandbox_workspace_write.network_access={(threadOptions.NetworkAccessEnabled.Value ? "true" : "false")}");
        }

        if (threadOptions?.WebSearchMode is not null)
        {
            args.Add("--config");
            args.Add($"web_search={JsonSerializer.Serialize(MapWebSearchMode(threadOptions.WebSearchMode.Value))}");
        }
        else if (threadOptions?.WebSearchEnabled is true)
        {
            args.Add("--config");
            args.Add("web_search=\"live\"");
        }
        else if (threadOptions?.WebSearchEnabled is false)
        {
            args.Add("--config");
            args.Add("web_search=\"disabled\"");
        }

        if (effectiveTurnOptions.ApprovalPolicy is CodexApprovalModePolicy turnModePolicy)
        {
            args.Add("--config");
            args.Add($"approval_policy={JsonSerializer.Serialize(MapApprovalMode(turnModePolicy.Mode))}");
        }
        else if (threadOptions?.ApprovalPolicy is CodexApprovalModePolicy modePolicy)
        {
            args.Add("--config");
            args.Add($"approval_policy={JsonSerializer.Serialize(MapApprovalMode(modePolicy.Mode))}");
        }
        else if (threadOptions?.ApprovalPolicy is not null)
        {
            throw new CodexCapabilityNotSupportedException("granular approval policies", CodexBackendSelection.Exec);
        }

        if (!string.IsNullOrWhiteSpace(threadId))
        {
            args.Add("resume");
            args.Add(threadId!);
        }

        foreach (string image in images)
        {
            args.Add("--image");
            args.Add(image);
        }

        return args;
    }

    private static void AddConfigOverrides(List<string> args, CodexConfigObject? config)
    {
        foreach (string overrideValue in CodexConfigSerialization.FlattenConfigOverrides(config))
        {
            args.Add("--config");
            args.Add(overrideValue);
        }
    }

    private static string NormalizeInput(IReadOnlyList<CodexInputItem> input, out IReadOnlyList<string> images)
    {
        List<string> textParts = new();
        List<string> imageParts = new();

        foreach (CodexInputItem item in input)
        {
            switch (item)
            {
                case CodexTextInput text when !string.IsNullOrWhiteSpace(text.Text):
                    textParts.Add(text.Text);
                    break;
                case CodexImageInput image when !string.IsNullOrWhiteSpace(image.Url):
                    imageParts.Add(image.Url);
                    break;
                case CodexLocalImageInput localImage when !string.IsNullOrWhiteSpace(localImage.Path):
                    imageParts.Add(localImage.Path);
                    break;
                case CodexSkillInput skill:
                    textParts.Add($"[{skill.Name}] {skill.Path}");
                    break;
                case CodexMentionInput mention:
                    textParts.Add($"[{mention.Name}] {mention.Path}");
                    break;
            }
        }

        images = imageParts;
        return string.Join(Environment.NewLine + Environment.NewLine, textParts);
    }

    private static string MapSandbox(CodexSandboxPolicy policy)
        => policy switch
        {
            CodexDangerFullAccessSandboxPolicy => "danger-full-access",
            CodexReadOnlySandboxPolicy => "read-only",
            CodexWorkspaceWriteSandboxPolicy => "workspace-write",
            _ => throw new CodexCapabilityNotSupportedException("sandbox policy", CodexBackendSelection.Exec),
        };

    private static string MapApprovalMode(CodexApprovalMode mode)
        => mode switch
        {
            CodexApprovalMode.Never => "never",
            CodexApprovalMode.OnRequest => "on-request",
            CodexApprovalMode.OnFailure => "on-failure",
            CodexApprovalMode.Untrusted => "untrusted",
            _ => "on-request",
        };

    private static string MapReasoningEffort(CodexReasoningEffort effort)
        => effort switch
        {
            CodexReasoningEffort.None => "minimal",
            CodexReasoningEffort.Minimal => "minimal",
            CodexReasoningEffort.Low => "low",
            CodexReasoningEffort.Medium => "medium",
            CodexReasoningEffort.High => "high",
            CodexReasoningEffort.XHigh => "xhigh",
            _ => "medium",
        };

    private static string MapWebSearchMode(CodexWebSearchMode mode)
        => mode switch
        {
            CodexWebSearchMode.Disabled => "disabled",
            CodexWebSearchMode.Cached => "cached",
            CodexWebSearchMode.Live => "live",
            _ => "disabled",
        };

    private static string CreateTurnId()
        => $"turn-{Guid.NewGuid():N}";
}

