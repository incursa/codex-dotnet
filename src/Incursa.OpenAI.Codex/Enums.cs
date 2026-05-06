namespace Incursa.OpenAI.Codex;

/// <summary>Identifies the backend used to run Codex requests.</summary>
public enum CodexBackendSelection
{
    /// <summary>Runs Codex through the local <c>codex exec</c> process.</summary>
    Exec,

    /// <summary>Runs Codex through the app-server transport.</summary>
    AppServer,
}

/// <summary>Controls when Codex asks for approval before acting.</summary>
public enum CodexApprovalMode
{
    /// <summary>Never asks for approval.</summary>
    Never,

    /// <summary>Asks for approval before executing the action.</summary>
    OnRequest,

    /// <summary>Asks for approval only after an action fails.</summary>
    OnFailure,

    /// <summary>Treats untrusted work as requiring approval.</summary>
    Untrusted,
}

/// <summary>Selects who reviews approval requests.</summary>
public enum CodexApprovalsReviewer
{
    /// <summary>Routes approval review to the user.</summary>
    User,

    /// <summary>Routes approval review to the guardian sub-agent.</summary>
    GuardianSubAgent,
}

/// <summary>Describes the lifecycle state of a collaborative agent.</summary>
public enum CodexCollabAgentStatus
{
    /// <summary>The agent has been created but not initialized yet.</summary>
    PendingInit,

    /// <summary>The agent is running normally.</summary>
    Running,

    /// <summary>The agent was interrupted before completion.</summary>
    Interrupted,

    /// <summary>The agent finished successfully.</summary>
    Completed,

    /// <summary>The agent finished because of an error.</summary>
    Errored,

    /// <summary>The agent was shut down.</summary>
    Shutdown,

    /// <summary>The requested agent could not be found.</summary>
    NotFound,
}

/// <summary>Identifies a tool invocation issued to a collaborative agent.</summary>
public enum CodexCollabAgentTool
{
    /// <summary>Spawns a new collaborative agent.</summary>
    SpawnAgent,

    /// <summary>Sends input to an existing collaborative agent.</summary>
    SendInput,

    /// <summary>Resumes a previously suspended collaborative agent.</summary>
    ResumeAgent,

    /// <summary>Waits for a collaborative agent to reach a terminal state.</summary>
    Wait,

    /// <summary>Closes a collaborative agent.</summary>
    CloseAgent,
}

/// <summary>Tracks progress for a collaborative-agent tool call.</summary>
public enum CodexCollabAgentToolCallStatus
{
    /// <summary>The tool call is still running.</summary>
    InProgress,

    /// <summary>The tool call completed successfully.</summary>
    Completed,

    /// <summary>The tool call failed.</summary>
    Failed,
}

/// <summary>Tracks a command-execution request.</summary>
public enum CodexCommandExecutionStatus
{
    /// <summary>The command is still running.</summary>
    InProgress,

    /// <summary>The command completed successfully.</summary>
    Completed,

    /// <summary>The command failed.</summary>
    Failed,

    /// <summary>The command was declined.</summary>
    Declined,
}

/// <summary>Tracks a dynamic tool call.</summary>
public enum CodexDynamicToolCallStatus
{
    /// <summary>The tool call is still running.</summary>
    InProgress,

    /// <summary>The tool call completed successfully.</summary>
    Completed,

    /// <summary>The tool call failed.</summary>
    Failed,
}

/// <summary>Indicates the type of user input being provided.</summary>
public enum CodexInputModality
{
    /// <summary>The input is text.</summary>
    Text,

    /// <summary>The input is an image.</summary>
    Image,
}

/// <summary>Tracks an MCP tool call.</summary>
public enum CodexMcpToolCallStatus
{
    /// <summary>The tool call is still running.</summary>
    InProgress,

    /// <summary>The tool call completed successfully.</summary>
    Completed,

    /// <summary>The tool call failed.</summary>
    Failed,
}

/// <summary>Identifies whether a message is commentary or the final answer.</summary>
public enum CodexMessagePhase
{
    /// <summary>The message is internal commentary.</summary>
    Commentary,

    /// <summary>The message is the final answer.</summary>
    FinalAnswer,
}

/// <summary>Controls whether network access is allowed.</summary>
public enum CodexNetworkAccess
{
    /// <summary>Network access is restricted.</summary>
    Restricted,

    /// <summary>Network access is enabled.</summary>
    Enabled,
}

/// <summary>Tracks an apply-patch request.</summary>
public enum CodexPatchApplyStatus
{
    /// <summary>The patch is still being applied.</summary>
    InProgress,

    /// <summary>The patch applied successfully.</summary>
    Completed,

    /// <summary>The patch failed to apply.</summary>
    Failed,

    /// <summary>The patch application was declined.</summary>
    Declined,
}

/// <summary>Describes the kind of file change in a patch.</summary>
public enum CodexPatchChangeKind
{
    /// <summary>Adds a file or file content.</summary>
    Add,

    /// <summary>Deletes a file or file content.</summary>
    Delete,

    /// <summary>Updates existing file content.</summary>
    Update,
}

/// <summary>Selects the response tone used by Codex.</summary>
public enum CodexPersonality
{
    /// <summary>Uses the default tone.</summary>
    None,

