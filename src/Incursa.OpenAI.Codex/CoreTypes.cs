using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-CATALOG-0307, REQ-CODEX-SDK-HELPERS-0313, REQ-CODEX-SDK-HELPERS-0314, REQ-CODEX-SDK-HELPERS-0315,
// REQ-CODEX-SDK-HELPERS-0319, REQ-CODEX-SDK-CATALOG-0302, REQ-CODEX-SDK-CATALOG-0305, REQ-CODEX-SDK-CATALOG-0309.

/// <summary>Base type for approval policy shapes used by Codex.</summary>
public abstract record CodexApprovalPolicy;

/// <summary>Approval policy that selects a single approval mode.</summary>
/// <param name="Mode">The approval mode to apply.</param>
public sealed record CodexApprovalModePolicy(CodexApprovalMode Mode) : CodexApprovalPolicy;

/// <summary>Approval policy that enables granular permission rules.</summary>
/// <param name="Granular">The granular approval rules to apply.</param>
public sealed record CodexGranularApprovalPolicy(CodexGranularApprovalRules Granular) : CodexApprovalPolicy;

/// <summary>Individual toggles for granular approval checks.</summary>
public sealed record CodexGranularApprovalRules
{
    /// <summary>Whether MCP elicitation requests require approval.</summary>
    public bool McpElicitations { get; init; }

    /// <summary>Whether permission requests require approval.</summary>
    public bool RequestPermissions { get; init; }

    /// <summary>Whether rule-based prompts require approval.</summary>
    public bool Rules { get; init; }

    /// <summary>Whether sandbox approval requests require approval.</summary>
    public bool SandboxApproval { get; init; }

    /// <summary>Whether skill usage requires approval.</summary>
    public bool SkillApproval { get; init; }
}

/// <summary>Base type for read-only access shapes inside sandbox policy definitions.</summary>
/// <param name="Type">The read-only access discriminator.</param>
public abstract record CodexReadOnlyAccess(string Type);

/// <summary>Read-only access that restricts access to a configured set of roots.</summary>
public sealed record CodexRestrictedReadOnlyAccess() : CodexReadOnlyAccess("restricted")
{
    /// <summary>Whether platform defaults are included in the readable root set.</summary>
    public bool IncludePlatformDefaults { get; init; } = true;

    /// <summary>Absolute or repository-relative paths that remain readable.</summary>
    public IReadOnlyList<string> ReadableRoots { get; init; } = [];
}

/// <summary>Read-only access that uses the full platform-default read set.</summary>
public sealed record CodexFullAccessReadOnlyAccess() : CodexReadOnlyAccess("fullAccess");

/// <summary>Base type for sandbox policy shapes used by Codex.</summary>
/// <param name="Type">The sandbox policy discriminator.</param>
public abstract record CodexSandboxPolicy(string Type);

/// <summary>Sandbox policy that grants unrestricted filesystem and process access.</summary>
public sealed record CodexDangerFullAccessSandboxPolicy() : CodexSandboxPolicy("dangerFullAccess");

/// <summary>Sandbox policy that keeps execution read-only.</summary>
public sealed record CodexReadOnlySandboxPolicy() : CodexSandboxPolicy("readOnly")
{
    /// <summary>The read-only access scope to expose to the model.</summary>
    public CodexReadOnlyAccess Access { get; init; } = new CodexFullAccessReadOnlyAccess();

    /// <summary>Whether network access is allowed while running read-only.</summary>
    public bool NetworkAccess { get; init; }
}

/// <summary>Sandbox policy that runs against an external sandbox service.</summary>
public sealed record CodexExternalSandboxPolicy() : CodexSandboxPolicy("externalSandbox")
{
    /// <summary>The network access mode to use for the external sandbox.</summary>
    public CodexNetworkAccess NetworkAccess { get; init; } = CodexNetworkAccess.Restricted;
}

