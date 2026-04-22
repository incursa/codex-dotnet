---
artifact_id: VER-CODEX-SDK-0003
artifact_type: verification
title: Codex SDK release-readiness source and surface verification
domain: codex-sdk
status: passed
owner: sdk-platform
verifies:
  - REQ-CODEX-SDK-0001
  - REQ-CODEX-SDK-0002
  - REQ-CODEX-SDK-0003
  - REQ-CODEX-SDK-0004
  - REQ-CODEX-SDK-0005
  - REQ-CODEX-SDK-0006
  - REQ-CODEX-SDK-0007
  - REQ-CODEX-SDK-0008
  - REQ-CODEX-SDK-0009
  - REQ-CODEX-SDK-0010
  - REQ-CODEX-SDK-0011
  - REQ-CODEX-SDK-0012
  - REQ-CODEX-SDK-0013
  - REQ-CODEX-SDK-0014
  - REQ-CODEX-SDK-0015
  - REQ-CODEX-SDK-0016
  - REQ-CODEX-SDK-0017
  - REQ-CODEX-SDK-0018
  - REQ-CODEX-SDK-0019
  - REQ-CODEX-SDK-0020
  - REQ-CODEX-SDK-0021
  - REQ-CODEX-SDK-0022
  - REQ-CODEX-SDK-0023
  - REQ-CODEX-SDK-0030
  - REQ-CODEX-SDK-0031
  - REQ-CODEX-SDK-0032
  - REQ-CODEX-SDK-0033
  - REQ-CODEX-SDK-0034
  - REQ-CODEX-SDK-0035
  - REQ-CODEX-SDK-0036
  - REQ-CODEX-SDK-0037
  - REQ-CODEX-SDK-0038
  - REQ-CODEX-SDK-0039
  - REQ-CODEX-SDK-0040
  - REQ-CODEX-SDK-0041
  - REQ-CODEX-SDK-0042
  - REQ-CODEX-SDK-0043
  - REQ-CODEX-SDK-0044
  - REQ-CODEX-SDK-0045
  - REQ-CODEX-SDK-0046
  - REQ-CODEX-SDK-0047
  - REQ-CODEX-SDK-0048
  - REQ-CODEX-SDK-0049
  - REQ-CODEX-SDK-0050
  - REQ-CODEX-SDK-0051
  - REQ-CODEX-SDK-0100
  - REQ-CODEX-SDK-0101
  - REQ-CODEX-SDK-0102
  - REQ-CODEX-SDK-0103
  - REQ-CODEX-SDK-0104
  - REQ-CODEX-SDK-0105
  - REQ-CODEX-SDK-0106
  - REQ-CODEX-SDK-API-0216
  - REQ-CODEX-SDK-API-0217
  - REQ-CODEX-SDK-API-0218
  - REQ-CODEX-SDK-API-0219
  - REQ-CODEX-SDK-API-0220
  - REQ-CODEX-SDK-TRANSPORT-0231
  - REQ-CODEX-SDK-TRANSPORT-0232
  - REQ-CODEX-SDK-TRANSPORT-0234
  - REQ-CODEX-SDK-TRANSPORT-0235
  - REQ-CODEX-SDK-TRANSPORT-0236
  - REQ-CODEX-SDK-TRANSPORT-0237
  - REQ-CODEX-SDK-TRANSPORT-0238
  - REQ-CODEX-SDK-TRANSPORT-0239
  - REQ-CODEX-SDK-TRANSPORT-0240
  - REQ-CODEX-SDK-TRANSPORT-0241
  - REQ-CODEX-SDK-TRANSPORT-0242
  - REQ-CODEX-SDK-TRANSPORT-0243
  - REQ-CODEX-SDK-TRANSPORT-0244
  - REQ-CODEX-SDK-TRANSPORT-0245
  - REQ-CODEX-SDK-TRANSPORT-0246
  - REQ-CODEX-SDK-TRANSPORT-0249
  - REQ-CODEX-SDK-TRANSPORT-0250
  - REQ-CODEX-SDK-STRUCTURE-0272
  - REQ-CODEX-SDK-STRUCTURE-0273
  - REQ-CODEX-SDK-CATALOG-0310
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
  - VER-CODEX-SDK-0001
  - VER-CODEX-SDK-0002
