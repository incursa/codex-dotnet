using System.Text;
using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

/// <summary>
/// Stable category for a normalized Codex turn event.
/// </summary>
public enum CodexTurnEventKind
{
    /// <summary>Low-value execution progress.</summary>
    Progress,

    /// <summary>Meaningful turn activity.</summary>
    Activity,

    /// <summary>Partial streamed assistant text.</summary>
    AssistantDelta,

    /// <summary>Concrete assistant message content.</summary>
    AssistantMessage,

    /// <summary>Final assistant response content.</summary>
    FinalResponse,

    /// <summary>Generated or referenced artifact.</summary>
    Artifact,

    /// <summary>User approval is needed before work can continue.</summary>
    ApprovalNeeded,

    /// <summary>Error or failure information.</summary>
    Error,

    /// <summary>The turn reached a terminal, incomplete, or lost state.</summary>
    Terminal,
}

/// <summary>
/// Relative importance for a normalized Codex turn event.
/// </summary>
public enum CodexTurnEventImportance
{
    /// <summary>Noise that most user interfaces can hide by default.</summary>
    Low,

    /// <summary>Normal turn activity.</summary>
    Normal,

    /// <summary>Important activity or output.</summary>
    High,

    /// <summary>Critical failure or user action required.</summary>
    Critical,
}

/// <summary>
/// Terminal state inferred from the Codex terminal event or stream closeout.
/// </summary>
public enum CodexTurnTerminalState
{
    /// <summary>No terminal state has been observed yet.</summary>
    None,

    /// <summary>Codex reported that the turn completed successfully.</summary>
    Completed,

    /// <summary>Codex reported that the turn failed.</summary>
    Failed,

    /// <summary>Codex reported that the turn was interrupted.</summary>
    Interrupted,

    /// <summary>The event stream ended before a Codex terminal event was observed.</summary>
    Incomplete,

    /// <summary>The terminal state was not recognized.</summary>
    Unknown,
}

/// <summary>
/// Source used to populate a turn's final response.
/// </summary>
public enum CodexFinalResponseSource
{
    /// <summary>No final response text was captured.</summary>
    None,

    /// <summary>The final response came from the terminal turn event.</summary>
    TerminalEvent,

    /// <summary>The final response came from a completed assistant message item before the terminal event.</summary>
    CompletedItem,

    /// <summary>The final response came from accumulated assistant text deltas.</summary>
    AssistantDelta,
}

/// <summary>
/// Summary of an artifact referenced or produced during a turn.
/// </summary>
public sealed record CodexTurnArtifactSummary
{
    /// <summary>Gets or sets the artifact item identifier.</summary>
    public string Id { get; init; } = "";

    /// <summary>Gets or sets the raw Codex item type.</summary>
    public string Type { get; init; } = "";

    /// <summary>Gets or sets the artifact path, if one was reported.</summary>
    public string? Path { get; init; }

    /// <summary>Gets or sets the artifact result value, if one was reported.</summary>
    public string? Result { get; init; }

    /// <summary>Gets or sets the artifact status, if one was reported.</summary>
    public string? Status { get; init; }
}

/// <summary>
/// Normalized turn event suitable for UI, tracing, and diagnostics clients.
/// </summary>
public sealed record CodexTurnEvent
{
    /// <summary>Gets or sets the monotonically increasing event sequence number for this turn.</summary>
    public long SequenceNumber { get; init; }

    /// <summary>Gets or sets the project or workspace identifier, when available.</summary>
    public string? ProjectId { get; init; }

    /// <summary>Gets or sets the working directory associated with the turn, when available.</summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>Gets or sets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets or sets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets or sets the raw Codex event type.</summary>
    public string RawEventType { get; init; } = "";

    /// <summary>Gets or sets the stable normalized event kind.</summary>
    public CodexTurnEventKind Kind { get; init; }

    /// <summary>Gets or sets the event importance.</summary>
    public CodexTurnEventImportance Importance { get; init; }

