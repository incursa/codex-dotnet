---
artifact_id: SPEC-CODEX-SDK-HELPERS
artifact_type: specification
title: Codex .NET SDK Supporting Type Inventory Requirements
domain: codex-sdk
capability: dotnet-sdk
status: approved
owner: sdk-platform
tags:
  - codex
  - dotnet
  - sdk
  - public-api
  - object-model
  - helper-types
related_artifacts:
  - SPEC-CODEX-SDK
  - SPEC-CODEX-SDK-API
  - SPEC-CODEX-SDK-TRANSPORT
  - SPEC-CODEX-SDK-DI
  - SPEC-CODEX-SDK-STRUCTURE
  - SPEC-CODEX-SDK-LIFECYCLE
  - SPEC-CODEX-SDK-CATALOG
  - ARC-CODEX-SDK-0001
---

# SPEC-CODEX-SDK-HELPERS - Codex .NET SDK Supporting Type Inventory Requirements

## Purpose

Enumerate the supporting enums, records, and discriminated unions that the Codex .NET SDK needs so the public API can stay strongly typed and map cleanly to both the TypeScript and Python upstream SDKs.

## Scope

This specification covers helper types used by the option records, result records, thread metadata, model metadata, item hierarchies, and turn event hierarchies.

It does not define transport mechanics, DI wiring, or the public root handles themselves; those live in the companion specifications.

## Context

The TypeScript SDK contributes the compact CLI-oriented scalar unions and item helpers.

The Python SDK contributes the richer app-server unions, nested helper records, and the fuller thread/item metadata shapes.

The .NET SDK should surface the same semantic space as idiomatic C# enums and records rather than leaving the helpers as anonymous strings and objects.

## Scalar Helpers

## REQ-CODEX-SDK-HELPERS-0313 Expose the scalar helper enums explicitly
The SDK MUST expose the following supporting enums as public, enum-backed value types:

- `CodexApprovalMode`
- `CodexApprovalsReviewer`
- `CodexBackendSelection`
- `CodexCollabAgentStatus`
- `CodexCollabAgentTool`
- `CodexCollabAgentToolCallStatus`
- `CodexCommandExecutionStatus`
- `CodexDynamicToolCallStatus`
- `CodexInputModality`
- `CodexMcpToolCallStatus`
- `CodexMessagePhase`
- `CodexNetworkAccess`
- `CodexPatchApplyStatus`
- `CodexPatchChangeKind`
- `CodexPersonality`
- `CodexReasoningEffort`
- `CodexReasoningSummary`
- `CodexSandboxMode`
- `CodexServiceTier`
- `CodexSessionSourceKind`
- `CodexSubAgentSourceKind`
- `CodexThreadActiveFlag`
- `CodexThreadSortKey`
- `CodexThreadSourceKind`
- `CodexTurnPlanStepStatus`
- `CodexTurnStatus`
- `CodexWebSearchContextSize`
- `CodexWebSearchMode`

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/threadOptions.ts
  - C:/src/openai/codex/sdk/typescript/src/items.ts
  - C:/src/openai/codex/sdk/typescript/src/events.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py

## Policy, Sandbox, and Session Hierarchies

## REQ-CODEX-SDK-HELPERS-0314 Expose the approval, sandbox, and access policy hierarchies
The SDK MUST expose the following helper record hierarchies:

- `CodexApprovalPolicy`
  - `CodexApprovalModePolicy`
  - `CodexGranularApprovalPolicy`
  - `CodexGranularApprovalRules`
- `CodexSandboxPolicy`
  - `CodexDangerFullAccessSandboxPolicy`
  - `CodexReadOnlySandboxPolicy`
  - `CodexExternalSandboxPolicy`
  - `CodexWorkspaceWriteSandboxPolicy`
- `CodexReadOnlyAccess`
  - `CodexRestrictedReadOnlyAccess`
  - `CodexFullAccessReadOnlyAccess`