/// <summary>Sandbox policy that allows writes in the workspace and selected roots.</summary>
public sealed record CodexWorkspaceWriteSandboxPolicy() : CodexSandboxPolicy("workspaceWrite")
{
    /// <summary>Whether slash-tmp is excluded from writable paths.</summary>
    public bool ExcludeSlashTmp { get; init; }

    /// <summary>Whether the tmpdir environment path is excluded from writable paths.</summary>
    public bool ExcludeTmpdirEnvVar { get; init; }

    /// <summary>Whether network access is allowed while writing in the workspace.</summary>
    public bool NetworkAccess { get; init; }

    /// <summary>The read-only roots that remain available alongside writable roots.</summary>
    public CodexReadOnlyAccess ReadOnlyAccess { get; init; } = new CodexFullAccessReadOnlyAccess();

    /// <summary>Roots that Codex may write to during the session.</summary>
    public IReadOnlyList<string> WritableRoots { get; init; } = [];
}

/// <summary>Base type for the source of a Codex session.</summary>
public abstract record CodexSessionSource;

/// <summary>Session source that stores the raw source kind value.</summary>
/// <param name="Value">The session source kind.</param>
public sealed record CodexSessionSourceValue(CodexSessionSourceKind Value) : CodexSessionSource;

/// <summary>Session source that stores a custom source label.</summary>
/// <param name="Custom">The custom session source label.</param>
public sealed record CodexCustomSessionSource(string Custom) : CodexSessionSource;

/// <summary>Session source that points to a spawned sub-agent.</summary>
/// <param name="SubAgent">The sub-agent source details.</param>
public sealed record CodexSubAgentSessionSource(CodexSubAgentSource SubAgent) : CodexSessionSource;

/// <summary>Base type for sub-agent source shapes.</summary>
public abstract record CodexSubAgentSource;

/// <summary>Sub-agent source that stores a raw source kind value.</summary>
/// <param name="Value">The sub-agent source kind.</param>
public sealed record CodexSubAgentSourceValue(CodexSubAgentSourceKind Value) : CodexSubAgentSource;

/// <summary>Sub-agent source that came from thread spawning.</summary>
/// <param name="ThreadSpawn">The thread-spawn metadata.</param>
public sealed record CodexThreadSpawnSubAgentSource(CodexThreadSpawn ThreadSpawn) : CodexSubAgentSource;

/// <summary>Sub-agent source that carries an arbitrary source label.</summary>
/// <param name="Other">The source label.</param>
public sealed record CodexOtherSubAgentSource(string Other) : CodexSubAgentSource;

/// <summary>Metadata describing how a thread spawned a sub-agent.</summary>
public sealed record CodexThreadSpawn
{
    /// <summary>Nickname assigned to the spawned agent, if any.</summary>
    public string? AgentNickname { get; init; }

    /// <summary>Role assigned to the spawned agent, if any.</summary>
    public string? AgentRole { get; init; }

    /// <summary>Spawn depth relative to the original thread.</summary>
    public int Depth { get; init; }

    /// <summary>Identifier of the parent thread that created the spawn.</summary>
    public string ParentThreadId { get; init; } = "";
}

/// <summary>Base type for the current state of a Codex thread.</summary>
/// <param name="Type">The thread status discriminator.</param>
public abstract record CodexThreadStatus(string Type);

/// <summary>Thread status indicating that the thread has not been loaded.</summary>
public sealed record CodexNotLoadedThreadStatus() : CodexThreadStatus("notLoaded");

/// <summary>Thread status indicating that the thread is idle.</summary>
public sealed record CodexIdleThreadStatus() : CodexThreadStatus("idle");

/// <summary>Thread status indicating that the thread hit a system error.</summary>
public sealed record CodexSystemErrorThreadStatus() : CodexThreadStatus("systemError");

/// <summary>Thread status indicating that the thread is actively running.</summary>
public sealed record CodexActiveThreadStatus() : CodexThreadStatus("active")
{
    /// <summary>Flags that describe the current active thread state.</summary>
    public IReadOnlyList<CodexThreadActiveFlag> ActiveFlags { get; init; } = [];
}

