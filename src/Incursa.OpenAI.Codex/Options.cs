using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-API-0202, REQ-CODEX-SDK-API-0205, REQ-CODEX-SDK-STRUCTURE-0282, REQ-CODEX-SDK-CATALOG-0305, REQ-CODEX-SDK-HELPERS-0319, REQ-CODEX-SDK-CATALOG-0311, REQ-CODEX-SDK-CATALOG-0312.

public delegate JsonObject? CodexApprovalHandler(string action, JsonObject? request);

public abstract record CodexConfigValue;

public sealed record CodexConfigStringValue(string Value) : CodexConfigValue;

public sealed record CodexConfigNumberValue(double Value) : CodexConfigValue;

public sealed record CodexConfigBooleanValue(bool Value) : CodexConfigValue;

public sealed record CodexConfigArrayValue : CodexConfigValue
{
    public IReadOnlyList<CodexConfigValue> Items { get; init; } = [];
}

public sealed record CodexConfigObject : CodexConfigValue
{
    public IReadOnlyDictionary<string, CodexConfigValue> Values { get; init; } =
        new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal);
}

public record CodexThreadOptions
{
    public CodexApprovalPolicy? ApprovalPolicy { get; init; }

    public CodexApprovalsReviewer? ApprovalsReviewer { get; init; }

    public string? BaseInstructions { get; init; }

    public CodexConfigObject? Config { get; init; }

    public string? DeveloperInstructions { get; init; }

    public bool? Ephemeral { get; init; }

    public string? Model { get; init; }

    public string? ModelProvider { get; init; }

    public CodexPersonality? Personality { get; init; }

    public CodexSandboxPolicy? Sandbox { get; init; }

    public CodexServiceTier? ServiceTier { get; init; }

    public string? WorkingDirectory { get; init; }

    public string? ServiceName { get; init; }

    public CodexReasoningEffort? ModelReasoningEffort { get; init; }

    public bool? NetworkAccessEnabled { get; init; }

    public CodexWebSearchMode? WebSearchMode { get; init; }

    public bool? WebSearchEnabled { get; init; }

    public bool? SkipGitRepoCheck { get; init; }

    public IReadOnlyList<string>? AdditionalDirectories { get; init; }
}

public sealed record CodexThreadForkOptions : CodexThreadOptions;

public sealed record CodexTurnOptions
{
    public CodexApprovalPolicy? ApprovalPolicy { get; init; }

    public CodexApprovalsReviewer? ApprovalsReviewer { get; init; }

    public CodexReasoningEffort? Effort { get; init; }

    public string? Model { get; init; }

    public JsonNode? OutputSchema { get; init; }

    public CodexPersonality? Personality { get; init; }

    public CodexSandboxPolicy? SandboxPolicy { get; init; }

    public CodexServiceTier? ServiceTier { get; init; }

    public CodexReasoningSummary? Summary { get; init; }

    public string? WorkingDirectory { get; init; }
}

public sealed record CodexThreadReadOptions
{
    public bool IncludeTurns { get; init; }
}

public sealed record CodexThreadListOptions
{
    public bool? Archived { get; init; }

    public string? Cursor { get; init; }

    public string? WorkingDirectory { get; init; }

    public int? Limit { get; init; }

    public IReadOnlyList<string>? ModelProviders { get; init; }

    public string? SearchTerm { get; init; }

    public CodexThreadSortKey? SortKey { get; init; }

    public IReadOnlyList<CodexThreadSourceKind>? SourceKinds { get; init; }
}

public sealed record CodexModelListOptions
{
    public bool IncludeHidden { get; init; }
}

public sealed class CodexClientOptions
{
    public CodexBackendSelection BackendSelection { get; set; } = CodexBackendSelection.AppServer;

    public string? CodexPathOverride { get; set; }

    public string? BaseUrl { get; set; }

    public string? ApiKey { get; set; }

    public CodexConfigObject? Config { get; set; }

    public IReadOnlyDictionary<string, string>? Environment { get; set; }

    public string? ClientName { get; set; }

    public string? ClientTitle { get; set; }

    public string? ClientVersion { get; set; }

    public CodexApprovalHandler? ApprovalHandler { get; set; }

    internal ICodexProcessLauncher? ProcessLauncher { get; set; }
}


