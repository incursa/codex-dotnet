using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-CATALOG-0306, REQ-CODEX-SDK-HELPERS-0316, REQ-CODEX-SDK-HELPERS-0317, REQ-CODEX-SDK-HELPERS-0318.

/// <summary>
/// Base type for conversation input items.
/// </summary>
/// <param name="Type">JSON discriminator for the input item kind.</param>
public abstract record CodexInputItem(string Type);

/// <summary>
/// Text content supplied as conversation input.
/// </summary>
public sealed record CodexTextInput() : CodexInputItem("text")
{
    /// <summary>
    /// Gets or sets the text payload.
    /// </summary>
    public string Text { get; init; } = "";
}

/// <summary>
/// Remote image input referenced by URL.
/// </summary>
public sealed record CodexImageInput() : CodexInputItem("image")
{
    /// <summary>
    /// Gets or sets the image URL.
    /// </summary>
    public string Url { get; init; } = "";
}

/// <summary>
/// Local image input referenced by file path.
/// </summary>
public sealed record CodexLocalImageInput() : CodexInputItem("localImage")
{
    /// <summary>
    /// Gets or sets the local file path.
    /// </summary>
    public string Path { get; init; } = "";
}

/// <summary>
/// Skill reference included as conversation input.
/// </summary>
public sealed record CodexSkillInput() : CodexInputItem("skill")
{
    /// <summary>
    /// Gets or sets the skill name.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Gets or sets the skill path.
    /// </summary>
    public string Path { get; init; } = "";
}

/// <summary>
/// Mention reference included as conversation input.
/// </summary>
public sealed record CodexMentionInput() : CodexInputItem("mention")
{
    /// <summary>
    /// Gets or sets the mention name.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Gets or sets the mention path.
    /// </summary>
    public string Path { get; init; } = "";
}

/// <summary>
/// Fallback conversation input item for unrecognized payloads.
/// </summary>
/// <param name="UnknownType">Original input item type string.</param>
public sealed record CodexUnknownInputItem(string UnknownType) : CodexInputItem(UnknownType)
{
    /// <summary>
    /// Gets or sets the raw payload, if preserved.
    /// </summary>
    public JsonObject? RawPayload { get; init; }
}

/// <summary>
/// Base type for command actions emitted by conversation items.
/// </summary>
/// <param name="Type">JSON discriminator for the command action kind.</param>
public abstract record CodexCommandAction(string Type);

/// <summary>
/// Command action that reads a file or path.
/// </summary>
public sealed record CodexReadCommandAction() : CodexCommandAction("read")
{
    /// <summary>
    /// Gets or sets the raw command name.
    /// </summary>
    public string Command { get; init; } = "";

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Gets or sets the target path.
    /// </summary>
    public string Path { get; init; } = "";
}

/// <summary>
/// Command action that lists files under a path.
/// </summary>
public sealed record CodexListFilesCommandAction() : CodexCommandAction("listFiles")
{
    /// <summary>
    /// Gets or sets the raw command name.
    /// </summary>
    public string Command { get; init; } = "";

    /// <summary>
    /// Gets or sets the target path, if any.
    /// </summary>
    public string? Path { get; init; }
}

/// <summary>
/// Command action that searches within a path.
/// </summary>
public sealed record CodexSearchCommandAction() : CodexCommandAction("search")
{
    /// <summary>
    /// Gets or sets the raw command name.
    /// </summary>
    public string Command { get; init; } = "";

    /// <summary>
    /// Gets or sets the search path, if any.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets or sets the search query, if any.
    /// </summary>
    public string? Query { get; init; }
}

/// <summary>
/// Fallback command action for an unrecognized command kind.
/// </summary>
public sealed record CodexUnknownCommandAction() : CodexCommandAction("unknown")
{
    /// <summary>
    /// Gets or sets the raw command name.
    /// </summary>
    public string Command { get; init; } = "";
}

/// <summary>
/// Base type for web search actions.
/// </summary>
/// <param name="Type">JSON discriminator for the web search action kind.</param>
public abstract record CodexWebSearchAction(string Type);

/// <summary>
/// Web search action that runs one or more queries.
/// </summary>
public sealed record CodexSearchWebSearchAction() : CodexWebSearchAction("search")
{
    /// <summary>
    /// Gets or sets the query list.
    /// </summary>
    public IReadOnlyList<string>? Queries { get; init; }

    /// <summary>
    /// Gets or sets the single query string, if present.
    /// </summary>
    public string? Query { get; init; }
}

/// <summary>
/// Web search action that opens a page by URL.
/// </summary>
public sealed record CodexOpenPageWebSearchAction() : CodexWebSearchAction("openPage")
{
    /// <summary>
    /// Gets or sets the page URL.
    /// </summary>
    public string? Url { get; init; }
}

/// <summary>
/// Web search action that finds text on a page.
/// </summary>
public sealed record CodexFindInPageWebSearchAction() : CodexWebSearchAction("findInPage")
{
    /// <summary>
    /// Gets or sets the text pattern to find.
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Gets or sets the page URL, if available.
    /// </summary>
    public string? Url { get; init; }
}

/// <summary>
/// Fallback web search action for an unrecognized kind.
/// </summary>
public sealed record CodexOtherWebSearchAction() : CodexWebSearchAction("other");

/// <summary>
/// Fragment of a hook prompt emitted by Codex.
/// </summary>
public sealed record CodexHookPromptFragment
{
    /// <summary>
    /// Gets or sets the hook run identifier.
    /// </summary>
    public string HookRunId { get; init; } = "";

    /// <summary>
    /// Gets or sets the prompt text.
    /// </summary>
    public string Text { get; init; } = "";
}

/// <summary>
/// Thread item describing a hook prompt.
/// </summary>
public sealed record CodexHookPromptItem() : CodexThreadItem("hookPrompt")
{
    /// <summary>
    /// Gets or sets the prompt fragments.
    /// </summary>
    public IReadOnlyList<CodexHookPromptFragment> Fragments { get; init; } = [];
}

/// <summary>
/// Base type for dynamic tool call output content items.
/// </summary>
/// <param name="Type">JSON discriminator for the content item kind.</param>
public abstract record CodexDynamicToolCallOutputContentItem(string Type);

/// <summary>
/// Text content returned from a dynamic tool call.
/// </summary>
public sealed record CodexInputTextDynamicToolCallOutputContentItem()
    : CodexDynamicToolCallOutputContentItem("inputText")
{
    /// <summary>
    /// Gets or sets the text payload.
    /// </summary>
    public string Text { get; init; } = "";
}