/// <summary>Basic Git metadata for the current workspace.</summary>
public sealed record CodexGitInfo
{
    /// <summary>The current branch name, if known.</summary>
    public string? Branch { get; init; }

    /// <summary>The configured remote origin URL, if known.</summary>
    public string? OriginUrl { get; init; }

    /// <summary>The current commit SHA, if known.</summary>
    public string? Sha { get; init; }
}

/// <summary>Patchable Git metadata for a thread update request.</summary>
public readonly record struct CodexThreadMetadataGitInfoUpdate
{
    /// <summary>Gets or sets the branch name patch value.</summary>
    public string? Branch { get; init; }

    /// <summary>Gets or sets a value indicating whether the branch field is included in the patch.</summary>
    public bool BranchSpecified { get; init; }

    /// <summary>Gets or sets the remote origin URL patch value.</summary>
    public string? OriginUrl { get; init; }

    /// <summary>Gets or sets a value indicating whether the origin URL field is included in the patch.</summary>
    public bool OriginUrlSpecified { get; init; }

    /// <summary>Gets or sets the commit SHA patch value.</summary>
    public string? Sha { get; init; }

    /// <summary>Gets or sets a value indicating whether the SHA field is included in the patch.</summary>
    public bool ShaSpecified { get; init; }
}

/// <summary>Availability note shown when a model is not immediately usable.</summary>
public sealed record CodexModelAvailabilityNux
{
    /// <summary>The message shown to the user.</summary>
    public string Message { get; init; } = "";
}

/// <summary>Upgrade guidance for a model that needs migration.</summary>
public sealed record CodexModelUpgradeInfo
{
    /// <summary>Markdown content that explains the migration path.</summary>
    public string? MigrationMarkdown { get; init; }

    /// <summary>The target model identifier.</summary>
    public string Model { get; init; } = "";

    /// <summary>Link to model upgrade guidance, if available.</summary>
    public string? ModelLink { get; init; }

    /// <summary>Short copy shown alongside the upgrade notice.</summary>
    public string? UpgradeCopy { get; init; }
}

/// <summary>One available reasoning-effort option for a model.</summary>
public sealed record CodexReasoningEffortOption
{
    /// <summary>User-facing description of the option.</summary>
    public string Description { get; init; } = "";

    /// <summary>The reasoning-effort value represented by the option.</summary>
    public CodexReasoningEffort ReasoningEffort { get; init; }
}

/// <summary>Service-tier metadata reported for a model.</summary>
public sealed record CodexModelServiceTier
{
    /// <summary>The service-tier request identifier, such as <c>priority</c> or <c>flex</c>.</summary>
    public string Id { get; init; } = "";

    /// <summary>The user-facing service-tier name.</summary>
    public string Name { get; init; } = "";

    /// <summary>The user-facing service-tier description.</summary>
    public string Description { get; init; } = "";
}

/// <summary>Single-file update metadata returned by Codex.</summary>
public sealed record CodexFileUpdateChange
{
    /// <summary>The unified diff for the file change.</summary>
    public string Diff { get; init; } = "";

    /// <summary>The kind of patch change that occurred.</summary>
    public CodexPatchChangeKind Kind { get; init; }

    /// <summary>The path of the file that changed.</summary>
    public string Path { get; init; } = "";
}

/// <summary>A 1-based line and column position inside a text buffer.</summary>
public sealed record CodexTextPosition
{
    /// <summary>The column number, in Unicode scalar values.</summary>
    public int Column { get; init; }

    /// <summary>The line number.</summary>
    public int Line { get; init; }
}

/// <summary>A text range that points to a start and end position.</summary>
public sealed record CodexTextRange
{
    /// <summary>The range end position.</summary>
    public CodexTextPosition End { get; init; } = new();

    /// <summary>The range start position.</summary>
    public CodexTextPosition Start { get; init; } = new();
}

/// <summary>A single entry in a memory citation.</summary>
public sealed record CodexMemoryCitationEntry
{
    /// <summary>The last line covered by the citation entry.</summary>
    public int LineEnd { get; init; }

