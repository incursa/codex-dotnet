using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-CATALOG-0307, REQ-CODEX-SDK-HELPERS-0313, REQ-CODEX-SDK-HELPERS-0314, REQ-CODEX-SDK-HELPERS-0315,
// REQ-CODEX-SDK-HELPERS-0319, REQ-CODEX-SDK-CATALOG-0302, REQ-CODEX-SDK-CATALOG-0305, REQ-CODEX-SDK-CATALOG-0309.

public abstract record CodexApprovalPolicy;

public sealed record CodexApprovalModePolicy(CodexApprovalMode Mode) : CodexApprovalPolicy;

public sealed record CodexGranularApprovalPolicy(CodexGranularApprovalRules Granular) : CodexApprovalPolicy;

public sealed record CodexGranularApprovalRules
{
    public bool McpElicitations { get; init; }

    public bool RequestPermissions { get; init; }

    public bool Rules { get; init; }

    public bool SandboxApproval { get; init; }

    public bool SkillApproval { get; init; }
}

public abstract record CodexReadOnlyAccess(string Type);

public sealed record CodexRestrictedReadOnlyAccess() : CodexReadOnlyAccess("restricted")
{
    public bool IncludePlatformDefaults { get; init; } = true;

    public IReadOnlyList<string> ReadableRoots { get; init; } = [];
}

public sealed record CodexFullAccessReadOnlyAccess() : CodexReadOnlyAccess("fullAccess");

public abstract record CodexSandboxPolicy(string Type);

public sealed record CodexDangerFullAccessSandboxPolicy() : CodexSandboxPolicy("dangerFullAccess");

public sealed record CodexReadOnlySandboxPolicy() : CodexSandboxPolicy("readOnly")
{
    public CodexReadOnlyAccess Access { get; init; } = new CodexFullAccessReadOnlyAccess();

    public bool NetworkAccess { get; init; }
}

public sealed record CodexExternalSandboxPolicy() : CodexSandboxPolicy("externalSandbox")
{
    public CodexNetworkAccess NetworkAccess { get; init; } = CodexNetworkAccess.Restricted;
}

public sealed record CodexWorkspaceWriteSandboxPolicy() : CodexSandboxPolicy("workspaceWrite")
{
    public bool ExcludeSlashTmp { get; init; }

    public bool ExcludeTmpdirEnvVar { get; init; }

    public bool NetworkAccess { get; init; }

    public CodexReadOnlyAccess ReadOnlyAccess { get; init; } = new CodexFullAccessReadOnlyAccess();

    public IReadOnlyList<string> WritableRoots { get; init; } = [];
}

public abstract record CodexSessionSource;

public sealed record CodexSessionSourceValue(CodexSessionSourceKind Value) : CodexSessionSource;

public sealed record CodexSubAgentSessionSource(CodexSubAgentSource SubAgent) : CodexSessionSource;

public abstract record CodexSubAgentSource;

public sealed record CodexThreadSpawnSubAgentSource(CodexThreadSpawn ThreadSpawn) : CodexSubAgentSource;

public sealed record CodexOtherSubAgentSource(string Other) : CodexSubAgentSource;

public sealed record CodexThreadSpawn
{
    public string? AgentNickname { get; init; }

    public string? AgentRole { get; init; }

    public int Depth { get; init; }

    public string ParentThreadId { get; init; } = "";
}

public abstract record CodexThreadStatus(string Type);

public sealed record CodexNotLoadedThreadStatus() : CodexThreadStatus("notLoaded");

public sealed record CodexIdleThreadStatus() : CodexThreadStatus("idle");

public sealed record CodexSystemErrorThreadStatus() : CodexThreadStatus("systemError");

public sealed record CodexActiveThreadStatus() : CodexThreadStatus("active")
{
    public IReadOnlyList<CodexThreadActiveFlag> ActiveFlags { get; init; } = [];
}

public sealed record CodexGitInfo
{
    public string? Branch { get; init; }

    public string? OriginUrl { get; init; }

    public string? Sha { get; init; }
}

public sealed record CodexModelAvailabilityNux
{
    public string Message { get; init; } = "";
}

public sealed record CodexModelUpgradeInfo
{
    public string? MigrationMarkdown { get; init; }

    public string Model { get; init; } = "";

    public string? ModelLink { get; init; }

    public string? UpgradeCopy { get; init; }
}

public sealed record CodexReasoningEffortOption
{
    public string Description { get; init; } = "";

    public CodexReasoningEffort ReasoningEffort { get; init; }
}

public sealed record CodexFileUpdateChange
{
    public string Diff { get; init; } = "";

    public CodexPatchChangeKind Kind { get; init; }

    public string Path { get; init; } = "";
}

