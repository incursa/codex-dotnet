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
        AddThreadOptions(payload, options);
        return payload;
    }

    public static JsonObject BuildThreadResumeParams(string threadId, CodexThreadOptions? options)
    {
        JsonObject payload = BuildThreadStartParams(options);
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
        if (!string.IsNullOrWhiteSpace(options.WorkingDirectory)) payload["cwd"] = options.WorkingDirectory;
        if (options.Limit.HasValue) payload["limit"] = options.Limit.Value;
        if (options.ModelProviders is { Count: > 0 }) payload["modelProviders"] = new JsonArray(options.ModelProviders.Select(value => JsonValue.Create(value)).ToArray());
        if (!string.IsNullOrWhiteSpace(options.SearchTerm)) payload["searchTerm"] = options.SearchTerm;
        if (options.SortKey is not null) payload["sortKey"] = options.SortKey.Value == CodexThreadSortKey.CreatedAt ? "created_at" : "updated_at";
        if (options.SourceKinds is { Count: > 0 }) payload["sourceKinds"] = new JsonArray(options.SourceKinds.Select(value => JsonValue.Create(MapThreadSourceKind(value))).ToArray());
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
            "error" => new CodexThreadErrorEvent
            {
                ThreadId = GetString(payload, "threadId") ?? string.Empty,
                TurnId = GetString(payload, "turnId"),
                WillRetry = GetBool(payload, "willRetry") ?? false,
                Error = ParseTurnError(GetObject(payload, "error") ?? payload),
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
            Path = GetString(payload, "path"),
            Source = new CodexSessionSourceValue(CodexSessionSourceKind.Unknown),
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
            Path = summary.Path,
            Source = summary.Source,
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
            "agentMessage" => new CodexAgentMessageItem
            {
                Id = id,
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
            "contextCompaction" => new CodexContextCompactionItem { Id = id },
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
            Id = GetString(payload, "id") ?? string.Empty,
            Model = GetString(payload, "model") ?? string.Empty,
            DisplayName = GetString(payload, "displayName") ?? string.Empty,
            Description = GetString(payload, "description") ?? string.Empty,
            Hidden = GetBool(payload, "hidden") ?? false,
            IsDefault = GetBool(payload, "isDefault") ?? false,
            DefaultReasoningEffort = ParseReasoningEffort(GetString(payload, "defaultReasoningEffort")),
            SupportedReasoningEfforts = [],
        };
    }

    private static void AddThreadOptions(JsonObject payload, CodexThreadOptions? options)
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
        => serviceTier == CodexServiceTier.Flex ? "flex" : "fast";

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

    private static string NormalizeEventType(string? value)
        => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Replace('/', '.');

    private static JsonObject? GetObject(JsonObject? payload, string name)
        => GetNode(payload, name) as JsonObject;

    private static JsonNode? GetNode(JsonObject? payload, string name)
        => payload is not null && payload.TryGetPropertyValue(name, out JsonNode? node) ? node : null;

    private static string? GetString(JsonObject? payload, string name)
        => GetNode(payload, name)?.GetValue<string>();

    private static bool? GetBool(JsonObject? payload, string name)
    {
        JsonNode? node = GetNode(payload, name);
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
}