/// <summary>
/// Image content returned from a dynamic tool call.
/// </summary>
public sealed record CodexInputImageDynamicToolCallOutputContentItem()
    : CodexDynamicToolCallOutputContentItem("inputImage")
{
    /// <summary>
    /// Gets or sets the image URL.
    /// </summary>
    public string ImageUrl { get; init; } = "";
}

/// <summary>
/// Error details for an MCP tool call.
/// </summary>
public sealed record CodexMcpToolCallError
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; init; } = "";
}

/// <summary>
/// Result details for an MCP tool call.
/// </summary>
public sealed record CodexMcpToolCallResult
{
    /// <summary>
    /// Gets or sets the raw content returned by the tool.
    /// </summary>
    public IReadOnlyList<JsonNode> Content { get; init; } = [];

    /// <summary>
    /// Gets or sets the structured content, if provided.
    /// </summary>
    public JsonNode? StructuredContent { get; init; }
}

/// <summary>
/// A single step in the structured plan for a turn.
/// </summary>
public sealed record CodexTurnPlanStep
{
    /// <summary>
    /// Gets or sets the plan step text.
    /// </summary>
    public string Step { get; init; } = "";

    /// <summary>
    /// Gets or sets the current plan step status.
    /// </summary>
    public CodexTurnPlanStepStatus Status { get; init; }
}

/// <summary>A single line of output captured from a hook run.</summary>
public sealed record CodexHookOutputEntry
{
    /// <summary>Gets or sets the output entry kind.</summary>
    public CodexHookOutputEntryKind Kind { get; init; }

    /// <summary>Gets or sets the output text.</summary>
    public string Text { get; init; } = "";
}

/// <summary>Summary metadata for a hook run.</summary>
public sealed record CodexHookRunSummary
{
    /// <summary>Gets or sets when the run completed, if it has finished.</summary>
    public long? CompletedAt { get; init; }

    /// <summary>Gets or sets the display order for the run.</summary>
    public int DisplayOrder { get; init; }

    /// <summary>Gets or sets the run duration in milliseconds, if available.</summary>
    public long? DurationMs { get; init; }

    /// <summary>Gets or sets the captured output entries.</summary>
    public IReadOnlyList<CodexHookOutputEntry> Entries { get; init; } = [];

    /// <summary>Gets or sets the hook event name.</summary>
    public CodexHookEventName EventName { get; init; }

    /// <summary>Gets or sets the execution mode.</summary>
    public CodexHookExecutionMode ExecutionMode { get; init; }

    /// <summary>Gets or sets the hook handler type.</summary>
    public CodexHookHandlerType HandlerType { get; init; }

    /// <summary>Gets or sets the run identifier.</summary>
    public string Id { get; init; } = "";

    /// <summary>Gets or sets the hook scope.</summary>
    public CodexHookScope Scope { get; init; }

    /// <summary>Gets or sets the hook source.</summary>
    public CodexHookSourceKind Source { get; init; } = CodexHookSourceKind.Unknown;

    /// <summary>Gets or sets the source path.</summary>
    public string SourcePath { get; init; } = "";

    /// <summary>Gets or sets when the run started.</summary>
    public long StartedAt { get; init; }

    /// <summary>Gets or sets the current run status.</summary>
    public CodexHookRunStatus Status { get; init; }

    /// <summary>Gets or sets the optional status message.</summary>
    public string? StatusMessage { get; init; }
}

/// <summary>Audio chunk data streamed by thread realtime notifications.</summary>
public sealed record CodexThreadRealtimeAudioChunk
{
    /// <summary>Gets or sets the chunk payload.</summary>
    public string Data { get; init; } = "";

    /// <summary>Gets or sets the item identifier, if the chunk is attached to one.</summary>
    public string? ItemId { get; init; }

    /// <summary>Gets or sets the channel count.</summary>
    public int NumChannels { get; init; }

    /// <summary>Gets or sets the sample rate.</summary>
    public int SampleRate { get; init; }

    /// <summary>Gets or sets the samples per channel, if reported.</summary>
    public int? SamplesPerChannel { get; init; }
}

/// <summary>Result metadata for a fuzzy-file-search match.</summary>
public sealed record CodexFuzzyFileSearchResult
{
    /// <summary>Gets or sets the file name.</summary>
    public string FileName { get; init; } = "";

    /// <summary>Gets or sets the matching indices, if provided.</summary>
    public IReadOnlyList<int>? Indices { get; init; }

    /// <summary>Gets or sets the match type.</summary>
    public CodexFuzzyFileSearchMatchType MatchType { get; init; } = CodexFuzzyFileSearchMatchType.Unknown;

    /// <summary>Gets or sets the full path.</summary>
    public string Path { get; init; } = "";

    /// <summary>Gets or sets the search root.</summary>
    public string Root { get; init; } = "";

    /// <summary>Gets or sets the search score.</summary>
    public int Score { get; init; }
}

/// <summary>
/// Base type for thread lifecycle and item events.
/// </summary>
/// <param name="Type">JSON discriminator for the event kind.</param>
public abstract record CodexThreadEvent(string Type);

/// <summary>
/// Event published when a thread is created.
/// </summary>
public sealed record CodexThreadStartedEvent() : CodexThreadEvent("thread.started")
{
    /// <summary>
    /// Gets or sets the thread summary.
    /// </summary>
    public CodexThreadSummary Thread { get; init; } = new();
}

/// <summary>
/// Event published when a turn starts.
/// </summary>
public sealed record CodexTurnStartedEvent() : CodexThreadEvent("turn.started")
{
    /// <summary>
    /// Gets or sets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets or sets the turn record.
    /// </summary>
    public CodexTurnRecord Turn { get; init; } = new();
}

/// <summary>
/// Event published when a turn completes.
/// </summary>
public sealed record CodexTurnCompletedEvent() : CodexThreadEvent("turn.completed")
{
    /// <summary>
    /// Gets or sets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets or sets the turn record.
    /// </summary>
    public CodexTurnRecord Turn { get; init; } = new();
}

/// <summary>
/// Event published when a turn fails.
/// </summary>
public sealed record CodexTurnFailedEvent() : CodexThreadEvent("turn.failed")
{
    /// <summary>
    /// Gets or sets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets or sets the turn record.
    /// </summary>
    public CodexTurnRecord Turn { get; init; } = new();
}