    /// <summary>Gets or sets when the SDK normalized this event.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Gets or sets the short event title, when applicable.</summary>
    public string? Title { get; init; }

    /// <summary>Gets or sets the event body text, when applicable.</summary>
    public string? Text { get; init; }

    /// <summary>Gets or sets structured event metadata.</summary>
    public IReadOnlyDictionary<string, string?> Metadata { get; init; } = new Dictionary<string, string?>(StringComparer.Ordinal);

    /// <summary>Gets or sets a value indicating whether this event represents turn closeout.</summary>
    public bool IsTerminal { get; init; }

    /// <summary>Gets or sets the terminal state represented by this event, if any.</summary>
    public CodexTurnTerminalState TerminalState { get; init; }

    /// <summary>Gets or sets a value indicating whether this event contributes to assistant output capture.</summary>
    public bool ContributesToFinalOutput { get; init; }

    /// <summary>Gets or sets a value indicating whether a UI can present this event by default.</summary>
    public bool IsUserVisibleByDefault { get; init; }
}

/// <summary>
/// Detailed result for a Codex turn after the event stream ends.
/// </summary>
public sealed record CodexTurnResult
{
    /// <summary>Gets or sets the project or workspace identifier, when available.</summary>
    public string? ProjectId { get; init; }

    /// <summary>Gets or sets the working directory associated with the turn, when available.</summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>Gets or sets the thread identifier.</summary>
    public string ThreadId { get; init; } = "";

    /// <summary>Gets or sets the turn identifier.</summary>
    public string TurnId { get; init; } = "";

    /// <summary>Gets or sets the normalized terminal state.</summary>
    public CodexTurnTerminalState TerminalState { get; init; }

    /// <summary>Gets or sets the raw Codex turn status, when available.</summary>
    public CodexTurnStatus? TurnStatus { get; init; }

    /// <summary>Gets or sets a value indicating whether a Codex terminal event was observed.</summary>
    public bool TerminalEventSeen { get; init; }

    /// <summary>Gets or sets the raw terminal event type, when one was observed.</summary>
    public string? TerminalEventType { get; init; }

    /// <summary>Gets or sets the final assistant response text, when captured.</summary>
    public string? FinalResponseText { get; init; }

    /// <summary>Gets or sets the source used to populate the final response.</summary>
    public CodexFinalResponseSource FinalResponseSource { get; init; }

    /// <summary>Gets or sets a value indicating whether the SDK saw a complete final response source.</summary>
    public bool FinalResponseComplete { get; init; }

    /// <summary>Gets or sets the terminal or stream error message, when available.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Gets or sets when the SDK started observing the turn.</summary>
    public DateTimeOffset StartedUtc { get; init; }

    /// <summary>Gets or sets when the SDK observed turn closeout or stream end.</summary>
    public DateTimeOffset? CompletedUtc { get; init; }

    /// <summary>Gets or sets the raw Codex event count observed by the SDK.</summary>
    public int RawEventCount { get; init; }

    /// <summary>Gets or sets the normalized Codex event count produced by the SDK.</summary>
    public int NormalizedEventCount { get; init; }

    /// <summary>Gets or sets the number of assistant output characters observed from messages and deltas.</summary>
    public int AssistantOutputCharCount { get; init; }

    /// <summary>Gets or sets the final response character count.</summary>
    public int FinalResponseCharCount { get; init; }

    /// <summary>Gets or sets artifact summaries captured during the turn.</summary>
    public IReadOnlyList<CodexTurnArtifactSummary> Artifacts { get; init; } = [];

    /// <summary>Gets or sets the final thread items captured for the turn.</summary>
    public IReadOnlyList<CodexThreadItem> Items { get; init; } = [];

    /// <summary>Gets or sets token usage reported for the turn, when available.</summary>
    public CodexUsage? Usage { get; init; }

    /// <summary>Gets or sets the diagnostics trace identifier, when supplied by a host application.</summary>
    public string? DiagnosticsTraceId { get; init; }
}