---

# VER-CODEX-SDK-0003 - Codex SDK release-readiness source and surface verification

## Scope

Verify the remaining Codex SDK release-readiness requirements through the checked-in source tree, sample app, package baselines, runtime pinning, and the repository test suite.

This artifact complements:

- `VER-CODEX-SDK-0001` for the opt-in live smoke subset
- `VER-CODEX-SDK-0002` for the automated repository test suite

## Requirements Verified

- REQ-CODEX-SDK-0001
- REQ-CODEX-SDK-0002
- REQ-CODEX-SDK-0003
- REQ-CODEX-SDK-0004
- REQ-CODEX-SDK-0005
- REQ-CODEX-SDK-0006
- REQ-CODEX-SDK-0007
- REQ-CODEX-SDK-0008
- REQ-CODEX-SDK-0009
- REQ-CODEX-SDK-0010
- REQ-CODEX-SDK-0011
- REQ-CODEX-SDK-0012
- REQ-CODEX-SDK-0013
- REQ-CODEX-SDK-0014
- REQ-CODEX-SDK-0015
- REQ-CODEX-SDK-0016
- REQ-CODEX-SDK-0017
- REQ-CODEX-SDK-0018
- REQ-CODEX-SDK-0019
- REQ-CODEX-SDK-0020
- REQ-CODEX-SDK-0021
- REQ-CODEX-SDK-0022
- REQ-CODEX-SDK-0023
- REQ-CODEX-SDK-0030
- REQ-CODEX-SDK-0031
- REQ-CODEX-SDK-0032
- REQ-CODEX-SDK-0033
- REQ-CODEX-SDK-0034
- REQ-CODEX-SDK-0035
- REQ-CODEX-SDK-0036
- REQ-CODEX-SDK-0037
- REQ-CODEX-SDK-0038
- REQ-CODEX-SDK-0039
- REQ-CODEX-SDK-0040
- REQ-CODEX-SDK-0041
- REQ-CODEX-SDK-0042
- REQ-CODEX-SDK-0043
- REQ-CODEX-SDK-0044
- REQ-CODEX-SDK-0045
- REQ-CODEX-SDK-0046
- REQ-CODEX-SDK-0047
- REQ-CODEX-SDK-0048
- REQ-CODEX-SDK-0049
- REQ-CODEX-SDK-0050
- REQ-CODEX-SDK-0051
- REQ-CODEX-SDK-0100
- REQ-CODEX-SDK-0101
- REQ-CODEX-SDK-0102
- REQ-CODEX-SDK-0103
- REQ-CODEX-SDK-0104
- REQ-CODEX-SDK-0105
- REQ-CODEX-SDK-0106
- REQ-CODEX-SDK-API-0216
- REQ-CODEX-SDK-API-0217
- REQ-CODEX-SDK-API-0218
- REQ-CODEX-SDK-API-0219
- REQ-CODEX-SDK-API-0220
- REQ-CODEX-SDK-TRANSPORT-0231
- REQ-CODEX-SDK-TRANSPORT-0232
- REQ-CODEX-SDK-TRANSPORT-0234
- REQ-CODEX-SDK-TRANSPORT-0235
- REQ-CODEX-SDK-TRANSPORT-0236
- REQ-CODEX-SDK-TRANSPORT-0237
- REQ-CODEX-SDK-TRANSPORT-0238
- REQ-CODEX-SDK-TRANSPORT-0239
- REQ-CODEX-SDK-TRANSPORT-0240
- REQ-CODEX-SDK-TRANSPORT-0241
- REQ-CODEX-SDK-TRANSPORT-0242
- REQ-CODEX-SDK-TRANSPORT-0243
- REQ-CODEX-SDK-TRANSPORT-0244
- REQ-CODEX-SDK-TRANSPORT-0245
- REQ-CODEX-SDK-TRANSPORT-0246
- REQ-CODEX-SDK-TRANSPORT-0249
- REQ-CODEX-SDK-TRANSPORT-0250
- REQ-CODEX-SDK-STRUCTURE-0272
- REQ-CODEX-SDK-STRUCTURE-0273
- REQ-CODEX-SDK-CATALOG-0310

## Verification Method

Mixed static and runtime verification:

