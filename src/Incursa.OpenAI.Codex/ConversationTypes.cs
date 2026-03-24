using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-CATALOG-0306, REQ-CODEX-SDK-HELPERS-0316, REQ-CODEX-SDK-HELPERS-0317, REQ-CODEX-SDK-HELPERS-0318.

public abstract record CodexInputItem(string Type);

public sealed record CodexTextInput() : CodexInputItem("text")
{
    public string Text { get; init; } = "";
}

public sealed record CodexImageInput() : CodexInputItem("image")
{
    public string Url { get; init; } = "";
}

public sealed record CodexLocalImageInput() : CodexInputItem("localImage")
{
    public string Path { get; init; } = "";
}

public sealed record CodexSkillInput() : CodexInputItem("skill")
{
    public string Name { get; init; } = "";

    public string Path { get; init; } = "";
}

public sealed record CodexMentionInput() : CodexInputItem("mention")
{
    public string Name { get; init; } = "";

    public string Path { get; init; } = "";
}

public abstract record CodexCommandAction(string Type);

public sealed record CodexReadCommandAction() : CodexCommandAction("read")
{
    public string Command { get; init; } = "";

    public string Name { get; init; } = "";

    public string Path { get; init; } = "";
}

public sealed record CodexListFilesCommandAction() : CodexCommandAction("listFiles")
{
    public string Command { get; init; } = "";

    public string? Path { get; init; }
}

public sealed record CodexSearchCommandAction() : CodexCommandAction("search")
{
    public string Command { get; init; } = "";

    public string? Path { get; init; }

    public string? Query { get; init; }
}

public sealed record CodexUnknownCommandAction() : CodexCommandAction("unknown")
{
    public string Command { get; init; } = "";
}

public abstract record CodexWebSearchAction(string Type);

public sealed record CodexSearchWebSearchAction() : CodexWebSearchAction("search")
{
    public IReadOnlyList<string>? Queries { get; init; }

    public string? Query { get; init; }
}

public sealed record CodexOpenPageWebSearchAction() : CodexWebSearchAction("openPage")
{
    public string? Url { get; init; }
}

public sealed record CodexFindInPageWebSearchAction() : CodexWebSearchAction("findInPage")
{
    public string? Pattern { get; init; }

    public string? Url { get; init; }
}

public sealed record CodexOtherWebSearchAction() : CodexWebSearchAction("other");

public abstract record CodexDynamicToolCallOutputContentItem(string Type);

public sealed record CodexInputTextDynamicToolCallOutputContentItem()
    : CodexDynamicToolCallOutputContentItem("inputText")
{
    public string Text { get; init; } = "";
}

public sealed record CodexInputImageDynamicToolCallOutputContentItem()
    : CodexDynamicToolCallOutputContentItem("inputImage")
{
    public string ImageUrl { get; init; } = "";
}

public sealed record CodexMcpToolCallError
{
    public string Message { get; init; } = "";
}

public sealed record CodexMcpToolCallResult
{
    public IReadOnlyList<JsonNode> Content { get; init; } = [];

    public JsonNode? StructuredContent { get; init; }
}

public abstract record CodexThreadEvent(string Type);

public sealed record CodexThreadStartedEvent() : CodexThreadEvent("thread.started")
{
    public CodexThreadSummary Thread { get; init; } = new();
}

public sealed record CodexTurnStartedEvent() : CodexThreadEvent("turn.started")
{
    public CodexTurnRecord Turn { get; init; } = new();
}

public sealed record CodexTurnCompletedEvent() : CodexThreadEvent("turn.completed")
{
    public CodexTurnRecord Turn { get; init; } = new();
}

public sealed record CodexTurnFailedEvent() : CodexThreadEvent("turn.failed")
{
    public CodexTurnRecord Turn { get; init; } = new();
}

public sealed record CodexItemStartedEvent() : CodexThreadEvent("item.started")
{
    public string ThreadId { get; init; } = "";

    public string TurnId { get; init; } = "";

    public CodexThreadItem Item { get; init; } = new CodexUnknownThreadItem("unknown");
}

public sealed record CodexItemUpdatedEvent() : CodexThreadEvent("item.updated")
{
    public string ThreadId { get; init; } = "";

    public string TurnId { get; init; } = "";

    public CodexThreadItem Item { get; init; } = new CodexUnknownThreadItem("unknown");
}

public sealed record CodexItemCompletedEvent() : CodexThreadEvent("item.completed")
{
    public string ThreadId { get; init; } = "";

    public string TurnId { get; init; } = "";

    public CodexThreadItem Item { get; init; } = new CodexUnknownThreadItem("unknown");
}

public sealed record CodexThreadErrorEvent() : CodexThreadEvent("error")
{
    public string ThreadId { get; init; } = "";

