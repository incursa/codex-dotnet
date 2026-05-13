using System.Globalization;
using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-TRANSPORT-0244, REQ-CODEX-SDK-CATALOG-0306, REQ-CODEX-SDK-HELPERS-0315, REQ-CODEX-SDK-HELPERS-0317, REQ-CODEX-SDK-HELPERS-0318, REQ-CODEX-SDK-HELPERS-0319.

internal static class CodexProtocol
{
    public static JsonObject CreateInitializeRequest(CodexClientOptions options)
        => new()
        {
            ["clientInfo"] = new JsonObject
            {
                ["name"] = string.IsNullOrWhiteSpace(options.ClientName) ? "Incursa.OpenAI.Codex" : options.ClientName!,
                ["title"] = string.IsNullOrWhiteSpace(options.ClientTitle) ? "Incursa OpenAI Codex" : options.ClientTitle!,
                ["version"] = string.IsNullOrWhiteSpace(options.ClientVersion)
                    ? typeof(CodexClient).Assembly.GetName().Version?.ToString() ?? "0.0.0"
                    : options.ClientVersion!,
            },
            ["capabilities"] = new JsonObject
            {
                ["experimentalApi"] = true,
            },
        };

    public static CodexRuntimeMetadata NormalizeMetadata(CodexRuntimeMetadata metadata)
    {
        if (metadata.ServerInfo is { Name: not null, Version: not null })
        {
            return metadata;
        }

        (string? name, string? version) = ParseUserAgent(metadata.UserAgent);
        string? resolvedName = metadata.ServerInfo?.Name ?? name;
        string? resolvedVersion = metadata.ServerInfo?.Version ?? version;

        if (string.IsNullOrWhiteSpace(resolvedName) || string.IsNullOrWhiteSpace(resolvedVersion))
        {
            throw new InvalidOperationException("initialize response missing required server identity");
        }

        return metadata with
        {
            ServerInfo = new CodexServerInfo
            {
                Name = resolvedName,
                Version = resolvedVersion,
            },
        };
    }

    public static JsonObject BuildThreadStartParams(CodexThreadOptions? options)
    {
        JsonObject payload = new();
        AddThreadOptions(payload, options, includeSessionSourceMetadata: true);
        return payload;
    }

    public static JsonObject BuildThreadResumeParams(string threadId, CodexThreadOptions? options)
    {
        JsonObject payload = new();
        AddThreadOptions(payload, options, includeSessionSourceMetadata: false);
        payload["threadId"] = threadId;
        return payload;
    }

    public static JsonObject BuildThreadForkParams(string threadId, CodexThreadForkOptions? options)
    {
        JsonObject payload = BuildThreadStartParams(options);
        payload["threadId"] = threadId;
        return payload;
    }

    public static JsonObject BuildThreadListParams(CodexThreadListOptions? options)
    {
        JsonObject payload = new();
        if (options is null)
        {
            return payload;
        }

        if (options.Archived.HasValue) payload["archived"] = options.Archived.Value;
        if (!string.IsNullOrWhiteSpace(options.Cursor)) payload["cursor"] = options.Cursor;
        if (options.WorkingDirectories is { Count: > 0 })
        {
            payload["cwd"] = new JsonArray(options.WorkingDirectories.Select(value => JsonValue.Create(value)).ToArray());
        }
        else if (!string.IsNullOrWhiteSpace(options.WorkingDirectory))
        {
            payload["cwd"] = options.WorkingDirectory;
        }

        if (options.Limit.HasValue) payload["limit"] = options.Limit.Value;
        if (options.ModelProviders is { Count: > 0 }) payload["modelProviders"] = new JsonArray(options.ModelProviders.Select(value => JsonValue.Create(value)).ToArray());
        if (!string.IsNullOrWhiteSpace(options.SearchTerm)) payload["searchTerm"] = options.SearchTerm;
        if (options.SortKey is not null) payload["sortKey"] = options.SortKey.Value == CodexThreadSortKey.CreatedAt ? "created_at" : "updated_at";
        if (options.SortDirection is not null) payload["sortDirection"] = MapThreadSortDirection(options.SortDirection.Value);
        if (options.SourceKinds is { Count: > 0 }) payload["sourceKinds"] = new JsonArray(options.SourceKinds.Select(value => JsonValue.Create(MapThreadSourceKind(value))).ToArray());
        if (options.UseStateDbOnly.HasValue) payload["useStateDbOnly"] = options.UseStateDbOnly.Value;
        return payload;
    }

    public static JsonObject BuildThreadReadParams(string threadId, CodexThreadReadOptions? options)
        => new()
        {
            ["threadId"] = threadId,
            ["includeTurns"] = options?.IncludeTurns ?? false,
        };

    public static JsonObject BuildThreadArchiveParams(string threadId)
        => new() { ["threadId"] = threadId };

    public static JsonObject BuildThreadUnarchiveParams(string threadId)
        => new() { ["threadId"] = threadId };

    public static JsonObject BuildThreadNameParams(string threadId, string name)
        => new()
        {
            ["threadId"] = threadId,
            ["name"] = name,
        };

    public static JsonObject BuildThreadCompactParams(string threadId)
        => new() { ["threadId"] = threadId };

    public static JsonObject BuildThreadGoalGetParams(string threadId)
        => new() { ["threadId"] = threadId };

    public static JsonObject BuildThreadGoalSetParams(
        string threadId,
        string? objective,
        CodexThreadGoalStatus? status,
        long? tokenBudget,
        bool tokenBudgetSpecified)
    {
        JsonObject payload = new() { ["threadId"] = threadId };
        if (objective is not null)
        {
            payload["objective"] = objective;
        }

        if (status.HasValue)
        {
            payload["status"] = MapThreadGoalStatus(status.Value);
        }

        if (tokenBudgetSpecified)
        {
            payload["tokenBudget"] = tokenBudget.HasValue ? JsonValue.Create(tokenBudget.Value) : null;
        }

        return payload;
    }

    public static JsonObject BuildThreadGoalClearParams(string threadId)
        => new() { ["threadId"] = threadId };

    public static JsonObject BuildThreadRollbackParams(string threadId, int numTurns)
        => new()
        {
            ["threadId"] = threadId,
            ["numTurns"] = numTurns,
        };

    public static JsonObject BuildThreadUnsubscribeParams(string threadId)
        => new() { ["threadId"] = threadId };

    public static JsonObject BuildThreadMetadataUpdateParams(string threadId, CodexGitInfo? gitInfo)
        => new()
        {
            ["threadId"] = threadId,
            ["gitInfo"] = gitInfo is null ? null : BuildGitInfoPayload(gitInfo),
        };

    public static JsonObject BuildThreadMetadataUpdateParams(string threadId, CodexThreadMetadataGitInfoUpdate gitInfo)
        => new()
        {
            ["threadId"] = threadId,
            ["gitInfo"] = BuildThreadMetadataGitInfoUpdatePayload(gitInfo),
        };

    public static JsonObject BuildThreadShellCommandParams(string threadId, string command)
        => new()
        {
            ["threadId"] = threadId,
            ["command"] = command,
        };

    public static JsonObject BuildModelListParams(CodexModelListOptions? options)
        => new() { ["includeHidden"] = options?.IncludeHidden ?? false };

    public static JsonObject BuildTurnStartParams(
        string? threadId,
        IReadOnlyList<CodexInputItem> input,
        CodexTurnOptions? options)
    {
        JsonObject payload = BuildTurnOptionsPayload(options);
        if (!string.IsNullOrWhiteSpace(threadId))
        {
            payload["threadId"] = threadId;
        }

        payload["input"] = BuildInputPayload(input);
        return payload;
    }

    public static JsonObject BuildTurnSteerParams(string threadId, string turnId, IReadOnlyList<CodexInputItem> input)
        => new()
        {
            ["threadId"] = threadId,
            ["expectedTurnId"] = turnId,
            ["input"] = BuildInputPayload(input),
        };

    public static JsonObject BuildTurnInterruptParams(string threadId, string turnId)
        => new()
        {
            ["threadId"] = threadId,
            ["turnId"] = turnId,
        };

    public static JsonArray BuildInputPayload(IReadOnlyList<CodexInputItem> input)
    {
        JsonArray payload = new();
        foreach (CodexInputItem item in input)
        {
            payload.Add(item switch
            {
                CodexTextInput text => new JsonObject { ["type"] = "text", ["text"] = text.Text },
                CodexImageInput image => new JsonObject { ["type"] = "image", ["url"] = image.Url },
                CodexLocalImageInput localImage => new JsonObject { ["type"] = "localImage", ["path"] = localImage.Path },
                CodexSkillInput skill => new JsonObject { ["type"] = "skill", ["name"] = skill.Name, ["path"] = skill.Path },
                CodexMentionInput mention => new JsonObject { ["type"] = "mention", ["name"] = mention.Name, ["path"] = mention.Path },
                _ => new JsonObject { ["type"] = item.Type },
            });
        }

        return payload;
    }