/// <summary>
/// Event published when a thread item starts.
/// </summary>
public sealed record CodexItemStartedEvent() : CodexThreadEvent("item.started")
{
    /// <summary>
    /// Gets or sets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets or sets the turn identifier.
    /// </summary>
    public string TurnId { get; init; } = "";

    /// <summary>
    /// Gets or sets the item snapshot.
    /// </summary>
    public CodexThreadItem Item { get; init; } = new CodexUnknownThreadItem("unknown");

    /// <summary>
    /// Gets or sets the start timestamp in milliseconds, if reported.
    /// </summary>
    public long StartedAtMs { get; init; }
}

/// <summary>
/// Event published when a thread item updates.
/// </summary>
public sealed record CodexItemUpdatedEvent() : CodexThreadEvent("item.updated")
{
    /// <summary>
    /// Gets or sets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets or sets the turn identifier.
    /// </summary>
    public string TurnId { get; init; } = "";

    /// <summary>
    /// Gets or sets the item snapshot.
    /// </summary>
    public CodexThreadItem Item { get; init; } = new CodexUnknownThreadItem("unknown");

    /// <summary>
    /// Gets or sets the completion timestamp in milliseconds, if reported.
    /// </summary>
    public long CompletedAtMs { get; init; }
}

/// <summary>
/// Event published when a thread item completes.
/// </summary>
public sealed record CodexItemCompletedEvent() : CodexThreadEvent("item.completed")
{
    /// <summary>
    /// Gets or sets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets or sets the turn identifier.
    /// </summary>
    public string TurnId { get; init; } = "";

    /// <summary>
    /// Gets or sets the item snapshot.
    /// </summary>
    public CodexThreadItem Item { get; init; } = new CodexUnknownThreadItem("unknown");

    /// <summary>
    /// Gets or sets the completion timestamp in milliseconds, if reported.
    /// </summary>
    public long CompletedAtMs { get; init; }
}

/// <summary>
/// Event published when an automatic approval review starts for an item.
/// </summary>
public sealed record CodexItemAutoApprovalReviewStartedEvent() : CodexThreadEvent("item.autoApprovalReview.started")
{
    /// <summary>
    /// Gets or sets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets or sets the turn identifier.
    /// </summary>
    public string TurnId { get; init; } = "";

    /// <summary>
    /// Gets or sets the review identifier.
    /// </summary>
    public string ReviewId { get; init; } = "";

    /// <summary>
    /// Gets or sets the reviewed item identifier, if one exists.
    /// </summary>
    public string? TargetItemId { get; init; }

    /// <summary>
    /// Gets or sets the review start timestamp in milliseconds.
    /// </summary>
    public long StartedAtMs { get; init; }

    /// <summary>
    /// Gets or sets the review action payload.
    /// </summary>
    public JsonNode? Action { get; init; }

    /// <summary>
    /// Gets or sets the review payload.
    /// </summary>
    public JsonNode? Review { get; init; }
}

/// <summary>
/// Event published when an automatic approval review completes for an item.
/// </summary>
public sealed record CodexItemAutoApprovalReviewCompletedEvent() : CodexThreadEvent("item.autoApprovalReview.completed")
{
    /// <summary>
    /// Gets or sets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets or sets the turn identifier.
    /// </summary>
    public string TurnId { get; init; } = "";

    /// <summary>
    /// Gets or sets the review identifier.
    /// </summary>
    public string ReviewId { get; init; } = "";

    /// <summary>
    /// Gets or sets the reviewed item identifier, if one exists.
    /// </summary>
    public string? TargetItemId { get; init; }

    /// <summary>
    /// Gets or sets the review start timestamp in milliseconds.
    /// </summary>
    public long StartedAtMs { get; init; }

    /// <summary>
    /// Gets or sets the review completion timestamp in milliseconds.
    /// </summary>
    public long CompletedAtMs { get; init; }

    /// <summary>
    /// Gets or sets the decision source identifier.
    /// </summary>
    public string DecisionSource { get; init; } = "";

    /// <summary>
    /// Gets or sets the review action payload.
    /// </summary>
    public JsonNode? Action { get; init; }

    /// <summary>
    /// Gets or sets the review payload.
    /// </summary>
    public JsonNode? Review { get; init; }
}

/// <summary>
/// Event published when a hook run starts.
/// </summary>
public sealed record CodexHookStartedEvent() : CodexThreadEvent("hook.started")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the optional turn identifier.</summary>
    public string? TurnId { get; init; }

    /// <summary>Gets the hook run summary.</summary>
    public CodexHookRunSummary Run { get; init; } = new();
}

/// <summary>
/// Event published when a hook run completes.
/// </summary>
public sealed record CodexHookCompletedEvent() : CodexThreadEvent("hook.completed")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the optional turn identifier.</summary>
    public string? TurnId { get; init; }

    /// <summary>Gets the hook run summary.</summary>
    public CodexHookRunSummary Run { get; init; } = new();
}

/// <summary>
/// Represents a process output delta notification.
/// </summary>
public sealed record CodexProcessOutputDeltaEvent() : CodexThreadEvent("process.outputDelta")
{
    /// <summary>Gets whether the output stream reached the configured cap.</summary>
    public bool CapReached { get; init; }

    /// <summary>Gets the streamed delta as base64.</summary>
    public string DeltaBase64 { get; init; } = "";

    /// <summary>Gets the process handle.</summary>
    public string ProcessHandle { get; init; } = "";

    /// <summary>Gets the output stream.</summary>
    public CodexProcessOutputStream Stream { get; init; } = CodexProcessOutputStream.Unknown;
}

/// <summary>
/// Represents a command execution output delta notification.
/// </summary>
public sealed record CodexCommandExecOutputDeltaEvent() : CodexThreadEvent("command.exec.outputDelta")
{
    /// <summary>Gets whether the output stream reached the configured cap.</summary>
    public bool CapReached { get; init; }

    /// <summary>Gets the streamed delta as base64.</summary>
    public string DeltaBase64 { get; init; } = "";

    /// <summary>Gets the client-supplied process identifier.</summary>
    public string ProcessId { get; init; } = "";

    /// <summary>Gets the output stream.</summary>
    public CodexProcessOutputStream Stream { get; init; } = CodexProcessOutputStream.Unknown;
}