    public string? TurnId { get; init; }

    public bool WillRetry { get; init; }

    public CodexTurnError Error { get; init; } = new();
}

public sealed record CodexUnknownThreadEvent(string UnknownType) : CodexThreadEvent(UnknownType)
{
    public JsonObject? RawPayload { get; init; }
}

public abstract record CodexThreadItem(string Type)
{
    public string Id { get; init; } = "";
}

public sealed record CodexUserMessageItem() : CodexThreadItem("userMessage")
{
    public IReadOnlyList<CodexInputItem> Content { get; init; } = [];
}

public sealed record CodexAgentMessageItem() : CodexThreadItem("agentMessage")
{
    public CodexMessagePhase? Phase { get; init; }

    public string Text { get; init; } = "";
}

public sealed record CodexPlanItem() : CodexThreadItem("plan")
{
    public string Text { get; init; } = "";
}

public sealed record CodexReasoningItem() : CodexThreadItem("reasoning")
{
    public IReadOnlyList<string>? Content { get; init; } = [];

    public IReadOnlyList<string>? Summary { get; init; } = [];
}

public sealed record CodexCommandExecutionItem() : CodexThreadItem("commandExecution")
{
    public string AggregatedOutput { get; init; } = "";

    public string Command { get; init; } = "";

    public IReadOnlyList<CodexCommandAction> CommandActions { get; init; } = [];

    public string Cwd { get; init; } = "";

    public int? DurationMs { get; init; }

    public int? ExitCode { get; init; }

    public string? ProcessId { get; init; }

    public CodexCommandExecutionStatus Status { get; init; }
}

public sealed record CodexFileChangeItem() : CodexThreadItem("fileChange")
{
    public IReadOnlyList<CodexFileUpdateChange> Changes { get; init; } = [];

    public CodexPatchApplyStatus Status { get; init; }
}

public sealed record CodexMcpToolCallItem() : CodexThreadItem("mcpToolCall")
{
    public JsonNode? Arguments { get; init; }

    public int? DurationMs { get; init; }

    public CodexMcpToolCallError? Error { get; init; }

    public string Server { get; init; } = "";

    public CodexMcpToolCallResult? Result { get; init; }

    public CodexMcpToolCallStatus Status { get; init; }

    public string Tool { get; init; } = "";
}

public sealed record CodexDynamicToolCallItem() : CodexThreadItem("dynamicToolCall")
{
    public JsonNode? Arguments { get; init; }

    public IReadOnlyList<CodexDynamicToolCallOutputContentItem>? ContentItems { get; init; }

    public int? DurationMs { get; init; }

    public CodexDynamicToolCallStatus Status { get; init; }

    public bool? Success { get; init; }

    public string Tool { get; init; } = "";
}

public sealed record CodexCollabAgentToolCallItem() : CodexThreadItem("collabAgentToolCall")
{
    public IReadOnlyDictionary<string, CodexCollabAgentState> AgentsStates { get; init; } =
        new Dictionary<string, CodexCollabAgentState>(StringComparer.Ordinal);

    public string? Model { get; init; }

    public string? Prompt { get; init; }

    public CodexReasoningEffort? ReasoningEffort { get; init; }

    public IReadOnlyList<string> ReceiverThreadIds { get; init; } = [];

    public string SenderThreadId { get; init; } = "";

    public CodexCollabAgentToolCallStatus Status { get; init; }

    public CodexCollabAgentTool Tool { get; init; }
}

public sealed record CodexWebSearchItem() : CodexThreadItem("webSearch")
{
    public CodexWebSearchAction? Action { get; init; }

    public string Query { get; init; } = "";
}

public sealed record CodexImageViewItem() : CodexThreadItem("imageView")
{
    public string Path { get; init; } = "";
}

public sealed record CodexImageGenerationItem() : CodexThreadItem("imageGeneration")
{
    public string Result { get; init; } = "";

    public string? RevisedPrompt { get; init; }

    public string Status { get; init; } = "";
}

public sealed record CodexEnteredReviewModeItem() : CodexThreadItem("enteredReviewMode")
{
    public string Review { get; init; } = "";
}

public sealed record CodexExitedReviewModeItem() : CodexThreadItem("exitedReviewMode")
{
    public string Review { get; init; } = "";
}

public sealed record CodexContextCompactionItem() : CodexThreadItem("contextCompaction");

public sealed record CodexTodoListItem() : CodexThreadItem("todoList")
{
    public IReadOnlyList<CodexTodoItem> Items { get; init; } = [];
}

public sealed record CodexErrorItem() : CodexThreadItem("error")
{
    public string Message { get; init; } = "";
}

public sealed record CodexUnknownThreadItem(string UnknownType) : CodexThreadItem(UnknownType)
{
    public JsonObject? RawPayload { get; init; }
}


