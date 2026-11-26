# Upstream parity policy

The upstream Python and TypeScript Codex SDKs inform this package's high-level behavior. This repository claims parity only for capabilities that map to a maintained .NET API and focused test evidence; it does not claim exhaustive parity with generated protocol schemas.

## Review inputs

- Upstream repository: [`github.com/openai/codex`](https://github.com/openai/codex)
- Primary behavioral inputs: the public Python and TypeScript SDK surfaces
- Local machine-readable record: [`upstream-parity.json`](upstream-parity.json)
- Review command: `scripts/Invoke-UpstreamParityReview.ps1`

The machine-readable record may pin revisions so a review can be reproduced. Do not copy those revision identifiers, commit counts, or one-run outcomes into evergreen documentation.

## Supported high-level surface

The .NET package provides maintained high-level operations for:

- client construction, disposal, metadata, and runtime initialization;
- account login, logout, cancellation, and account inspection;
- thread start, list, resume, fork, archive, unarchive, and model listing;
- turn run, streaming, steer, interrupt, history, and external-message handling;
- nested configuration, reasoning effort, service-tier, source-attribution, and section filtering;
- raw CLI configuration overrides and preserved raw payload access where documented; and
- app-server approval and sandbox policies that the installed runtime can represent.

The public API and focused tests are authoritative for exact names and wire mappings.

## Deliberate limitations

- Expanded generated thread, turn, and list schemas are not fully modeled in the high-level .NET contracts. Use preserved raw payloads where available.
- Legacy personality fields remain compatibility metadata and should not be used to select model tone.
- `CodexApprovalMode.OnFailure` remains for source compatibility. App-server callers should use a mode supported by their installed runtime; the exec backend may pass CLI-specific values through configuration.

## Review rule

Update [`upstream-parity.json`](upstream-parity.json) and focused tests when upstream behavior changes. Update this policy only when the supported boundary or review method changes. Point-in-time findings belong in the review artifact, work item, or pull request.