/// <summary>
/// Represents a process exit notification.
/// </summary>
public sealed record CodexProcessExitedEvent() : CodexThreadEvent("process.exited")
{
    /// <summary>Gets the process exit code.</summary>
    public int ExitCode { get; init; }

    /// <summary>Gets the process handle.</summary>
    public string ProcessHandle { get; init; } = "";

    /// <summary>Gets the buffered stderr output.</summary>
    public string Stderr { get; init; } = "";

    /// <summary>Gets whether stderr reached the configured cap.</summary>
    public bool StderrCapReached { get; init; }

    /// <summary>Gets the buffered stdout output.</summary>
    public string Stdout { get; init; } = "";

    /// <summary>Gets whether stdout reached the configured cap.</summary>
    public bool StdoutCapReached { get; init; }
}

/// <summary>
/// Represents a warning notification.
/// </summary>
public sealed record CodexWarningEvent() : CodexThreadEvent("warning")
{
    /// <summary>Gets the warning message.</summary>
    public string Message { get; init; } = "";

    /// <summary>Gets the optional target thread identifier.</summary>
    public string? ThreadId { get; init; }
}

/// <summary>
/// Represents a config warning notification.
/// </summary>
public sealed record CodexConfigWarningEvent() : CodexThreadEvent("configWarning")
{
    /// <summary>Gets the optional extra warning details.</summary>
    public string? Details { get; init; }

    /// <summary>Gets the optional config file path.</summary>
    public string? Path { get; init; }

    /// <summary>Gets the optional text range inside the config file.</summary>
    public CodexTextRange? Range { get; init; }

    /// <summary>Gets the warning summary.</summary>
    public string Summary { get; init; } = "";
}

/// <summary>
/// Represents a deprecation notice notification.
/// </summary>
public sealed record CodexDeprecationNoticeEvent() : CodexThreadEvent("deprecationNotice")
{
    /// <summary>Gets optional extra guidance.</summary>
    public string? Details { get; init; }

    /// <summary>Gets the deprecation summary.</summary>
    public string Summary { get; init; } = "";
}

/// <summary>
/// Represents a guardian warning notification.
/// </summary>
public sealed record CodexGuardianWarningEvent() : CodexThreadEvent("guardianWarning")
{
    /// <summary>Gets the warning message.</summary>
    public string Message { get; init; } = "";

    /// <summary>Gets the target thread identifier.</summary>
    public string ThreadId { get; init; } = "";
}

/// <summary>
/// Represents an account update notification.
/// </summary>
public sealed record CodexAccountUpdatedEvent() : CodexThreadEvent("account.updated")
{
    /// <summary>Gets the auth mode.</summary>
    public CodexAuthMode AuthMode { get; init; } = CodexAuthMode.Unknown;

    /// <summary>Gets the plan type.</summary>
    public CodexPlanType PlanType { get; init; } = CodexPlanType.Unknown;
}

/// <summary>
/// Represents a completed account login notification.
/// </summary>
public sealed record CodexAccountLoginCompletedEvent() : CodexThreadEvent("account.login.completed")
{
    /// <summary>Gets the login error message, if any.</summary>
    public string? Error { get; init; }

    /// <summary>Gets the login identifier, if any.</summary>
    public string? LoginId { get; init; }

    /// <summary>Gets whether the login succeeded.</summary>
    public bool Success { get; init; }
}

/// <summary>
/// Represents a resolved server request notification.
/// </summary>
public sealed record CodexServerRequestResolvedEvent() : CodexThreadEvent("serverRequest.resolved")
{
    /// <summary>Gets the resolved request identifier.</summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>Gets the thread that resolved the request.</summary>
    public string ThreadId { get; init; } = string.Empty;
}

/// <summary>
/// Represents an app list refresh notification.
/// </summary>
public sealed record CodexAppListUpdatedEvent() : CodexThreadEvent("app.list.updated")
{
    /// <summary>Gets the raw app inventory list.</summary>
    public IReadOnlyList<JsonObject> Data { get; init; } = [];
}

/// <summary>
/// Represents a skills inventory refresh notification.
/// </summary>
public sealed record CodexSkillsChangedEvent() : CodexThreadEvent("skills.changed");

/// <summary>
/// Represents a file-system change notification.
/// </summary>
public sealed record CodexFsChangedEvent() : CodexThreadEvent("fs.changed")
{
    /// <summary>Gets the changed paths.</summary>
    public IReadOnlyList<string> ChangedPaths { get; init; } = [];

    /// <summary>Gets the watch identifier.</summary>
    public string WatchId { get; init; } = "";
}

/// <summary>
/// Represents a fuzzy-file-search session completion notification.
/// </summary>
public sealed record CodexFuzzyFileSearchSessionCompletedEvent() : CodexThreadEvent("fuzzyFileSearch.sessionCompleted")
{
    /// <summary>Gets the search session identifier.</summary>
    public string SessionId { get; init; } = "";
}

/// <summary>
/// Represents a fuzzy-file-search session update notification.
/// </summary>
public sealed record CodexFuzzyFileSearchSessionUpdatedEvent() : CodexThreadEvent("fuzzyFileSearch.sessionUpdated")
{
    /// <summary>Gets the search result list.</summary>
    public IReadOnlyList<CodexFuzzyFileSearchResult> Files { get; init; } = [];

    /// <summary>Gets the search query.</summary>
    public string Query { get; init; } = "";

    /// <summary>Gets the search session identifier.</summary>
    public string SessionId { get; init; } = "";
}

/// <summary>
/// Represents a completed MCP server OAuth login notification.
/// </summary>
public sealed record CodexMcpServerOauthLoginCompletedEvent() : CodexThreadEvent("mcpServer.oauthLogin.completed")
{
    /// <summary>Gets the error message, if any.</summary>
    public string? Error { get; init; }

    /// <summary>Gets the server name.</summary>
    public string Name { get; init; } = "";

    /// <summary>Gets whether the login succeeded.</summary>
    public bool Success { get; init; }
}

/// <summary>
/// Represents an MCP server startup status update notification.
/// </summary>
public sealed record CodexMcpServerStartupStatusUpdatedEvent() : CodexThreadEvent("mcpServer.startupStatus.updated")
{
    /// <summary>Gets the error message, if any.</summary>
    public string? Error { get; init; }

    /// <summary>Gets the server name.</summary>
    public string Name { get; init; } = "";

    /// <summary>Gets the startup state.</summary>
    public CodexMcpServerStartupState Status { get; init; } = CodexMcpServerStartupState.Unknown;
}