- source review of the checked-in SDK implementation, sample app, package baselines, and docs
- repository test suite execution
- opt-in live smoke execution for the live-backed subset, when enabled

## Preconditions

- The checked-in source tree is available.
- The .NET 10 SDK from `global.json` is available.
- The local `codex` executable is available for live smoke execution.

## Procedure or Approach

1. Review the checked-in implementation files for the core SDK and DI extensions.
2. Review the runnable sample app and README instructions for the major flows.
3. Review the public API baseline files, the snapshot test that maintains them, and the release script that consumes them to select the semver bump.
4. Confirm the runtime pin and target framework baseline are recorded in the repository.
5. Review `quality/upstream-parity.json` to confirm the last reviewed Python and TypeScript upstream commits are recorded for parity follow-up work.
6. Run the repository test suite.
7. Run the live smoke subset only when `CODEX_LIVE_TESTS=1` is explicitly enabled.

## Expected Result

The checked-in repository state, sample app, package baselines, runtime pin, and test suite together satisfy the remaining release-readiness requirements.

## Evidence

- `C:/src/incursa/codex/tests/Incursa.OpenAI.Codex.Tests/PublicApiSnapshotTests.cs`
- `C:/src/incursa/codex/tests/Incursa.OpenAI.Codex.Tests/CodexAsyncSurfaceTests.cs`
- `C:/src/incursa/codex/tests/Incursa.OpenAI.Codex.Tests/CodexHelperTypeTests.cs`
- `C:/src/incursa/codex/tests/Incursa.OpenAI.Codex.Tests/CodexProtocolTests.cs`
- `C:/src/incursa/codex/tests/Incursa.OpenAI.Codex.Tests/CodexRuntimeBehaviorTests.cs`
- `C:/src/incursa/codex/tests/Incursa.OpenAI.Codex.Tests/CodexDiagnosticsTests.cs`
- `C:/src/incursa/codex/tests/Incursa.OpenAI.Codex.Tests/CodexServiceCollectionExtensionsTests.cs`
- `C:/src/incursa/codex/tests/Incursa.OpenAI.Codex.Tests/CodexTurnSessionTests.cs`
- `C:/src/incursa/codex/tests/Incursa.OpenAI.Codex.Tests/CodexLiveIntegrationTests.cs`
- `C:/src/incursa/codex/tests/Incursa.OpenAI.Codex.Tests/CodexSampleFlowLiveTests.cs`
- `C:/src/incursa/codex/tests/Incursa.OpenAI.Codex.Tests/SampleProgramTests.cs`
- `C:/src/incursa/codex/scripts/release.ps1`
- `C:/src/incursa/codex-dotnet/quality/upstream-parity.json`
- `C:/src/incursa/codex/AGENTS.md`
- `C:/src/incursa/codex/samples/Incursa.OpenAI.Codex.Sample/Program.cs`
- `C:/src/incursa/codex/samples/Incursa.OpenAI.Codex.Sample/Incursa.OpenAI.Codex.Sample.csproj`
- `C:/src/incursa/codex/samples/Incursa.OpenAI.Codex.Sample/README.md`
- `C:/src/incursa/codex/README.md`
- `C:/src/incursa/codex/docs/sample-modes.md`
- `C:/src/incursa/codex/src/Incursa.OpenAI.Codex/README.md`
- `C:/src/incursa/codex/src/Incursa.OpenAI.Codex.Extensions/README.md`
- `C:/src/incursa/codex/src/Incursa.OpenAI.Codex/PublicAPI.Shipped.txt`
- `C:/src/incursa/codex/src/Incursa.OpenAI.Codex/PublicAPI.Unshipped.txt`
- `C:/src/incursa/codex/src/Incursa.OpenAI.Codex.Extensions/PublicAPI.Shipped.txt`
- `C:/src/incursa/codex/src/Incursa.OpenAI.Codex.Extensions/PublicAPI.Unshipped.txt`
- `C:/src/incursa/codex/global.json`
- `C:/src/incursa/codex/Incursa.OpenAI.Codex.slnx`
- `C:/src/incursa/codex/specs/requirements/codex-sdk/TRACEABILITY.md`

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
- VER-CODEX-SDK-0001
- VER-CODEX-SDK-0002
