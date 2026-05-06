using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-API-0202, REQ-CODEX-SDK-API-0205, REQ-CODEX-SDK-STRUCTURE-0282, REQ-CODEX-SDK-CATALOG-0305, REQ-CODEX-SDK-HELPERS-0319, REQ-CODEX-SDK-CATALOG-0311, REQ-CODEX-SDK-CATALOG-0312.

/// <summary>
/// Handles a runtime approval request and returns the approval response payload.
/// </summary>
/// <param name="action">The approval action requested by the Codex runtime.</param>
/// <param name="request">The raw approval request payload, when one was supplied.</param>
/// <returns>The response payload to send back to the runtime, or <see langword="null"/> for no payload.</returns>
public delegate JsonObject? CodexApprovalHandler(string action, JsonObject? request);

/// <summary>
/// Represents a value that can be serialized into Codex configuration.
/// </summary>
public abstract record CodexConfigValue;

/// <summary>
/// Represents a string value in Codex configuration.
/// </summary>
/// <param name="Value">The string configuration value.</param>
public sealed record CodexConfigStringValue(string Value) : CodexConfigValue;

/// <summary>
/// Represents a numeric value in Codex configuration.
/// </summary>
/// <param name="Value">The numeric configuration value.</param>
public sealed record CodexConfigNumberValue(double Value) : CodexConfigValue;

/// <summary>
/// Represents a Boolean value in Codex configuration.
/// </summary>
/// <param name="Value">The Boolean configuration value.</param>
public sealed record CodexConfigBooleanValue(bool Value) : CodexConfigValue;

/// <summary>
/// Represents an array value in Codex configuration.
/// </summary>
public sealed record CodexConfigArrayValue : CodexConfigValue
{
    /// <summary>
    /// Gets the ordered configuration values in the array.
    /// </summary>
    public IReadOnlyList<CodexConfigValue> Items { get; init; } = [];
}

/// <summary>
/// Represents an object value in Codex configuration.
/// </summary>
public sealed record CodexConfigObject : CodexConfigValue
{
    /// <summary>
    /// Gets the object properties keyed by configuration name.
    /// </summary>
    public IReadOnlyDictionary<string, CodexConfigValue> Values { get; init; } =
        new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal);
}

/// <summary>
/// Configures defaults used when creating or resuming a Codex thread.
/// </summary>
public record CodexThreadOptions
{
    /// <summary>
    /// Gets the approval policy applied to turns in the thread.
    /// </summary>
    public CodexApprovalPolicy? ApprovalPolicy { get; init; }

    /// <summary>
    /// Gets the reviewer used for approval prompts.
    /// </summary>
    public CodexApprovalsReviewer? ApprovalsReviewer { get; init; }

    /// <summary>
    /// Gets base instructions that seed the thread.
    /// </summary>
    public string? BaseInstructions { get; init; }

    /// <summary>
    /// Gets additional runtime configuration to pass to Codex.
    /// </summary>
    public CodexConfigObject? Config { get; init; }

    /// <summary>
    /// Gets developer instructions that guide the thread.
    /// </summary>
    public string? DeveloperInstructions { get; init; }

    /// <summary>
    /// Gets a value indicating whether the thread should be ephemeral.
    /// </summary>
    public bool? Ephemeral { get; init; }

    /// <summary>
    /// Gets the model identifier to use for the thread.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets the model provider identifier to use for the thread.
    /// </summary>
    public string? ModelProvider { get; init; }

    /// <summary>
    /// Gets the personality setting applied to the thread.
    /// </summary>
    public CodexPersonality? Personality { get; init; }

    /// <summary>
    /// Gets the sandbox policy applied to the thread.
    /// </summary>
    public CodexSandboxPolicy? Sandbox { get; init; }

    /// <summary>
    /// Gets the service tier requested for the thread.
    /// </summary>
    public CodexServiceTier? ServiceTier { get; init; }

    /// <summary>
    /// Gets the working directory used by the thread.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets the service name reported to the Codex runtime.
    /// </summary>
    public string? ServiceName { get; init; }

    /// <summary>
    /// Gets the default reasoning effort used for thread turns.
    /// </summary>
    public CodexReasoningEffort? ModelReasoningEffort { get; init; }

    /// <summary>
    /// Gets a value indicating whether network access should be enabled.
    /// </summary>
    public bool? NetworkAccessEnabled { get; init; }

    /// <summary>
    /// Gets the web-search mode requested for the thread.
    /// </summary>
    public CodexWebSearchMode? WebSearchMode { get; init; }

    /// <summary>
    /// Gets a value indicating whether web search should be enabled.
    /// </summary>
    public bool? WebSearchEnabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether the runtime should skip the Git repository check.
    /// </summary>
    public bool? SkipGitRepoCheck { get; init; }