/// <summary>
/// Represents a remote-control status change notification.
/// </summary>
public sealed record CodexRemoteControlStatusChangedEvent() : CodexThreadEvent("remoteControl.status.changed")
{
    /// <summary>Gets the environment identifier, if any.</summary>
    public string? EnvironmentId { get; init; }

    /// <summary>Gets the installation identifier.</summary>
    public string InstallationId { get; init; } = "";

    /// <summary>Gets the connection status.</summary>
    public CodexRemoteControlConnectionStatus Status { get; init; } = CodexRemoteControlConnectionStatus.Unknown;
}

/// <summary>
/// Represents a Windows sandbox setup completion notification.
/// </summary>
public sealed record CodexWindowsSandboxSetupCompletedEvent() : CodexThreadEvent("windowsSandbox.setupCompleted")
{
    /// <summary>Gets the error message, if any.</summary>
    public string? Error { get; init; }

    /// <summary>Gets the sandbox mode.</summary>
    public CodexWindowsSandboxSetupMode Mode { get; init; } = CodexWindowsSandboxSetupMode.Unknown;

    /// <summary>Gets whether the setup succeeded.</summary>
    public bool Success { get; init; }
}

/// <summary>
/// Represents a Windows world-writable warning notification.
/// </summary>
public sealed record CodexWindowsWorldWritableWarningEvent() : CodexThreadEvent("windows.worldWritableWarning")
{
    /// <summary>Gets the number of extra paths beyond the sample set.</summary>
    public int ExtraCount { get; init; }

    /// <summary>Gets whether the scan failed.</summary>
    public bool FailedScan { get; init; }

    /// <summary>Gets the sampled paths.</summary>
    public IReadOnlyList<string> SamplePaths { get; init; } = [];
}

/// <summary>
/// Represents a model reroute notification.
/// </summary>
public sealed record CodexModelReroutedEvent() : CodexThreadEvent("model.rerouted")
{
    /// <summary>Gets the source model.</summary>
    public string FromModel { get; init; } = "";

    /// <summary>Gets the reroute reason.</summary>
    public CodexModelRerouteReason Reason { get; init; } = CodexModelRerouteReason.Unknown;

    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the destination model.</summary>
    public string ToModel { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";
}

/// <summary>
/// Represents a model verification notification.
/// </summary>
public sealed record CodexModelVerificationEvent() : CodexThreadEvent("model.verification")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets the verification values.</summary>
    public IReadOnlyList<CodexModelVerificationValue> Verifications { get; init; } = [];
}

/// <summary>
/// Represents a thread realtime session start notification.
/// </summary>
public sealed record CodexThreadRealtimeStartedEvent() : CodexThreadEvent("thread.realtime.started")
{
    /// <summary>Gets the realtime session identifier.</summary>
    public string? RealtimeSessionId { get; init; }

    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the realtime conversation version.</summary>
    public CodexRealtimeConversationVersion Version { get; init; } = CodexRealtimeConversationVersion.Unknown;
}

/// <summary>
/// Represents a thread realtime item addition notification.
/// </summary>
public sealed record CodexThreadRealtimeItemAddedEvent() : CodexThreadEvent("thread.realtime.itemAdded")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the raw item payload.</summary>
    public JsonNode? Item { get; init; }
}

/// <summary>
/// Represents a thread realtime transcript delta notification.
/// </summary>
public sealed record CodexThreadRealtimeTranscriptDeltaEvent() : CodexThreadEvent("thread.realtime.transcript.delta")
{
    /// <summary>Gets the text delta.</summary>
    public string Delta { get; init; } = "";

    /// <summary>Gets the speaker role.</summary>
    public string Role { get; init; } = "";

    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";
}

/// <summary>
/// Represents a thread realtime transcript completion notification.
/// </summary>
public sealed record CodexThreadRealtimeTranscriptDoneEvent() : CodexThreadEvent("thread.realtime.transcript.done")
{
    /// <summary>Gets the speaker role.</summary>
    public string Role { get; init; } = "";

    /// <summary>Gets the completed transcript text.</summary>
    public string Text { get; init; } = "";

    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";
}

/// <summary>
/// Represents a thread realtime output-audio delta notification.
/// </summary>
public sealed record CodexThreadRealtimeOutputAudioDeltaEvent() : CodexThreadEvent("thread.realtime.outputAudio.delta")
{
    /// <summary>Gets the audio chunk.</summary>
    public CodexThreadRealtimeAudioChunk Audio { get; init; } = new();

    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";
}

/// <summary>
/// Represents a thread realtime SDP notification.
/// </summary>
public sealed record CodexThreadRealtimeSdpEvent() : CodexThreadEvent("thread.realtime.sdp")
{
    /// <summary>Gets the SDP payload.</summary>
    public string Sdp { get; init; } = "";

    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";
}

/// <summary>
/// Represents a thread realtime error notification.
/// </summary>
public sealed record CodexThreadRealtimeErrorEvent() : CodexThreadEvent("thread.realtime.error")
{
    /// <summary>Gets the error message.</summary>
    public string Message { get; init; } = "";

    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";
}

/// <summary>
/// Represents a thread realtime closed notification.
/// </summary>
public sealed record CodexThreadRealtimeClosedEvent() : CodexThreadEvent("thread.realtime.closed")
{
    /// <summary>Gets the close reason, if any.</summary>
    public string? Reason { get; init; }

    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";
}

/// <summary>
/// Event published when a thread-level error occurs.
/// </summary>
public sealed record CodexThreadErrorEvent() : CodexThreadEvent("error")
{
    /// <summary>
    /// Gets or sets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets or sets the turn identifier, if available.
    /// </summary>
    public string? TurnId { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the operation will retry.
    /// </summary>
    public bool WillRetry { get; init; }

    /// <summary>
    /// Gets or sets the error payload.
    /// </summary>
    public CodexTurnError Error { get; init; } = new();
}

/// <summary>
/// Represents an app-server notification that a thread status changed.
/// </summary>
public sealed record CodexThreadStatusChangedEvent() : CodexThreadEvent("thread.status.changed")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets the updated thread status.
    /// </summary>
    public CodexThreadStatus Status { get; init; } = new CodexNotLoadedThreadStatus();
}

/// <summary>
/// Represents an app-server notification that a thread was archived.
/// </summary>
public sealed record CodexThreadArchivedEvent() : CodexThreadEvent("thread.archived")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";
}