    /// <summary>The first line covered by the citation entry.</summary>
    public int LineStart { get; init; }

    /// <summary>The note attached to the cited range.</summary>
    public string Note { get; init; } = "";

    /// <summary>The cited path.</summary>
    public string Path { get; init; } = "";
}

/// <summary>A citation bundle attached to an agent message.</summary>
public sealed record CodexMemoryCitation
{
    /// <summary>The cited entries.</summary>
    public IReadOnlyList<CodexMemoryCitationEntry> Entries { get; init; } = [];

    /// <summary>The thread identifiers associated with the citation.</summary>
    public IReadOnlyList<string> ThreadIds { get; init; } = [];
}

/// <summary>A single todo item tracked by Codex.</summary>
public sealed record CodexTodoItem
{
    /// <summary>Whether the item has been completed.</summary>
    public bool Completed { get; init; }

    /// <summary>The todo text shown to the user.</summary>
    public string Text { get; init; } = "";
}

/// <summary>Runtime state for a collaborator agent.</summary>
public sealed record CodexCollabAgentState
{
    /// <summary>Optional message from the agent.</summary>
    public string? Message { get; init; }

    /// <summary>The current collaborator status.</summary>
    public CodexCollabAgentStatus Status { get; init; }
}

/// <summary>Error information attached to a thread record.</summary>
public sealed record CodexThreadError
{
    /// <summary>The human-readable error message.</summary>
    public string Message { get; init; } = "";
}

/// <summary>Error information attached to an individual turn.</summary>
public sealed record CodexTurnError
{
    /// <summary>The human-readable error message.</summary>
    public string Message { get; init; } = "";

    /// <summary>Optional extra detail for diagnostics.</summary>
    public string? AdditionalDetails { get; init; }

    /// <summary>Structured error details, when the service returns them.</summary>
    public JsonNode? CodexErrorInfo { get; init; }
}

/// <summary>Token counts broken down by input and output categories.</summary>
public sealed record CodexTokenUsageBreakdown
{
    /// <summary>Cached input tokens reused from earlier requests.</summary>
    public int CachedInputTokens { get; init; }

    /// <summary>Prompt tokens sent to the model.</summary>
    public int InputTokens { get; init; }

    /// <summary>Completion tokens returned by the model.</summary>
    public int OutputTokens { get; init; }

    /// <summary>Tokens spent on reasoning output, if reported separately.</summary>
    public int ReasoningOutputTokens { get; init; }

    /// <summary>Total tokens counted for the request.</summary>
    public int TotalTokens { get; init; }
}

/// <summary>Total and last-request token usage for a Codex session.</summary>
public sealed record CodexUsage
{
    /// <summary>Token usage for the most recent request.</summary>
    public CodexTokenUsageBreakdown Last { get; init; } = new();

    /// <summary>Model context window size in tokens, if known.</summary>
    public int? ModelContextWindow { get; init; }

    /// <summary>Cumulative token usage across the session.</summary>
    public CodexTokenUsageBreakdown Total { get; init; } = new();
}

/// <summary>
/// Represents account-level Codex rate-limit information reported by the app-server backend.
/// </summary>
public sealed record CodexAccountRateLimitsResult
{
    /// <summary>
    /// Gets the reported rate-limit buckets in stable display order.
    /// </summary>
    public IReadOnlyList<CodexRateLimitSnapshot> RateLimits { get; init; } = [];

    /// <summary>
    /// Gets the reported rate-limit buckets keyed by Codex limit identifier.
    /// </summary>
    public IReadOnlyDictionary<string, CodexRateLimitSnapshot> RateLimitsByLimitId { get; init; } =
        new Dictionary<string, CodexRateLimitSnapshot>(StringComparer.Ordinal);
}

/// <summary>
/// Represents the current state of one Codex rate-limit bucket.
/// </summary>
public sealed record CodexRateLimitSnapshot
{
    /// <summary>
    /// Gets the optional account credit state associated with this limit.
    /// </summary>
    public CodexCreditsSnapshot? Credits { get; init; }

