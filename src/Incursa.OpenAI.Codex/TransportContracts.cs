namespace Incursa.OpenAI.Codex;

// Traceability: REQ-CODEX-SDK-CATALOG-0302, REQ-CODEX-SDK-CATALOG-0303, REQ-CODEX-SDK-CATALOG-0307, REQ-CODEX-SDK-HELPERS-0315, REQ-CODEX-SDK-HELPERS-0319.

internal interface ICodexTransport : IAsyncDisposable
{
    CodexRuntimeCapabilities Capabilities { get; }

    Task<CodexRuntimeMetadata> InitializeAsync(CancellationToken cancellationToken);

    Task<CodexThreadHandleState> StartThreadAsync(CodexThreadOptions? options, CancellationToken cancellationToken);

    Task<CodexThreadHandleState> ResumeThreadAsync(string threadId, CodexThreadOptions? options, CancellationToken cancellationToken);

    Task<CodexThreadHandleState> ForkThreadAsync(string threadId, CodexThreadForkOptions? options, CancellationToken cancellationToken);

    Task<CodexThreadListResult> ListThreadsAsync(CodexThreadListOptions? options, CancellationToken cancellationToken);

    Task<CodexThreadSnapshot> ReadThreadAsync(string threadId, CodexThreadReadOptions? options, CancellationToken cancellationToken);

    Task ArchiveThreadAsync(string threadId, CancellationToken cancellationToken);

    Task<CodexThreadHandleState> UnarchiveThreadAsync(string threadId, CancellationToken cancellationToken);

    Task<CodexModelListResult> ListModelsAsync(CodexModelListOptions? options, CancellationToken cancellationToken);

    Task<CodexThreadSnapshot> SetThreadNameAsync(string threadId, string name, CancellationToken cancellationToken);

    Task CompactThreadAsync(string threadId, CancellationToken cancellationToken);

    Task<CodexTurnSession> StartTurnAsync(
        string? threadId,
        IReadOnlyList<CodexInputItem> input,
        CodexThreadOptions? threadOptions,
        CodexTurnOptions? options,
        CancellationToken cancellationToken);
}

internal sealed record CodexThreadHandleState(CodexThreadSnapshot Snapshot, CodexThreadOptions? Defaults);

internal static class CodexResultHelpers
{
    public static string? SelectFinalResponse(IReadOnlyList<CodexThreadItem> items)
    {
        CodexAgentMessageItem? finalAnswer = items
            .OfType<CodexAgentMessageItem>()
            .LastOrDefault(item => item.Phase == CodexMessagePhase.FinalAnswer && !string.IsNullOrWhiteSpace(item.Text));

        if (finalAnswer is not null)
        {
            return finalAnswer.Text;
        }

        CodexAgentMessageItem? phaseLess = items
            .OfType<CodexAgentMessageItem>()
            .LastOrDefault(item => item.Phase is null && !string.IsNullOrWhiteSpace(item.Text));

        return phaseLess?.Text;
    }

    public static CodexException ToException(CodexTurnRecord turn)
        => turn.Error is null
            ? new CodexInvalidRequestException($"Turn '{turn.Id}' failed without a populated error object.")
            : new CodexInvalidRequestException(turn.Error.Message);
}