/// <summary>
/// Represents an app-server notification that a thread was closed.
/// </summary>
public sealed record CodexThreadClosedEvent() : CodexThreadEvent("thread.closed")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";
}

/// <summary>
/// Represents an app-server notification that a thread was compacted.
/// </summary>
public sealed record CodexThreadCompactedEvent() : CodexThreadEvent("thread.compacted")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets the turn identifier associated with the compaction.
    /// </summary>
    public string TurnId { get; init; } = "";
}

/// <summary>
/// Represents an app-server notification that a thread name changed.
/// </summary>
public sealed record CodexThreadNameUpdatedEvent() : CodexThreadEvent("thread.name.updated")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets the updated thread name, when one is available.
    /// </summary>
    public string? ThreadName { get; init; }
}

/// <summary>
/// Represents an app-server notification that thread token usage changed.
/// </summary>
public sealed record CodexThreadTokenUsageUpdatedEvent() : CodexThreadEvent("thread.tokenUsage.updated")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets the turn identifier associated with the usage update.
    /// </summary>
    public string TurnId { get; init; } = "";

    /// <summary>
    /// Gets the token-usage snapshot.
    /// </summary>
    public CodexUsage TokenUsage { get; init; } = new();
}

/// <summary>
/// Represents an app-server notification that a thread was unarchived.
/// </summary>
public sealed record CodexThreadUnarchivedEvent() : CodexThreadEvent("thread.unarchived")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";
}

/// <summary>
/// Represents an app-server notification that thread settings changed.
/// </summary>
public sealed record CodexThreadSettingsUpdatedEvent() : CodexThreadEvent("thread.settings.updated")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets the raw thread settings payload.
    /// </summary>
    public JsonObject? ThreadSettings { get; init; }
}

/// <summary>
/// Represents an app-server notification that a turn diff changed.
/// </summary>
public sealed record CodexTurnDiffUpdatedEvent() : CodexThreadEvent("turn.diff.updated")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets the turn identifier.
    /// </summary>
    public string TurnId { get; init; } = "";

    /// <summary>
    /// Gets the rendered turn diff.
    /// </summary>
    public string Diff { get; init; } = "";
}

/// <summary>
/// Represents an app-server notification that the current account rate-limit snapshot changed.
/// </summary>
public sealed record CodexAccountRateLimitsUpdatedEvent() : CodexThreadEvent("account.rateLimits.updated")
{
    /// <summary>
    /// Gets the rate-limit snapshot reported by Codex.
    /// </summary>
    public CodexRateLimitSnapshot RateLimits { get; init; } = new();
}

/// <summary>
/// Represents an app-server notification that a thread goal was updated.
/// </summary>
public sealed record CodexThreadGoalUpdatedEvent() : CodexThreadEvent("thread.goal.updated")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets the turn identifier associated with the update, if any.
    /// </summary>
    public string? TurnId { get; init; }

    /// <summary>
    /// Gets the updated goal.
    /// </summary>
    public CodexThreadGoal Goal { get; init; } = new();
}

/// <summary>
/// Represents an app-server notification that a thread goal was cleared.
/// </summary>
public sealed record CodexThreadGoalClearedEvent() : CodexThreadEvent("thread.goal.cleared")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";
}

/// <summary>
/// Represents an app-server notification that the structured turn plan changed.
/// </summary>
public sealed record CodexTurnPlanUpdatedEvent() : CodexThreadEvent("turn.plan.updated")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets the turn identifier.
    /// </summary>
    public string TurnId { get; init; } = "";

    /// <summary>
    /// Gets the optional explanation supplied with the plan update.
    /// </summary>
    public string? Explanation { get; init; }

    /// <summary>
    /// Gets the ordered structured plan steps.
    /// </summary>
    public IReadOnlyList<CodexTurnPlanStep> Plan { get; init; } = [];
}

/// <summary>
/// Represents an experimental streaming delta for a plan item.
/// </summary>
public sealed record CodexPlanDeltaEvent() : CodexThreadEvent("item.plan.delta")
{
    /// <summary>
    /// Gets the thread identifier.
    /// </summary>
    public string ThreadId { get; init; } = "";

    /// <summary>
    /// Gets the turn identifier.
    /// </summary>
    public string TurnId { get; init; } = "";

    /// <summary>
    /// Gets the plan item identifier.
    /// </summary>
    public string ItemId { get; init; } = "";

    /// <summary>
    /// Gets the streamed plan text delta.
    /// </summary>
    public string Delta { get; init; } = "";
}

/// <summary>
/// Represents a streamed delta for an agent message item.
/// </summary>
public sealed record CodexAgentMessageDeltaEvent() : CodexThreadEvent("item.agentMessage.delta")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets the item identifier.</summary>
    public string ItemId { get; init; } = "";

    /// <summary>Gets the streamed text delta.</summary>
    public string Delta { get; init; } = "";
}

/// <summary>
/// Represents a streamed delta for a command execution item.
/// </summary>
public sealed record CodexCommandExecutionOutputDeltaEvent() : CodexThreadEvent("item.commandExecution.outputDelta")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets the item identifier.</summary>
    public string ItemId { get; init; } = "";

    /// <summary>Gets the streamed output delta.</summary>
    public string Delta { get; init; } = "";
}

/// <summary>
/// Represents terminal input written to a command execution item.
/// </summary>
public sealed record CodexCommandExecutionTerminalInteractionEvent() : CodexThreadEvent("item.commandExecution.terminalInteraction")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets the item identifier.</summary>
    public string ItemId { get; init; } = "";

    /// <summary>Gets the process identifier.</summary>
    public string ProcessId { get; init; } = "";

    /// <summary>Gets the terminal input.</summary>
    public string Stdin { get; init; } = "";
}

/// <summary>
/// Represents a streamed delta for a file change item.
/// </summary>
public sealed record CodexFileChangeOutputDeltaEvent() : CodexThreadEvent("item.fileChange.outputDelta")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets the item identifier.</summary>
    public string ItemId { get; init; } = "";

    /// <summary>Gets the streamed output delta.</summary>
    public string Delta { get; init; } = "";
}

/// <summary>
/// Represents a patch update for a file change item.
/// </summary>
public sealed record CodexFileChangePatchUpdatedEvent() : CodexThreadEvent("item.fileChange.patchUpdated")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets the item identifier.</summary>
    public string ItemId { get; init; } = "";

    /// <summary>Gets the latest file changes.</summary>
    public IReadOnlyList<CodexFileUpdateChange> Changes { get; init; } = [];
}