internal sealed class CodexTurnOutcomeBuilder
{
    private const string SyntheticStreamEndedType = "turn.stream.ended";
    private const string SyntheticStreamFailedType = "turn.stream.failed";
    private readonly Func<DateTimeOffset> _clock;
    private readonly List<CodexThreadItem> _items = [];
    private readonly List<CodexTurnArtifactSummary> _artifacts = [];
    private readonly StringBuilder _assistantDeltas = new();
    private string _threadId;
    private string _turnId;
    private string? _finalResponseText;
    private CodexFinalResponseSource _finalResponseSource;
    private string? _lastEmittedFinalResponseText;
    private CodexTurnTerminalState _terminalState;
    private CodexTurnStatus? _turnStatus;
    private string? _terminalEventType;
    private string? _errorMessage;
    private CodexUsage? _usage;
    private DateTimeOffset? _completedUtc;
    private long _sequenceNumber;

    public CodexTurnOutcomeBuilder(
        string? threadId,
        string? turnId,
        string? workingDirectory,
        Func<DateTimeOffset>? clock = null,
        string? projectId = null,
        string? diagnosticsTraceId = null)
    {
        _threadId = string.IsNullOrWhiteSpace(threadId) ? string.Empty : threadId!;
        _turnId = string.IsNullOrWhiteSpace(turnId) ? string.Empty : turnId!;
        WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? null : workingDirectory;
        ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId;
        DiagnosticsTraceId = string.IsNullOrWhiteSpace(diagnosticsTraceId) ? null : diagnosticsTraceId;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        StartedUtc = _clock();
    }

    public string? ProjectId { get; }

    public string? WorkingDirectory { get; }

    public string? DiagnosticsTraceId { get; }

    public DateTimeOffset StartedUtc { get; }

    public int RawEventCount { get; private set; }

    public int NormalizedEventCount { get; private set; }

    public bool TerminalEventSeen { get; private set; }

