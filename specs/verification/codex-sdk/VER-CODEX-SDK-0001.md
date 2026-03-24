---
artifact_id: VER-CODEX-SDK-0001
artifact_type: verification
title: Codex live file interrogation verification
domain: codex-sdk
status: passed
owner: sdk-platform
verifies:
  - REQ-CODEX-SDK-TRANSPORT-0233
  - REQ-CODEX-SDK-LIFECYCLE-0294
  - REQ-CODEX-SDK-LIFECYCLE-0295
  - REQ-CODEX-SDK-HELPERS-0318
related_artifacts:
  - SPEC-CODEX-SDK
  - SPEC-CODEX-SDK-API
  - SPEC-CODEX-SDK-TRANSPORT
  - SPEC-CODEX-SDK-DI
  - SPEC-CODEX-SDK-STRUCTURE
  - SPEC-CODEX-SDK-LIFECYCLE
  - SPEC-CODEX-SDK-CATALOG
  - SPEC-CODEX-SDK-HELPERS
  - ARC-CODEX-SDK-0001
---

# VER-CODEX-SDK-0001 - Codex live file interrogation verification

## Scope

Verify the live Codex executable against a sample file on disk using the opt-in integration tests.

## Requirements Verified

- REQ-CODEX-SDK-TRANSPORT-0233
- REQ-CODEX-SDK-LIFECYCLE-0294
- REQ-CODEX-SDK-LIFECYCLE-0295
- REQ-CODEX-SDK-HELPERS-0318

## Verification Method

Opt-in live smoke execution against the local `codex` executable.

## Preconditions

- The local `codex` executable is available.
- `CODEX_LIVE_TESTS=1` is set when running the live subset.
- The live tests can create a temporary sample file on disk.

## Procedure or Approach

1. Create a temporary workspace and write a sample text file.
2. Ask Codex a basic question about the file contents through `RunAsync`.
3. Ask Codex a basic question about the same file through `RunStreamedAsync`.
4. Ask Codex a basic question about the same file through `StartTurnAsync`.
5. Confirm the responses match the expected file facts.

## Expected Result

The live Codex executable reads the sample file, streams events, and completes turns successfully.

## Evidence

- `tests/Incursa.OpenAI.Codex.Tests/CodexLiveIntegrationTests.cs`
- `CODEX_LIVE_TESTS=1 dotnet test C:\src\incursa\codex\tests\Incursa.OpenAI.Codex.Tests\Incursa.OpenAI.Codex.Tests.csproj --filter FullyQualifiedName~CodexLiveIntegrationTests -v minimal`

## Status

This `passed` status applies to every requirement listed in `verifies`.

passed

## Related Artifacts

- SPEC-CODEX-SDK
- SPEC-CODEX-SDK-API
- SPEC-CODEX-SDK-TRANSPORT
- SPEC-CODEX-SDK-DI
- SPEC-CODEX-SDK-STRUCTURE
- SPEC-CODEX-SDK-LIFECYCLE
- SPEC-CODEX-SDK-CATALOG
- SPEC-CODEX-SDK-HELPERS
- ARC-CODEX-SDK-0001
