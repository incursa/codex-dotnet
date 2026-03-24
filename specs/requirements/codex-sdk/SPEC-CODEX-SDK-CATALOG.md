---
artifact_id: SPEC-CODEX-SDK-CATALOG
artifact_type: specification
title: Codex .NET SDK Async Public Member Catalog Requirements
domain: codex-sdk
capability: dotnet-sdk
status: approved
owner: sdk-platform
tags:
  - codex
  - dotnet
  - sdk
  - async
  - public-api
  - member-catalog
related_artifacts:
  - SPEC-CODEX-SDK
  - SPEC-CODEX-SDK-API
  - SPEC-CODEX-SDK-TRANSPORT
  - SPEC-CODEX-SDK-DI
  - SPEC-CODEX-SDK-STRUCTURE
  - SPEC-CODEX-SDK-LIFECYCLE
  - SPEC-CODEX-SDK-HELPERS
  - ARC-CODEX-SDK-0001
---

# SPEC-CODEX-SDK-CATALOG - Codex .NET SDK Async Public Member Catalog Requirements

## Purpose

Pin the async-only public member inventory for the Codex .NET SDK so implementation can begin with a concrete class, method, and record catalog.

## Scope

This specification covers the async public runtime surface, the core option and result records, the input/event/item families, and the optional DI extension entry points.

It does not redefine the transport protocol internals or the backend selection rules already covered in companion specifications.

## Context

The TypeScript SDK contributes the CLI-backed conversation shape and the core run-result behavior.

The Python SDK contributes the richer app-server method surface, the typed response wrappers, and the retryable error taxonomy.

The .NET SDK should project those capabilities as idiomatic C# async members without adding synchronous convenience wrappers.

## Async Surface

## REQ-CODEX-SDK-CATALOG-0301 Expose an async-only public runtime surface
The SDK MUST expose only asynchronous public runtime operations and omit synchronous convenience wrappers for any runtime method that has an async counterpart.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## CodexClient

## REQ-CODEX-SDK-CATALOG-0302 Expose `CodexClient` with its async lifecycle and thread methods
The SDK MUST expose `CodexClient` with `Metadata`, `Capabilities`, `InitializeAsync`, `StartThreadAsync`, `ResumeThreadAsync`, `ForkThreadAsync`, `ListThreadsAsync`, `ReadThreadAsync`, `ArchiveThreadAsync`, `UnarchiveThreadAsync`, and `ListModelsAsync`.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/codex.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## CodexThread

## REQ-CODEX-SDK-CATALOG-0303 Expose `CodexThread` with its async conversation methods
The SDK MUST expose `CodexThread` with `Id`, `RunAsync`, `RunStreamedAsync`, `StartTurnAsync`, `ReadAsync`, `SetNameAsync`, and `CompactAsync`.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## CodexTurn

## REQ-CODEX-SDK-CATALOG-0304 Expose `CodexTurn` with its async turn-control methods
The SDK MUST expose `CodexTurn` with `ThreadId`, `Id`, `StreamAsync`, `RunAsync`, `SteerAsync`, and `InterruptAsync`.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py
  - C:/src/openai/codex/sdk/typescript/src/thread.ts

## Options and Queries

## REQ-CODEX-SDK-CATALOG-0305 Expose the async option and query record families
The SDK MUST expose `CodexClientOptions` as a mutable options class and `CodexThreadOptions`, `CodexTurnOptions`, `CodexThreadListOptions`, `CodexThreadReadOptions`, `CodexThreadForkOptions`, and `CodexModelListOptions` as immutable records with init-only properties.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/codexOptions.ts
  - C:/src/openai/codex/sdk/typescript/src/threadOptions.ts
  - C:/src/openai/codex/sdk/typescript/src/turnOptions.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## Inputs and Items

## REQ-CODEX-SDK-CATALOG-0306 Expose the async input, event, and item record families
The SDK MUST expose `CodexInputItem`, `CodexTextInput`, `CodexImageInput`, `CodexLocalImageInput`, `CodexSkillInput`, `CodexMentionInput`, `CodexThreadEvent`, and `CodexThreadItem` as record-based families that preserve wire discriminators and unknown fallback payloads.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/typescript/src/events.ts
  - C:/src/openai/codex/sdk/typescript/src/items.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_inputs.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py

## Results, Metadata, and Capability

## REQ-CODEX-SDK-CATALOG-0307 Expose the async result, metadata, capability, and token-usage records
The SDK MUST expose `CodexRunResult`, `CodexTurnRecord`, `CodexThreadListResult`, `CodexThreadSummary`, `CodexThreadSnapshot`, `CodexModelListResult`, `CodexModel`, `CodexUsage`, `CodexTokenUsageBreakdown`, `CodexThreadError`, `CodexServerInfo`, `CodexRuntimeMetadata`, and `CodexRuntimeCapabilities`.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_run.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py

## Errors and DI

## REQ-CODEX-SDK-CATALOG-0308 Expose the typed async exception hierarchy
The SDK MUST expose `CodexException`, `CodexTransportClosedException`, `CodexJsonRpcException`, `CodexParseException`, `CodexInvalidRequestException`, `CodexMethodNotFoundException`, `CodexInvalidParamsException`, `CodexInternalRpcException`, `CodexServerBusyException`, `CodexRetryLimitExceededException`, and `CodexCapabilityNotSupportedException`.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/errors.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## REQ-CODEX-SDK-CATALOG-0309 Expose async DI extension methods
The optional DI package MUST expose `AddCodex`-style `IServiceCollection` extension methods and register the async-only Codex client surface without exposing transport internals.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents.Extensions/ServiceCollectionExtensions.cs
  - C:/src/incursa/chatkit-dotnet/src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-CATALOG-0310 Expose supporting enum and value-object types
The SDK MUST expose supporting enum and value-object types for backend selection, thread status, session source, git metadata, model availability metadata, model upgrade metadata, input modality, reasoning effort options, and thread sort and source filters.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/threadOptions.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py

## Async Omission

## REQ-CODEX-SDK-CATALOG-0311 Omit synchronous public runtime members
The SDK MUST NOT expose synchronous counterparts on `CodexClient`, `CodexThread`, or `CodexTurn`.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-CATALOG-0312 Use asynchronous disposal for the root client
`CodexClient` MUST use `IAsyncDisposable` rather than `IDisposable`.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