    public IReadOnlyList<CodexTurnEvent> Process(CodexThreadEvent evt)
    {
        RawEventCount++;
        DateTimeOffset timestamp = _clock();
        List<CodexTurnEvent> events = [];

        switch (evt)
        {
            case CodexThreadStartedEvent threadStarted:
                BindThreadId(threadStarted.Thread.Id);
                AddEvent(events, evt.Type, CodexTurnEventKind.Activity, CodexTurnEventImportance.Normal, "Thread started", threadStarted.Thread.Name ?? threadStarted.Thread.Id, timestamp, isUserVisibleByDefault: false);
                break;

            case CodexTurnStartedEvent turnStarted:
                ApplyTurnRecord(turnStarted.Turn);
                _terminalState = CodexTurnTerminalState.None;
                AddEvent(events, evt.Type, CodexTurnEventKind.Activity, CodexTurnEventImportance.Normal, "Turn started", turnStarted.Turn.Id, timestamp, Metadata("status", turnStarted.Turn.Status.ToString()), isUserVisibleByDefault: false);
                break;

            case CodexAgentMessageDeltaEvent agentDelta:
                BindThreadId(agentDelta.ThreadId);
                BindTurnId(agentDelta.TurnId);
                RecordAssistantDelta(agentDelta.Delta);
                AddEvent(events, evt.Type, CodexTurnEventKind.AssistantDelta, CodexTurnEventImportance.High, "Assistant response delta", agentDelta.Delta, timestamp, Metadata("itemId", agentDelta.ItemId), contributesToFinalOutput: true, isUserVisibleByDefault: true);
                break;

            case CodexItemCompletedEvent completed:
                BindThreadId(completed.ThreadId);
                BindTurnId(completed.TurnId);
                _items.Add(completed.Item);
                AddItemCompletedEvents(events, evt.Type, completed.Item, timestamp);
                break;

            case CodexItemStartedEvent started:
                BindThreadId(started.ThreadId);
                BindTurnId(started.TurnId);
                AddEvent(events, evt.Type, CodexTurnEventKind.Progress, CodexTurnEventImportance.Low, DescribeItemTitle("Item started", started.Item), DescribeItemText(started.Item), timestamp, DescribeItemMetadata(started.Item), isUserVisibleByDefault: false);
                break;

            case CodexItemUpdatedEvent updated:
                BindThreadId(updated.ThreadId);
                BindTurnId(updated.TurnId);
                AddEvent(events, evt.Type, CodexTurnEventKind.Progress, CodexTurnEventImportance.Low, DescribeItemTitle("Item updated", updated.Item), DescribeItemText(updated.Item), timestamp, DescribeItemMetadata(updated.Item), isUserVisibleByDefault: false);
                break;

            case CodexTurnCompletedEvent completedTurn:
                ApplyTerminalTurn(evt.Type, completedTurn.Turn, timestamp, events);
                break;

            case CodexTurnFailedEvent failedTurn:
                ApplyTerminalTurn(evt.Type, failedTurn.Turn, timestamp, events);
                break;

            case CodexThreadErrorEvent threadError:
                BindThreadId(threadError.ThreadId);
                BindTurnId(threadError.TurnId);
                _errorMessage = threadError.Error.Message;
                AddEvent(events, evt.Type, CodexTurnEventKind.Error, CodexTurnEventImportance.Critical, "Thread error", threadError.Error.Message, timestamp, Metadata("willRetry", threadError.WillRetry.ToString()), isUserVisibleByDefault: true);
                break;

            case CodexTurnPlanUpdatedEvent planUpdated:
                BindThreadId(planUpdated.ThreadId);
                BindTurnId(planUpdated.TurnId);
                AddEvent(events, evt.Type, CodexTurnEventKind.Activity, CodexTurnEventImportance.Normal, "Plan updated", planUpdated.Explanation, timestamp, Metadata("planStepCount", planUpdated.Plan.Count.ToString()), isUserVisibleByDefault: true);
                break;

            case CodexPlanDeltaEvent planDelta:
                BindThreadId(planDelta.ThreadId);
                BindTurnId(planDelta.TurnId);
                AddEvent(events, evt.Type, CodexTurnEventKind.Activity, CodexTurnEventImportance.Normal, "Plan delta", planDelta.Delta, timestamp, Metadata("itemId", planDelta.ItemId), isUserVisibleByDefault: false);
                break;

            case CodexThreadCompactedEvent compacted:
                BindThreadId(compacted.ThreadId);
                BindTurnId(compacted.TurnId);
                AddEvent(events, evt.Type, CodexTurnEventKind.Activity, CodexTurnEventImportance.High, "Thread compacted", null, timestamp, isUserVisibleByDefault: true);
                break;

            case CodexUnknownThreadEvent unknown when IsUnknownAgentMessageDelta(unknown, out string? delta):
                BindThreadId(GetString(unknown.RawPayload, "threadId"));
                BindTurnId(GetString(unknown.RawPayload, "turnId"));
                RecordAssistantDelta(delta);
                AddEvent(events, unknown.UnknownType, CodexTurnEventKind.AssistantDelta, CodexTurnEventImportance.High, "Assistant response delta", delta, timestamp, contributesToFinalOutput: true, isUserVisibleByDefault: true);
                break;

            case CodexUnknownThreadEvent unknown when IsApprovalRequestType(unknown.UnknownType):
                BindThreadId(GetString(unknown.RawPayload, "threadId"));
                BindTurnId(GetString(unknown.RawPayload, "turnId"));
                AddEvent(events, unknown.UnknownType, CodexTurnEventKind.ApprovalNeeded, CodexTurnEventImportance.Critical, "Approval needed", null, timestamp, isUserVisibleByDefault: true);
                break;

            default:
                AddEvent(events, evt.Type, CodexTurnEventKind.Progress, CodexTurnEventImportance.Low, evt.Type, null, timestamp, isUserVisibleByDefault: false);
                break;
        }

        return events;
    }

