namespace Incursa.OpenAI.Codex;

public enum CodexBackendSelection
{
    Exec,
    AppServer,
}

public enum CodexApprovalMode
{
    Never,
    OnRequest,
    OnFailure,
    Untrusted,
}

public enum CodexApprovalsReviewer
{
    User,
    GuardianSubAgent,
}

public enum CodexCollabAgentStatus
{
    PendingInit,
    Running,
    Interrupted,
    Completed,
    Errored,
    Shutdown,
    NotFound,
}

public enum CodexCollabAgentTool
{
    SpawnAgent,
    SendInput,
    ResumeAgent,
    Wait,
    CloseAgent,
}

public enum CodexCollabAgentToolCallStatus
{
    InProgress,
    Completed,
    Failed,
}

public enum CodexCommandExecutionStatus
{
    InProgress,
    Completed,
    Failed,
    Declined,
}

public enum CodexDynamicToolCallStatus
{
    InProgress,
    Completed,
    Failed,
}

public enum CodexInputModality
{
    Text,
    Image,
}

public enum CodexMcpToolCallStatus
{
    InProgress,
    Completed,
    Failed,
}

public enum CodexMessagePhase
{
    Commentary,
    FinalAnswer,
}

public enum CodexNetworkAccess
{
    Restricted,
    Enabled,
}

public enum CodexPatchApplyStatus
{
    InProgress,
    Completed,
    Failed,
    Declined,
}

public enum CodexPatchChangeKind
{
    Add,
    Delete,
    Update,
}

public enum CodexPersonality
{
    None,
    Friendly,
    Pragmatic,
}

public enum CodexReasoningEffort
{
    None,
    Minimal,
    Low,
    Medium,
    High,
    XHigh,
}

public enum CodexReasoningSummary
{
    None,
    Auto,
    Concise,
    Detailed,
}

public enum CodexSandboxMode
{
    ReadOnly,
    WorkspaceWrite,
    DangerFullAccess,
}

public enum CodexServiceTier
{
    Fast,
    Flex,
}

public enum CodexSessionSourceKind
{
    Cli,
    Vscode,
    Exec,
    AppServer,
    Unknown,
}

public enum CodexSubAgentSourceKind
{
    Review,
    Compact,
    MemoryConsolidation,
}

public enum CodexThreadActiveFlag
{
    WaitingOnApproval,
    WaitingOnUserInput,
}

public enum CodexThreadSortKey
{
    CreatedAt,
    UpdatedAt,
}

public enum CodexThreadSourceKind
{
    Cli,
    Vscode,
    Exec,
    AppServer,
    SubAgent,
    SubAgentReview,
    SubAgentCompact,
    SubAgentThreadSpawn,
    SubAgentOther,
}

public enum CodexTurnPlanStepStatus
{
    Pending,
    InProgress,
    Completed,
}

public enum CodexTurnStatus
{
    Completed,
    Interrupted,
    Failed,
    InProgress,
}

public enum CodexWebSearchContextSize
{
    Low,
    Medium,
    High,
}

public enum CodexWebSearchMode
{
    Disabled,
    Cached,
    Live,
}