    /// <summary>
    /// Gets the stable Codex limit identifier, such as <c>codex</c>.
    /// </summary>
    public string? LimitId { get; init; }

    /// <summary>
    /// Gets the human-readable limit name reported by Codex.
    /// </summary>
    public string? LimitName { get; init; }

    /// <summary>
    /// Gets the Codex plan type reported by the runtime.
    /// </summary>
    public CodexPlanType PlanType { get; init; }

    /// <summary>
    /// Gets the primary window, typically the shorter rolling window.
    /// </summary>
    public CodexRateLimitWindow? Primary { get; init; }

    /// <summary>
    /// Gets the secondary window, typically the longer rolling window.
    /// </summary>
    public CodexRateLimitWindow? Secondary { get; init; }

    /// <summary>
    /// Gets the Codex rate-limit reached reason, when one is currently active.
    /// </summary>
    public string? RateLimitReachedType { get; init; }
}

/// <summary>
/// Represents usage and reset timing for a Codex rate-limit window.
/// </summary>
public sealed record CodexRateLimitWindow
{
    /// <summary>
    /// Gets the reported percentage of the window currently used.
    /// </summary>
    public int UsedPercent { get; init; }

    /// <summary>
    /// Gets the UTC reset time for the window, when Codex reports one.
    /// </summary>
    public DateTimeOffset? ResetsAt { get; init; }

    /// <summary>
    /// Gets the duration of the window in minutes, when Codex reports one.
    /// </summary>
    public long? WindowDurationMinutes { get; init; }
}

/// <summary>
/// Represents optional credit information associated with a Codex account limit.
/// </summary>
public sealed record CodexCreditsSnapshot
{
    /// <summary>
    /// Gets the reported credit balance, when Codex reports one.
    /// </summary>
    public double? Balance { get; init; }

    /// <summary>
    /// Gets whether the account currently has credits available, when reported.
    /// </summary>
    public bool? HasCredits { get; init; }

    /// <summary>
    /// Gets whether the account is reported as having unlimited credits.
    /// </summary>
    public bool? Unlimited { get; init; }
}

/// <summary>Represents the current goal configured for a Codex thread.</summary>
public sealed record CodexThreadGoal
{
    /// <summary>The thread that owns the goal.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>The objective Codex should keep pursuing.</summary>
    public string Objective { get; init; } = "";

    /// <summary>The current goal status.</summary>
    public CodexThreadGoalStatus Status { get; init; }

    /// <summary>The optional token budget for the goal.</summary>
    public long? TokenBudget { get; init; }

    /// <summary>The cumulative tokens consumed while pursuing the goal.</summary>
    public long TokensUsed { get; init; }

    /// <summary>The cumulative elapsed time in seconds while pursuing the goal.</summary>
    public long TimeUsedSeconds { get; init; }

    /// <summary>When the goal was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the goal was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>Result of executing a Codex run request.</summary>
public sealed record CodexRunResult
{
    /// <summary>Thread items returned by the run.</summary>
    public IReadOnlyList<CodexThreadItem> Items { get; init; } = [];

    /// <summary>The final assistant response, if one was produced.</summary>
    public string? FinalResponse { get; init; }

    /// <summary>Token usage reported for the run.</summary>
    public CodexUsage? Usage { get; init; }
}

/// <summary>Recorded state for a single thread turn.</summary>
public sealed record CodexTurnRecord
{
    /// <summary>The turn identifier.</summary>
    public string Id { get; init; } = "";

    /// <summary>The terminal status for the turn.</summary>
    public CodexTurnStatus Status { get; init; }

    /// <summary>Thread items emitted during the turn.</summary>
    public IReadOnlyList<CodexThreadItem> Items { get; init; } = [];

    /// <summary>Error details when the turn failed.</summary>
    public CodexTurnError? Error { get; init; }

    /// <summary>Token usage reported for the turn.</summary>
    public CodexUsage? Usage { get; init; }
}

/// <summary>Summary view of a Codex thread.</summary>
public record CodexThreadSummary
{
    /// <summary>The thread identifier.</summary>
    public string Id { get; init; } = "";