    public IReadOnlyList<CodexTurnEvent> CompleteStream()
    {
        if (TerminalEventSeen)
        {
            return [];
        }

        List<CodexTurnEvent> events = [];
        DateTimeOffset timestamp = _clock();
        _completedUtc = timestamp;
        _terminalState = CodexTurnTerminalState.Incomplete;
        ResolveFallbackFinalResponse(finalResponseComplete: false, events, timestamp);
        AddEvent(
            events,
            SyntheticStreamEndedType,
            CodexTurnEventKind.Terminal,
            CodexTurnEventImportance.Critical,
            "Turn stream ended without a terminal event",
            "The SDK did not observe turn.completed or turn.failed.",
            timestamp,
            isTerminal: true,
            terminalState: CodexTurnTerminalState.Incomplete,
            isUserVisibleByDefault: true);
        return events;
    }

    public void RecordStreamException(Exception exception)
    {
        if (TerminalEventSeen)
        {
            _errorMessage ??= exception.Message;
            return;
        }

        _completedUtc = _clock();
        _terminalState = CodexTurnTerminalState.Incomplete;
        _terminalEventType = SyntheticStreamFailedType;
        _errorMessage = exception.Message;
    }

    public CodexTurnResult ToResult()
    {
        ResolveFallbackFinalResponse(finalResponseComplete: TerminalEventSeen, events: null, timestamp: _completedUtc ?? _clock());
        return new CodexTurnResult
        {
            ProjectId = ProjectId,
            WorkingDirectory = WorkingDirectory,
            ThreadId = _threadId,
            TurnId = _turnId,
            TerminalState = _terminalState,
            TurnStatus = _turnStatus,
            TerminalEventSeen = TerminalEventSeen,
            TerminalEventType = _terminalEventType,
            FinalResponseText = _finalResponseText,
            FinalResponseSource = _finalResponseSource,
            FinalResponseComplete = !string.IsNullOrWhiteSpace(_finalResponseText) && TerminalEventSeen,
            ErrorMessage = _errorMessage,
            StartedUtc = StartedUtc,
            CompletedUtc = _completedUtc,
            RawEventCount = RawEventCount,
            NormalizedEventCount = NormalizedEventCount,
            AssistantOutputCharCount = CountAssistantOutputCharacters(),
            FinalResponseCharCount = _finalResponseText?.Length ?? 0,
            Artifacts = _artifacts.ToArray(),
            Items = _items.ToArray(),
            Usage = _usage,
            DiagnosticsTraceId = DiagnosticsTraceId,
        };
    }

    private void ApplyTerminalTurn(string rawEventType, CodexTurnRecord turn, DateTimeOffset timestamp, List<CodexTurnEvent> events)
    {
        ApplyTurnRecord(turn);
        TerminalEventSeen = true;
        _terminalEventType = rawEventType;
        _terminalState = MapTerminalState(turn.Status);
        _completedUtc = timestamp;

        if (turn.Items.Count > 0)
        {
            _items.Clear();
            _items.AddRange(turn.Items);
            AddArtifactSummaries(turn.Items);
        }

        if (!string.IsNullOrWhiteSpace(turn.Error?.Message))
        {
            _errorMessage = turn.Error.Message;
            AddEvent(events, rawEventType, CodexTurnEventKind.Error, CodexTurnEventImportance.Critical, "Turn failed", turn.Error.Message, timestamp, isUserVisibleByDefault: true);
        }

        string? finalResponse = CodexResultHelpers.SelectFinalResponse(_items);
        if (!string.IsNullOrWhiteSpace(finalResponse))
        {
            SetFinalResponse(finalResponse, CodexFinalResponseSource.TerminalEvent, finalResponseComplete: true, events, timestamp);
        }
        else
        {
            ResolveFallbackFinalResponse(finalResponseComplete: true, events, timestamp);
        }

        AddEvent(
            events,
            rawEventType,
            CodexTurnEventKind.Terminal,
            _terminalState == CodexTurnTerminalState.Completed ? CodexTurnEventImportance.High : CodexTurnEventImportance.Critical,
            _terminalState == CodexTurnTerminalState.Completed ? "Turn completed" : "Turn failed",
            _errorMessage,
            timestamp,
            Metadata("status", turn.Status.ToString()),
            isTerminal: true,
            terminalState: _terminalState,
            isUserVisibleByDefault: true);
    }