    public static JsonObject BuildConfigPayload(CodexConfigObject? config)
    {
        JsonObject payload = new();
        if (config is null)
        {
            return payload;
        }

        foreach (KeyValuePair<string, CodexConfigValue> pair in config.Values)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                payload[pair.Key] = ToJsonNode(pair.Value);
            }
        }

        return payload;
    }

    public static CodexThreadEvent ParseThreadEvent(JsonObject message)
    {
        JsonObject payload = message;
        string normalizedType = NormalizeEventType(GetString(message, "type") ?? GetString(message, "method"));
        if (message.TryGetPropertyValue("params", out JsonNode? paramsNode) && paramsNode is JsonObject paramsObject)
        {
            payload = paramsObject;
        }

        return normalizedType switch
        {
            "thread.started" => new CodexThreadStartedEvent { Thread = ParseThreadSummary(GetObject(payload, "thread") ?? payload) },
            "turn.started" => new CodexTurnStartedEvent { Turn = ParseTurnRecord(GetObject(payload, "turn") ?? payload) },
            "turn.completed" => new CodexTurnCompletedEvent { Turn = ParseTurnRecord(GetObject(payload, "turn") ?? payload) },
            "turn.failed" => new CodexTurnFailedEvent { Turn = ParseTurnRecord(GetObject(payload, "turn") ?? payload) },
            "item.started" => new CodexItemStartedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                Item = ParseThreadItem(GetObject(payload, "item") ?? payload),
            },
            "item.updated" => new CodexItemUpdatedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                Item = ParseThreadItem(GetObject(payload, "item") ?? payload),
            },
            "item.completed" => new CodexItemCompletedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                Item = ParseThreadItem(GetObject(payload, "item") ?? payload),
            },
            "item.autoApprovalReview.started" => new CodexItemAutoApprovalReviewStartedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                ReviewId = GetString(payload, "reviewId") ?? string.Empty,
                TargetItemId = GetString(payload, "targetItemId"),
                StartedAtMs = GetLongAny(payload, "startedAtMs") ?? 0,
                Action = GetNode(payload, "action")?.DeepClone(),
                Review = GetNode(payload, "review")?.DeepClone(),
            },
            "item.autoApprovalReview.completed" => new CodexItemAutoApprovalReviewCompletedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                ReviewId = GetString(payload, "reviewId") ?? string.Empty,
                TargetItemId = GetString(payload, "targetItemId"),
                StartedAtMs = GetLongAny(payload, "startedAtMs") ?? 0,
                CompletedAtMs = GetLongAny(payload, "completedAtMs") ?? 0,
                DecisionSource = GetString(payload, "decisionSource") ?? string.Empty,
                Action = GetNode(payload, "action")?.DeepClone(),
                Review = GetNode(payload, "review")?.DeepClone(),
            },
            "item.agentMessage.delta" => new CodexAgentMessageDeltaEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                ItemId = GetString(payload, "itemId") ?? string.Empty,
                Delta = GetString(payload, "delta") ?? string.Empty,
            },
            "item.commandExecution.outputDelta" => new CodexCommandExecutionOutputDeltaEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                ItemId = GetString(payload, "itemId") ?? string.Empty,
                Delta = GetString(payload, "delta") ?? string.Empty,
            },
            "item.fileChange.outputDelta" => new CodexFileChangeOutputDeltaEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                ItemId = GetString(payload, "itemId") ?? string.Empty,
                Delta = GetString(payload, "delta") ?? string.Empty,
            },
            "item.fileChange.patchUpdated" => new CodexFileChangePatchUpdatedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                ItemId = GetString(payload, "itemId") ?? string.Empty,
                Changes = ParseFileUpdateChanges(GetNode(payload, "changes")),
            },
            "item.mcpToolCall.progress" => new CodexMcpToolCallProgressEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                ItemId = GetString(payload, "itemId") ?? string.Empty,
                Message = GetString(payload, "message") ?? string.Empty,
            },
            "item.reasoning.textDelta" => new CodexReasoningTextDeltaEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                ItemId = GetString(payload, "itemId") ?? string.Empty,
                ContentIndex = GetInt(payload, "contentIndex") ?? 0,
                Delta = GetString(payload, "delta") ?? string.Empty,
            },
            "item.reasoning.summaryPartAdded" => new CodexReasoningSummaryPartAddedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                ItemId = GetString(payload, "itemId") ?? string.Empty,
                SummaryIndex = GetInt(payload, "summaryIndex") ?? 0,
            },
            "item.reasoning.summaryTextDelta" => new CodexReasoningSummaryTextDeltaEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                ItemId = GetString(payload, "itemId") ?? string.Empty,
                SummaryIndex = GetInt(payload, "summaryIndex") ?? 0,
                Delta = GetString(payload, "delta") ?? string.Empty,
            },
            "error" => new CodexThreadErrorEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId"),
                WillRetry = GetBool(payload, "willRetry") ?? false,
                Error = ParseTurnError(GetObject(payload, "error") ?? payload),
            },
            "thread.status.changed" => new CodexThreadStatusChangedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                Status = ParseThreadStatus(GetObject(payload, "status")),
            },
            "thread.archived" => new CodexThreadArchivedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "thread.closed" => new CodexThreadClosedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "thread.compacted" => new CodexThreadCompactedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
            },
            "thread.name.updated" => new CodexThreadNameUpdatedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                ThreadName = GetString(payload, "threadName"),
            },
            "thread.tokenUsage.updated" => new CodexThreadTokenUsageUpdatedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                TokenUsage = ParseUsage(GetObject(payload, "tokenUsage")) ?? new CodexUsage(),
            },
            "thread.unarchived" => new CodexThreadUnarchivedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "turn.diff.updated" => new CodexTurnDiffUpdatedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                Diff = GetString(payload, "diff") ?? string.Empty,
            },
            "account.rateLimits.updated" => new CodexAccountRateLimitsUpdatedEvent
            {
                RateLimits = ParseRateLimitSnapshot(
                    GetObjectAny(payload, "rateLimits", "rate_limits") ?? payload,
                    fallbackLimitId: null),
            },
            "thread.goal.updated" => new CodexThreadGoalUpdatedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId"),
                Goal = ParseThreadGoal(GetObject(payload, "goal") ?? payload),
            },
            "thread.goal.cleared" => new CodexThreadGoalClearedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "turn.plan.updated" => new CodexTurnPlanUpdatedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                Explanation = GetString(payload, "explanation"),
                Plan = ParseTurnPlanSteps(payload),
            },
            "item.plan.delta" => new CodexPlanDeltaEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                ItemId = GetString(payload, "itemId") ?? string.Empty,
                Delta = GetString(payload, "delta") ?? string.Empty,
            },
            "hook.started" => new CodexHookStartedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId"),
                Run = ParseHookRunSummary(GetObject(payload, "run") ?? payload),
            },
            "hook.completed" => new CodexHookCompletedEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId"),
                Run = ParseHookRunSummary(GetObject(payload, "run") ?? payload),
            },
            "process.outputDelta" => new CodexProcessOutputDeltaEvent
            {
                CapReached = GetBool(payload, "capReached") ?? false,
                DeltaBase64 = GetString(payload, "deltaBase64") ?? string.Empty,
                ProcessHandle = GetString(payload, "processHandle") ?? string.Empty,
                Stream = ParseProcessOutputStream(GetString(payload, "stream")),
            },
            "process.exited" => new CodexProcessExitedEvent
            {
                ExitCode = GetInt(payload, "exitCode") ?? 0,
                ProcessHandle = GetString(payload, "processHandle") ?? string.Empty,
                Stderr = GetString(payload, "stderr") ?? string.Empty,
                StderrCapReached = GetBool(payload, "stderrCapReached") ?? false,
                Stdout = GetString(payload, "stdout") ?? string.Empty,
                StdoutCapReached = GetBool(payload, "stdoutCapReached") ?? false,
            },
            "warning" => new CodexWarningEvent
            {
                Message = GetString(payload, "message") ?? string.Empty,
                ThreadId = GetString(payload, "threadId"),
            },
            "configWarning" => new CodexConfigWarningEvent
            {
                Details = GetString(payload, "details"),
                Path = GetString(payload, "path"),
                Range = ParseTextRange(GetObject(payload, "range")),
                Summary = GetString(payload, "summary") ?? string.Empty,
            },
            "deprecationNotice" => new CodexDeprecationNoticeEvent
            {
                Details = GetString(payload, "details"),
                Summary = GetString(payload, "summary") ?? string.Empty,
            },
            "guardianWarning" => new CodexGuardianWarningEvent
            {
                Message = GetString(payload, "message") ?? string.Empty,
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "account.updated" => new CodexAccountUpdatedEvent
            {
                AuthMode = ParseAuthMode(GetString(payload, "authMode")),
                PlanType = ParsePlanType(GetString(payload, "planType")),
            },
            "account.login.completed" => new CodexAccountLoginCompletedEvent
            {
                Error = GetString(payload, "error"),
                LoginId = GetString(payload, "loginId"),
                Success = GetBool(payload, "success") ?? false,
            },
            "serverRequest.resolved" => new CodexServerRequestResolvedEvent
            {
                RequestId = GetString(payload, "requestId") ?? string.Empty,
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "app.list.updated" => new CodexAppListUpdatedEvent
            {
                Data = GetNode(payload, "data") is JsonArray appArray
                    ? appArray.OfType<JsonObject>().Select(app => app.DeepClone().AsObject()).ToArray()
                    : [],
            },
            "skills.changed" => new CodexSkillsChangedEvent(),
            "fs.changed" => new CodexFsChangedEvent
            {
                ChangedPaths = GetStringList(payload, "changedPaths") ?? [],
                WatchId = GetString(payload, "watchId") ?? string.Empty,
            },
            "fuzzyFileSearch.sessionCompleted" => new CodexFuzzyFileSearchSessionCompletedEvent
            {
                SessionId = GetString(payload, "sessionId") ?? string.Empty,
            },
            "fuzzyFileSearch.sessionUpdated" => new CodexFuzzyFileSearchSessionUpdatedEvent
            {
                Files = ParseFuzzyFileSearchResults(GetNode(payload, "files")),
                Query = GetString(payload, "query") ?? string.Empty,
                SessionId = GetString(payload, "sessionId") ?? string.Empty,
            },
            "mcpServer.oauthLogin.completed" => new CodexMcpServerOauthLoginCompletedEvent
            {
                Error = GetString(payload, "error"),
                Name = GetString(payload, "name") ?? string.Empty,
                Success = GetBool(payload, "success") ?? false,
            },
            "mcpServer.startupStatus.updated" => new CodexMcpServerStartupStatusUpdatedEvent
            {
                Error = GetString(payload, "error"),
                Name = GetString(payload, "name") ?? string.Empty,
                Status = ParseMcpServerStartupState(GetString(payload, "status")),
            },
            "remoteControl.status.changed" => new CodexRemoteControlStatusChangedEvent
            {
                EnvironmentId = GetString(payload, "environmentId"),
                InstallationId = GetString(payload, "installationId") ?? string.Empty,
                Status = ParseRemoteControlConnectionStatus(GetString(payload, "status")),
            },
            "windowsSandbox.setupCompleted" => new CodexWindowsSandboxSetupCompletedEvent
            {
                Error = GetString(payload, "error"),
                Mode = ParseWindowsSandboxSetupMode(GetString(payload, "mode")),
                Success = GetBool(payload, "success") ?? false,
            },
            "windows.worldWritableWarning" => new CodexWindowsWorldWritableWarningEvent
            {
                ExtraCount = GetInt(payload, "extraCount") ?? 0,
                FailedScan = GetBool(payload, "failedScan") ?? false,
                SamplePaths = GetStringList(payload, "samplePaths") ?? [],
            },
            "model.rerouted" => new CodexModelReroutedEvent
            {
                FromModel = GetString(payload, "fromModel") ?? string.Empty,
                Reason = ParseModelRerouteReason(GetString(payload, "reason")),
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                ToModel = GetString(payload, "toModel") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
            },
            "model.verification" => new CodexModelVerificationEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId") ?? string.Empty,
                Verifications = ParseModelVerificationValues(GetNode(payload, "verifications")),
            },
            "thread.realtime.started" => new CodexThreadRealtimeStartedEvent
            {
                RealtimeSessionId = GetString(payload, "realtimeSessionId"),
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                Version = ParseRealtimeConversationVersion(GetString(payload, "version")),
            },
            "thread.realtime.itemAdded" => new CodexThreadRealtimeItemAddedEvent
            {
                Item = GetNode(payload, "item")?.DeepClone(),
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "thread.realtime.transcript.delta" => new CodexThreadRealtimeTranscriptDeltaEvent
            {
                Delta = GetString(payload, "delta") ?? string.Empty,
                Role = GetString(payload, "role") ?? string.Empty,
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "thread.realtime.transcript.done" => new CodexThreadRealtimeTranscriptDoneEvent
            {
                Role = GetString(payload, "role") ?? string.Empty,
                Text = GetString(payload, "text") ?? string.Empty,
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "thread.realtime.outputAudio.delta" => new CodexThreadRealtimeOutputAudioDeltaEvent
            {
                Audio = ParseThreadRealtimeAudioChunk(GetObject(payload, "audio")),
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "thread.realtime.sdp" => new CodexThreadRealtimeSdpEvent
            {
                Sdp = GetString(payload, "sdp") ?? string.Empty,
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "thread.realtime.error" => new CodexThreadRealtimeErrorEvent
            {
                Message = GetString(payload, "message") ?? string.Empty,
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            "thread.realtime.closed" => new CodexThreadRealtimeClosedEvent
            {
                Reason = GetString(payload, "reason"),
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
            },
            _ => new CodexUnknownThreadEvent(normalizedType) { RawPayload = payload },
        };
    }

    public static CodexThreadSummary ParseThreadSummary(JsonObject payload)
    {
        return new CodexThreadSummary
        {
            Id = GetString(payload, "id") ?? string.Empty,
            Name = GetString(payload, "name"),
            Preview = GetString(payload, "preview") ?? string.Empty,
            Status = ParseThreadStatus(GetObject(payload, "status")),
            ModelProvider = GetString(payload, "modelProvider") ?? string.Empty,
            CreatedAt = ParseDateTimeOffset(payload, "createdAt"),
            UpdatedAt = ParseDateTimeOffset(payload, "updatedAt"),
            Ephemeral = GetBool(payload, "ephemeral") ?? false,
            CliVersion = GetString(payload, "cliVersion") ?? string.Empty,
            Cwd = GetString(payload, "cwd") ?? string.Empty,
            Path = GetString(payload, "path"),
            SessionId = GetString(payload, "sessionId") ?? string.Empty,
            ForkedFromId = GetString(payload, "forkedFromId"),
            Source = ParseSessionSource(GetNode(payload, "source")),
            ThreadSource = ParseThreadSource(GetString(payload, "threadSource")),
            SessionStartSource = ParseThreadStartSource(GetString(payload, "sessionStartSource")),
            AgentRole = GetString(payload, "agentRole"),
            AgentNickname = GetString(payload, "agentNickname"),
            GitInfo = ParseGitInfo(GetObject(payload, "gitInfo")),
        };
    }

    public static CodexThreadSnapshot ParseThreadSnapshot(JsonObject payload)
    {
        CodexThreadSummary summary = ParseThreadSummary(payload);
        IReadOnlyList<CodexTurnRecord> turns = [];
        if (payload.TryGetPropertyValue("turns", out JsonNode? turnsNode) && turnsNode is JsonArray turnsArray)
        {
            turns = turnsArray.OfType<JsonObject>().Select(ParseTurnRecord).ToArray();
        }

        return new CodexThreadSnapshot
        {
            Id = summary.Id,
            Name = summary.Name,
            Preview = summary.Preview,
            Status = summary.Status,
            ModelProvider = summary.ModelProvider,
            CreatedAt = summary.CreatedAt,
            UpdatedAt = summary.UpdatedAt,
            Ephemeral = summary.Ephemeral,
            CliVersion = summary.CliVersion,
            Cwd = summary.Cwd,
            Path = summary.Path,
            SessionId = summary.SessionId,
            ForkedFromId = summary.ForkedFromId,
            Source = summary.Source,
            ThreadSource = summary.ThreadSource,
            SessionStartSource = summary.SessionStartSource,
            AgentRole = summary.AgentRole,
            AgentNickname = summary.AgentNickname,
            GitInfo = summary.GitInfo,
            Turns = turns,
        };
    }

    public static CodexThreadHandleState ParseThreadHandleState(JsonObject payload, CodexThreadOptions? defaults)
        => new(ParseThreadSnapshot(GetObject(payload, "thread") ?? payload), defaults);

    public static CodexThreadListResult ParseThreadListResult(JsonNode? node)
    {
        if (node is not JsonObject payload)
        {
            return new CodexThreadListResult();
        }

        JsonNode? listNode = GetNode(payload, "data") ?? GetNode(payload, "threads");
        IReadOnlyList<CodexThreadSummary> threads = listNode is JsonArray array ? array.OfType<JsonObject>().Select(ParseThreadSummary).ToArray() : [];

        return new CodexThreadListResult
        {
            BackwardsCursor = GetString(payload, "backwardsCursor"),
            Threads = threads,
            NextCursor = GetString(payload, "nextCursor"),
        };
    }

    public static CodexModelListResult ParseModelListResult(JsonNode? node)
    {
        if (node is not JsonObject payload)
        {
            return new CodexModelListResult();
        }

        JsonNode? listNode = GetNode(payload, "data") ?? GetNode(payload, "models");
        IReadOnlyList<CodexModel> models = listNode is JsonArray array ? array.OfType<JsonObject>().Select(ParseModel).ToArray() : [];

        return new CodexModelListResult
        {
            Models = models,
            NextCursor = GetString(payload, "nextCursor"),
        };
    }

    public static CodexAccountRateLimitsResult ParseAccountRateLimitsResult(JsonNode? node)
    {
        if (node is not JsonObject payload)
        {
            return new CodexAccountRateLimitsResult();
        }

        List<CodexRateLimitSnapshot> rateLimits = [];
        Dictionary<string, CodexRateLimitSnapshot> byLimitId = new(StringComparer.Ordinal);

        void AddSnapshot(CodexRateLimitSnapshot snapshot, string? limitIdKey)
        {
            string? limitId = string.IsNullOrWhiteSpace(snapshot.LimitId) ? limitIdKey : snapshot.LimitId;
            string? dictionaryKey = string.IsNullOrWhiteSpace(limitIdKey) ? limitId : limitIdKey;
            if (string.IsNullOrWhiteSpace(dictionaryKey))
            {
                rateLimits.Add(snapshot);
                return;
            }

            CodexRateLimitSnapshot normalized = string.Equals(limitId, snapshot.LimitId, StringComparison.Ordinal)
                ? snapshot
                : snapshot with { LimitId = limitId };
            if (byLimitId.TryAdd(dictionaryKey, normalized))
            {
                rateLimits.Add(normalized);
            }
        }

        if (GetObjectAny(payload, "rateLimitsByLimitId", "rate_limits_by_limit_id") is { Count: > 0 } rateLimitsById)
        {
            foreach (KeyValuePair<string, JsonNode?> pair in rateLimitsById)
            {
                if (pair.Value is JsonObject snapshot)
                {
                    AddSnapshot(ParseRateLimitSnapshot(snapshot, pair.Key), pair.Key);
                }
            }
        }

        JsonNode? rateLimitsNode = GetNodeAny(payload, "rateLimits", "rate_limits");
        if (rateLimitsNode is JsonObject singleLimit)
        {
            AddSnapshot(ParseRateLimitSnapshot(singleLimit, fallbackLimitId: null), limitIdKey: null);
        }
        else if (rateLimitsNode is JsonArray rateLimitsArray)
        {
            foreach (JsonObject snapshot in rateLimitsArray.OfType<JsonObject>())
            {
                AddSnapshot(ParseRateLimitSnapshot(snapshot, fallbackLimitId: null), limitIdKey: null);
            }
        }

        return new CodexAccountRateLimitsResult
        {
            RateLimits = rateLimits,
            RateLimitsByLimitId = byLimitId,
        };
    }

    public static CodexThreadGoal ParseThreadGoal(JsonObject payload)
        => new()
        {
            ThreadId = GetString(payload, "threadId") ?? string.Empty,
            Objective = GetString(payload, "objective") ?? string.Empty,
            Status = ParseThreadGoalStatus(GetString(payload, "status")),
            TokenBudget = GetLongAny(payload, "tokenBudget"),
            TokensUsed = GetLongAny(payload, "tokensUsed") ?? 0,
            TimeUsedSeconds = GetLongAny(payload, "timeUsedSeconds") ?? 0,
            CreatedAt = ParseNullableDateTimeOffset(payload, "createdAt") ?? DateTimeOffset.UnixEpoch,
            UpdatedAt = ParseNullableDateTimeOffset(payload, "updatedAt") ?? DateTimeOffset.UnixEpoch,
        };

    public static CodexTurnRecord ParseTurnRecord(JsonObject payload)
    {
        IReadOnlyList<CodexThreadItem> items = [];
        if (payload.TryGetPropertyValue("items", out JsonNode? itemsNode) && itemsNode is JsonArray itemsArray)
        {
            items = itemsArray.OfType<JsonObject>().Select(ParseThreadItem).ToArray();
        }

        return new CodexTurnRecord
        {
            Id = GetString(payload, "id") ?? string.Empty,
            Status = ParseTurnStatus(GetString(payload, "status")),
            Items = items,
            Error = ParseTurnError(GetObject(payload, "error")),
            Usage = ParseUsage(GetObject(payload, "usage")),
        };
    }

    public static CodexTurnError ParseTurnError(JsonObject? payload)
    {
        if (payload is null)
        {
            return new CodexTurnError();
        }

        return new CodexTurnError
        {
            Message = GetString(payload, "message") ?? string.Empty,
            AdditionalDetails = GetString(payload, "additionalDetails"),
            CodexErrorInfo = GetNode(payload, "codexErrorInfo"),
        };
    }

    public static CodexUsage? ParseUsage(JsonObject? payload)
    {
        if (payload is null)
        {
            return null;
        }

        return new CodexUsage
        {
            Last = ParseTokenUsageBreakdown(GetObject(payload, "last")),
            ModelContextWindow = GetInt(payload, "modelContextWindow"),
            Total = ParseTokenUsageBreakdown(GetObject(payload, "total")),
        };
    }

    public static CodexTokenUsageBreakdown ParseTokenUsageBreakdown(JsonObject? payload)
    {
        if (payload is null)
        {
            return new CodexTokenUsageBreakdown();
        }

        return new CodexTokenUsageBreakdown
        {
            CachedInputTokens = GetInt(payload, "cachedInputTokens") ?? 0,
            InputTokens = GetInt(payload, "inputTokens") ?? 0,
            OutputTokens = GetInt(payload, "outputTokens") ?? 0,
            ReasoningOutputTokens = GetInt(payload, "reasoningOutputTokens") ?? 0,
            TotalTokens = GetInt(payload, "totalTokens") ?? 0,
        };
    }

    public static CodexThreadItem ParseThreadItem(JsonObject payload)
    {
        string type = NormalizeItemType(GetString(payload, "type"));
        string id = GetString(payload, "id") ?? string.Empty;

        return type switch
        {
            "userMessage" => new CodexUserMessageItem
            {
                Id = id,
                Content = ParseInputItems(GetNode(payload, "content")),
            },
            "hookPrompt" => new CodexHookPromptItem
            {
                Id = id,
                Fragments = ParseHookPromptFragments(GetNode(payload, "fragments")),
            },
            "agentMessage" => new CodexAgentMessageItem
            {
                Id = id,
                MemoryCitation = ParseMemoryCitation(GetObject(payload, "memoryCitation")),
                Phase = ParseMessagePhase(GetString(payload, "phase")),
                Text = GetString(payload, "text") ?? string.Empty,
            },
            "plan" => new CodexPlanItem
            {
                Id = id,
                Text = GetString(payload, "text") ?? string.Empty,
            },
            "reasoning" => new CodexReasoningItem
            {
                Id = id,
                Content = GetStringList(payload, "content"),
                Summary = GetStringList(payload, "summary"),
            },
            "commandExecution" => new CodexCommandExecutionItem
            {
                AggregatedOutput = GetString(payload, "aggregatedOutput") ?? string.Empty,
                Command = GetString(payload, "command") ?? string.Empty,
                CommandActions = ParseCommandActions(GetNode(payload, "commandActions")),
                Cwd = GetString(payload, "cwd") ?? string.Empty,
                DurationMs = GetInt(payload, "durationMs"),
                ExitCode = GetInt(payload, "exitCode"),
                Id = id,
                ProcessId = GetString(payload, "processId"),
                Source = ParseCommandExecutionSource(GetString(payload, "source")),
                Status = ParseCommandExecutionStatus(GetString(payload, "status")),
            },
            "fileChange" => new CodexFileChangeItem
            {
                Id = id,
                Changes = ParseFileUpdateChanges(GetNode(payload, "changes")),
                Status = ParsePatchApplyStatus(GetString(payload, "status")),
            },
            "mcpToolCall" => new CodexMcpToolCallItem
            {
                Arguments = GetNode(payload, "arguments")?.DeepClone(),
                DurationMs = GetInt(payload, "durationMs"),
                Error = ParseMcpToolCallError(GetObject(payload, "error")),
                Id = id,
                McpAppResourceUri = GetString(payload, "mcpAppResourceUri"),
                Result = ParseMcpToolCallResult(GetObject(payload, "result")),
                Server = GetString(payload, "server") ?? string.Empty,
                Status = ParseMcpToolCallStatus(GetString(payload, "status")),
                Tool = GetString(payload, "tool") ?? string.Empty,
            },
            "dynamicToolCall" => new CodexDynamicToolCallItem
            {
                Arguments = GetNode(payload, "arguments")?.DeepClone(),
                ContentItems = ParseDynamicToolCallOutputContentItems(GetNode(payload, "contentItems")),
                DurationMs = GetInt(payload, "durationMs"),
                Id = id,
                Namespace = GetString(payload, "namespace"),
                Status = ParseDynamicToolCallStatus(GetString(payload, "status")),
                Success = GetBool(payload, "success"),
                Tool = GetString(payload, "tool") ?? string.Empty,
            },
            "collabAgentToolCall" => new CodexCollabAgentToolCallItem
            {
                AgentsStates = ParseCollabAgentStates(GetObject(payload, "agentsStates")),
                Id = id,
                Model = GetString(payload, "model"),
                Prompt = GetString(payload, "prompt"),
                ReasoningEffort = ParseReasoningEffort(GetString(payload, "reasoningEffort")),
                ReceiverThreadIds = GetStringList(payload, "receiverThreadIds") ?? [],
                SenderThreadId = GetString(payload, "senderThreadId") ?? string.Empty,
                Status = ParseCollabAgentToolCallStatus(GetString(payload, "status")),
                Tool = ParseCollabAgentTool(GetString(payload, "tool")),
            },
            "webSearch" => new CodexWebSearchItem
            {
                Id = id,
                Action = ParseWebSearchAction(GetObject(payload, "action")),
                Query = GetString(payload, "query") ?? string.Empty,
            },
            "imageView" => new CodexImageViewItem
            {
                Id = id,
                Path = GetString(payload, "path") ?? string.Empty,
            },
            "imageGeneration" => new CodexImageGenerationItem
            {
                Id = id,
                Result = GetString(payload, "result") ?? string.Empty,
                RevisedPrompt = GetString(payload, "revisedPrompt"),
                SavedPath = GetString(payload, "savedPath"),
                Status = GetString(payload, "status") ?? string.Empty,
            },
            "enteredReviewMode" => new CodexEnteredReviewModeItem
            {
                Id = id,
                Review = GetString(payload, "review") ?? string.Empty,
            },
            "exitedReviewMode" => new CodexExitedReviewModeItem
            {
                Id = id,
                Review = GetString(payload, "review") ?? string.Empty,
            },
            "contextCompaction" => new CodexContextCompactionItem { Id = id },
            "todoList" => new CodexTodoListItem
            {
                Id = id,
                Items = ParseTodoItems(GetNode(payload, "items")),
            },
            "error" => new CodexErrorItem
            {
                Id = id,
                Message = GetString(payload, "message") ?? string.Empty,
            },
            _ => new CodexUnknownThreadItem(type) { Id = id, RawPayload = payload },
        };
    }

    public static CodexModel ParseModel(JsonObject payload)
    {
        return new CodexModel
        {
            AvailabilityNux = ParseModelAvailabilityNux(GetObject(payload, "availabilityNux")),
            Id = GetString(payload, "id") ?? string.Empty,
            Model = GetString(payload, "model") ?? string.Empty,
            DisplayName = GetString(payload, "displayName") ?? string.Empty,
            Description = GetString(payload, "description") ?? string.Empty,
            Hidden = GetBool(payload, "hidden") ?? false,
            IsDefault = GetBool(payload, "isDefault") ?? false,
            DefaultReasoningEffort = ParseReasoningEffort(GetString(payload, "defaultReasoningEffort")),
            InputModalities = ParseInputModalities(payload),
            AdditionalSpeedTiers = GetStringList(payload, "additionalSpeedTiers") ?? [],
            SupportedReasoningEfforts = ParseReasoningEffortOptions(payload),
            SupportsPersonality = GetBool(payload, "supportsPersonality"),
            Upgrade = GetString(payload, "upgrade"),
            UpgradeInfo = ParseModelUpgradeInfo(GetObject(payload, "upgradeInfo")),
            ServiceTiers = ParseModelServiceTiers(payload),
        };
    }

    private static CodexRateLimitSnapshot ParseRateLimitSnapshot(JsonObject payload, string? fallbackLimitId)
        => new()
        {
            Credits = ParseCreditsSnapshot(GetObjectAny(payload, "credits")),
            LimitId = GetStringAny(payload, "limitId", "limit_id") ?? fallbackLimitId,
            LimitName = GetStringAny(payload, "limitName", "limit_name"),
            PlanType = ParsePlanType(GetStringAny(payload, "planType", "plan_type")),
            Primary = ParseRateLimitWindow(GetObjectAny(payload, "primary")),
            Secondary = ParseRateLimitWindow(GetObjectAny(payload, "secondary")),
            RateLimitReachedType = GetStringAny(payload, "rateLimitReachedType", "rate_limit_reached_type"),
        };

    private static CodexRateLimitWindow? ParseRateLimitWindow(JsonObject? payload)
    {
        if (payload is null)
        {
            return null;
        }

        return new CodexRateLimitWindow
        {
            UsedPercent = Math.Clamp(GetIntAny(payload, "usedPercent", "used_percent") ?? 0, 0, 100),
            ResetsAt = ParseNullableDateTimeOffset(payload, "resetsAt", "resets_at"),
            WindowDurationMinutes = GetLongAny(
                payload,
                "windowDurationMins",
                "windowDurationMinutes",
                "window_duration_mins",
                "window_duration_minutes",
                "window_minutes"),
        };
    }

    private static CodexCreditsSnapshot? ParseCreditsSnapshot(JsonObject? payload)
    {
        if (payload is null)
        {
            return null;
        }

        return new CodexCreditsSnapshot
        {
            Balance = GetDoubleAny(payload, "balance"),
            HasCredits = GetBoolAny(payload, "hasCredits", "has_credits"),
            Unlimited = GetBoolAny(payload, "unlimited"),
        };
    }

    private static void AddThreadOptions(JsonObject payload, CodexThreadOptions? options, bool includeSessionSourceMetadata)
    {
        if (options is null)
        {
            return;
        }

        if (options.Config is not null)
        {
            payload["config"] = BuildConfigPayload(options.Config);
        }

        if (!string.IsNullOrWhiteSpace(options.BaseInstructions)) payload["baseInstructions"] = options.BaseInstructions;
        if (!string.IsNullOrWhiteSpace(options.DeveloperInstructions)) payload["developerInstructions"] = options.DeveloperInstructions;
        if (options.Ephemeral.HasValue) payload["ephemeral"] = options.Ephemeral.Value;
        if (!string.IsNullOrWhiteSpace(options.Model)) payload["model"] = options.Model;
        if (!string.IsNullOrWhiteSpace(options.ModelProvider)) payload["modelProvider"] = options.ModelProvider;
        if (options.Personality is not null) payload["personality"] = MapPersonality(options.Personality.Value);
        if (options.Sandbox is not null) payload["sandbox"] = BuildSandboxPolicyPayload(options.Sandbox);
        if (options.ServiceTier is not null) payload["serviceTier"] = MapServiceTier(options.ServiceTier.Value);
        if (!string.IsNullOrWhiteSpace(options.WorkingDirectory)) payload["cwd"] = options.WorkingDirectory;
        if (!string.IsNullOrWhiteSpace(options.ServiceName)) payload["serviceName"] = options.ServiceName;
        if (includeSessionSourceMetadata && options.SessionStartSource is not null) payload["sessionStartSource"] = MapThreadStartSource(options.SessionStartSource.Value);
        if (includeSessionSourceMetadata && options.ThreadSource is not null) payload["threadSource"] = MapThreadSource(options.ThreadSource.Value);
        if (options.ModelReasoningEffort is not null) payload["modelReasoningEffort"] = MapReasoningEffort(options.ModelReasoningEffort.Value);
        if (options.NetworkAccessEnabled.HasValue) payload["networkAccessEnabled"] = options.NetworkAccessEnabled.Value;
        if (options.WebSearchMode is not null) payload["webSearchMode"] = MapWebSearchMode(options.WebSearchMode.Value);
        if (options.WebSearchEnabled.HasValue) payload["webSearchEnabled"] = options.WebSearchEnabled.Value;
        if (options.SkipGitRepoCheck.HasValue) payload["skipGitRepoCheck"] = options.SkipGitRepoCheck.Value;

        if (options.AdditionalDirectories is { Count: > 0 })
        {
            payload["additionalDirectories"] = new JsonArray(options.AdditionalDirectories.Select(value => JsonValue.Create(value)).ToArray());
        }

        if (options.ApprovalPolicy is not null) payload["approvalPolicy"] = BuildApprovalPolicyPayload(options.ApprovalPolicy);
        if (options.ApprovalsReviewer is not null) payload["approvalsReviewer"] = MapApprovalsReviewer(options.ApprovalsReviewer.Value);
    }

    private static JsonObject BuildTurnOptionsPayload(CodexTurnOptions? options)
    {
        JsonObject payload = new();
        if (options is null)
        {
            return payload;
        }

        if (options.ApprovalPolicy is not null) payload["approvalPolicy"] = BuildApprovalPolicyPayload(options.ApprovalPolicy);
        if (options.ApprovalsReviewer is not null) payload["approvalsReviewer"] = MapApprovalsReviewer(options.ApprovalsReviewer.Value);
        if (options.Effort is not null) payload["effort"] = MapReasoningEffort(options.Effort.Value);
        if (!string.IsNullOrWhiteSpace(options.Model)) payload["model"] = options.Model;
        if (options.OutputSchema is not null) payload["outputSchema"] = options.OutputSchema.DeepClone();
        if (options.Personality is not null) payload["personality"] = MapPersonality(options.Personality.Value);
        if (options.SandboxPolicy is not null) payload["sandboxPolicy"] = BuildSandboxPolicyPayload(options.SandboxPolicy);
        if (options.ServiceTier is not null) payload["serviceTier"] = MapServiceTier(options.ServiceTier.Value);
        if (options.Summary is not null) payload["summary"] = MapReasoningSummary(options.Summary.Value);
        if (!string.IsNullOrWhiteSpace(options.WorkingDirectory)) payload["workingDirectory"] = options.WorkingDirectory;
        return payload;
    }

    private static JsonObject BuildApprovalPolicyPayload(CodexApprovalPolicy policy)
    {
        return policy switch
        {
            CodexApprovalModePolicy modePolicy => new JsonObject { ["value"] = MapApprovalMode(modePolicy.Mode) },
            CodexGranularApprovalPolicy granularPolicy => new JsonObject
            {
                ["granular"] = new JsonObject
                {
                    ["mcpElicitations"] = granularPolicy.Granular.McpElicitations,
                    ["requestPermissions"] = granularPolicy.Granular.RequestPermissions,
                    ["rules"] = granularPolicy.Granular.Rules,
                    ["sandboxApproval"] = granularPolicy.Granular.SandboxApproval,
                    ["skillApproval"] = granularPolicy.Granular.SkillApproval,
                },
            },
            _ => new JsonObject(),
        };
    }

    private static JsonNode? BuildSandboxPolicyPayload(CodexSandboxPolicy policy)
    {
        return policy switch
        {
            CodexDangerFullAccessSandboxPolicy => new JsonObject { ["type"] = "dangerFullAccess" },
            CodexReadOnlySandboxPolicy readOnly => new JsonObject
            {
                ["type"] = "readOnly",
                ["networkAccess"] = readOnly.NetworkAccess,
            },
            CodexExternalSandboxPolicy external => new JsonObject
            {
                ["type"] = "externalSandbox",
                ["networkAccess"] = MapNetworkAccess(external.NetworkAccess),
            },
            CodexWorkspaceWriteSandboxPolicy workspaceWrite => new JsonObject
            {
                ["type"] = "workspaceWrite",
                ["excludeSlashTmp"] = workspaceWrite.ExcludeSlashTmp,
                ["excludeTmpdirEnvVar"] = workspaceWrite.ExcludeTmpdirEnvVar,
                ["networkAccess"] = workspaceWrite.NetworkAccess,
            },
            _ => null,
        };
    }

    private static JsonNode? ToJsonNode(CodexConfigValue value)
    {
        return value switch
        {
            CodexConfigStringValue stringValue => JsonValue.Create(stringValue.Value),
            CodexConfigNumberValue numberValue => JsonValue.Create(numberValue.Value),
            CodexConfigBooleanValue booleanValue => JsonValue.Create(booleanValue.Value),
            CodexConfigArrayValue arrayValue => new JsonArray(arrayValue.Items.Select(ToJsonNode).ToArray()),
            CodexConfigObject objectValue => BuildConfigPayload(objectValue),
            _ => null,
        };
    }

    private static IReadOnlyList<string>? GetStringList(JsonObject? payload, string name)
    {
        JsonNode? node = GetNode(payload, name);
        if (node is not JsonArray array)
        {
            return null;
        }

        return array.Select(item => item?.GetValue<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).ToArray();
    }

    private static IReadOnlyList<CodexTurnPlanStep> ParseTurnPlanSteps(JsonObject payload)
    {
        JsonNode? planNode = GetNode(payload, "plan");
        if (planNode is not JsonArray array)
        {
            return [];
        }

        return array
            .OfType<JsonObject>()
            .Select(step => new CodexTurnPlanStep
            {
                Step = GetString(step, "step") ?? string.Empty,
                Status = ParseTurnPlanStepStatus(GetString(step, "status")),
            })
            .ToArray();
    }

    private static IReadOnlyList<CodexModelServiceTier> ParseModelServiceTiers(JsonObject payload)
    {
        JsonNode? serviceTiersNode = GetNode(payload, "serviceTiers");
        if (serviceTiersNode is not JsonArray array)
        {
            return [];
        }

        return array
            .OfType<JsonObject>()
            .Select(serviceTier => new CodexModelServiceTier
            {
                Id = GetString(serviceTier, "id") ?? string.Empty,
                Name = GetString(serviceTier, "name") ?? string.Empty,
                Description = GetString(serviceTier, "description") ?? string.Empty,
            })
            .ToArray();
    }

    private static CodexModelAvailabilityNux? ParseModelAvailabilityNux(JsonObject? payload)
    {
        if (payload is null)
        {
            return null;
        }

        return new CodexModelAvailabilityNux
        {
            Message = GetString(payload, "message") ?? string.Empty,
        };
    }

    private static IReadOnlyList<CodexInputModality>? ParseInputModalities(JsonObject payload)
    {
        JsonNode? modalitiesNode = GetNode(payload, "inputModalities");
        if (modalitiesNode is not JsonArray array)
        {
            return null;
        }

        List<CodexInputModality> result = [];
        foreach (JsonNode? item in array)
        {
            string? value = item?.GetValue<string>();
            if (TryParseInputModality(value, out CodexInputModality modality))
            {
                result.Add(modality);
            }
        }

        return result;
    }

    private static IReadOnlyList<CodexReasoningEffortOption> ParseReasoningEffortOptions(JsonObject payload)
    {
        JsonNode? optionsNode = GetNode(payload, "supportedReasoningEfforts");
        if (optionsNode is not JsonArray array)
        {
            return [];
        }

        return array
            .OfType<JsonObject>()
            .Select(option => new CodexReasoningEffortOption
            {
                Description = GetString(option, "description") ?? string.Empty,
                ReasoningEffort = ParseReasoningEffort(GetString(option, "reasoningEffort")),
            })
            .ToArray();
    }

    private static CodexModelUpgradeInfo? ParseModelUpgradeInfo(JsonObject? payload)
    {
        if (payload is null)
        {
            return null;
        }

        return new CodexModelUpgradeInfo
        {
            MigrationMarkdown = GetString(payload, "migrationMarkdown"),
            Model = GetString(payload, "model") ?? string.Empty,
            ModelLink = GetString(payload, "modelLink"),
            UpgradeCopy = GetString(payload, "upgradeCopy"),
        };
    }

    private static CodexMemoryCitation? ParseMemoryCitation(JsonObject? payload)
    {
        if (payload is null)
        {
            return null;
        }

        IReadOnlyList<CodexMemoryCitationEntry> entries = [];
        JsonNode? entriesNode = GetNode(payload, "entries");
        if (entriesNode is JsonArray entriesArray)
        {
            entries = entriesArray.OfType<JsonObject>().Select(ParseMemoryCitationEntry).ToArray();
        }

        return new CodexMemoryCitation
        {
            Entries = entries,
            ThreadIds = GetStringList(payload, "threadIds") ?? [],
        };
    }

    private static CodexMemoryCitationEntry ParseMemoryCitationEntry(JsonObject payload)
        => new()
        {
            LineEnd = GetInt(payload, "lineEnd") ?? 0,
            LineStart = GetInt(payload, "lineStart") ?? 0,
            Note = GetString(payload, "note") ?? string.Empty,
            Path = GetString(payload, "path") ?? string.Empty,
        };

    private static CodexHookPromptFragment ParseHookPromptFragment(JsonObject payload)
        => new()
        {
            HookRunId = GetString(payload, "hookRunId") ?? string.Empty,
            Text = GetString(payload, "text") ?? string.Empty,
        };

    private static CodexFileUpdateChange ParseFileUpdateChange(JsonObject payload)
        => new()
        {
            Diff = GetString(payload, "diff") ?? string.Empty,
            Kind = ParsePatchChangeKind(GetNode(payload, "kind")),
            Path = GetString(payload, "path") ?? string.Empty,
        };

    private static CodexPatchChangeKind ParsePatchChangeKind(JsonNode? node)
    {
        string? type = null;
        if (node is JsonObject payload)
        {
            type = GetString(payload, "type") ?? GetString(payload, "kind");
        }
        else if (node is JsonValue value && value.TryGetValue<string>(out string? text))
        {
            type = text;
        }

        return type switch
        {
            "add" => CodexPatchChangeKind.Add,
            "delete" => CodexPatchChangeKind.Delete,
            "update" => CodexPatchChangeKind.Update,
            _ => CodexPatchChangeKind.Update,
        };
    }

    private static CodexCommandAction ParseCommandAction(JsonObject payload)
    {
        string type = NormalizeCommandActionType(GetString(payload, "type"));
        return type switch
        {
            "read" => new CodexReadCommandAction
            {
                Command = GetString(payload, "command") ?? string.Empty,
                Name = GetString(payload, "name") ?? string.Empty,
                Path = GetString(payload, "path") ?? string.Empty,
            },
            "listFiles" => new CodexListFilesCommandAction
            {
                Command = GetString(payload, "command") ?? string.Empty,
                Path = GetString(payload, "path"),
            },
            "search" => new CodexSearchCommandAction
            {
                Command = GetString(payload, "command") ?? string.Empty,
                Path = GetString(payload, "path"),
                Query = GetString(payload, "query"),
            },
            _ => new CodexUnknownCommandAction
            {
                Command = GetString(payload, "command") ?? string.Empty,
            },
        };
    }

    private static string NormalizeCommandActionType(string? value)
        => value switch
        {
            "list_files" => "listFiles",
            "read" => "read",
            "search" => "search",
            "unknown" => "unknown",
            null or "" => "unknown",
            _ => value,
        };

    private static CodexWebSearchAction? ParseWebSearchAction(JsonObject? payload)
    {
        if (payload is null)
        {
            return null;
        }

        string type = NormalizeWebSearchActionType(GetString(payload, "type"));
        return type switch
        {
            "search" => new CodexSearchWebSearchAction
            {
                Queries = GetStringList(payload, "queries"),
                Query = GetString(payload, "query"),
            },
            "openPage" => new CodexOpenPageWebSearchAction
            {
                Url = GetString(payload, "url"),
            },
            "findInPage" => new CodexFindInPageWebSearchAction
            {
                Pattern = GetString(payload, "pattern"),
                Url = GetString(payload, "url"),
            },
            _ => new CodexOtherWebSearchAction(),
        };
    }

    private static string NormalizeWebSearchActionType(string? value)
        => value switch
        {
            "open_page" => "openPage",
            "find_in_page" => "findInPage",
            "search" => "search",
            "other" => "other",
            null or "" => "other",
            _ => value,
        };

    private static CodexDynamicToolCallOutputContentItem ParseDynamicToolCallOutputContentItem(JsonObject payload)
    {
        string type = NormalizeDynamicToolCallOutputContentItemType(GetString(payload, "type"));
        return type switch
        {
            "inputText" => new CodexInputTextDynamicToolCallOutputContentItem
            {
                Text = GetString(payload, "text") ?? string.Empty,
            },
            "inputImage" => new CodexInputImageDynamicToolCallOutputContentItem
            {
                ImageUrl = GetString(payload, "imageUrl") ?? string.Empty,
            },
            _ => new CodexInputTextDynamicToolCallOutputContentItem
            {
                Text = GetString(payload, "text") ?? string.Empty,
            },
        };
    }

    private static string NormalizeDynamicToolCallOutputContentItemType(string? value)
        => value switch
        {
            "input_text" => "inputText",
            "input_image" => "inputImage",
            "inputText" => "inputText",
            "inputImage" => "inputImage",
            null or "" => "inputText",
            _ => value,
        };

    private static CodexInputItem ParseInputItem(JsonObject payload)
    {
        string type = NormalizeInputType(GetString(payload, "type"));
        return type switch
        {
            "text" => new CodexTextInput
            {
                Text = GetString(payload, "text") ?? string.Empty,
            },
            "image" => new CodexImageInput
            {
                Url = GetString(payload, "url") ?? string.Empty,
            },
            "localImage" => new CodexLocalImageInput
            {
                Path = GetString(payload, "path") ?? string.Empty,
            },
            "skill" => new CodexSkillInput
            {
                Name = GetString(payload, "name") ?? string.Empty,
                Path = GetString(payload, "path") ?? string.Empty,
            },
            "mention" => new CodexMentionInput
            {
                Name = GetString(payload, "name") ?? string.Empty,
                Path = GetString(payload, "path") ?? string.Empty,
            },
            _ => new CodexUnknownInputItem(type) { RawPayload = payload },
        };
    }

    private static string NormalizeInputType(string? value)
        => value switch
        {
            "local_image" => "localImage",
            "localImage" => "localImage",
            "text" => "text",
            "image" => "image",
            "skill" => "skill",
            "mention" => "mention",
            null or "" => "unknown",
            _ => value,
        };

    private static CodexCollabAgentState ParseCollabAgentState(JsonObject payload)
        => new()
        {
            Message = GetString(payload, "message"),
            Status = ParseCollabAgentStatus(GetString(payload, "status")),
        };

    private static CodexCollabAgentStatus ParseCollabAgentStatus(string? value)
        => value switch
        {
            "pendingInit" => CodexCollabAgentStatus.PendingInit,
            "running" => CodexCollabAgentStatus.Running,
            "interrupted" => CodexCollabAgentStatus.Interrupted,
            "completed" => CodexCollabAgentStatus.Completed,
            "errored" => CodexCollabAgentStatus.Errored,
            "shutdown" => CodexCollabAgentStatus.Shutdown,
            "notFound" => CodexCollabAgentStatus.NotFound,
            _ => CodexCollabAgentStatus.PendingInit,
        };

    private static CodexCollabAgentTool ParseCollabAgentTool(string? value)
        => value switch
        {
            "spawnAgent" => CodexCollabAgentTool.SpawnAgent,
            "sendInput" => CodexCollabAgentTool.SendInput,
            "resumeAgent" => CodexCollabAgentTool.ResumeAgent,
            "wait" => CodexCollabAgentTool.Wait,
            "closeAgent" => CodexCollabAgentTool.CloseAgent,
            _ => CodexCollabAgentTool.SpawnAgent,
        };

    private static CodexTodoItem ParseTodoItem(JsonObject payload)
        => new()
        {
            Completed = GetBool(payload, "completed") ?? false,
            Text = GetString(payload, "text") ?? string.Empty,
        };

    private static CodexMcpToolCallError ParseMcpToolCallError(JsonObject? payload)
        => new()
        {
            Message = GetString(payload, "message") ?? string.Empty,
        };

    private static CodexMcpToolCallResult ParseMcpToolCallResult(JsonObject? payload)
    {
        if (payload is null)
        {
            return new CodexMcpToolCallResult();
        }

        IReadOnlyList<JsonNode> content = [];
        JsonNode? contentNode = GetNode(payload, "content");
        if (contentNode is JsonArray contentArray)
        {
            content = contentArray.Where(node => node is not null).Select(node => node!.DeepClone()).ToArray();
        }

        return new CodexMcpToolCallResult
        {
            Content = content,
            StructuredContent = GetNode(payload, "structuredContent"),
        };
    }

    private static CodexCommandExecutionSource? ParseCommandExecutionSource(string? value)
        => value switch
        {
            "agent" => CodexCommandExecutionSource.Agent,
            "userShell" => CodexCommandExecutionSource.UserShell,
            "unifiedExecStartup" => CodexCommandExecutionSource.UnifiedExecStartup,
            "unifiedExecInteraction" => CodexCommandExecutionSource.UnifiedExecInteraction,
            _ => null,
        };

    private static IReadOnlyList<CodexInputItem> ParseInputItems(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return [];
        }

        return array.OfType<JsonObject>().Select(ParseInputItem).ToArray();
    }

    private static IReadOnlyList<CodexHookPromptFragment> ParseHookPromptFragments(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return [];
        }

        return array.OfType<JsonObject>().Select(ParseHookPromptFragment).ToArray();
    }

    private static IReadOnlyList<CodexCommandAction> ParseCommandActions(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return [];
        }

        return array.OfType<JsonObject>().Select(ParseCommandAction).ToArray();
    }

    private static IReadOnlyList<CodexFileUpdateChange> ParseFileUpdateChanges(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return [];
        }

        return array.OfType<JsonObject>().Select(ParseFileUpdateChange).ToArray();
    }

    private static IReadOnlyList<CodexDynamicToolCallOutputContentItem>? ParseDynamicToolCallOutputContentItems(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return null;
        }

        return array.OfType<JsonObject>().Select(ParseDynamicToolCallOutputContentItem).ToArray();
    }

    private static IReadOnlyDictionary<string, CodexCollabAgentState> ParseCollabAgentStates(JsonObject? payload)
    {
        Dictionary<string, CodexCollabAgentState> result = new(StringComparer.Ordinal);
        if (payload is null)
        {
            return result;
        }

        foreach (KeyValuePair<string, JsonNode?> pair in payload)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value is JsonObject state)
            {
                result[pair.Key] = ParseCollabAgentState(state);
            }
        }

        return result;
    }

    private static IReadOnlyList<CodexTodoItem> ParseTodoItems(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return [];
        }

        return array.OfType<JsonObject>().Select(ParseTodoItem).ToArray();
    }

    private static CodexCommandExecutionStatus ParseCommandExecutionStatus(string? value)
        => value switch
        {
            "inProgress" or "in_progress" => CodexCommandExecutionStatus.InProgress,
            "completed" => CodexCommandExecutionStatus.Completed,
            "failed" => CodexCommandExecutionStatus.Failed,
            "declined" => CodexCommandExecutionStatus.Declined,
            _ => CodexCommandExecutionStatus.InProgress,
        };

    private static CodexPatchApplyStatus ParsePatchApplyStatus(string? value)
        => value switch
        {
            "inProgress" or "in_progress" => CodexPatchApplyStatus.InProgress,
            "completed" => CodexPatchApplyStatus.Completed,
            "failed" => CodexPatchApplyStatus.Failed,
            "declined" => CodexPatchApplyStatus.Declined,
            _ => CodexPatchApplyStatus.InProgress,
        };

    private static CodexMcpToolCallStatus ParseMcpToolCallStatus(string? value)
        => value switch
        {
            "inProgress" or "in_progress" => CodexMcpToolCallStatus.InProgress,
            "completed" => CodexMcpToolCallStatus.Completed,
            "failed" => CodexMcpToolCallStatus.Failed,
            _ => CodexMcpToolCallStatus.InProgress,
        };

    private static CodexDynamicToolCallStatus ParseDynamicToolCallStatus(string? value)
        => value switch
        {
            "inProgress" or "in_progress" => CodexDynamicToolCallStatus.InProgress,
            "completed" => CodexDynamicToolCallStatus.Completed,
            "failed" => CodexDynamicToolCallStatus.Failed,
            _ => CodexDynamicToolCallStatus.InProgress,
        };

    private static CodexCollabAgentToolCallStatus ParseCollabAgentToolCallStatus(string? value)
        => value switch
        {
            "inProgress" or "in_progress" => CodexCollabAgentToolCallStatus.InProgress,
            "completed" => CodexCollabAgentToolCallStatus.Completed,
            "failed" => CodexCollabAgentToolCallStatus.Failed,
            _ => CodexCollabAgentToolCallStatus.InProgress,
        };

    private static CodexThreadStatus ParseThreadStatus(JsonObject? payload)
    {
        string? type = GetString(payload, "type");
        return type switch
        {
            "idle" => new CodexIdleThreadStatus(),
            "active" => new CodexActiveThreadStatus(),
            "systemError" => new CodexSystemErrorThreadStatus(),
            _ => new CodexNotLoadedThreadStatus(),
        };
    }

    private static CodexTurnStatus ParseTurnStatus(string? value)
        => value switch
        {
            "completed" => CodexTurnStatus.Completed,
            "interrupted" => CodexTurnStatus.Interrupted,
            "failed" => CodexTurnStatus.Failed,
            "inProgress" => CodexTurnStatus.InProgress,
            _ => CodexTurnStatus.InProgress,
        };

    private static CodexTurnPlanStepStatus ParseTurnPlanStepStatus(string? value)
        => value switch
        {
            "completed" => CodexTurnPlanStepStatus.Completed,
            "inProgress" or "in_progress" => CodexTurnPlanStepStatus.InProgress,
            _ => CodexTurnPlanStepStatus.Pending,
        };

    private static CodexReasoningEffort ParseReasoningEffort(string? value)
        => value switch
        {
            "none" => CodexReasoningEffort.None,
            "minimal" => CodexReasoningEffort.Minimal,
            "low" => CodexReasoningEffort.Low,
            "medium" => CodexReasoningEffort.Medium,
            "high" => CodexReasoningEffort.High,
            "xhigh" => CodexReasoningEffort.XHigh,
            _ => CodexReasoningEffort.Medium,
        };

    private static bool TryParseInputModality(string? value, out CodexInputModality modality)
    {
        switch (value)
        {
            case "text":
                modality = CodexInputModality.Text;
                return true;
            case "image":
                modality = CodexInputModality.Image;
                return true;
            default:
                modality = default;
                return false;
        }
    }

    private static CodexMessagePhase? ParseMessagePhase(string? value)
        => value switch
        {
            "commentary" => CodexMessagePhase.Commentary,
            "finalAnswer" => CodexMessagePhase.FinalAnswer,
            "final_answer" => CodexMessagePhase.FinalAnswer,
            _ => null,
        };

    private static string NormalizeItemType(string? value)
        => value switch
        {
            "agent_message" => "agentMessage",
            "command_execution" => "commandExecution",
            "file_change" => "fileChange",
            "mcp_tool_call" => "mcpToolCall",
            "dynamic_tool_call" => "dynamicToolCall",
            "collab_agent_tool_call" => "collabAgentToolCall",
            "web_search" => "webSearch",
            "hook_prompt" => "hookPrompt",
            "image_view" => "imageView",
            "image_generation" => "imageGeneration",
            "entered_review_mode" => "enteredReviewMode",
            "exited_review_mode" => "exitedReviewMode",
            "todo_list" => "todoList",
            "context_compaction" => "contextCompaction",
            "user_message" => "userMessage",
            null or "" => "unknown",
            _ => value,
        };

    private static string MapApprovalsReviewer(CodexApprovalsReviewer reviewer)
        => reviewer == CodexApprovalsReviewer.GuardianSubAgent ? "guardian_subagent" : "user";

    private static string MapApprovalMode(CodexApprovalMode mode)
        => mode switch
        {
            CodexApprovalMode.Never => "never",
            CodexApprovalMode.OnRequest => "on-request",
            CodexApprovalMode.OnFailure => "on-failure",
            CodexApprovalMode.Untrusted => "untrusted",
            _ => "on-request",
        };

    private static string MapPersonality(CodexPersonality personality)
        => personality switch
        {
            CodexPersonality.Friendly => "friendly",
            CodexPersonality.Pragmatic => "pragmatic",
            _ => "none",
        };

    private static string MapReasoningSummary(CodexReasoningSummary summary)
        => summary switch
        {
            CodexReasoningSummary.Auto => "auto",
            CodexReasoningSummary.Concise => "concise",
            CodexReasoningSummary.Detailed => "detailed",
            _ => "none",
        };

    private static string MapServiceTier(CodexServiceTier serviceTier)
        => serviceTier == CodexServiceTier.Flex ? "flex" : "priority";

    private static string MapNetworkAccess(CodexNetworkAccess networkAccess)
        => networkAccess == CodexNetworkAccess.Enabled ? "enabled" : "restricted";

    private static string MapReasoningEffort(CodexReasoningEffort effort)
        => effort switch
        {
            CodexReasoningEffort.None => "none",
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
            CodexWebSearchMode.Live => "live",
            CodexWebSearchMode.Cached => "cached",
            _ => "disabled",
        };

    private static string MapThreadSourceKind(CodexThreadSourceKind sourceKind)
        => sourceKind switch
        {
            CodexThreadSourceKind.Cli => "cli",
            CodexThreadSourceKind.Vscode => "vscode",
            CodexThreadSourceKind.Exec => "exec",
            CodexThreadSourceKind.AppServer => "appServer",
            CodexThreadSourceKind.SubAgent => "subAgent",
            CodexThreadSourceKind.SubAgentReview => "subAgentReview",
            CodexThreadSourceKind.SubAgentCompact => "subAgentCompact",
            CodexThreadSourceKind.SubAgentThreadSpawn => "subAgentThreadSpawn",
            CodexThreadSourceKind.SubAgentOther => "subAgentOther",
            _ => "unknown",
        };

    private static string MapThreadSortDirection(CodexThreadSortDirection sortDirection)
        => sortDirection == CodexThreadSortDirection.Asc ? "asc" : "desc";

    private static string MapThreadSource(CodexThreadSource threadSource)
        => threadSource switch
        {
            CodexThreadSource.User => "user",
            CodexThreadSource.Subagent => "subagent",
            CodexThreadSource.MemoryConsolidation => "memory_consolidation",
            _ => "user",
        };

    private static string MapThreadStartSource(CodexThreadStartSource startSource)
        => startSource == CodexThreadStartSource.Clear ? "clear" : "startup";

    private static string MapThreadGoalStatus(CodexThreadGoalStatus status)
        => status switch
        {
            CodexThreadGoalStatus.Paused => "paused",
            CodexThreadGoalStatus.BudgetLimited => "budgetLimited",
            CodexThreadGoalStatus.Complete => "complete",
            _ => "active",
        };

    private static CodexThreadGoalStatus ParseThreadGoalStatus(string? status)
        => status switch
        {
            "paused" => CodexThreadGoalStatus.Paused,
            "budgetLimited" or "budget_limited" => CodexThreadGoalStatus.BudgetLimited,
            "complete" => CodexThreadGoalStatus.Complete,
            _ => CodexThreadGoalStatus.Active,
        };

    private static CodexThreadSource? ParseThreadSource(string? value)
        => value switch
        {
            "user" => CodexThreadSource.User,
            "subagent" => CodexThreadSource.Subagent,
            "memory_consolidation" => CodexThreadSource.MemoryConsolidation,
            _ => null,
        };

    private static CodexThreadStartSource? ParseThreadStartSource(string? value)
        => value switch
        {
            "startup" => CodexThreadStartSource.Startup,
            "clear" => CodexThreadStartSource.Clear,
            _ => null,
        };

    private static CodexThreadSortDirection? ParseThreadSortDirection(string? value)
        => value switch
        {
            "asc" => CodexThreadSortDirection.Asc,
            "desc" => CodexThreadSortDirection.Desc,
            _ => null,
        };

    internal static CodexThreadUnsubscribeStatus ParseThreadUnsubscribeStatus(string? value)
        => value switch
        {
            "notSubscribed" => CodexThreadUnsubscribeStatus.NotSubscribed,
            "unsubscribed" => CodexThreadUnsubscribeStatus.Unsubscribed,
            _ => CodexThreadUnsubscribeStatus.NotLoaded,
        };

    private static DateTimeOffset ParseDateTimeOffset(JsonObject payload, string name)
    {
        JsonNode? node = GetNode(payload, name);
        if (node is JsonValue value)
        {
            if (value.TryGetValue<long>(out long seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds);
            }

            if (value.TryGetValue<string>(out string? text) && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset parsed))
            {
                return parsed;
            }
        }

        return DateTimeOffset.UnixEpoch;
    }

    private static (string? Name, string? Version) ParseUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return (null, null);
        }

        string trimmed = userAgent.Trim();
        int slash = trimmed.IndexOf('/');
        if (slash > 0 && slash < trimmed.Length - 1)
        {
            return (trimmed[..slash], trimmed[(slash + 1)..]);
        }

        string[] parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }

        return (trimmed, null);
    }

    private static CodexGitInfo? ParseGitInfo(JsonObject? payload)
    {
        if (payload is null)
        {
            return null;
        }

        return new CodexGitInfo
        {
            Branch = GetString(payload, "branch"),
            OriginUrl = GetString(payload, "originUrl"),
            Sha = GetString(payload, "sha"),
        };
    }

    private static JsonObject BuildThreadMetadataGitInfoUpdatePayload(CodexThreadMetadataGitInfoUpdate gitInfo)
    {
        JsonObject payload = new();

        if (gitInfo.BranchSpecified)
        {
            payload["branch"] = gitInfo.Branch;
        }

        if (gitInfo.OriginUrlSpecified)
        {
            payload["originUrl"] = gitInfo.OriginUrl;
        }

        if (gitInfo.ShaSpecified)
        {
            payload["sha"] = gitInfo.Sha;
        }

        return payload;
    }

    private static CodexPlanType ParsePlanType(string? value)
        => value switch
        {
            "free" => CodexPlanType.Free,
            "go" => CodexPlanType.Go,
            "plus" => CodexPlanType.Plus,
            "pro" => CodexPlanType.Pro,
            "prolite" => CodexPlanType.Prolite,
            "team" => CodexPlanType.Team,
            "self_serve_business_usage_based" => CodexPlanType.SelfServeBusinessUsageBased,
            "business" => CodexPlanType.Business,
            "enterprise_cbp_usage_based" => CodexPlanType.EnterpriseCbpUsageBased,
            "enterprise" => CodexPlanType.Enterprise,
            "edu" => CodexPlanType.Edu,
            _ => CodexPlanType.Unknown,
        };

    private static CodexAuthMode ParseAuthMode(string? value)
        => value switch
        {
            "apikey" => CodexAuthMode.ApiKey,
            "chatgpt" => CodexAuthMode.Chatgpt,
            "chatgptAuthTokens" => CodexAuthMode.ChatgptAuthTokens,
            "agentIdentity" => CodexAuthMode.AgentIdentity,
            _ => CodexAuthMode.Unknown,
        };

    private static CodexProcessOutputStream ParseProcessOutputStream(string? value)
        => value switch
        {
            "stdout" => CodexProcessOutputStream.Stdout,
            "stderr" => CodexProcessOutputStream.Stderr,
            _ => CodexProcessOutputStream.Unknown,
        };

    private static CodexHookEventName ParseHookEventName(string? value)
        => value switch
        {
            "preToolUse" => CodexHookEventName.PreToolUse,
            "permissionRequest" => CodexHookEventName.PermissionRequest,
            "postToolUse" => CodexHookEventName.PostToolUse,
            "preCompact" => CodexHookEventName.PreCompact,
            "postCompact" => CodexHookEventName.PostCompact,
            "sessionStart" => CodexHookEventName.SessionStart,
            "userPromptSubmit" => CodexHookEventName.UserPromptSubmit,
            "stop" => CodexHookEventName.Stop,
            _ => CodexHookEventName.Unknown,
        };

    private static CodexHookExecutionMode ParseHookExecutionMode(string? value)
        => value switch
        {
            "sync" => CodexHookExecutionMode.Sync,
            "async" => CodexHookExecutionMode.Async,
            _ => CodexHookExecutionMode.Unknown,
        };

    private static CodexHookHandlerType ParseHookHandlerType(string? value)
        => value switch
        {
            "command" => CodexHookHandlerType.Command,
            "prompt" => CodexHookHandlerType.Prompt,
            "agent" => CodexHookHandlerType.Agent,
            _ => CodexHookHandlerType.Unknown,
        };

    private static CodexHookOutputEntryKind ParseHookOutputEntryKind(string? value)
        => value switch
        {
            "warning" => CodexHookOutputEntryKind.Warning,
            "stop" => CodexHookOutputEntryKind.Stop,
            "feedback" => CodexHookOutputEntryKind.Feedback,
            "context" => CodexHookOutputEntryKind.Context,
            "error" => CodexHookOutputEntryKind.Error,
            _ => CodexHookOutputEntryKind.Unknown,
        };

    private static CodexHookRunStatus ParseHookRunStatus(string? value)
        => value switch
        {
            "running" => CodexHookRunStatus.Running,
            "completed" => CodexHookRunStatus.Completed,
            "failed" => CodexHookRunStatus.Failed,
            "blocked" => CodexHookRunStatus.Blocked,
            "stopped" => CodexHookRunStatus.Stopped,
            _ => CodexHookRunStatus.Unknown,
        };

    private static CodexHookScope ParseHookScope(string? value)
        => value switch
        {
            "thread" => CodexHookScope.Thread,
            "turn" => CodexHookScope.Turn,
            _ => CodexHookScope.Unknown,
        };

    private static CodexHookSourceKind ParseHookSourceKind(string? value)
        => value switch
        {
            "system" => CodexHookSourceKind.System,
            "user" => CodexHookSourceKind.User,
            "project" => CodexHookSourceKind.Project,
            "mdm" => CodexHookSourceKind.Mdm,
            "sessionFlags" => CodexHookSourceKind.SessionFlags,
            "plugin" => CodexHookSourceKind.Plugin,
            "cloudRequirements" => CodexHookSourceKind.CloudRequirements,
            "legacyManagedConfigFile" => CodexHookSourceKind.LegacyManagedConfigFile,
            "legacyManagedConfigMdm" => CodexHookSourceKind.LegacyManagedConfigMdm,
            _ => CodexHookSourceKind.Unknown,
        };

    private static CodexRealtimeConversationVersion ParseRealtimeConversationVersion(string? value)
        => value switch
        {
            "v1" => CodexRealtimeConversationVersion.V1,
            "v2" => CodexRealtimeConversationVersion.V2,
            _ => CodexRealtimeConversationVersion.Unknown,
        };

    private static CodexFuzzyFileSearchMatchType ParseFuzzyFileSearchMatchType(string? value)
        => value switch
        {
            "file" => CodexFuzzyFileSearchMatchType.File,
            "directory" => CodexFuzzyFileSearchMatchType.Directory,
            _ => CodexFuzzyFileSearchMatchType.Unknown,
        };

    private static CodexMcpServerStartupState ParseMcpServerStartupState(string? value)
        => value switch
        {
            "starting" => CodexMcpServerStartupState.Starting,
            "ready" => CodexMcpServerStartupState.Ready,
            "failed" => CodexMcpServerStartupState.Failed,
            "cancelled" => CodexMcpServerStartupState.Cancelled,
            _ => CodexMcpServerStartupState.Unknown,
        };

    private static CodexRemoteControlConnectionStatus ParseRemoteControlConnectionStatus(string? value)
        => value switch
        {
            "disabled" => CodexRemoteControlConnectionStatus.Disabled,
            "connecting" => CodexRemoteControlConnectionStatus.Connecting,
            "connected" => CodexRemoteControlConnectionStatus.Connected,
            "errored" => CodexRemoteControlConnectionStatus.Errored,
            _ => CodexRemoteControlConnectionStatus.Unknown,
        };

    private static CodexWindowsSandboxSetupMode ParseWindowsSandboxSetupMode(string? value)
        => value switch
        {
            "elevated" => CodexWindowsSandboxSetupMode.Elevated,
            "unelevated" => CodexWindowsSandboxSetupMode.Unelevated,
            _ => CodexWindowsSandboxSetupMode.Unknown,
        };

    private static CodexModelRerouteReason ParseModelRerouteReason(string? value)
        => value switch
        {
            "highRiskCyberActivity" => CodexModelRerouteReason.HighRiskCyberActivity,
            _ => CodexModelRerouteReason.Unknown,
        };

    private static CodexModelVerificationValue ParseModelVerificationValue(string? value)
        => value switch
        {
            "trustedAccessForCyber" => CodexModelVerificationValue.TrustedAccessForCyber,
            _ => CodexModelVerificationValue.Unknown,
        };

    private static CodexTextPosition ParseTextPosition(JsonObject? payload)
        => new()
        {
            Column = GetInt(payload, "column") ?? 0,
            Line = GetInt(payload, "line") ?? 0,
        };

    private static CodexTextRange? ParseTextRange(JsonObject? payload)
    {
        if (payload is null)
        {
            return null;
        }

        return new CodexTextRange
        {
            End = ParseTextPosition(GetObject(payload, "end")),
            Start = ParseTextPosition(GetObject(payload, "start")),
        };
    }

    private static CodexHookOutputEntry ParseHookOutputEntry(JsonObject payload)
        => new()
        {
            Kind = ParseHookOutputEntryKind(GetString(payload, "kind")),
            Text = GetString(payload, "text") ?? string.Empty,
        };

    private static CodexHookRunSummary ParseHookRunSummary(JsonObject payload)
        => new()
        {
            CompletedAt = GetLongAny(payload, "completedAt"),
            DisplayOrder = GetInt(payload, "displayOrder") ?? 0,
            DurationMs = GetLongAny(payload, "durationMs"),
            Entries = GetNode(payload, "entries") is JsonArray entriesArray
                ? entriesArray.OfType<JsonObject>().Select(ParseHookOutputEntry).ToArray()
                : [],
            EventName = ParseHookEventName(GetString(payload, "eventName")),
            ExecutionMode = ParseHookExecutionMode(GetString(payload, "executionMode")),
            HandlerType = ParseHookHandlerType(GetString(payload, "handlerType")),
            Id = GetString(payload, "id") ?? string.Empty,
            Scope = ParseHookScope(GetString(payload, "scope")),
            Source = ParseHookSourceKind(GetString(payload, "source")),
            SourcePath = GetString(payload, "sourcePath") ?? string.Empty,
            StartedAt = GetLongAny(payload, "startedAt") ?? 0,
            Status = ParseHookRunStatus(GetString(payload, "status")),
            StatusMessage = GetString(payload, "statusMessage"),
        };

    private static CodexThreadRealtimeAudioChunk ParseThreadRealtimeAudioChunk(JsonObject? payload)
        => payload is null
            ? new CodexThreadRealtimeAudioChunk()
            : new CodexThreadRealtimeAudioChunk
            {
                Data = GetString(payload, "data") ?? string.Empty,
                ItemId = GetString(payload, "itemId"),
                NumChannels = GetInt(payload, "numChannels") ?? 0,
                SampleRate = GetInt(payload, "sampleRate") ?? 0,
                SamplesPerChannel = GetInt(payload, "samplesPerChannel"),
            };

    private static IReadOnlyList<int> ParseIntList(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return [];
        }

        return array
            .OfType<JsonValue>()
            .Select(value =>
            {
                if (value.TryGetValue<int>(out int intValue))
                {
                    return intValue;
                }

                if (value.TryGetValue<long>(out long longValue))
                {
                    return checked((int)longValue);
                }

                return 0;
            })
            .ToArray();
    }

    private static IReadOnlyList<CodexModelVerificationValue> ParseModelVerificationValues(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return [];
        }

        return array
            .OfType<JsonValue>()
            .Select(value => ParseModelVerificationValue(value.TryGetValue<string>(out string? text) ? text : null))
            .ToArray();
    }

    private static IReadOnlyList<CodexFuzzyFileSearchResult> ParseFuzzyFileSearchResults(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return [];
        }

        return array
            .OfType<JsonObject>()
            .Select(payload => new CodexFuzzyFileSearchResult
            {
                FileName = GetString(payload, "fileName") ?? string.Empty,
                Indices = ParseIntList(GetNode(payload, "indices")),
                MatchType = ParseFuzzyFileSearchMatchType(GetString(payload, "matchType")),
                Path = GetString(payload, "path") ?? string.Empty,
                Root = GetString(payload, "root") ?? string.Empty,
                Score = GetInt(payload, "score") ?? 0,
            })
            .ToArray();
    }

    private static CodexSessionSourceKind ParseSessionSourceKind(string? value)
        => value switch
        {
            "cli" => CodexSessionSourceKind.Cli,
            "vscode" => CodexSessionSourceKind.Vscode,
            "exec" => CodexSessionSourceKind.Exec,
            "appServer" => CodexSessionSourceKind.AppServer,
            _ => CodexSessionSourceKind.Unknown,
        };

    private static JsonObject BuildGitInfoPayload(CodexGitInfo gitInfo)
    {
        JsonObject payload = new();

        if (!string.IsNullOrWhiteSpace(gitInfo.Branch))
        {
            payload["branch"] = gitInfo.Branch;
        }

        if (!string.IsNullOrWhiteSpace(gitInfo.OriginUrl))
        {
            payload["originUrl"] = gitInfo.OriginUrl;
        }

        if (!string.IsNullOrWhiteSpace(gitInfo.Sha))
        {
            payload["sha"] = gitInfo.Sha;
        }

        return payload;
    }

    private static CodexSessionSource ParseSessionSource(JsonNode? node)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out string? text))
        {
            return new CodexSessionSourceValue(ParseSessionSourceKind(text));
        }

        if (node is JsonObject payload)
        {
            if (GetString(payload, "custom") is string custom)
            {
                return new CodexCustomSessionSource(custom);
            }

            JsonNode? subAgentNode = GetNode(payload, "subAgent");
            if (subAgentNode is not null)
            {
                return new CodexSubAgentSessionSource(ParseSubAgentSource(subAgentNode));
            }
        }

        return new CodexSessionSourceValue(CodexSessionSourceKind.Unknown);
    }

    private static CodexSubAgentSource ParseSubAgentSource(JsonNode? node)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out string? text))
        {
            return text switch
            {
                "review" => new CodexSubAgentSourceValue(CodexSubAgentSourceKind.Review),
                "compact" => new CodexSubAgentSourceValue(CodexSubAgentSourceKind.Compact),
                "memory_consolidation" => new CodexSubAgentSourceValue(CodexSubAgentSourceKind.MemoryConsolidation),
                _ => new CodexOtherSubAgentSource(text),
            };
        }

        if (node is JsonObject payload)
        {
            if (GetObject(payload, "threadSpawn") is JsonObject threadSpawn)
            {
                return new CodexThreadSpawnSubAgentSource(ParseThreadSpawn(threadSpawn));
            }

            if (GetString(payload, "other") is string other)
            {
                return new CodexOtherSubAgentSource(other);
            }
        }

        return new CodexOtherSubAgentSource(string.Empty);
    }

    private static CodexThreadSpawn ParseThreadSpawn(JsonObject payload)
        => new()
        {
            AgentNickname = GetString(payload, "agentNickname"),
            AgentRole = GetString(payload, "agentRole"),
            Depth = GetInt(payload, "depth") ?? 0,
            ParentThreadId = GetString(payload, "parentThreadId") ?? string.Empty,
        };

    private static string NormalizeEventType(string? value)
        => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Replace('/', '.');

    private static JsonObject? GetObject(JsonObject? payload, string name)
        => GetNode(payload, name) as JsonObject;

    private static JsonObject? GetObjectAny(JsonObject? payload, params string[] names)
        => GetNodeAny(payload, names) as JsonObject;

    private static JsonNode? GetNode(JsonObject? payload, string name)
        => payload is not null && payload.TryGetPropertyValue(name, out JsonNode? node) ? node : null;

    private static JsonNode? GetNodeAny(JsonObject? payload, params string[] names)
    {
        if (payload is null)
        {
            return null;
        }

        foreach (string name in names)
        {
            if (payload.TryGetPropertyValue(name, out JsonNode? node))
            {
                return node;
            }
        }

        return null;
    }

    private static string? GetString(JsonObject? payload, string name)
        => GetNode(payload, name)?.GetValue<string>();

    private static string? GetStringAny(JsonObject? payload, params string[] names)
    {
        if (GetNodeAny(payload, names) is JsonValue value && value.TryGetValue<string>(out string? result))
        {
            return result;
        }

        return null;
    }

    private static bool? GetBool(JsonObject? payload, string name)
    {
        JsonNode? node = GetNode(payload, name);
        if (node is JsonValue value && value.TryGetValue<bool>(out bool result))
        {
            return result;
        }

        return null;
    }

    private static bool? GetBoolAny(JsonObject? payload, params string[] names)
    {
        JsonNode? node = GetNodeAny(payload, names);
        if (node is JsonValue value && value.TryGetValue<bool>(out bool result))
        {
            return result;
        }

        return null;
    }

    private static int? GetInt(JsonObject? payload, string name)
    {
        JsonNode? node = GetNode(payload, name);
        if (node is JsonValue value)
        {
            if (value.TryGetValue<int>(out int result))
            {
                return result;
            }

            if (value.TryGetValue<long>(out long longResult))
            {
                return checked((int)longResult);
            }
        }

        return null;
    }

    private static int? GetIntAny(JsonObject? payload, params string[] names)
    {
        long? value = GetLongAny(payload, names);
        if (value.HasValue)
        {
            return checked((int)value.Value);
        }

        return null;
    }

    private static long? GetLongAny(JsonObject? payload, params string[] names)
    {
        JsonNode? node = GetNodeAny(payload, names);
        if (node is JsonValue value)
        {
            if (value.TryGetValue<long>(out long result))
            {
                return result;
            }

            if (value.TryGetValue<int>(out int intResult))
            {
                return intResult;
            }

            if (value.TryGetValue<double>(out double doubleResult))
            {
                return checked((long)doubleResult);
            }

            if (value.TryGetValue<string>(out string? text) && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static double? GetDoubleAny(JsonObject? payload, params string[] names)
    {
        JsonNode? node = GetNodeAny(payload, names);
        if (node is JsonValue value)
        {
            if (value.TryGetValue<double>(out double result))
            {
                return result;
            }

            if (value.TryGetValue<long>(out long longResult))
            {
                return longResult;
            }

            if (value.TryGetValue<int>(out int intResult))
            {
                return intResult;
            }

            if (value.TryGetValue<string>(out string? text) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static DateTimeOffset? ParseNullableDateTimeOffset(JsonObject? payload, params string[] names)
    {
        JsonNode? node = GetNodeAny(payload, names);
        if (node is not JsonValue value)
        {
            return null;
        }

        if (value.TryGetValue<long>(out long seconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }

        if (value.TryGetValue<int>(out int intSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(intSeconds);
        }

        if (value.TryGetValue<double>(out double doubleSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(checked((long)doubleSeconds));
        }

        if (value.TryGetValue<string>(out string? text))
        {
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedSeconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(parsedSeconds);
            }

            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}
