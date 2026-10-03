# Upstream parity review

Status: reviewed against the pinned upstream SDK commit on 2026-10-02. The
review records implemented high-level parity and open differences separately;
it does not claim complete parity with the generated protocol schema.

## Review inputs

- Upstream repository: [`github.com/openai/codex`](https://github.com/openai/codex)
- Reviewed checkout: commit `86a54b051c08f34f373c507ae16a91915ab08700` with tracked paths `sdk/python` and `sdk/typescript`
- Python path: `sdk/python` (40 changed files since the baseline)
- TypeScript path: `sdk/typescript` (18 changed files since the baseline)
- Baseline commit: `319d03056e9b345fe9d129873c3a808c5df783df`
- Reviewed head: [`86a54b051c08f34f373c507ae16a91915ab08700`](https://github.com/openai/codex/commit/86a54b051c08f34f373c507ae16a91915ab08700)
- Commit range: 3,872 commits from the baseline to the reviewed head

The Python SDK is the primary source for the generated v2 contract. The
TypeScript SDK is checked as the second public SDK in the same monorepo.

## Implemented in the .NET public surface

The current .NET package has corresponding high-level operations for:

| Upstream capability | .NET surface | Evidence |
| --- | --- | --- |
| Client construction, disposal, metadata, and runtime initialization | `CodexClient` | `CodexClient.cs` |
| API-key, ChatGPT, device-code, account read, logout, and login cancellation | `CodexClient` | `AppServerTransport.cs` |
| Thread start/list/resume/fork/archive/unarchive and model listing | `CodexClient` | `CodexClient.cs` |
| Thread run/turn/read/name/compact | `CodexThread` | `CodexClient.cs` |
| Turn stream/run/steer/interrupt | `CodexTurn` | `CodexClient.cs`, `TurnExecutionTypes.cs` |
| External tool or application context | `CodexExternalMessageInput` | `ConversationTypes.cs`, `CodexProtocol.cs` |
| Nested config overrides | `CodexConfigObject` | `CodexConfigSerialization.cs` |
| Maximum reasoning effort | `CodexReasoningEffort.Max` | `Enums.cs`, `CodexProtocol.cs` |
| Ultra and persistent reasoning effort values | `CodexReasoningEffort.Ultra`, `CodexReasoningEffort.Persistent` | `Enums.cs`, `CodexProtocol.cs`, parity surface tests |
| Thread history inclusion on resume/fork/read | `IncludeTurns` mapped to the current `excludeTurns` wire field | `Options.cs`, `CodexProtocol.cs`, protocol tests |
| Per-turn service-tier override | `CodexTurnOptions.ServiceTierForTurn` | `Options.cs`, `CodexProtocol.cs`, runtime behavior tests |
| Per-turn source attribution | `CodexTurnOptions.TurnTrigger` mapped to `turnTrigger` | `Options.cs`, `CodexProtocol.cs`, transport tests |
| Thread-list section filtering | `CodexThreadListOptions.SectionId` | `Options.cs`, `CodexProtocol.cs`, parity surface tests |
| Raw CLI config overrides | `CodexClientOptions.RawConfigOverrides` | `Options.cs`, `ExecTransport.cs`, runtime behavior tests |
| Cyber-access turn selection | `CodexCyberAccessProgram` and turn option mapping | `Enums.cs`, `CodexProtocol.cs`, runtime behavior tests |
| Cache-write token accounting | `CodexTokenUsageBreakdown.CacheWriteInputTokens` | `CoreTypes.cs`, runtime behavior tests |
| Joined-turn subscription and history replay | App-server turn-session groups fan out events and replay history to attached handles | `AppServerTransport.cs`, transport tests |
| Current app-server v2 approval and thread sandbox wire shapes | `CodexApprovalPolicy`, `CodexSandboxPolicy` | `CodexProtocol.cs`, transport tests |

The latest protocol migration is covered by focused tests for string approval
policies, snake_case granular approval fields, thread sandbox mode values,
derived network access, and rejection of unrepresentable thread sandbox shapes.

## Residual differences

These upstream additions remain outside the current .NET public surface or need
wire-level follow-up:

| Upstream addition | Current .NET state | Consumer impact |
| --- | --- | --- |
| Expanded generated thread, turn, and list schema | Not modeled in the high-level .NET contracts | Use the preserved raw payloads where available; generated-schema parity is not a release claim. |
| Python deprecated personality semantics and `supports_personality` behavior | Legacy .NET personality fields remain for compatibility | Treat personality as compatibility metadata; do not rely on it to select model tone. |

The public enum still contains `CodexApprovalMode.OnFailure` for source
compatibility. Current app-server v2 does not accept that mode; new app-server
callers should use `Never`, `OnRequest`, or `Untrusted`. The exec backend can
still pass `on-failure` through its CLI configuration when the installed Codex
runtime accepts that value. The current upstream Python approval helper exposes
`auto_review` and `deny_all` rather than a portable on-failure mode.

## Review rule

Update `quality/upstream-parity.json` and this document only after an upstream
capability is mapped to implementation and focused test evidence. Advance the
reviewed commit when the next upstream checkout is available, then reassess
each residual item rather than converting baseline movement into a parity claim.