    private void ApplyTurnRecord(CodexTurnRecord turn)
    {
        BindTurnId(turn.Id);
        _turnStatus = turn.Status;
        _usage = turn.Usage ?? _usage;
        if (!string.IsNullOrWhiteSpace(turn.Error?.Message))
        {
            _errorMessage = turn.Error.Message;
        }
    }

    private void AddItemCompletedEvents(List<CodexTurnEvent> events, string rawEventType, CodexThreadItem item, DateTimeOffset timestamp)
    {
        switch (item)
        {
            case CodexAgentMessageItem agentMessage:
                RecordAssistantMessage(agentMessage.Text);
                if (agentMessage.Phase == CodexMessagePhase.FinalAnswer)
                {
                    SetFinalResponse(agentMessage.Text, CodexFinalResponseSource.CompletedItem, finalResponseComplete: false, events, timestamp);
                }
                else
                {
                    AddEvent(events, rawEventType, CodexTurnEventKind.AssistantMessage, CodexTurnEventImportance.Normal, "Assistant message", agentMessage.Text, timestamp, Metadata("phase", agentMessage.Phase?.ToString()), contributesToFinalOutput: agentMessage.Phase is null, isUserVisibleByDefault: agentMessage.Phase is null);
                }
                break;

            case CodexErrorItem error:
                _errorMessage = error.Message;
                AddEvent(events, rawEventType, CodexTurnEventKind.Error, CodexTurnEventImportance.Critical, "Error", error.Message, timestamp, isUserVisibleByDefault: true);
                break;

            case CodexImageViewItem or CodexImageGenerationItem:
                AddArtifactSummary(item);
                AddEvent(events, rawEventType, CodexTurnEventKind.Artifact, CodexTurnEventImportance.High, DescribeItemTitle("Artifact", item), DescribeItemText(item), timestamp, DescribeItemMetadata(item), isUserVisibleByDefault: true);
                break;

            case CodexFileChangeItem:
                AddArtifactSummary(item);
                AddEvent(events, rawEventType, CodexTurnEventKind.Activity, CodexTurnEventImportance.Normal, DescribeItemTitle("Item completed", item), DescribeItemText(item), timestamp, DescribeItemMetadata(item), isUserVisibleByDefault: false);
                break;

            default:
                AddEvent(events, rawEventType, CodexTurnEventKind.Activity, CodexTurnEventImportance.Normal, DescribeItemTitle("Item completed", item), DescribeItemText(item), timestamp, DescribeItemMetadata(item), isUserVisibleByDefault: false);
                break;
        }
    }

    private void SetFinalResponse(
        string? text,
        CodexFinalResponseSource source,
        bool finalResponseComplete,
        List<CodexTurnEvent>? events,
        DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string normalized = text.Trim();
        _finalResponseText = normalized;
        _finalResponseSource = source;

        if (events is not null && !string.Equals(_lastEmittedFinalResponseText, normalized, StringComparison.Ordinal))
        {
            _lastEmittedFinalResponseText = normalized;
            AddEvent(
                events,
                source == CodexFinalResponseSource.AssistantDelta ? SyntheticStreamEndedType : "turn.finalResponse",
                CodexTurnEventKind.FinalResponse,
                CodexTurnEventImportance.High,
                "Final response",
                normalized,
                timestamp,
                Metadata("source", source.ToString(), "complete", finalResponseComplete.ToString()),
                contributesToFinalOutput: true,
                isUserVisibleByDefault: true);
        }
    }

    private void ResolveFallbackFinalResponse(bool finalResponseComplete, List<CodexTurnEvent>? events, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(_finalResponseText))
        {
            return;
        }

