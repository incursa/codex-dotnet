using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexRuntimeBehaviorTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0302")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0314")]
    public async Task InitializeAsync_UsesExecBackendMetadataAndCapabilities()
    {
        ScriptedCodexProcessLauncher launcher = new();
        await using CodexClient client = CreateExecClient(launcher);

        CodexRuntimeMetadata metadata = await client.InitializeAsync();

        Assert.Empty(launcher.StartInfos);
        Assert.Equal("codex-exec", metadata.ServerInfo!.Name);
        Assert.False(string.IsNullOrWhiteSpace(metadata.ServerInfo.Version));
        Assert.Equal(CodexBackendSelection.Exec, client.Capabilities!.BackendSelection);
        Assert.True(client.Capabilities.SupportsStartThread);
        Assert.False(client.Capabilities.SupportsListThreads);
        Assert.False(client.Capabilities.SupportsTurnSteering);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0235")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0303")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0304")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0317")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0319")]
    public async Task RunAsync_ReturnsCompletedResultAndTranslatesCliOptions()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        bool responseQueued = false;
        process.StdIn.TextWritten = text =>
        {
            if (responseQueued || !text.Contains("hello codex", StringComparison.Ordinal))
            {
                return;
            }

            responseQueued = true;
            process.EnqueueStdout(TestJson.Notification(
                "turn.started",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-1",
                        ["status"] = "inProgress",
                    },
                }));
            process.EnqueueStdout(TestJson.Notification(
                "item.completed",
                new JsonObject
                {
                    ["threadId"] = "thread-1",
                    ["turnId"] = "turn-1",
                    ["item"] = new JsonObject
                    {
                        ["type"] = "reasoning",
                        ["id"] = "reasoning-1",
                        ["content"] = new JsonArray { "step one", "step two" },
                        ["summary"] = new JsonArray { "thinking" },
                    },
                }));
            process.EnqueueStdout(TestJson.Notification(
                "turn.completed",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-1",
                        ["status"] = "completed",
                        ["items"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "reasoning",
                                ["id"] = "reasoning-1",
                                ["content"] = new JsonArray { "step one", "step two" },
                                ["summary"] = new JsonArray { "thinking" },
                            },
                            new JsonObject
                            {
                                ["type"] = "agentMessage",
                                ["id"] = "message-1",
                                ["phase"] = "finalAnswer",
                                ["text"] = "Echo: hello codex",
                            },
                        },
                        ["usage"] = new JsonObject
                        {
                            ["last"] = new JsonObject
                            {
                                ["inputTokens"] = 10,
                                ["outputTokens"] = 20,
                                ["reasoningOutputTokens"] = 5,
                                ["totalTokens"] = 35,
                            },
                            ["total"] = new JsonObject
                            {
                                ["inputTokens"] = 10,
                                ["outputTokens"] = 20,
                                ["reasoningOutputTokens"] = 5,
                                ["totalTokens"] = 35,
                            },
                        },
                    },
                }));
            process.Complete();
        };

        launcher.Factory = _ => process;

        CodexClientOptions options = new()
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = "codex",
            ProcessLauncher = launcher,
        };

        await using CodexClient client = new(options);
        string workDir = Path.Combine(Path.GetTempPath(), "codex-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        try
        {
            CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
            {
                WorkingDirectory = workDir,
                Model = "gpt-5",
                Sandbox = new CodexDangerFullAccessSandboxPolicy(),
                SkipGitRepoCheck = true,
                AdditionalDirectories = [@"C:\extra-one", @"C:\extra-two"],
                ModelReasoningEffort = CodexReasoningEffort.High,
            });

            CodexRunResult result = await thread.RunAsync("hello codex");

            Assert.Equal(workDir, launcher.StartInfos.Single().WorkingDirectory);
            Assert.Contains("exec", launcher.StartInfos.Single().Arguments);
            Assert.Contains("--experimental-json", launcher.StartInfos.Single().Arguments);
            Assert.Contains("--model", launcher.StartInfos.Single().Arguments);
            Assert.Contains("gpt-5", launcher.StartInfos.Single().Arguments);
            Assert.Contains("--sandbox", launcher.StartInfos.Single().Arguments);
            Assert.Contains("danger-full-access", launcher.StartInfos.Single().Arguments);
            Assert.Contains("--cd", launcher.StartInfos.Single().Arguments);
            Assert.Contains(workDir, launcher.StartInfos.Single().Arguments);
            Assert.Contains("--skip-git-repo-check", launcher.StartInfos.Single().Arguments);
            Assert.Contains("--add-dir", launcher.StartInfos.Single().Arguments);
            Assert.Contains(@"C:\extra-one", launcher.StartInfos.Single().Arguments);
            Assert.Contains(@"C:\extra-two", launcher.StartInfos.Single().Arguments);

            Assert.Equal("Echo: hello codex", result.FinalResponse);
            Assert.NotNull(result.Usage);
            Assert.Contains(result.Items, item => item is CodexReasoningItem);
            Assert.Contains(
                result.Items,
                item => item is CodexAgentMessageItem message && message.Phase == CodexMessagePhase.FinalAnswer);
            Assert.Equal(35, result.Usage!.Total.TotalTokens);
        }
        finally
        {
            try
            {
                Directory.Delete(workDir, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0235")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0303")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0304")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0319")]
    public async Task RunAsync_AppliesTurnOverridesBeforeThreadDefaults()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        bool responseQueued = false;
        process.StdIn.TextWritten = text =>
        {
            if (responseQueued || !text.Contains("override input", StringComparison.Ordinal))
            {
                return;
            }

            responseQueued = true;
            process.EnqueueStdout(TestJson.Notification(
                "turn.started",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-override",
                        ["status"] = "inProgress",
                    },
                }));
            process.EnqueueStdout(TestJson.Notification(
                "turn.completed",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-override",
                        ["status"] = "completed",
                        ["items"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "agentMessage",
                                ["id"] = "message-override",
                                ["phase"] = "finalAnswer",
                                ["text"] = "Echo: override input",
                            },
                        },
                        ["usage"] = new JsonObject
                        {
                            ["last"] = new JsonObject
                            {
                                ["totalTokens"] = 1,
                            },
                            ["total"] = new JsonObject
                            {
                                ["totalTokens"] = 1,
                            },
                        },
                    },
                }));
            process.Complete();
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateExecClient(launcher);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/thread-default",
            Model = "thread-model",
            Sandbox = new CodexReadOnlySandboxPolicy(),
            ModelReasoningEffort = CodexReasoningEffort.Low,
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnFailure),
        });

        CodexRunResult result = await thread.RunAsync(
            "override input",
            new CodexTurnOptions
            {
                WorkingDirectory = "/turn-override",
                Model = "turn-model",
                SandboxPolicy = new CodexDangerFullAccessSandboxPolicy(),
                Effort = CodexReasoningEffort.High,
                ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
            });

        List<string> args = launcher.StartInfos.Single().Arguments.ToList();
        Assert.Equal("/turn-override", launcher.StartInfos.Single().WorkingDirectory);
        Assert.Contains("--model", args);
        Assert.Contains("turn-model", args);
        Assert.Contains("--sandbox", args);
        Assert.Contains("danger-full-access", args);
        Assert.Contains("--cd", args);
        Assert.Contains("/turn-override", args);
        Assert.Contains("model_reasoning_effort=\"high\"", args);
        Assert.Contains("approval_policy=\"on-request\"", args);
        Assert.Equal("Echo: override input", result.FinalResponse);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    public async Task RunStreamedAsync_EmitsLifecycleEvents()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        bool responseQueued = false;
        process.StdIn.TextWritten = text =>
        {
            if (responseQueued || !text.Contains("streamed input", StringComparison.Ordinal))
            {
                return;
            }

            responseQueued = true;
            process.EnqueueStdout(TestJson.Notification(
                "turn.started",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-2",
                        ["status"] = "inProgress",
                    },
                }));
            process.EnqueueStdout(TestJson.Notification(
                "item.started",
                new JsonObject
                {
                    ["threadId"] = "thread-2",
                    ["turnId"] = "turn-2",
                    ["item"] = new JsonObject
                    {
                        ["type"] = "agentMessage",
                        ["id"] = "message-2",
                        ["phase"] = "commentary",
                        ["text"] = "Working",
                    },
                }));
            process.EnqueueStdout(TestJson.Notification(
                "item.completed",
                new JsonObject
                {
                    ["threadId"] = "thread-2",
                    ["turnId"] = "turn-2",
                    ["item"] = new JsonObject
                    {
                        ["type"] = "agentMessage",
                        ["id"] = "message-2",
                        ["phase"] = "finalAnswer",
                        ["text"] = "Done",
                    },
                }));
            process.EnqueueStdout(TestJson.Notification(
                "turn.completed",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-2",
                        ["status"] = "completed",
                        ["items"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "agentMessage",
                                ["id"] = "message-2",
                                ["phase"] = "finalAnswer",
                                ["text"] = "Done",
                            },
                        },
                    },
                }));
            process.Complete();
        };

        launcher.Factory = _ => process;

        await using CodexClient client = new(new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = "codex",
            ProcessLauncher = launcher,
        });

        CodexThread thread = await client.StartThreadAsync();

        List<Type> eventTypes = new();
        await foreach (CodexThreadEvent evt in thread.RunStreamedAsync("streamed input"))
        {
            eventTypes.Add(evt.GetType());
        }

        Assert.Contains(typeof(CodexTurnStartedEvent), eventTypes);
        Assert.Contains(typeof(CodexItemStartedEvent), eventTypes);
        Assert.Contains(typeof(CodexItemCompletedEvent), eventTypes);
        Assert.Contains(typeof(CodexTurnCompletedEvent), eventTypes);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0237")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0304")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0317")]
    public async Task StartTurnAsync_WithoutTerminalEventProducesFailureWithStderrTail()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        bool responseQueued = false;
        process.StdIn.TextWritten = text =>
        {
            if (responseQueued || !text.Contains("broken input", StringComparison.Ordinal))
            {
                return;
            }

            responseQueued = true;
            process.EnqueueStdout(TestJson.Notification(
                "turn.started",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-3",
                        ["status"] = "inProgress",
                    },
                }));
            process.EnqueueStderr("stderr-one");
            process.EnqueueStderr("stderr-two");
            process.Complete(7);
        };

        launcher.Factory = _ => process;

        await using CodexClient client = new(new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = "codex",
            ProcessLauncher = launcher,
        });

        CodexThread thread = await client.StartThreadAsync();
        CodexTurn turn = await thread.StartTurnAsync("broken input");

        List<CodexThreadEvent> events = new();
        await foreach (CodexThreadEvent evt in turn.StreamAsync())
        {
            events.Add(evt);
        }

        CodexTurnFailedEvent failed = Assert.IsType<CodexTurnFailedEvent>(events.Last());
        Assert.Equal(CodexTurnStatus.Failed, failed.Turn.Status);
        Assert.Contains("stderr-two", failed.Turn.Error!.AdditionalDetails ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("stderr-one", failed.Turn.Error.AdditionalDetails ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0237")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0245")]
    public async Task StartTurnAsync_DoesNotLeakOutputSchemaDirectoryWhenLaunchFails()
    {
        ThrowingCodexProcessLauncher launcher = new();

        await using CodexClient client = new(new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = "codex",
            ProcessLauncher = launcher,
        });

        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = Path.Combine(Path.GetTempPath(), "codex-tests", Guid.NewGuid().ToString("N")),
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => thread.RunAsync(
            "launch failure",
            new CodexTurnOptions
            {
                OutputSchema = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["summary"] = new JsonObject
                        {
                            ["type"] = "string",
                        },
                    },
                },
            }));

        Assert.Equal("boom", exception.Message);

        CodexProcessStartInfo startInfo = Assert.Single(launcher.StartInfos);
        string schemaPath = GetArgumentValue(startInfo.Arguments, "--output-schema");
        Assert.False(Directory.Exists(Path.GetDirectoryName(schemaPath)!));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0237")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0304")]
    public async Task StartTurnAsync_CancellationDoesNotKillLiveTurnAfterStartup()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        TaskCompletionSource promptWritten = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool responseQueued = false;

        process.StdIn.TextWritten = text =>
        {
            if (!text.Contains("cancel me", StringComparison.Ordinal))
            {
                return;
            }

            promptWritten.TrySetResult();
            if (responseQueued)
            {
                return;
            }
        };

        process.StdIn.LineWritten = _ => { };
        launcher.Factory = _ => process;

        await using CodexClient client = CreateExecClient(launcher);
        CodexThread thread = await client.StartThreadAsync();

        CancellationTokenSource startCts = new();
        CodexTurn turn = await thread.StartTurnAsync(
            "cancel me",
            new CodexTurnOptions(),
            startCts.Token);

        await promptWritten.Task.WaitAsync(TimeSpan.FromSeconds(5));
        startCts.Cancel();

        if (!responseQueued)
        {
            responseQueued = true;
            process.EnqueueStdout(TestJson.Notification(
                "turn.started",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-cancel",
                        ["status"] = "inProgress",
                    },
                }));
            process.EnqueueStdout(TestJson.Notification(
                "turn.completed",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-cancel",
                        ["status"] = "completed",
                        ["items"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "agentMessage",
                                ["id"] = "message-cancel",
                                ["phase"] = "finalAnswer",
                                ["text"] = "done",
                            },
                        },
                        ["usage"] = new JsonObject
                        {
                            ["last"] = new JsonObject
                            {
                                ["totalTokens"] = 1,
                            },
                            ["total"] = new JsonObject
                            {
                                ["totalTokens"] = 1,
                            },
                        },
                    },
                }));
            process.Complete();
        }

        CodexRunResult result = await turn.RunAsync();

        Assert.False(process.KillCalled);
        Assert.Equal("done", result.FinalResponse);
    }

    private static CodexClient CreateExecClient(ScriptedCodexProcessLauncher launcher)
    {
        CodexClientOptions options = new()
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = "codex",
            ProcessLauncher = launcher,
        };

        return new CodexClient(options);
    }

    private static string GetArgumentValue(IReadOnlyList<string> arguments, string name)
    {
        for (int index = 0; index < arguments.Count - 1; index++)
        {
            if (string.Equals(arguments[index], name, StringComparison.Ordinal))
            {
                return arguments[index + 1];
            }
        }

        throw new InvalidOperationException($"Argument '{name}' was not present.");
    }

    private sealed class ThrowingCodexProcessLauncher : ICodexProcessLauncher
    {
        public List<CodexProcessStartInfo> StartInfos { get; } = [];

        public Task<ICodexProcess> StartAsync(CodexProcessStartInfo startInfo, CancellationToken cancellationToken)
        {
            StartInfos.Add(startInfo);
            return Task.FromException<ICodexProcess>(new InvalidOperationException("boom"));
        }
    }
}


