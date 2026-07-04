# Upstream Parity Review

Status: current after the latest Python and TypeScript upstream comparison.

## Current Read

- Upstream repo: `C:\shared\src\openai\codex`
- Reviewed baseline commit: `319d03056e9b345fe9d129873c3a808c5df783df`
- Upstream head commit: `319d03056e9b345fe9d129873c3a808c5df783df`
- The Python SDK is the primary source of truth for this comparison.
- The TypeScript SDK is still checked because both SDKs live in the same upstream monorepo.

## Compared Paths

### Python SDK

- Tracked path: `sdk/python`
- Baseline commit: `319d03056e9b345fe9d129873c3a808c5df783df`
- Latest commit range: `319d03056e9b345fe9d129873c3a808c5df783df..319d03056e9b345fe9d129873c3a808c5df783df`
- Commit count: 0
- Changed file count: 0

### TypeScript SDK

- Tracked path: `sdk/typescript`
- Baseline commit: `319d03056e9b345fe9d129873c3a808c5df783df`
- Latest commit range: `319d03056e9b345fe9d129873c3a808c5df783df..319d03056e9b345fe9d129873c3a808c5df783df`
- Commit count: 0
- Changed file count: 0

## Notes

- No upstream SDK changes are pending relative to the recorded baseline.
- The Python SDK account/login additions are covered by .NET API-key login, ChatGPT browser login, ChatGPT device-code login, ChatGPT auth-token handoff, account read, logout, and login cancel/wait handles.
- The Python `TurnResult` collection behavior was rechecked against the existing .NET `CodexRunResult` and `CodexTurnResult` behavior; no response-selection change was needed because commentary-only turns already preserve nullable final responses and final-answer phase selection.
- The TypeScript MCP result metadata update is covered by preserving upstream `_meta` on `CodexMcpToolCallResult.Meta`.
- Keep the Python SDK as the primary review source when the next upstream delta lands.