        string? finalResponse = CodexResultHelpers.SelectFinalResponse(_items);
        if (!string.IsNullOrWhiteSpace(finalResponse))
        {
            SetFinalResponse(finalResponse, CodexFinalResponseSource.CompletedItem, finalResponseComplete, events, timestamp);
            return;
        }

        if (_assistantDeltas.Length > 0)
        {
            SetFinalResponse(_assistantDeltas.ToString(), CodexFinalResponseSource.AssistantDelta, finalResponseComplete, events, timestamp);
        }
    }

    private void AddArtifactSummaries(IEnumerable<CodexThreadItem> items)
    {
        foreach (CodexThreadItem item in items)
        {
            AddArtifactSummary(item);
        }
    }

    private void AddArtifactSummary(CodexThreadItem item)
    {
        CodexTurnArtifactSummary? artifact = item switch
        {
            CodexImageViewItem imageView => new CodexTurnArtifactSummary
            {
                Id = imageView.Id,
                Type = imageView.Type,
                Path = imageView.Path,
            },
            CodexImageGenerationItem imageGeneration => new CodexTurnArtifactSummary
            {
                Id = imageGeneration.Id,
                Type = imageGeneration.Type,
                Path = imageGeneration.SavedPath,
                Result = imageGeneration.Result,
                Status = imageGeneration.Status,
            },
            CodexFileChangeItem fileChange => new CodexTurnArtifactSummary
            {
                Id = fileChange.Id,
                Type = fileChange.Type,
                Status = fileChange.Status.ToString(),
            },
            _ => null,
        };

        if (artifact is null || _artifacts.Any(existing => string.Equals(existing.Id, artifact.Id, StringComparison.Ordinal) && string.Equals(existing.Type, artifact.Type, StringComparison.Ordinal)))
        {
            return;
        }

        _artifacts.Add(artifact);
    }

    private void AddEvent(
        List<CodexTurnEvent> events,
        string rawEventType,
        CodexTurnEventKind kind,
        CodexTurnEventImportance importance,
        string? title,
        string? text,
        DateTimeOffset timestamp,
        IReadOnlyDictionary<string, string?>? metadata = null,
        bool isTerminal = false,
        CodexTurnTerminalState terminalState = CodexTurnTerminalState.None,
        bool contributesToFinalOutput = false,
        bool isUserVisibleByDefault = false)
    {
        NormalizedEventCount++;
        events.Add(new CodexTurnEvent
        {
            SequenceNumber = ++_sequenceNumber,
            ProjectId = ProjectId,
            WorkingDirectory = WorkingDirectory,
            ThreadId = _threadId,
            TurnId = _turnId,
            RawEventType = rawEventType,
            Kind = kind,
            Importance = importance,
            Timestamp = timestamp,
            Title = title,
            Text = text,
            Metadata = metadata ?? new Dictionary<string, string?>(StringComparer.Ordinal),
            IsTerminal = isTerminal,
            TerminalState = terminalState,
            ContributesToFinalOutput = contributesToFinalOutput,
            IsUserVisibleByDefault = isUserVisibleByDefault,
        });
    }

    private void BindThreadId(string? threadId)
    {
        if (!string.IsNullOrWhiteSpace(threadId))
        {
            _threadId = threadId;
        }
    }

    private void BindTurnId(string? turnId)
    {
        if (!string.IsNullOrWhiteSpace(turnId))
        {
            _turnId = turnId;
        }
    }

    private void RecordAssistantDelta(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _assistantDeltas.Append(text);
        }
    }

    private void RecordAssistantMessage(string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            _assistantDeltas.Append(text);
        }
    }

    private int CountAssistantOutputCharacters()
        => Math.Max(_assistantDeltas.Length, _finalResponseText?.Length ?? 0);

    private static CodexTurnTerminalState MapTerminalState(CodexTurnStatus status)
        => status switch
        {
            CodexTurnStatus.Completed => CodexTurnTerminalState.Completed,
            CodexTurnStatus.Failed => CodexTurnTerminalState.Failed,
            CodexTurnStatus.Interrupted => CodexTurnTerminalState.Interrupted,
            CodexTurnStatus.InProgress => CodexTurnTerminalState.Incomplete,
            _ => CodexTurnTerminalState.Unknown,
        };

    private static IReadOnlyDictionary<string, string?> Metadata(params string?[] values)
    {
        Dictionary<string, string?> metadata = new(StringComparer.Ordinal);
        for (int index = 0; index + 1 < values.Length; index += 2)
        {
            string? key = values[index];
            if (!string.IsNullOrWhiteSpace(key))
            {
                metadata[key] = values[index + 1];
            }
        }

        return metadata;
    }

    private static string DescribeItemTitle(string prefix, CodexThreadItem item)
        => $"{prefix}: {item.Type}";

    private static string? DescribeItemText(CodexThreadItem item)
        => item switch
        {
            CodexAgentMessageItem agentMessage => agentMessage.Text,
            CodexPlanItem plan => plan.Text,
            CodexCommandExecutionItem command => command.Command,
            CodexMcpToolCallItem mcp => $"{mcp.Server}/{mcp.Tool}",
            CodexDynamicToolCallItem dynamicTool => dynamicTool.Tool,
            CodexWebSearchItem webSearch => webSearch.Query,
            CodexImageViewItem imageView => imageView.Path,
            CodexImageGenerationItem imageGeneration => imageGeneration.SavedPath ?? imageGeneration.Result,
            CodexErrorItem error => error.Message,
            _ => null,
        };

    private static IReadOnlyDictionary<string, string?> DescribeItemMetadata(CodexThreadItem item)
        => item switch
        {
            CodexAgentMessageItem agentMessage => Metadata("itemId", agentMessage.Id, "phase", agentMessage.Phase?.ToString()),
            CodexCommandExecutionItem command => Metadata("itemId", command.Id, "status", command.Status.ToString(), "exitCode", command.ExitCode?.ToString()),
            CodexFileChangeItem fileChange => Metadata("itemId", fileChange.Id, "status", fileChange.Status.ToString(), "changeCount", fileChange.Changes.Count.ToString()),
            CodexMcpToolCallItem mcp => Metadata("itemId", mcp.Id, "server", mcp.Server, "tool", mcp.Tool, "status", mcp.Status.ToString()),
            CodexDynamicToolCallItem dynamicTool => Metadata("itemId", dynamicTool.Id, "tool", dynamicTool.Tool, "status", dynamicTool.Status.ToString()),
            CodexImageViewItem imageView => Metadata("itemId", imageView.Id, "path", imageView.Path),
            CodexImageGenerationItem imageGeneration => Metadata("itemId", imageGeneration.Id, "path", imageGeneration.SavedPath, "result", imageGeneration.Result, "status", imageGeneration.Status),
            _ => Metadata("itemId", item.Id),
        };

    private static bool IsUnknownAgentMessageDelta(CodexUnknownThreadEvent unknown, out string? delta)
    {
        delta = null;
        if (!string.Equals(unknown.UnknownType, "item.agentMessage.delta", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        delta = GetString(unknown.RawPayload, "delta")
            ?? GetString(unknown.RawPayload, "text")
            ?? GetString(GetObject(unknown.RawPayload, "message"), "text");
        return !string.IsNullOrEmpty(delta);
    }

    private static bool IsApprovalRequestType(string type)
        => type.Contains("requestApproval", StringComparison.OrdinalIgnoreCase)
            || type.Contains("approval.required", StringComparison.OrdinalIgnoreCase);

    private static JsonObject? GetObject(JsonObject? payload, string name)
        => payload is not null
            && payload.TryGetPropertyValue(name, out JsonNode? node)
            && node is JsonObject obj
                ? obj
                : null;

    private static string? GetString(JsonObject? payload, string name)
        => payload is not null
            && payload.TryGetPropertyValue(name, out JsonNode? node)
            && node is JsonValue value
            && value.TryGetValue(out string? text)
            && !string.IsNullOrEmpty(text)
                ? text
                : null;
}