- `CodexSessionSource`
  - `CodexSessionSourceValue`
  - `CodexSubAgentSessionSource`
- `CodexSubAgentSource`
  - `CodexThreadSpawnSubAgentSource`
  - `CodexOtherSubAgentSource`
- `CodexThreadSpawn`

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py

## Thread, Turn, and Model Metadata Helpers

## REQ-CODEX-SDK-HELPERS-0315 Expose the thread, turn, session, and model metadata helper records
The SDK MUST expose the following helper records and discriminated unions:

- `CodexThreadStatus`
  - `CodexNotLoadedThreadStatus`
  - `CodexIdleThreadStatus`
  - `CodexSystemErrorThreadStatus`
  - `CodexActiveThreadStatus`
- `CodexGitInfo`
- `CodexModelAvailabilityNux`
- `CodexModelUpgradeInfo`
- `CodexReasoningEffortOption`
- `CodexTurnRecord`
- `CodexThreadSpawnSubAgentSource`
- `CodexOtherSubAgentSource`

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py

## Structured Input and Tool Helpers

## REQ-CODEX-SDK-HELPERS-0316 Expose the structured command, web-search, and tool-call helpers
The SDK MUST expose the following helper records and helper hierarchies used by items and turn metadata:

- `CodexCommandAction`
  - `CodexReadCommandAction`
  - `CodexListFilesCommandAction`
  - `CodexSearchCommandAction`
  - `CodexUnknownCommandAction`
- `CodexWebSearchAction`
  - `CodexSearchWebSearchAction`
  - `CodexOpenPageWebSearchAction`
  - `CodexFindInPageWebSearchAction`
  - `CodexOtherWebSearchAction`
- `CodexDynamicToolCallOutputContentItem`
  - `CodexInputTextDynamicToolCallOutputContentItem`
  - `CodexInputImageDynamicToolCallOutputContentItem`
- `CodexMcpToolCallError`
- `CodexMcpToolCallResult`
- `CodexFileUpdateChange`
- `CodexTodoItem`
- `CodexCollabAgentState`

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/typescript/src/items.ts

## Concrete Thread Item Subtypes

## REQ-CODEX-SDK-HELPERS-0317 Expose the concrete thread item subtype inventory explicitly
The SDK MUST expose the following concrete `CodexThreadItem` subtypes:

- `CodexUserMessageItem`
- `CodexAgentMessageItem`
- `CodexPlanItem`
- `CodexReasoningItem`
- `CodexCommandExecutionItem`
- `CodexFileChangeItem`
- `CodexMcpToolCallItem`
- `CodexDynamicToolCallItem`
- `CodexCollabAgentToolCallItem`
- `CodexWebSearchItem`
- `CodexImageViewItem`
- `CodexImageGenerationItem`
- `CodexEnteredReviewModeItem`
- `CodexExitedReviewModeItem`
- `CodexContextCompactionItem`
- `CodexTodoListItem`
- `CodexErrorItem`

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/items.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py

## Concrete Thread Event Subtypes

## REQ-CODEX-SDK-HELPERS-0318 Expose the concrete thread event subtype inventory explicitly
The SDK MUST expose the following concrete `CodexThreadEvent` subtypes:

- `CodexThreadStartedEvent`
- `CodexTurnStartedEvent`
- `CodexTurnCompletedEvent`
- `CodexTurnFailedEvent`
- `CodexItemStartedEvent`
- `CodexItemUpdatedEvent`
- `CodexItemCompletedEvent`
- `CodexThreadErrorEvent`

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0001
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/events.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py

## Config and Result Helpers

## REQ-CODEX-SDK-HELPERS-0319 Expose the config, usage, and turn-result helpers explicitly
The SDK MUST expose the following helper records that support the public options and results:

- `CodexConfigValue`
- `CodexConfigObject`
- `CodexUsage`
- `CodexTokenUsageBreakdown`
- `CodexThreadError`
- `CodexTurnError`

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/codexOptions.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
