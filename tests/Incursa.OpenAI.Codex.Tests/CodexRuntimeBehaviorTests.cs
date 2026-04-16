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
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0314")]
    public async Task InitializeAsync_UsesProvidedExecClientIdentity()
    {
        await using CodexClient client = new(new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = "codex",
            ClientName = "TraceClient",
            ClientVersion = "9.9.9",
        });

        CodexRuntimeMetadata metadata = await client.InitializeAsync();

        Assert.Equal("TraceClient/9.9.9", metadata.UserAgent);
        Assert.Equal("codex-exec", metadata.ServerInfo!.Name);
        Assert.Equal("9.9.9", metadata.ServerInfo.Version);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0238")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0239")]
    public async Task InitializeAsync_IsIdempotentAndStartsAppServerOnce()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        int initializeRequests = 0;

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() != "initialize")
            {
                return;
            }

            initializeRequests++;
            process.EnqueueStdout(TestJson.Response(
                request["id"]!.GetValue<string>(),
                new JsonObject
                {
                    ["userAgent"] = "codex-app-server/1.2.3",
                    ["platformFamily"] = "Unix",
                    ["platformOs"] = "Linux",
                }));
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);

        CodexRuntimeMetadata[] metadata = await Task.WhenAll(client.InitializeAsync(), client.InitializeAsync());

        Assert.Single(launcher.StartInfos);
        Assert.Equal(1, initializeRequests);
        Assert.Same(metadata[0], metadata[1]);
        Assert.Equal("codex-app-server", client.Metadata!.ServerInfo!.Name);
        Assert.True(client.Capabilities!.SupportsStartThread);
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
            Config = new CodexConfigObject
            {
                Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                {
                    ["client"] = new CodexConfigObject
                    {
                        Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                        {
                            ["feature"] = new CodexConfigStringValue("enabled"),
                        },
                    },
                },
            },
            ProcessLauncher = launcher,
        };

        await using CodexClient client = new(options);
        string workDir = Path.Combine(Path.GetTempPath(), "codex-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        try
        {
            CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
            {
                Config = new CodexConfigObject
                {
                    Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                    {
                        ["thread"] = new CodexConfigObject
                        {
                            Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                            {
                                ["feature"] = new CodexConfigStringValue("override"),
                            },
                        },
                    },
                },
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
            Assert.Contains("client.feature=\"enabled\"", launcher.StartInfos.Single().Arguments);
            Assert.Contains("thread.feature=\"override\"", launcher.StartInfos.Single().Arguments);

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
            WebSearchEnabled = true,
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
        Assert.Contains(@"web_search=""live""", args);
        Assert.Contains("approval_policy=\"on-request\"", args);
        Assert.Equal("Echo: override input", result.FinalResponse);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0235")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0317")]
    public async Task RunAsync_FallsBackWhenCliEnumValuesAreOutOfRange()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        bool responseQueued = false;
        process.StdIn.TextWritten = text =>
        {
            if (responseQueued || !text.Contains("fallback input", StringComparison.Ordinal))
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
                        ["id"] = "turn-fallback",
                        ["status"] = "inProgress",
                    },
                }));
            process.EnqueueStdout(TestJson.Notification(
                "turn.completed",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-fallback",
                        ["status"] = "completed",
                        ["items"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "agentMessage",
                                ["id"] = "message-fallback",
                                ["phase"] = "finalAnswer",
                                ["text"] = "Echo: fallback input",
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

        await using CodexClient client = new(new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = "codex",
            BaseUrl = "https://example.com/api",
            ProcessLauncher = launcher,
        });

        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WebSearchMode = (CodexWebSearchMode)123,
            ModelReasoningEffort = CodexReasoningEffort.Low,
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnFailure),
        });

        CodexRunResult result = await thread.RunAsync(
            "fallback input",
            new CodexTurnOptions
            {
                Effort = (CodexReasoningEffort)123,
                ApprovalPolicy = new CodexApprovalModePolicy((CodexApprovalMode)123),
            });

        List<string> args = launcher.StartInfos.Single().Arguments.ToList();
        Assert.Contains(@"openai_base_url=""https://example.com/api""", args);
        Assert.Contains(@"model_reasoning_effort=""medium""", args);
        Assert.Contains(@"web_search=""disabled""", args);
        Assert.Contains(@"approval_policy=""on-request""", args);
        Assert.Equal("Echo: fallback input", result.FinalResponse);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0235")]
    [CoverageType(RequirementCoverageType.Negative)]
    public async Task RunAsync_RejectsGranularApprovalPolicyOnExecBackend()
    {
        ScriptedCodexProcessLauncher launcher = new();
        await using CodexClient client = new(new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = "codex",
            ProcessLauncher = launcher,
        });

        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            ApprovalPolicy = new CodexGranularApprovalPolicy(new CodexGranularApprovalRules
            {
                McpElicitations = true,
                RequestPermissions = true,
                Rules = true,
                SandboxApproval = true,
                SkillApproval = true,
            }),
        });

        CodexCapabilityNotSupportedException exception = await Assert.ThrowsAsync<CodexCapabilityNotSupportedException>(() => thread.RunAsync("granular approval"));

        Assert.Equal(CodexBackendSelection.Exec, exception.BackendSelection);
        Assert.Equal("granular approval policies", exception.Operation);
        Assert.Contains("does not support 'granular approval policies'", exception.Message, StringComparison.Ordinal);
        Assert.Empty(launcher.StartInfos);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0235")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0317")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0319")]
    public async Task RunAsync_ResumesThreadAndNormalizesTypedInputs()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        TaskCompletionSource<string> promptSeen = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool responseQueued = false;

        process.StdIn.TextWritten = text =>
        {
            if (!text.Contains("primary", StringComparison.Ordinal))
            {
                return;
            }

            promptSeen.TrySetResult(text);
            if (responseQueued)
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
                        ["id"] = "turn-resume",
                        ["status"] = "inProgress",
                    },
                }));
            process.EnqueueStdout(TestJson.Notification(
                "turn.completed",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-resume",
                        ["status"] = "completed",
                        ["items"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "agentMessage",
                                ["id"] = "message-resume",
                                ["phase"] = "finalAnswer",
                                ["text"] = "Echo: primary",
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

        await using CodexClient client = new(new CodexClientOptions
        {
            BackendSelection = CodexBackendSelection.Exec,
            CodexPathOverride = "codex",
            ProcessLauncher = launcher,
        });

        CodexThread thread = await client.ResumeThreadAsync("thread-resume", new CodexThreadOptions
        {
            Model = "thread-model",
            NetworkAccessEnabled = false,
            WebSearchEnabled = false,
            WorkingDirectory = "/thread-dir",
        });

        CodexRunResult result = await thread.RunAsync(
            new CodexInputItem[]
            {
                new CodexTextInput { Text = "primary" },
                new CodexTextInput { Text = " " },
                new CodexSkillInput { Name = "skill", Path = "/skills/trace" },
                new CodexMentionInput { Name = "mention", Path = "/mentions/trace" },
                new CodexImageInput { Url = "https://example.com/image.png" },
                new CodexImageInput { Url = " " },
                new CodexLocalImageInput { Path = @"C:\images\trace.png" },
                new CodexLocalImageInput { Path = " " },
            },
            new CodexTurnOptions
            {
                Model = " ",
                WorkingDirectory = "/turn-dir",
            });

        string prompt = await promptSeen.Task.WaitAsync(TimeSpan.FromSeconds(5));
        List<string> args = launcher.StartInfos.Single().Arguments.ToList();

        Assert.Equal("/turn-dir", launcher.StartInfos.Single().WorkingDirectory);
        Assert.Contains("resume", args);
        Assert.Contains("thread-resume", args);
        Assert.Contains("--model", args);
        Assert.Contains("thread-model", args);
        Assert.Contains(@"sandbox_workspace_write.network_access=false", args);
        Assert.Contains(@"web_search=""disabled""", args);
        Assert.Contains("--image", args);
        Assert.Contains("https://example.com/image.png", args);
        Assert.Contains(@"C:\images\trace.png", args);
        Assert.DoesNotContain(" ", args);
        Assert.Equal(
            string.Join(Environment.NewLine + Environment.NewLine, ["primary", "[skill] /skills/trace", "[mention] /mentions/trace"]),
            prompt);
        Assert.Equal("Echo: primary", result.FinalResponse);
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
        Assert.Equal("Codex exec exited unexpectedly with code 7.", failed.Turn.Error!.Message);
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
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0238")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0239")]
    public async Task DisposeAsync_IsIdempotentAndPreventsFurtherUse()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() != "initialize")
            {
                return;
            }

            process.EnqueueStdout(TestJson.Response(
                request["id"]!.GetValue<string>(),
                new JsonObject
                {
                    ["userAgent"] = "codex-app-server/1.2.3",
                    ["platformFamily"] = "Unix",
                    ["platformOs"] = "Linux",
                }));
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        await client.InitializeAsync();

        await client.DisposeAsync();
        await client.DisposeAsync();

        Assert.True(process.HasExited);

        await Assert.ThrowsAsync<CodexTransportClosedException>(async () => await client.StartThreadAsync());
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
        await process.StdOut.ReadStarted.WaitAsync(TimeSpan.FromSeconds(5));
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

    private static CodexClient CreateAppServerClient(ScriptedCodexProcessLauncher launcher)
    {
        CodexClientOptions options = new()
        {
            BackendSelection = CodexBackendSelection.AppServer,
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
