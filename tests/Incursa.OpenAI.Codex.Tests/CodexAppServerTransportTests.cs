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

        CodexTurn turn = await thread.StartTurnAsync("primary context");
        List<CodexThreadEvent> events = new();
        await foreach (CodexThreadEvent evt in turn.StreamAsync())
        {
            events.Add(evt);
            if (evt is CodexTurnCompletedEvent)
            {
                break;
            }
        }

        Assert.Contains(events, evt => evt is CodexTurnStartedEvent);
        Assert.Contains(events, evt => evt is CodexUnknownThreadEvent unknown && unknown.UnknownType == "custom.runtime-event");
        CodexTurnCompletedEvent completed = Assert.IsType<CodexTurnCompletedEvent>(events.Last(evt => evt is CodexTurnCompletedEvent));
        Assert.Equal("Done", ((CodexAgentMessageItem)completed.Turn.Items.Single()).Text);
        Assert.Contains(process.StdIn.Lines, line => line.Contains("\"decision\":\"accept\"", StringComparison.Ordinal));
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