    /// <summary>Uses a friendly tone.</summary>
    Friendly,

    /// <summary>Uses a pragmatic tone.</summary>
    Pragmatic,
}

/// <summary>Selects the requested reasoning budget.</summary>
public enum CodexReasoningEffort
{
    /// <summary>Uses the default effort.</summary>
    None,

    /// <summary>Uses minimal effort.</summary>
    Minimal,

    /// <summary>Uses low effort.</summary>
    Low,

    /// <summary>Uses medium effort.</summary>
    Medium,

    /// <summary>Uses high effort.</summary>
    High,

    /// <summary>Uses extra-high effort.</summary>
    XHigh,
}

/// <summary>Selects how much reasoning summary to return.</summary>
public enum CodexReasoningSummary
{
    /// <summary>Uses the default summary behavior.</summary>
    None,

    /// <summary>Lets Codex choose the summary length.</summary>
    Auto,

    /// <summary>Returns a concise summary.</summary>
    Concise,

    /// <summary>Returns a detailed summary.</summary>
    Detailed,
}

/// <summary>Selects the filesystem sandbox level.</summary>
public enum CodexSandboxMode
{
    /// <summary>Prevents writes to the workspace.</summary>
    ReadOnly,

    /// <summary>Allows writes within the workspace.</summary>
    WorkspaceWrite,

    /// <summary>Allows unrestricted filesystem access.</summary>
    DangerFullAccess,
}

/// <summary>Selects the requested service tier.</summary>
public enum CodexServiceTier
{
    /// <summary>Uses the fast tier.</summary>
    Fast,

    /// <summary>Uses the flex tier.</summary>
    Flex,
}

/// <summary>Identifies the source that created a session.</summary>
public enum CodexSessionSourceKind
{
    /// <summary>The session came from the CLI.</summary>
    Cli,

    /// <summary>The session came from VS Code.</summary>
    Vscode,

    /// <summary>The session came from the exec backend.</summary>
    Exec,

    /// <summary>The session came from the app-server backend.</summary>
    AppServer,

    /// <summary>The session source could not be identified.</summary>
    Unknown,
}

/// <summary>Identifies why a sub-agent was created.</summary>
public enum CodexSubAgentSourceKind
{
    /// <summary>The sub-agent was created for review.</summary>
    Review,

    /// <summary>The sub-agent was created for context compaction.</summary>
    Compact,

    /// <summary>The sub-agent was created for memory consolidation.</summary>
    MemoryConsolidation,
}

/// <summary>Flags conditions that keep a thread active.</summary>
public enum CodexThreadActiveFlag
{
    /// <summary>The thread is waiting on approval.</summary>
    WaitingOnApproval,

    /// <summary>The thread is waiting on user input.</summary>
    WaitingOnUserInput,
}

/// <summary>Selects the sort key for thread lists.</summary>
public enum CodexThreadSortKey
{
    /// <summary>Sorts by creation time.</summary>
    CreatedAt,

    /// <summary>Sorts by last update time.</summary>
    UpdatedAt,
}

/// <summary>Identifies the origin of a thread or thread item.</summary>
public enum CodexThreadSourceKind
{
    /// <summary>The thread originated from the CLI.</summary>
    Cli,

    /// <summary>The thread originated from VS Code.</summary>
    Vscode,

    /// <summary>The thread originated from the exec backend.</summary>
    Exec,

    /// <summary>The thread originated from the app-server backend.</summary>
    AppServer,

    /// <summary>The thread originated from a sub-agent.</summary>
    SubAgent,

    /// <summary>The thread originated from a review sub-agent.</summary>
    SubAgentReview,

    /// <summary>The thread originated from a compaction sub-agent.</summary>
    SubAgentCompact,

    /// <summary>The thread was spawned by a sub-agent.</summary>
    SubAgentThreadSpawn,

    /// <summary>The thread originated from another sub-agent source.</summary>
    SubAgentOther,
}

/// <summary>Tracks the state of a turn-plan step.</summary>
public enum CodexTurnPlanStepStatus
{
    /// <summary>The step is pending.</summary>
    Pending,

    /// <summary>The step is in progress.</summary>
    InProgress,

    /// <summary>The step completed successfully.</summary>
    Completed,
}

/// <summary>Tracks the lifecycle state of a turn.</summary>
public enum CodexTurnStatus
{
    /// <summary>The turn completed successfully.</summary>
    Completed,

    /// <summary>The turn was interrupted.</summary>
    Interrupted,

    /// <summary>The turn failed.</summary>
    Failed,

    /// <summary>The turn is still in progress.</summary>
    InProgress,
}

/// <summary>Selects how much web-search context to include.</summary>
public enum CodexWebSearchContextSize
{
    /// <summary>Uses a low context size.</summary>
    Low,

    /// <summary>Uses a medium context size.</summary>
    Medium,

    /// <summary>Uses a high context size.</summary>
    High,
}

/// <summary>Controls whether web search is used.</summary>
public enum CodexWebSearchMode
{
    /// <summary>Disables web search.</summary>
    Disabled,

    /// <summary>Uses cached web search results when available.</summary>
    Cached,

    /// <summary>Uses live web search results.</summary>
    Live,
}