    /// <summary>
    /// Gets additional directories made available to the thread.
    /// </summary>
    public IReadOnlyList<string>? AdditionalDirectories { get; init; }
}

/// <summary>
/// Configures a request to fork an existing Codex thread.
/// </summary>
public sealed record CodexThreadForkOptions : CodexThreadOptions;

/// <summary>
/// Configures an individual Codex turn.
/// </summary>
public sealed record CodexTurnOptions
{
    /// <summary>
    /// Gets the approval policy applied to this turn.
    /// </summary>
    public CodexApprovalPolicy? ApprovalPolicy { get; init; }

    /// <summary>
    /// Gets the reviewer used for approval prompts during this turn.
    /// </summary>
    public CodexApprovalsReviewer? ApprovalsReviewer { get; init; }

    /// <summary>
    /// Gets the reasoning effort requested for this turn.
    /// </summary>
    public CodexReasoningEffort? Effort { get; init; }

    /// <summary>
    /// Gets the model identifier used for this turn.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets the structured output schema requested for this turn.
    /// </summary>
    public JsonNode? OutputSchema { get; init; }

    /// <summary>
    /// Gets the personality setting applied to this turn.
    /// </summary>
    public CodexPersonality? Personality { get; init; }

    /// <summary>
    /// Gets the sandbox policy applied to this turn.
    /// </summary>
    public CodexSandboxPolicy? SandboxPolicy { get; init; }

    /// <summary>
    /// Gets the service tier requested for this turn.
    /// </summary>
    public CodexServiceTier? ServiceTier { get; init; }

    /// <summary>
    /// Gets the reasoning-summary setting requested for this turn.
    /// </summary>
    public CodexReasoningSummary? Summary { get; init; }

    /// <summary>
    /// Gets the working directory used by this turn.
    /// </summary>
    public string? WorkingDirectory { get; init; }
}

/// <summary>
/// Configures a request to read a Codex thread snapshot.
/// </summary>
public sealed record CodexThreadReadOptions
{
    /// <summary>
    /// Gets a value indicating whether turn records should be included.
    /// </summary>
    public bool IncludeTurns { get; init; }
}

/// <summary>
/// Configures a request to list Codex threads.
/// </summary>
public sealed record CodexThreadListOptions
{
    /// <summary>
    /// Gets an archive filter for listed threads.
    /// </summary>
    public bool? Archived { get; init; }

    /// <summary>
    /// Gets the pagination cursor returned by a previous list request.
    /// </summary>
    public string? Cursor { get; init; }

    /// <summary>
    /// Gets the working-directory filter for listed threads.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets the maximum number of threads to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Gets the model-provider filters for listed threads.
    /// </summary>
    public IReadOnlyList<string>? ModelProviders { get; init; }

    /// <summary>
    /// Gets the search text used to filter listed threads.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Gets the sort key used for listed threads.
    /// </summary>
    public CodexThreadSortKey? SortKey { get; init; }

    /// <summary>
    /// Gets the source-kind filters for listed threads.
    /// </summary>
    public IReadOnlyList<CodexThreadSourceKind>? SourceKinds { get; init; }
}

/// <summary>
/// Configures a request to list available Codex models.
/// </summary>
public sealed record CodexModelListOptions
{
    /// <summary>
    /// Gets a value indicating whether hidden models should be returned.
    /// </summary>
    public bool IncludeHidden { get; init; }
}

/// <summary>
/// Configures the Codex client and the transport it uses to communicate with the runtime.
/// </summary>
public sealed class CodexClientOptions
{
    /// <summary>
    /// Gets or sets the backend transport selected for the client.
    /// </summary>
    public CodexBackendSelection BackendSelection { get; set; } = CodexBackendSelection.AppServer;

    /// <summary>
    /// Gets or sets an explicit path to the Codex executable.
    /// </summary>
    public string? CodexPathOverride { get; set; }

    /// <summary>
    /// Gets or sets the base URL used by the app-server backend.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the API key supplied to the selected backend.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets additional runtime configuration.
    /// </summary>
    public CodexConfigObject? Config { get; set; }

    /// <summary>
    /// Gets or sets environment variables passed to spawned Codex processes.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Environment { get; set; }

    /// <summary>
    /// Gets or sets the machine-readable client name reported to the runtime.
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets the human-readable client title reported to the runtime.
    /// </summary>
    public string? ClientTitle { get; set; }

    /// <summary>
    /// Gets or sets the client version reported to the runtime.
    /// </summary>
    public string? ClientVersion { get; set; }

    /// <summary>
    /// Gets or sets the callback used to handle runtime approval requests.
    /// </summary>
    public CodexApprovalHandler? ApprovalHandler { get; set; }

    internal ICodexProcessLauncher? ProcessLauncher { get; set; }
}