public sealed record CodexTodoItem
{
    public bool Completed { get; init; }

    public string Text { get; init; } = "";
}

public sealed record CodexCollabAgentState
{
    public string? Message { get; init; }

    public CodexCollabAgentStatus Status { get; init; }
}

public sealed record CodexThreadError
{
    public string Message { get; init; } = "";
}

public sealed record CodexTurnError
{
    public string Message { get; init; } = "";

    public string? AdditionalDetails { get; init; }

    public JsonNode? CodexErrorInfo { get; init; }
}

public sealed record CodexTokenUsageBreakdown
{
    public int CachedInputTokens { get; init; }

    public int InputTokens { get; init; }

    public int OutputTokens { get; init; }

    public int ReasoningOutputTokens { get; init; }

    public int TotalTokens { get; init; }
}

public sealed record CodexUsage
{
    public CodexTokenUsageBreakdown Last { get; init; } = new();

    public int? ModelContextWindow { get; init; }

    public CodexTokenUsageBreakdown Total { get; init; } = new();
}

public sealed record CodexRunResult
{
    public IReadOnlyList<CodexThreadItem> Items { get; init; } = [];

    public string? FinalResponse { get; init; }

    public CodexUsage? Usage { get; init; }
}

public sealed record CodexTurnRecord
{
    public string Id { get; init; } = "";

    public CodexTurnStatus Status { get; init; }

    public IReadOnlyList<CodexThreadItem> Items { get; init; } = [];

    public CodexTurnError? Error { get; init; }

    public CodexUsage? Usage { get; init; }
}

public record CodexThreadSummary
{
    public string Id { get; init; } = "";

    public string? Name { get; init; }

    public string Preview { get; init; } = "";

    public CodexThreadStatus Status { get; init; } = new CodexNotLoadedThreadStatus();

    public string ModelProvider { get; init; } = "";

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public bool Ephemeral { get; init; }

    public string CliVersion { get; init; } = "";

    public string? Path { get; init; }

    public CodexSessionSource Source { get; init; } = new CodexSessionSourceValue(CodexSessionSourceKind.Unknown);

    public string? AgentRole { get; init; }

    public string? AgentNickname { get; init; }

    public CodexGitInfo? GitInfo { get; init; }
}

public sealed record CodexThreadSnapshot : CodexThreadSummary
{
    public IReadOnlyList<CodexTurnRecord> Turns { get; init; } = [];
}

public sealed record CodexThreadListResult
{
    public IReadOnlyList<CodexThreadSummary> Threads { get; init; } = [];

    public string? NextCursor { get; init; }
}

public sealed record CodexModel
{
    public CodexModelAvailabilityNux? AvailabilityNux { get; init; }

    public CodexReasoningEffort DefaultReasoningEffort { get; init; }

    public string Description { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public bool Hidden { get; init; }

    public string Id { get; init; } = "";

    public IReadOnlyList<CodexInputModality>? InputModalities { get; init; }

    public bool IsDefault { get; init; }

    public string Model { get; init; } = "";

    public IReadOnlyList<CodexReasoningEffortOption> SupportedReasoningEfforts { get; init; } = [];

    public bool? SupportsPersonality { get; init; }

    public string? Upgrade { get; init; }

    public CodexModelUpgradeInfo? UpgradeInfo { get; init; }
}

public sealed record CodexModelListResult
{
    public IReadOnlyList<CodexModel> Models { get; init; } = [];

    public string? NextCursor { get; init; }
}

public sealed record CodexServerInfo
{
    public string? Name { get; init; }

    public string? Version { get; init; }
}

public sealed record CodexRuntimeMetadata
{
    public CodexServerInfo? ServerInfo { get; init; }

    public string? PlatformFamily { get; init; }

    public string? PlatformOs { get; init; }

    public string? UserAgent { get; init; }
}

public sealed record CodexRuntimeCapabilities
{
    public CodexBackendSelection BackendSelection { get; init; }

    public bool ExperimentalApi { get; init; }

    public bool SupportsArchiveThread { get; init; }

    public bool SupportsCompactThread { get; init; }

    public bool SupportsForkThread { get; init; }

    public bool SupportsListModels { get; init; }

    public bool SupportsListThreads { get; init; }

    public bool SupportsReadThread { get; init; }

    public bool SupportsResumeThread { get; init; }

    public bool SupportsSetThreadName { get; init; }

    public bool SupportsStartThread { get; init; }

    public bool SupportsThreadStreaming { get; init; }

    public bool SupportsTurnInterruption { get; init; }

    public bool SupportsTurnSteering { get; init; }

    public bool SupportsUnarchiveThread { get; init; }

    public IReadOnlyList<string> OptOutNotificationMethods { get; init; } = [];
}