/// <summary>
/// Represents progress for an MCP tool call item.
/// </summary>
public sealed record CodexMcpToolCallProgressEvent() : CodexThreadEvent("item.mcpToolCall.progress")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets the item identifier.</summary>
    public string ItemId { get; init; } = "";

    /// <summary>Gets the progress message.</summary>
    public string Message { get; init; } = "";
}

/// <summary>
/// Represents a streamed delta for reasoning text.
/// </summary>
public sealed record CodexReasoningTextDeltaEvent() : CodexThreadEvent("item.reasoning.textDelta")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets the item identifier.</summary>
    public string ItemId { get; init; } = "";

    /// <summary>Gets the content index for the delta.</summary>
    public int ContentIndex { get; init; }

    /// <summary>Gets the streamed text delta.</summary>
    public string Delta { get; init; } = "";
}

/// <summary>
/// Represents that a reasoning summary part was added.
/// </summary>
public sealed record CodexReasoningSummaryPartAddedEvent() : CodexThreadEvent("item.reasoning.summaryPartAdded")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets the item identifier.</summary>
    public string ItemId { get; init; } = "";

    /// <summary>Gets the summary index.</summary>
    public int SummaryIndex { get; init; }
}

/// <summary>
/// Represents a streamed delta for reasoning summary text.
/// </summary>
public sealed record CodexReasoningSummaryTextDeltaEvent() : CodexThreadEvent("item.reasoning.summaryTextDelta")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets the item identifier.</summary>
    public string ItemId { get; init; } = "";

    /// <summary>Gets the summary index.</summary>
    public int SummaryIndex { get; init; }

    /// <summary>Gets the streamed text delta.</summary>
    public string Delta { get; init; } = "";
}

/// <summary>
/// Represents an internal raw response item completion notification.
/// </summary>
public sealed record CodexRawResponseItemCompletedEvent() : CodexThreadEvent("rawResponseItem.completed")
{
    /// <summary>Gets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets the raw response item payload.</summary>
    public JsonNode? Item { get; init; }
}

/// <summary>
/// Represents completion of an external-agent configuration import.
/// </summary>
public sealed record CodexExternalAgentConfigImportCompletedEvent()
    : CodexThreadEvent("externalAgentConfig.import.completed");

/// <summary>
/// Fallback event for an unrecognized thread event type.
/// </summary>
/// <param name="UnknownType">Original event type string.</param>
public sealed record CodexUnknownThreadEvent(string UnknownType) : CodexThreadEvent(UnknownType)
{
    /// <summary>
    /// Gets or sets the raw payload, if preserved.
    /// </summary>
    public JsonObject? RawPayload { get; init; }
}

/// <summary>
/// Base type for thread items.
/// </summary>
/// <param name="Type">JSON discriminator for the item kind.</param>
public abstract record CodexThreadItem(string Type)
{
    /// <summary>
    /// Gets or sets the item identifier.
    /// </summary>
    public string Id { get; init; } = "";
}

/// <summary>
/// Thread item containing user message content.
/// </summary>
public sealed record CodexUserMessageItem() : CodexThreadItem("userMessage")
{
    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public IReadOnlyList<CodexInputItem> Content { get; init; } = [];
}

/// <summary>
/// Thread item containing an agent message.
/// </summary>
public sealed record CodexAgentMessageItem() : CodexThreadItem("agentMessage")
{
    /// <summary>
    /// Gets or sets the memory citation, if one was attached.
    /// </summary>
    public CodexMemoryCitation? MemoryCitation { get; init; }

    /// <summary>
    /// Gets or sets the message phase, if available.
    /// </summary>
    public CodexMessagePhase? Phase { get; init; }

    /// <summary>
    /// Gets or sets the message text.
    /// </summary>
    public string Text { get; init; } = "";
}

/// <summary>
/// Thread item containing the current plan text.
/// </summary>
public sealed record CodexPlanItem() : CodexThreadItem("plan")
{
    /// <summary>
    /// Gets or sets the plan text.
    /// </summary>
    public string Text { get; init; } = "";
}

/// <summary>
/// Thread item containing reasoning content and summary.
/// </summary>
public sealed record CodexReasoningItem() : CodexThreadItem("reasoning")
{
    /// <summary>
    /// Gets or sets the full reasoning content, if captured.
    /// </summary>
    public IReadOnlyList<string>? Content { get; init; } = [];

    /// <summary>
    /// Gets or sets the compact reasoning summary, if captured.
    /// </summary>
    public IReadOnlyList<string>? Summary { get; init; } = [];
}

/// <summary>
/// Thread item describing a command execution.
/// </summary>
public sealed record CodexCommandExecutionItem() : CodexThreadItem("commandExecution")
{
    /// <summary>
    /// Gets or sets the combined standard output and error text.
    /// </summary>
    public string AggregatedOutput { get; init; } = "";

    /// <summary>
    /// Gets or sets the command line.
    /// </summary>
    public string Command { get; init; } = "";

    /// <summary>
    /// Gets or sets the parsed command actions.
    /// </summary>
    public IReadOnlyList<CodexCommandAction> CommandActions { get; init; } = [];

    /// <summary>
    /// Gets or sets the source of the command execution, if known.
    /// </summary>
    public CodexCommandExecutionSource? Source { get; init; }

    /// <summary>
    /// Gets or sets the working directory.
    /// </summary>
    public string Cwd { get; init; } = "";

    /// <summary>
    /// Gets or sets the command duration in milliseconds, if known.
    /// </summary>
    public int? DurationMs { get; init; }

    /// <summary>
    /// Gets or sets the exit code, if known.
    /// </summary>
    public int? ExitCode { get; init; }

    /// <summary>
    /// Gets or sets the process identifier, if known.
    /// </summary>
    public string? ProcessId { get; init; }

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    public CodexCommandExecutionStatus Status { get; init; }
}

/// <summary>
/// Thread item describing a file change operation.
/// </summary>
public sealed record CodexFileChangeItem() : CodexThreadItem("fileChange")
{
    /// <summary>
    /// Gets or sets the file changes.
    /// </summary>
    public IReadOnlyList<CodexFileUpdateChange> Changes { get; init; } = [];

    /// <summary>
    /// Gets or sets the patch application status.
    /// </summary>
    public CodexPatchApplyStatus Status { get; init; }
}