    /// <summary>The thread name, if one has been assigned.</summary>
    public string? Name { get; init; }

    /// <summary>Short preview text for the thread.</summary>
    public string Preview { get; init; } = "";

    /// <summary>The current thread status.</summary>
    public CodexThreadStatus Status { get; init; } = new CodexNotLoadedThreadStatus();

    /// <summary>The model provider that owns the thread.</summary>
    public string ModelProvider { get; init; } = "";

    /// <summary>When the thread was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the thread was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Whether the thread is ephemeral.</summary>
    public bool Ephemeral { get; init; }

    /// <summary>The CLI version that produced the thread metadata.</summary>
    public string CliVersion { get; init; } = "";

    /// <summary>Working directory captured for the thread.</summary>
    public string Cwd { get; init; } = "";

    /// <summary>Workspace path associated with the thread, if any.</summary>
    public string? Path { get; init; }

    /// <summary>The session identifier associated with the thread tree.</summary>
    public string SessionId { get; init; } = "";

    /// <summary>The identifier of the thread this one was forked from, if any.</summary>
    public string? ForkedFromId { get; init; }

    /// <summary>The source that created the thread.</summary>
    public CodexSessionSource Source { get; init; } = new CodexSessionSourceValue(CodexSessionSourceKind.Unknown);

    /// <summary>The thread origin classification, if any.</summary>
    public CodexThreadSource? ThreadSource { get; init; }

    /// <summary>The source that started the current thread session, if known.</summary>
    public CodexThreadStartSource? SessionStartSource { get; init; }

    /// <summary>Role associated with the spawning agent, if any.</summary>
    public string? AgentRole { get; init; }

    /// <summary>Nickname associated with the spawning agent, if any.</summary>
    public string? AgentNickname { get; init; }

    /// <summary>Git metadata captured for the thread workspace, if available.</summary>
    public CodexGitInfo? GitInfo { get; init; }
}

/// <summary>Thread summary that also includes the recorded turns.</summary>
public sealed record CodexThreadSnapshot : CodexThreadSummary
{
    /// <summary>The turns that make up the thread.</summary>
    public IReadOnlyList<CodexTurnRecord> Turns { get; init; } = [];
}

/// <summary>Result of listing threads.</summary>
public sealed record CodexThreadListResult
{
    /// <summary>The threads returned by the query.</summary>
    public IReadOnlyList<CodexThreadSummary> Threads { get; init; } = [];

    /// <summary>Cursor for the previous page, if more results are available.</summary>
    public string? BackwardsCursor { get; init; }

    /// <summary>Cursor for the next page, if more results are available.</summary>
    public string? NextCursor { get; init; }
}

/// <summary>Model metadata returned by Codex.</summary>
public sealed record CodexModel
{
    /// <summary>Optional availability note for the model.</summary>
    public CodexModelAvailabilityNux? AvailabilityNux { get; init; }

    /// <summary>Deprecated speed-tier identifiers reported by older runtimes.</summary>
    public IReadOnlyList<string> AdditionalSpeedTiers { get; init; } = [];

    /// <summary>The default reasoning effort for the model.</summary>
    public CodexReasoningEffort DefaultReasoningEffort { get; init; }

    /// <summary>Long-form model description.</summary>
    public string Description { get; init; } = "";

    /// <summary>Display name shown in user interfaces.</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>Whether the model is hidden from normal lists.</summary>
    public bool Hidden { get; init; }

    /// <summary>Stable model identifier.</summary>
    public string Id { get; init; } = "";

    /// <summary>Supported input modalities, if the service reports them.</summary>
    public IReadOnlyList<CodexInputModality>? InputModalities { get; init; }

    /// <summary>Whether the model is the default choice.</summary>
    public bool IsDefault { get; init; }

    /// <summary>Canonical model name used by the backend.</summary>
    public string Model { get; init; } = "";

