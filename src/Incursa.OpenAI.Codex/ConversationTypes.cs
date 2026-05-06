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
