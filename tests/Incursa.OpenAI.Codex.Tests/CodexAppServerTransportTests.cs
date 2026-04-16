using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexAppServerTransportTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0238")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0239")]
    public async Task InitializeAsync_PerformsHandshakeAndNormalizesMetadata()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        bool completed = false;

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            string method = request["method"]!.GetValue<string>();
            if (method == "initialize")
            {
                process.EnqueueStdout(TestJson.Response(
                    idNode.GetValue<string>(),
                    new JsonObject
                    {
                        ["userAgent"] = "codex-app-server/1.2.3",
                        ["platformFamily"] = "Unix",
                        ["platformOs"] = "Linux",
                    }));
            }
            else if (!completed && method == "model/list")
            {
                completed = true;
                process.EnqueueStdout(TestJson.Response(
                    idNode.GetValue<string>(),
                    new JsonObject
                    {
                        ["models"] = new JsonArray(),
                    }));
                process.Complete();
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexRuntimeMetadata metadata = await client.InitializeAsync();

        Assert.Equal("codex-app-server", metadata.ServerInfo!.Name);
        Assert.Equal("1.2.3", metadata.ServerInfo.Version);
        Assert.True(client.Capabilities!.SupportsStartThread);
        Assert.True(client.Capabilities.SupportsTurnSteering);
        Assert.True(client.Capabilities.SupportsListModels);

        Assert.Contains(process.StdIn.Lines, line => line.Contains("\"method\":\"initialized\"", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0238")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0239")]
    [CoverageType(RequirementCoverageType.Positive)]
    public async Task InitializeAsync_PreservesExplicitServerInfoFromHandshake()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        bool completed = false;

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            string method = request["method"]!.GetValue<string>();
            if (method == "initialize")
            {
                process.EnqueueStdout(TestJson.Response(
                    idNode.GetValue<string>(),
                    new JsonObject
                    {
                        ["serverInfo"] = new JsonObject
                        {
                            ["name"] = "codex-app-server-custom",
                            ["version"] = "9.9.9",
                        },
                        ["userAgent"] = "codex-app-server/1.2.3",
                        ["platformFamily"] = "Unix",
                        ["platformOs"] = "Linux",
                    }));
            }
            else if (!completed && method == "model/list")
            {
                completed = true;
                process.EnqueueStdout(TestJson.Response(
                    idNode.GetValue<string>(),
                    new JsonObject
                    {
                        ["models"] = new JsonArray(),
                    }));
                process.Complete();
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexRuntimeMetadata metadata = await client.InitializeAsync();

        Assert.Equal("codex-app-server-custom", metadata.ServerInfo!.Name);
        Assert.Equal("9.9.9", metadata.ServerInfo.Version);
        Assert.True(client.Capabilities!.SupportsStartThread);
        Assert.True(client.Capabilities.SupportsTurnSteering);
        Assert.True(client.Capabilities.SupportsListModels);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0238")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0239")]
    [CoverageType(RequirementCoverageType.Negative)]
    public async Task InitializeAsync_RejectsHandshakeWithoutServerIdentity()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request["method"]?.GetValue<string>() == "initialize")
            {
                string id = request["id"]!.GetValue<string>();
                process.EnqueueStdout(TestJson.Response(
                    id,
                    new JsonObject
                    {
                        ["platformFamily"] = "Unix",
                        ["platformOs"] = "Linux",
                    }));
                process.Complete();
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.InitializeAsync());

        Assert.Contains("missing required server identity", exception.Message, StringComparison.Ordinal);
        Assert.Single(launcher.StartInfos);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0238")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0244")]
    [Trait("Requirement", "REQ-CODEX-SDK-DI-0263")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0309")]
    [CoverageType(RequirementCoverageType.Positive)]
    public async Task StartThreadAsync_EmitsClientConfigAndGranularApprovalPolicy()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            string method = request["method"]!.GetValue<string>();
            if (method == "initialize")
            {
                process.EnqueueStdout(TestJson.Response(
                    idNode.GetValue<string>(),
                    new JsonObject
                    {
                        ["userAgent"] = "codex-app-server/1.2.3",
                        ["platformFamily"] = "Unix",
                        ["platformOs"] = "Linux",
                    }));
                return;
            }

            if (method == "thread/start")
            {
                JsonObject payload = request["params"]!.AsObject();
                Assert.Equal("thread-model", payload["model"]!.GetValue<string>());
                Assert.Equal("override", payload["config"]!["thread"]!["feature"]!.GetValue<string>());
                Assert.Equal("user", payload["approvalsReviewer"]!.GetValue<string>());

                JsonObject granular = payload["approvalPolicy"]!["granular"]!.AsObject();
                Assert.True(granular["mcpElicitations"]!.GetValue<bool>());
                Assert.False(granular["requestPermissions"]!.GetValue<bool>());
                Assert.True(granular["rules"]!.GetValue<bool>());
                Assert.False(granular["sandboxApproval"]!.GetValue<bool>());
                Assert.True(granular["skillApproval"]!.GetValue<bool>());

                process.EnqueueStdout(TestJson.Response(
                    idNode.GetValue<string>(),
                    new JsonObject
                    {
                        ["thread"] = CreateThreadSnapshot("thread-1"),
                    }));
                process.Complete();
            }
        };

        launcher.Factory = _ => process;

        CodexClientOptions options = new()
        {
            BackendSelection = CodexBackendSelection.AppServer,
            CodexPathOverride = "codex",
            ProcessLauncher = launcher,
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
        };

        await using CodexClient client = new(options);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "thread-model",
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
            ApprovalPolicy = new CodexGranularApprovalPolicy(new CodexGranularApprovalRules
            {
                McpElicitations = true,
                RequestPermissions = false,
                Rules = true,
                SandboxApproval = false,
                SkillApproval = true,
            }),
            ApprovalsReviewer = CodexApprovalsReviewer.User,
        });

        Assert.Equal("thread-1", thread.Id);
        Assert.Contains("client.feature=\"enabled\"", launcher.StartInfos.Single().Arguments);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0240")]
    public async Task ThreadAndModelOperations_UseTypedAppServerRequests()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            string method = methodValue.GetValue<string>();
            switch (method)
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1"),
                        }));
                    break;
                case "thread/read":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1", name: "Read thread", includeTurns: true),
                        }));
                    break;
                case "thread/list":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["data"] = new JsonArray
                            {
                                CreateThreadSummary("thread-1", name: "Read thread"),
                                CreateThreadSummary("thread-2", name: "Forked thread"),
                            },
                            ["nextCursor"] = "thread-cursor",
                        }));
                    break;
                case "thread/name/set":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1", name: "Renamed thread"),
                        }));
                    break;
                case "thread/archive":
                    process.EnqueueStdout(TestJson.Response(id, new JsonObject()));
                    break;
                case "thread/unarchive":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1", name: "Renamed thread"),
                        }));
                    break;
                case "thread/fork":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-2", name: "Forked thread"),
                        }));
                    break;
                case "thread/compact/start":
                    process.EnqueueStdout(TestJson.Response(id, new JsonObject()));
                    break;
                case "model/list":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["models"] = new JsonArray
                            {
                                CreateModel("model-1", "gpt-5"),
                            },
                            ["nextCursor"] = "model-cursor",
                        }));
                    break;
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
            AdditionalDirectories = ["/extra"],
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
            ModelReasoningEffort = CodexReasoningEffort.Medium,
        });

        Assert.Equal("thread-1", thread.Id);

        CodexThreadSnapshot readSnapshot = await thread.ReadAsync(includeTurns: true);
        Assert.Equal("Read thread", readSnapshot.Name);
        Assert.Single(readSnapshot.Turns);
        Assert.Equal("turn-1", readSnapshot.Turns[0].Id);

        CodexThreadSnapshot renamedSnapshot = await thread.SetNameAsync("Renamed thread");
        Assert.Equal("Renamed thread", renamedSnapshot.Name);

        await thread.CompactAsync();

        await client.ArchiveThreadAsync(thread.Id!);

        CodexThread unarchivedThread = await client.UnarchiveThreadAsync(thread.Id!);
        Assert.Equal("thread-1", unarchivedThread.Id);

        CodexThread forkedThread = await client.ForkThreadAsync(thread.Id!, new CodexThreadForkOptions
        {
            WorkingDirectory = "/fork",
        });
        Assert.Equal("thread-2", forkedThread.Id);

        CodexThreadListResult threads = await client.ListThreadsAsync(new CodexThreadListOptions
        {
            Archived = true,
            Cursor = "cursor-1",
            WorkingDirectory = "/work",
            Limit = 10,
            ModelProviders = ["openai"],
            SearchTerm = "trace",
            SortKey = CodexThreadSortKey.UpdatedAt,
            SourceKinds = [CodexThreadSourceKind.AppServer],
        });

        Assert.Equal("thread-cursor", threads.NextCursor);
        Assert.Equal(2, threads.Threads.Count);
        Assert.Equal("thread-1", threads.Threads[0].Id);

        CodexModelListResult models = await client.ListModelsAsync(new CodexModelListOptions
        {
            IncludeHidden = true,
        });

        Assert.Equal("model-cursor", models.NextCursor);
        Assert.Single(models.Models);
        Assert.Equal("model-1", models.Models[0].Id);
        Assert.Equal(CodexReasoningEffort.Medium, models.Models[0].DefaultReasoningEffort);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0240")]
    [CoverageType(RequirementCoverageType.Edge)]
    public async Task SetThreadNameAsync_ReturnsFallbackSnapshotWhenServerOmitsThreadObject()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            switch (methodValue.GetValue<string>())
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-rename"),
                        }));
                    break;
                case "thread/name/set":
                    process.EnqueueStdout(TestJson.Response(id, new JsonObject()));
                    process.Complete();
                    break;
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
        });

        CodexThreadSnapshot renamed = await thread.SetNameAsync("Renamed thread");

        Assert.Equal("thread-rename", renamed.Id);
        Assert.Equal("Renamed thread", renamed.Name);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0240")]
    [CoverageType(RequirementCoverageType.Negative)]
    public async Task ReadThreadAsync_RejectsResponsesWithoutThreadObject()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            switch (methodValue.GetValue<string>())
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1"),
                        }));
                    break;
                case "thread/read":
                    process.EnqueueStdout(TestJson.Response(id, new JsonObject()));
                    process.Complete();
                    break;
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
        });

        CodexInvalidRequestException exception = await Assert.ThrowsAsync<CodexInvalidRequestException>(() => thread.ReadAsync());

        Assert.Contains("Expected 'thread' to be a JSON object.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0240")]
    [CoverageType(RequirementCoverageType.Negative)]
    public async Task StartTurnAsync_RejectsResponsesWithoutTurnObject()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            switch (methodValue.GetValue<string>())
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1"),
                        }));
                    break;
                case "turn/start":
                    process.EnqueueStdout(TestJson.Response(id, new JsonObject()));
                    process.Complete();
                    break;
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
        });

        CodexInvalidRequestException exception = await Assert.ThrowsAsync<CodexInvalidRequestException>(() => thread.StartTurnAsync("broken"));

        Assert.Contains("Expected 'turn' to be a JSON object.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0241")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0243")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    public async Task TurnStream_PreservesUnknownNotificationsAndCompletes()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            string method = methodValue.GetValue<string>();
            switch (method)
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1"),
                        }));
                    break;
                case "turn/start":
                    process.EnqueueStdout(TestJson.Message(
                        "item/commandExecution/requestApproval",
                        new JsonObject
                        {
                            ["reason"] = "needs approval",
                        },
                        "approval-1"));
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["threadId"] = "thread-1",
                            ["turn"] = new JsonObject
                            {
                                ["id"] = "turn-1",
                                ["status"] = "inProgress",
                            },
                        }));
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
                        "custom/runtime-event",
                        new JsonObject
                        {
                            ["note"] = "mystery",
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
                                        ["type"] = "agentMessage",
                                        ["id"] = "message-1",
                                        ["phase"] = "finalAnswer",
                                        ["text"] = "Done",
                                    },
                                },
                                ["usage"] = new JsonObject
                                {
                                    ["last"] = new JsonObject
                                    {
                                        ["totalTokens"] = 2,
                                    },
                                    ["total"] = new JsonObject
                                    {
                                        ["totalTokens"] = 2,
                                    },
                                },
                            },
                        }));
                    break;
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
        });

        CodexTurn turn = await thread.StartTurnAsync("primary context");
        List<CodexThreadEvent> events = new();
        await foreach (CodexThreadEvent evt in turn.StreamAsync())
        {
            events.Add(evt);
        }

        Assert.Contains(events, evt => evt is CodexTurnStartedEvent);
        Assert.Contains(events, evt => evt is CodexUnknownThreadEvent unknown && unknown.UnknownType == "custom.runtime-event");
        CodexTurnCompletedEvent completed = Assert.IsType<CodexTurnCompletedEvent>(events.Last(evt => evt is CodexTurnCompletedEvent));
        Assert.Equal("Done", ((CodexAgentMessageItem)completed.Turn.Items.Single()).Text);
        Assert.Contains(process.StdIn.Lines, line => line.Contains("\"decision\":\"accept\"", StringComparison.Ordinal));

        process.Complete();
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0304")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    public async Task TurnControls_StreamAndControlRequestsRemainConcurrent()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        bool sawSteer = false;
        bool sawInterrupt = false;

        void MaybeComplete()
        {
            if (!sawSteer || !sawInterrupt)
            {
                return;
            }

            process.EnqueueStdout(TestJson.Notification(
                "turn.completed",
                new JsonObject
                {
                    ["turn"] = new JsonObject
                    {
                        ["id"] = "turn-1",
                        ["status"] = "interrupted",
                        ["items"] = new JsonArray(),
                    },
                }));
            process.Complete();
        }

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            string method = methodValue.GetValue<string>();
            switch (method)
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1"),
                        }));
                    break;
                case "turn/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["threadId"] = "thread-1",
                            ["turn"] = new JsonObject
                            {
                                ["id"] = "turn-1",
                                ["status"] = "inProgress",
                            },
                        }));
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
                    break;
                case "turn/steer":
                    sawSteer = true;
                    process.EnqueueStdout(TestJson.Response(id, new JsonObject()));
                    MaybeComplete();
                    break;
                case "turn/interrupt":
                    sawInterrupt = true;
                    process.EnqueueStdout(TestJson.Response(id, new JsonObject()));
                    MaybeComplete();
                    break;
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
        });

        CodexTurn turn = await thread.StartTurnAsync("primary context");
        await turn.SteerAsync([new CodexTextInput { Text = "extra context" }]);
        await turn.InterruptAsync();

        List<CodexThreadEvent> events = new();
        await foreach (CodexThreadEvent evt in turn.StreamAsync())
        {
            events.Add(evt);
        }

        Assert.Contains(events, evt => evt is CodexTurnStartedEvent);
        CodexTurnCompletedEvent completed = Assert.IsType<CodexTurnCompletedEvent>(events.Last(evt => evt is CodexTurnCompletedEvent));
        Assert.Equal(CodexTurnStatus.Interrupted, completed.Turn.Status);
        Assert.Contains(process.StdIn.Lines, line => line.Contains("\"method\":\"turn/steer\"", StringComparison.Ordinal));
        Assert.Contains(process.StdIn.Lines, line => line.Contains("\"method\":\"turn/interrupt\"", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0241")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    [CoverageType(RequirementCoverageType.Positive)]
    public async Task TurnStream_DefaultApprovalHandlerAcceptsFileChangeRequests()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            switch (methodValue.GetValue<string>())
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1"),
                        }));
                    break;
                case "turn/start":
                    process.EnqueueStdout(TestJson.Message(
                        "item/fileChange/requestApproval",
                        new JsonObject
                        {
                            ["reason"] = "needs review",
                        },
                        "approval-1"));
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["threadId"] = "thread-1",
                            ["turn"] = new JsonObject
                            {
                                ["id"] = "turn-1",
                                ["status"] = "inProgress",
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
                                        ["type"] = "agentMessage",
                                        ["id"] = "message-1",
                                        ["phase"] = "finalAnswer",
                                        ["text"] = "Done",
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
                    break;
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
        });

        CodexTurn turn = await thread.StartTurnAsync("file change");
        List<CodexThreadEvent> events = new();
        await foreach (CodexThreadEvent evt in turn.StreamAsync())
        {
            events.Add(evt);
        }

        Assert.Contains(events, evt => evt is CodexTurnCompletedEvent completed && completed.Turn.Id == "turn-1");

        JsonObject approvalResponse = process.StdIn.Lines
            .Select(line => JsonNode.Parse(line)!.AsObject())
            .Single(message => message["id"]?.GetValue<string>() == "approval-1");
        JsonObject approvalResult = approvalResponse["result"]!.AsObject();
        Assert.Equal("accept", approvalResult["decision"]!.GetValue<string>());
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0241")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    [CoverageType(RequirementCoverageType.Positive)]
    public async Task TurnStream_CustomApprovalHandlerOverridesDefaultDecision()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        TaskCompletionSource approvalResponseSeen = new(TaskCreationOptions.RunContinuationsAsynchronously);

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request.TryGetPropertyValue("id", out JsonNode? responseIdNode) && responseIdNode?.GetValue<string>() == "approval-1")
            {
                approvalResponseSeen.TrySetResult();
                return;
            }

            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            switch (methodValue.GetValue<string>())
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1"),
                        }));
                    break;
                case "turn/start":
                    process.EnqueueStdout(TestJson.Message(
                        "item/commandExecution/requestApproval",
                        new JsonObject
                        {
                            ["reason"] = "custom decision needed",
                        },
                        "approval-1"));
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["threadId"] = "thread-1",
                            ["turn"] = new JsonObject
                            {
                                ["id"] = "turn-1",
                                ["status"] = "inProgress",
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
                                        ["type"] = "agentMessage",
                                        ["id"] = "message-1",
                                        ["phase"] = "finalAnswer",
                                        ["text"] = "Done",
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
                    break;
            }
        };

        launcher.Factory = _ => process;

        CodexClientOptions options = new()
        {
            BackendSelection = CodexBackendSelection.AppServer,
            CodexPathOverride = "codex",
            ProcessLauncher = launcher,
            ApprovalHandler = (action, request) => new JsonObject
            {
                ["decision"] = "reject",
                ["action"] = action,
                ["reasonSeen"] = request?["reason"]?.GetValue<string>(),
            },
        };

        await using CodexClient client = new(options);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
        });

        CodexTurn turn = await thread.StartTurnAsync("custom approval");
        await approvalResponseSeen.Task.WaitAsync(TimeSpan.FromSeconds(5));

        JsonObject approvalResponse = process.StdIn.Lines
            .Select(line => JsonNode.Parse(line)!.AsObject())
            .Single(message => message["id"]?.GetValue<string>() == "approval-1");
        JsonObject approvalResult = approvalResponse["result"]!.AsObject();
        Assert.Equal("turn-1", turn.Id);
        Assert.Equal("reject", approvalResult["decision"]!.GetValue<string>());
        Assert.Equal("item/commandExecution/requestApproval", approvalResult["action"]!.GetValue<string>());
        Assert.Equal("custom decision needed", approvalResult["reasonSeen"]!.GetValue<string>());

        process.Complete();
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0241")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    [CoverageType(RequirementCoverageType.Edge)]
    public async Task TurnStream_CustomApprovalHandlerNullResultFallsBackToEmptyResponse()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        TaskCompletionSource<JsonObject> approvalResponseSeen = new(TaskCreationOptions.RunContinuationsAsynchronously);

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (request.TryGetPropertyValue("id", out JsonNode? responseIdNode) && responseIdNode?.GetValue<string>() == "approval-1")
            {
                approvalResponseSeen.TrySetResult(request["result"]!.AsObject());
                return;
            }

            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            switch (methodValue.GetValue<string>())
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1"),
                        }));
                    break;
                case "turn/start":
                    process.EnqueueStdout(TestJson.Message(
                        "item/commandExecution/requestApproval",
                        new JsonObject
                        {
                            ["reason"] = "null handler fallback",
                        },
                        "approval-1"));
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["threadId"] = "thread-1",
                            ["turn"] = new JsonObject
                            {
                                ["id"] = "turn-1",
                                ["status"] = "inProgress",
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
                                        ["type"] = "agentMessage",
                                        ["id"] = "message-1",
                                        ["phase"] = "finalAnswer",
                                        ["text"] = "Done",
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
                    break;
            }
        };

        launcher.Factory = _ => process;

        CodexClientOptions options = new()
        {
            BackendSelection = CodexBackendSelection.AppServer,
            CodexPathOverride = "codex",
            ProcessLauncher = launcher,
            ApprovalHandler = (_, _) => null,
        };

        await using CodexClient client = new(options);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
        });

        CodexTurn turn = await thread.StartTurnAsync("null approval");
        await approvalResponseSeen.Task.WaitAsync(TimeSpan.FromSeconds(5));

        JsonObject approvalResult = await approvalResponseSeen.Task;
        Assert.Equal("turn-1", turn.Id);
        Assert.Empty(approvalResult);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0241")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    [CoverageType(RequirementCoverageType.Edge)]
    public async Task TurnStream_BuffersNotificationsBeforeSessionRegistration()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            switch (methodValue.GetValue<string>())
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1"),
                        }));
                    break;
                case "turn/start":
                    process.EnqueueStdout(TestJson.Notification(
                        "item.completed",
                        new JsonObject
                        {
                            ["threadId"] = "thread-1",
                            ["turnId"] = "turn-buffered",
                            ["item"] = new JsonObject
                            {
                                ["type"] = "agentMessage",
                                ["id"] = "message-buffered",
                                ["phase"] = "finalAnswer",
                                ["text"] = "Buffered",
                            },
                        }));
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["threadId"] = "thread-1",
                            ["turn"] = new JsonObject
                            {
                                ["id"] = "turn-buffered",
                                ["status"] = "inProgress",
                            },
                        }));
                    process.EnqueueStdout(TestJson.Notification(
                        "turn.started",
                        new JsonObject
                        {
                            ["turn"] = new JsonObject
                            {
                                ["id"] = "turn-buffered",
                                ["status"] = "inProgress",
                            },
                        }));
                    process.EnqueueStdout(TestJson.Notification(
                        "turn.completed",
                        new JsonObject
                        {
                            ["turn"] = new JsonObject
                            {
                                ["id"] = "turn-buffered",
                                ["status"] = "completed",
                                ["items"] = new JsonArray
                                {
                                    new JsonObject
                                    {
                                        ["type"] = "agentMessage",
                                        ["id"] = "message-buffered",
                                        ["phase"] = "finalAnswer",
                                        ["text"] = "Buffered",
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
                    break;
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
        });

        CodexTurn turn = await thread.StartTurnAsync("buffered context");
        List<CodexThreadEvent> events = new();
        await foreach (CodexThreadEvent evt in turn.StreamAsync())
        {
            events.Add(evt);
        }

        Assert.Contains(events, evt => evt is CodexItemCompletedEvent completed && completed.TurnId == "turn-buffered" && ((CodexAgentMessageItem)completed.Item).Text == "Buffered");
        Assert.Contains(events, evt => evt is CodexTurnStartedEvent started && started.Turn.Id == "turn-buffered");
        Assert.Contains(events, evt => evt is CodexTurnCompletedEvent completed && completed.Turn.Id == "turn-buffered");
        Assert.All(events.OfType<CodexItemCompletedEvent>(), evt => Assert.Equal("turn-buffered", evt.TurnId));

        process.Complete();
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0241")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    [CoverageType(RequirementCoverageType.Edge)]
    public async Task TurnStream_RoutesUnkeyedNotificationsToTheMostRecentSession()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            switch (methodValue.GetValue<string>())
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-1"),
                        }));
                    break;
                case "turn/start":
                    process.EnqueueStdout(TestJson.Notification(
                        "custom.event",
                        new JsonObject
                        {
                            ["note"] = "fallback",
                        }));
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["threadId"] = "thread-1",
                            ["turn"] = new JsonObject
                            {
                                ["id"] = "turn-fallback",
                                ["status"] = "inProgress",
                            },
                        }));
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
                                        ["text"] = "Fallback",
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
                    break;
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
        });

        CodexTurn turn = await thread.StartTurnAsync("fallback");

        List<CodexThreadEvent> events = new();
        await foreach (CodexThreadEvent evt in turn.StreamAsync())
        {
            events.Add(evt);
        }

        Assert.Contains(events, evt => evt is CodexUnknownThreadEvent unknown
            && unknown.UnknownType == "custom.event"
            && unknown.RawPayload?["note"]?.GetValue<string>() == "fallback");
        Assert.Contains(events, evt => evt is CodexTurnCompletedEvent completed && completed.Turn.Id == "turn-fallback");

        process.Complete();
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0304")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    public async Task TurnStreams_RouteInterleavedNotificationsToMatchingSessions()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();
        int threadStarts = 0;
        int turnStarts = 0;

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            string method = methodValue.GetValue<string>();
            switch (method)
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    threadStarts++;
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot(threadStarts == 1 ? "thread-a" : "thread-b"),
                        }));
                    break;
                case "turn/start":
                    turnStarts++;
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["threadId"] = turnStarts == 1 ? "thread-a" : "thread-b",
                            ["turn"] = new JsonObject
                            {
                                ["id"] = turnStarts == 1 ? "turn-a" : "turn-b",
                                ["status"] = "inProgress",
                            },
                        }));
                    break;
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexThread threadA = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work-a",
            Model = "gpt-5",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
        });
        CodexThread threadB = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work-b",
            Model = "gpt-5",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
        });

        CodexTurn turnA = await threadA.StartTurnAsync("turn a");
        CodexTurn turnB = await threadB.StartTurnAsync("turn b");

        process.EnqueueStdout(TestJson.Notification(
            "turn.started",
            new JsonObject
            {
                ["turn"] = new JsonObject
                {
                    ["id"] = "turn-a",
                    ["status"] = "inProgress",
                },
            }));
        process.EnqueueStdout(TestJson.Notification(
            "turn.started",
            new JsonObject
            {
                ["turn"] = new JsonObject
                {
                    ["id"] = "turn-b",
                    ["status"] = "inProgress",
                },
            }));
        process.EnqueueStdout(TestJson.Notification(
            "item.completed",
            new JsonObject
            {
                ["threadId"] = "thread-b",
                ["turnId"] = "turn-b",
                ["item"] = new JsonObject
                {
                    ["type"] = "agentMessage",
                    ["id"] = "message-b",
                    ["phase"] = "finalAnswer",
                    ["text"] = "B",
                },
            }));
        process.EnqueueStdout(TestJson.Notification(
            "item.completed",
            new JsonObject
            {
                ["threadId"] = "thread-a",
                ["turnId"] = "turn-a",
                ["item"] = new JsonObject
                {
                    ["type"] = "agentMessage",
                    ["id"] = "message-a",
                    ["phase"] = "finalAnswer",
                    ["text"] = "A",
                },
            }));
        process.EnqueueStdout(TestJson.Notification(
            "turn.completed",
            new JsonObject
            {
                ["turn"] = new JsonObject
                {
                    ["id"] = "turn-b",
                    ["status"] = "completed",
                    ["items"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "agentMessage",
                            ["id"] = "message-b",
                            ["phase"] = "finalAnswer",
                            ["text"] = "B",
                        },
                    },
                },
            }));
        process.EnqueueStdout(TestJson.Notification(
            "turn.completed",
            new JsonObject
            {
                ["turn"] = new JsonObject
                {
                    ["id"] = "turn-a",
                    ["status"] = "completed",
                    ["items"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "agentMessage",
                            ["id"] = "message-a",
                            ["phase"] = "finalAnswer",
                            ["text"] = "A",
                        },
                    },
                },
            }));
        process.Complete();

        List<CodexThreadEvent> turnAEvents = new();
        await foreach (CodexThreadEvent evt in turnA.StreamAsync())
        {
            turnAEvents.Add(evt);
        }

        List<CodexThreadEvent> turnBEvents = new();
        await foreach (CodexThreadEvent evt in turnB.StreamAsync())
        {
            turnBEvents.Add(evt);
        }

        Assert.Contains(turnAEvents, evt => evt is CodexTurnStartedEvent started && started.Turn.Id == "turn-a");
        Assert.Contains(turnAEvents, evt => evt is CodexItemCompletedEvent completed && completed.TurnId == "turn-a" && ((CodexAgentMessageItem)completed.Item).Text == "A");
        Assert.Contains(turnAEvents, evt => evt is CodexTurnCompletedEvent completed && completed.Turn.Id == "turn-a");
        Assert.All(turnAEvents.OfType<CodexItemCompletedEvent>(), evt => Assert.Equal("turn-a", evt.TurnId));

        Assert.Contains(turnBEvents, evt => evt is CodexTurnStartedEvent started && started.Turn.Id == "turn-b");
        Assert.Contains(turnBEvents, evt => evt is CodexItemCompletedEvent completed && completed.TurnId == "turn-b" && ((CodexAgentMessageItem)completed.Item).Text == "B");
        Assert.Contains(turnBEvents, evt => evt is CodexTurnCompletedEvent completed && completed.Turn.Id == "turn-b");
        Assert.All(turnBEvents.OfType<CodexItemCompletedEvent>(), evt => Assert.Equal("turn-b", evt.TurnId));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0238")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0241")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0303")]
    [Trait("Requirement", "REQ-CODEX-SDK-CATALOG-0304")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0319")]
    [CoverageType(RequirementCoverageType.Positive)]
    public async Task StartResumeAndForkThreadAsync_PreserveThreadDefaultsForImplicitTurns()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            switch (methodValue.GetValue<string>())
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    return;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-start"),
                        }));
                    return;
                case "thread/resume":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-resumed"),
                        }));
                    return;
                case "thread/fork":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-forked"),
                        }));
                    return;
                case "turn/start":
                    JsonObject payload = request["params"]!.AsObject();
                    string prompt = payload["input"]!.AsArray()[0]!.AsObject()["text"]!.GetValue<string>();

                    switch (prompt)
                    {
                        case "start defaults":
                            Assert.Equal("thread-start", payload["threadId"]!.GetValue<string>());
                            Assert.Equal("/start-dir", payload["workingDirectory"]!.GetValue<string>());
                            Assert.Equal("start-model", payload["model"]!.GetValue<string>());
                            Assert.Equal("on-request", payload["approvalPolicy"]!["value"]!.GetValue<string>());
                            Assert.Equal("dangerFullAccess", payload["sandboxPolicy"]!["type"]!.GetValue<string>());
                            Assert.Equal("high", payload["effort"]!.GetValue<string>());
                            process.EnqueueStdout(TestJson.Response(
                                id,
                                new JsonObject
                                {
                                    ["threadId"] = "thread-start",
                                    ["turn"] = new JsonObject
                                    {
                                        ["id"] = "turn-start",
                                        ["status"] = "inProgress",
                                    },
                                }));
                            process.EnqueueStdout(TestJson.Notification(
                                "turn.started",
                                new JsonObject
                                {
                                    ["turn"] = new JsonObject
                                    {
                                        ["id"] = "turn-start",
                                        ["status"] = "inProgress",
                                    },
                                }));
                            process.EnqueueStdout(TestJson.Notification(
                                "turn.completed",
                                new JsonObject
                                {
                                    ["turn"] = new JsonObject
                                    {
                                        ["id"] = "turn-start",
                                        ["status"] = "completed",
                                        ["items"] = new JsonArray
                                        {
                                            new JsonObject
                                            {
                                                ["type"] = "agentMessage",
                                                ["id"] = "message-start",
                                                ["phase"] = "finalAnswer",
                                                ["text"] = "Echo: start defaults",
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
                            return;
                        case "resume defaults":
                            Assert.Equal("thread-resumed", payload["threadId"]!.GetValue<string>());
                            Assert.Equal("/resume-dir", payload["workingDirectory"]!.GetValue<string>());
                            Assert.Equal("resume-model", payload["model"]!.GetValue<string>());
                            Assert.Equal("on-failure", payload["approvalPolicy"]!["value"]!.GetValue<string>());
                            Assert.Equal("readOnly", payload["sandboxPolicy"]!["type"]!.GetValue<string>());
                            Assert.Equal("low", payload["effort"]!.GetValue<string>());
                            process.EnqueueStdout(TestJson.Response(
                                id,
                                new JsonObject
                                {
                                    ["threadId"] = "thread-resumed",
                                    ["turn"] = new JsonObject
                                    {
                                        ["id"] = "turn-resume",
                                        ["status"] = "inProgress",
                                    },
                                }));
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
                                                ["text"] = "Echo: resume defaults",
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
                            return;
                        case "fork defaults":
                            Assert.Equal("thread-forked", payload["threadId"]!.GetValue<string>());
                            Assert.Equal("/fork-dir", payload["workingDirectory"]!.GetValue<string>());
                            Assert.Equal("fork-model", payload["model"]!.GetValue<string>());
                            Assert.Equal("on-request", payload["approvalPolicy"]!["value"]!.GetValue<string>());
                            Assert.Equal("dangerFullAccess", payload["sandboxPolicy"]!["type"]!.GetValue<string>());
                            Assert.Equal("medium", payload["effort"]!.GetValue<string>());
                            process.EnqueueStdout(TestJson.Response(
                                id,
                                new JsonObject
                                {
                                    ["threadId"] = "thread-forked",
                                    ["turn"] = new JsonObject
                                    {
                                        ["id"] = "turn-fork",
                                        ["status"] = "inProgress",
                                    },
                                }));
                            process.EnqueueStdout(TestJson.Notification(
                                "turn.started",
                                new JsonObject
                                {
                                    ["turn"] = new JsonObject
                                    {
                                        ["id"] = "turn-fork",
                                        ["status"] = "inProgress",
                                    },
                                }));
                            process.EnqueueStdout(TestJson.Notification(
                                "turn.completed",
                                new JsonObject
                                {
                                    ["turn"] = new JsonObject
                                    {
                                        ["id"] = "turn-fork",
                                        ["status"] = "completed",
                                        ["items"] = new JsonArray
                                        {
                                            new JsonObject
                                            {
                                                ["type"] = "agentMessage",
                                                ["id"] = "message-fork",
                                                ["phase"] = "finalAnswer",
                                                ["text"] = "Echo: fork defaults",
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
                            return;
                        default:
                            throw new InvalidOperationException($"Unexpected prompt '{prompt}'.");
                    }
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);

        CodexThread startedThread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/start-dir",
            Model = "start-model",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
            Sandbox = new CodexDangerFullAccessSandboxPolicy(),
            ModelReasoningEffort = CodexReasoningEffort.High,
        });

        CodexRunResult startedResult = await startedThread.RunAsync("start defaults");
        Assert.Equal("Echo: start defaults", startedResult.FinalResponse);

        CodexThread resumedThread = await client.ResumeThreadAsync(startedThread.Id!, new CodexThreadOptions
        {
            WorkingDirectory = "/resume-dir",
            Model = "resume-model",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnFailure),
            Sandbox = new CodexReadOnlySandboxPolicy(),
            ModelReasoningEffort = CodexReasoningEffort.Low,
        });

        CodexRunResult resumedResult = await resumedThread.RunAsync("resume defaults");
        Assert.Equal("Echo: resume defaults", resumedResult.FinalResponse);

        CodexThread forkedThread = await client.ForkThreadAsync(startedThread.Id!, new CodexThreadForkOptions
        {
            WorkingDirectory = "/fork-dir",
            Model = "fork-model",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
            Sandbox = new CodexDangerFullAccessSandboxPolicy(),
            ModelReasoningEffort = CodexReasoningEffort.Medium,
        });

        CodexRunResult forkedResult = await forkedThread.RunAsync("fork defaults");
        Assert.Equal("Echo: fork defaults", forkedResult.FinalResponse);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0241")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0318")]
    [CoverageType(RequirementCoverageType.Edge)]
    public async Task TurnStream_UnknownApprovalRequestsUseEmptyResponseWhenNoHandlerIsConfigured()
    {
        ScriptedCodexProcessLauncher launcher = new();
        ScriptedCodexProcess process = new();

        process.StdIn.LineWritten = line =>
        {
            JsonObject request = JsonNode.Parse(line)!.AsObject();
            if (!request.TryGetPropertyValue("id", out JsonNode? idNode) || idNode is null)
            {
                return;
            }

            if (!request.TryGetPropertyValue("method", out JsonNode? methodNode) || methodNode is not JsonValue methodValue)
            {
                return;
            }

            string id = idNode.GetValue<string>();
            switch (methodValue.GetValue<string>())
            {
                case "initialize":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["userAgent"] = "codex-app-server/1.2.3",
                            ["platformFamily"] = "Unix",
                            ["platformOs"] = "Linux",
                        }));
                    break;
                case "thread/start":
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["thread"] = CreateThreadSnapshot("thread-approval"),
                        }));
                    break;
                case "turn/start":
                    process.EnqueueStdout(TestJson.Message(
                        "item/custom/requestApproval",
                        new JsonObject
                        {
                            ["reason"] = "needs fallback",
                        },
                        "approval-1"));
                    process.EnqueueStdout(TestJson.Response(
                        id,
                        new JsonObject
                        {
                            ["threadId"] = "thread-approval",
                            ["turn"] = new JsonObject
                            {
                                ["id"] = "turn-approval",
                                ["status"] = "inProgress",
                            },
                        }));
                    process.EnqueueStdout(TestJson.Notification(
                        "turn.completed",
                        new JsonObject
                        {
                            ["turn"] = new JsonObject
                            {
                                ["id"] = "turn-approval",
                                ["status"] = "completed",
                                ["items"] = new JsonArray
                                {
                                    new JsonObject
                                    {
                                        ["type"] = "agentMessage",
                                        ["id"] = "message-approval",
                                        ["phase"] = "finalAnswer",
                                        ["text"] = "Done",
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
                    break;
            }
        };

        launcher.Factory = _ => process;

        await using CodexClient client = CreateAppServerClient(launcher);
        CodexThread thread = await client.StartThreadAsync(new CodexThreadOptions
        {
            WorkingDirectory = "/work",
            Model = "gpt-5",
            ApprovalPolicy = new CodexApprovalModePolicy(CodexApprovalMode.OnRequest),
        });

        CodexTurn turn = await thread.StartTurnAsync("fallback approval");
        List<CodexThreadEvent> events = new();
        await foreach (CodexThreadEvent evt in turn.StreamAsync())
        {
            events.Add(evt);
        }

        Assert.Contains(events, evt => evt is CodexTurnCompletedEvent completed && completed.Turn.Id == "turn-approval");

        JsonObject approvalResponse = process.StdIn.Lines
            .Select(line => JsonNode.Parse(line)!.AsObject())
            .Single(message => message["id"]?.GetValue<string>() == "approval-1");
        Assert.Empty(approvalResponse["result"]!.AsObject());
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

    private static JsonObject CreateThreadSummary(string id, string? name = null)
        => new()
        {
            ["id"] = id,
            ["name"] = name,
            ["preview"] = $"Preview for {id}",
            ["status"] = new JsonObject
            {
                ["type"] = "idle",
            },
            ["modelProvider"] = "openai",
            ["createdAt"] = 1,
            ["updatedAt"] = 2,
            ["ephemeral"] = false,
            ["cliVersion"] = "1.2.3",
            ["path"] = "/work",
        };

    private static JsonObject CreateThreadSnapshot(string id, string? name = null, bool includeTurns = false)
    {
        JsonObject snapshot = CreateThreadSummary(id, name);
        if (includeTurns)
        {
            snapshot["turns"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "turn-1",
                    ["status"] = "completed",
                    ["items"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "agentMessage",
                            ["id"] = "message-1",
                            ["phase"] = "finalAnswer",
                            ["text"] = "Snapshot answer",
                        },
                    },
                    ["usage"] = new JsonObject
                    {
                        ["last"] = new JsonObject
                        {
                            ["totalTokens"] = 12,
                        },
                        ["total"] = new JsonObject
                        {
                            ["totalTokens"] = 12,
                        },
                    },
                },
            };
        }

        return snapshot;
    }

    private static JsonObject CreateModel(string id, string model)
        => new()
        {
            ["id"] = id,
            ["model"] = model,
            ["displayName"] = model.ToUpperInvariant(),
            ["description"] = $"Model {model}",
            ["hidden"] = false,
            ["isDefault"] = true,
            ["defaultReasoningEffort"] = "medium",
        };
}