    /// <summary>Reasoning-effort options supported by the model.</summary>
    public IReadOnlyList<CodexReasoningEffortOption> SupportedReasoningEfforts { get; init; } = [];

    /// <summary>Service tiers supported by the model.</summary>
    public IReadOnlyList<CodexModelServiceTier> ServiceTiers { get; init; } = [];

    /// <summary>Whether the model supports personality controls.</summary>
    public bool? SupportsPersonality { get; init; }

    /// <summary>Short upgrade note shown to the user, if any.</summary>
    public string? Upgrade { get; init; }

    /// <summary>Structured upgrade guidance for the model, if available.</summary>
    public CodexModelUpgradeInfo? UpgradeInfo { get; init; }
}

/// <summary>Result of listing available models.</summary>
public sealed record CodexModelListResult
{
    /// <summary>The models returned by the query.</summary>
    public IReadOnlyList<CodexModel> Models { get; init; } = [];

    /// <summary>Cursor for the next page, if more results are available.</summary>
    public string? NextCursor { get; init; }
}

/// <summary>Version information for the Codex server.</summary>
public sealed record CodexServerInfo
{
    /// <summary>The server name, if reported.</summary>
    public string? Name { get; init; }

    /// <summary>The server version, if reported.</summary>
    public string? Version { get; init; }
}

/// <summary>Host runtime metadata captured by the client.</summary>
public sealed record CodexRuntimeMetadata
{
    /// <summary>Server information, if available.</summary>
    public CodexServerInfo? ServerInfo { get; init; }

    /// <summary>Platform family reported by the host.</summary>
    public string? PlatformFamily { get; init; }

    /// <summary>Platform operating system reported by the host.</summary>
    public string? PlatformOs { get; init; }

    /// <summary>User agent string reported by the host.</summary>
    public string? UserAgent { get; init; }
}

/// <summary>Capability flags exposed by the Codex runtime.</summary>
public sealed record CodexRuntimeCapabilities
{
    /// <summary>The backend selection strategy in use.</summary>
    public CodexBackendSelection BackendSelection { get; init; }

    /// <summary>Whether the experimental API surface is enabled.</summary>
    public bool ExperimentalApi { get; init; }

    /// <summary>Whether reading account rate limits is supported.</summary>
    public bool SupportsAccountRateLimits { get; init; }

    /// <summary>Whether archiving threads is supported.</summary>
    public bool SupportsArchiveThread { get; init; }

    /// <summary>Whether compacting threads is supported.</summary>
    public bool SupportsCompactThread { get; init; }

    /// <summary>Whether forking threads is supported.</summary>
    public bool SupportsForkThread { get; init; }

    /// <summary>Whether listing models is supported.</summary>
    public bool SupportsListModels { get; init; }

    /// <summary>Whether listing threads is supported.</summary>
    public bool SupportsListThreads { get; init; }

    /// <summary>Whether reading threads is supported.</summary>
    public bool SupportsReadThread { get; init; }

    /// <summary>Whether resuming threads is supported.</summary>
    public bool SupportsResumeThread { get; init; }

    /// <summary>Whether setting a thread name is supported.</summary>
    public bool SupportsSetThreadName { get; init; }

    /// <summary>Whether reading, setting, and clearing thread goals is supported.</summary>
    public bool SupportsThreadGoals { get; init; }

    /// <summary>Whether starting threads is supported.</summary>
    public bool SupportsStartThread { get; init; }

    /// <summary>Whether thread streaming is supported.</summary>
    public bool SupportsThreadStreaming { get; init; }

    /// <summary>Whether turn interruption is supported.</summary>
    public bool SupportsTurnInterruption { get; init; }

    /// <summary>Whether turn steering is supported.</summary>
    public bool SupportsTurnSteering { get; init; }

    /// <summary>Whether unarchiving threads is supported.</summary>
    public bool SupportsUnarchiveThread { get; init; }

    /// <summary>Notification methods that are not supported for opt-out.</summary>
    public IReadOnlyList<string> OptOutNotificationMethods { get; init; } = [];
}