/// <summary>
/// Thread item describing an MCP tool call.
/// </summary>
public sealed record CodexMcpToolCallItem() : CodexThreadItem("mcpToolCall")
{
    /// <summary>
    /// Gets or sets the tool arguments.
    /// </summary>
    public JsonNode? Arguments { get; init; }

    /// <summary>
    /// Gets or sets the duration in milliseconds, if known.
    /// </summary>
    public int? DurationMs { get; init; }

    /// <summary>
    /// Gets or sets the tool error, if any.
    /// </summary>
    public CodexMcpToolCallError? Error { get; init; }

    /// <summary>
    /// Gets or sets the MCP server name.
    /// </summary>
    public string Server { get; init; } = "";

    /// <summary>
    /// Gets or sets the optional MCP app resource URI.
    /// </summary>
    public string? McpAppResourceUri { get; init; }

    /// <summary>
    /// Gets or sets the tool result, if any.
    /// </summary>
    public CodexMcpToolCallResult? Result { get; init; }

    /// <summary>
    /// Gets or sets the call status.
    /// </summary>
    public CodexMcpToolCallStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the tool name.
    /// </summary>
    public string Tool { get; init; } = "";
}

/// <summary>
/// Thread item describing a dynamic tool call.
/// </summary>
public sealed record CodexDynamicToolCallItem() : CodexThreadItem("dynamicToolCall")
{
    /// <summary>
    /// Gets or sets the tool arguments.
    /// </summary>
    public JsonNode? Arguments { get; init; }

    /// <summary>
    /// Gets or sets the output content items.
    /// </summary>
    public IReadOnlyList<CodexDynamicToolCallOutputContentItem>? ContentItems { get; init; }

    /// <summary>
    /// Gets or sets the duration in milliseconds, if known.
    /// </summary>
    public int? DurationMs { get; init; }

    /// <summary>
    /// Gets or sets the tool namespace, if one was supplied.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// Gets or sets the call status.
    /// </summary>
    public CodexDynamicToolCallStatus Status { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the call succeeded.
    /// </summary>
    public bool? Success { get; init; }

    /// <summary>
    /// Gets or sets the tool name.
    /// </summary>
    public string Tool { get; init; } = "";
}

/// <summary>
/// Thread item describing a collaborative agent tool call.
/// </summary>
public sealed record CodexCollabAgentToolCallItem() : CodexThreadItem("collabAgentToolCall")
{
    /// <summary>
    /// Gets or sets the per-agent state map.
    /// </summary>
    public IReadOnlyDictionary<string, CodexCollabAgentState> AgentsStates { get; init; } =
        new Dictionary<string, CodexCollabAgentState>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the model name, if specified.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets or sets the prompt, if specified.
    /// </summary>
    public string? Prompt { get; init; }

    /// <summary>
    /// Gets or sets the reasoning effort, if specified.
    /// </summary>
    public CodexReasoningEffort? ReasoningEffort { get; init; }

    /// <summary>
    /// Gets or sets the receiver thread identifiers.
    /// </summary>
    public IReadOnlyList<string> ReceiverThreadIds { get; init; } = [];

    /// <summary>
    /// Gets or sets the sender thread identifier.
    /// </summary>
    public string SenderThreadId { get; init; } = "";

    /// <summary>
    /// Gets or sets the call status.
    /// </summary>
    public CodexCollabAgentToolCallStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the collaborative agent tool.
    /// </summary>
    public CodexCollabAgentTool Tool { get; init; }
}

/// <summary>
/// Thread item describing a web search request.
/// </summary>
public sealed record CodexWebSearchItem() : CodexThreadItem("webSearch")
{
    /// <summary>
    /// Gets or sets the resolved action, if any.
    /// </summary>
    public CodexWebSearchAction? Action { get; init; }

    /// <summary>
    /// Gets or sets the primary query text.
    /// </summary>
    public string Query { get; init; } = "";
}

/// <summary>
/// Thread item describing an image viewer request.
/// </summary>
public sealed record CodexImageViewItem() : CodexThreadItem("imageView")
{
    /// <summary>
    /// Gets or sets the image path.
    /// </summary>
    public string Path { get; init; } = "";
}

/// <summary>
/// Thread item describing an image generation result.
/// </summary>
public sealed record CodexImageGenerationItem() : CodexThreadItem("imageGeneration")
{
    /// <summary>
    /// Gets or sets the generated result text or URL.
    /// </summary>
    public string Result { get; init; } = "";

    /// <summary>
    /// Gets or sets the revised prompt, if any.
    /// </summary>
    public string? RevisedPrompt { get; init; }

    /// <summary>
    /// Gets or sets the saved image path, if any.
    /// </summary>
    public string? SavedPath { get; init; }

    /// <summary>
    /// Gets or sets the generation status.
    /// </summary>
    public string Status { get; init; } = "";
}

/// <summary>
/// Thread item indicating review mode was entered.
/// </summary>
public sealed record CodexEnteredReviewModeItem() : CodexThreadItem("enteredReviewMode")
{
    /// <summary>
    /// Gets or sets the review text.
    /// </summary>
    public string Review { get; init; } = "";
}

/// <summary>
/// Thread item indicating review mode was exited.
/// </summary>
public sealed record CodexExitedReviewModeItem() : CodexThreadItem("exitedReviewMode")
{
    /// <summary>
    /// Gets or sets the review text.
    /// </summary>
    public string Review { get; init; } = "";
}

/// <summary>
/// Thread item indicating context compaction.
/// </summary>
public sealed record CodexContextCompactionItem() : CodexThreadItem("contextCompaction");

/// <summary>
/// Thread item containing TODO items.
/// </summary>
public sealed record CodexTodoListItem() : CodexThreadItem("todoList")
{
    /// <summary>
    /// Gets or sets the TODO items.
    /// </summary>
    public IReadOnlyList<CodexTodoItem> Items { get; init; } = [];
}

/// <summary>
/// Thread item describing an error.
/// </summary>
public sealed record CodexErrorItem() : CodexThreadItem("error")
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; init; } = "";
}

/// <summary>
/// Fallback thread item for an unrecognized item type.
/// </summary>
/// <param name="UnknownType">Original item type string.</param>
public sealed record CodexUnknownThreadItem(string UnknownType) : CodexThreadItem(UnknownType)
{
    /// <summary>
    /// Gets or sets the raw payload, if preserved.
    /// </summary>
    public JsonObject? RawPayload { get; init; }
}
