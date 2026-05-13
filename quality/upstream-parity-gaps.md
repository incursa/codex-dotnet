# Upstream Parity Review

Status: current after the latest Python and TypeScript upstream comparison.

## Current Read

- The TypeScript SDK remains the smaller `codex exec` surface. The .NET SDK now covers that launch path plus the structured plan and fast-mode work.
- The Python SDK is the broader app-server surface. The reviewed notification, thread-management, metadata, model, and realtime surfaces are now covered in this branch.

## Covered Surfaces

- Structured turn-plan events: `turn.plan.updated` and `item.plan.delta`
- Fast-mode service-tier mapping
- Typed account and rate-limit plan handling
- Model service-tier metadata and related model fields
- Core thread start/resume/fork/read/archive/unarchive/name/compact/goal flows
- Thread rollback, unsubscribe, metadata update, and shell-command flows
- Thread session-origin metadata and list filters
- Core turn start/steer/interrupt flows
- Thread lifecycle, diagnostics, model, operational, and realtime notifications
- `serverRequest.resolved`
- `item.autoApprovalReview.started` and `item.autoApprovalReview.completed`
- Expanded thread-item parsing for the upstream thread item set
- Thread-item deltas for agent messages, command output, file changes, MCP tool calls, and reasoning
- Thread summary and list metadata for `cwd`, `sessionId`, `forkedFromId`, `source`, `threadSource`, `sessionStartSource`, `backwardsCursor`, `sortDirection`, `useStateDbOnly`, and list `cwd` arrays

## Notes

- The .NET fallback path still preserves unknown thread events and thread items, so the SDK remains usable if upstream adds new shapes later.
- The current local API keeps a backward-compatibility `UpdateMetadataAsync(CodexGitInfo?)` overload, but the patch-wrapper overload now matches the upstream wire shape.
