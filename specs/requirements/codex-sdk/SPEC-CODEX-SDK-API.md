---
artifact_id: SPEC-CODEX-SDK-API
artifact_type: specification
title: Codex .NET SDK Public API and Object Model Requirements
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
related_artifacts:
  - SPEC-CODEX-SDK
  - SPEC-CODEX-SDK-TRANSPORT
  - SPEC-CODEX-SDK-DI
  - SPEC-CODEX-SDK-CATALOG
  - SPEC-CODEX-SDK-HELPERS
  - ARC-CODEX-SDK-0001
---

# SPEC-CODEX-SDK-API - Codex .NET SDK Public API and Object Model Requirements

## Purpose

Define the idiomatic C# public surface for Codex conversations, turns, inputs, results, events, errors, and capability metadata.

## Scope

This specification covers the public client, thread, turn, event, item, options, result, and exception types that a .NET consumer should see.

It does not define the transport process model or the package registration surface, which are covered in companion documents.

## Context

The TypeScript SDK contributes the CLI-backed `Thread.run()` and `Thread.runStreamed()` conversation model.

The Python SDK contributes the richer app-server `Codex`/`Thread`/`TurnHandle` model, the final-response selection rules, the expanded input union, and the immutable result and metadata shapes.

The .NET SDK should combine those behaviors in a way that is natural in C#, async-only for runtime operations, and explicit about stateful versus stateless runtime handling.

## CodexClient

## REQ-CODEX-SDK-API-0201 Expose the root client façade
The SDK MUST expose `CodexClient` as the root high-level entry point for conversation lifecycle, thread management, and model discovery.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/README.md
  - C:/src/openai/codex/sdk/python/README.md
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-API-0202 Model live handles and immutable data separately
The SDK MUST model live runtime handles as classes and immutable inputs, events, items, results, and metadata as record-like data types, while keeping `CodexClientOptions` as the mutable configuration exception.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py
  - C:/src/openai/codex/sdk/typescript/src/thread.ts

## REQ-CODEX-SDK-API-0203 Support client disposal
`CodexClient` MUST implement `IAsyncDisposable` so the runtime transport is released when the client is disposed asynchronously.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/typescript/src/exec.ts

## REQ-CODEX-SDK-API-0204 Expose runtime metadata and capabilities
`CodexClient` MUST expose runtime metadata and capability information after startup, and its metadata normalization path recovers server identity from the user-agent when the server omits it.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_signatures.py

## REQ-CODEX-SDK-API-0205 Capture client configuration in options objects
`CodexClientOptions` MUST capture runtime path selection, backend mode, configuration overrides, environment controls, client identity, and approval-handler settings in a mutable, binding-friendly options object.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/codexOptions.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-API-0206 Expose client-level thread and model methods
`CodexClient` MUST expose async methods for starting, resuming, forking, listing, reading, archiving, and unarchiving threads, and for listing models when the backend supports those operations.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/typescript/src/codex.ts

## CodexThread

## REQ-CODEX-SDK-API-0207 Model threads as stateful handles
`CodexThread` MUST be a stateful conversation handle whose `Id` may be null until the first turn binds it, after which the identity must remain stable for the lifetime of the handle.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## REQ-CODEX-SDK-API-0208 Expose thread execution methods
`CodexThread` MUST expose `RunAsync`, `RunStreamedAsync`, `StartTurnAsync`, `ReadAsync`, `SetNameAsync`, and `CompactAsync` methods that match the upstream conversation semantics.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-API-0209 Define run-result selection rules
`CodexRunResult` MUST expose `FinalResponse`, `Items`, and `Usage`, and the final-response selection logic prefers the last completed assistant message with a `final_answer` phase before falling back to the last phase-less assistant message.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_run.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py
  - C:/src/openai/codex/sdk/typescript/src/thread.ts

## CodexTurn

## REQ-CODEX-SDK-API-0210 Model turns as ephemeral single-consumer handles
`CodexTurn` MUST be an ephemeral single-turn handle that exposes `StreamAsync`, `RunAsync`, `SteerAsync`, and `InterruptAsync` methods.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py
  - C:/src/openai/codex/sdk/typescript/src/thread.ts

## REQ-CODEX-SDK-API-0211 Capture per-turn overrides in dedicated options
`CodexTurnOptions` MUST capture per-turn overrides such as structured output schema, model selection, sandbox policy, approval policy, service tier, and reasoning hints, while cancellation flows through standard `CancellationToken` parameters on the public methods.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/turnOptions.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/typescript/src/thread.ts

## Query Options

## REQ-CODEX-SDK-API-0212 Expose thread and model query payloads
The SDK MUST expose immutable query payload types for thread listing, thread reading, thread forking, and model listing.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## Inputs and Items

## REQ-CODEX-SDK-API-0213 Expose the combined Codex input union
The SDK MUST expose the combined Codex input union, including text, remote image, local image, skill, and mention entries.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_inputs.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## REQ-CODEX-SDK-API-0214 Preserve the thread item and event hierarchies
The SDK MUST expose `CodexThreadEvent` and `CodexThreadItem` as polymorphic hierarchies that preserve the upstream discriminators and unknown fallback payloads.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/events.ts
  - C:/src/openai/codex/sdk/typescript/src/items.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py

## Result and Metadata Shapes

## REQ-CODEX-SDK-API-0215 Expose the supporting result and metadata records
The SDK MUST expose `CodexThreadError`, `CodexTokenUsageBreakdown`, `CodexUsage`, `CodexServerInfo`, `CodexRuntimeCapabilities`, `CodexRuntimeMetadata`, `CodexThreadListResult`, `CodexThreadSummary`, `CodexThreadSnapshot`, `CodexModel`, and `CodexModelListResult` as immutable data shapes.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_run.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py

## Errors and Serialization

## REQ-CODEX-SDK-API-0216 Expose a typed exception hierarchy
The SDK MUST expose a `CodexException` hierarchy that preserves process, JSON-RPC, capability, and retryable overload details.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/errors.py
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## REQ-CODEX-SDK-API-0217 Preserve idiomatic C# names and wire aliases
Public members MUST use idiomatic PascalCase names while JSON serialization and deserialization preserve upstream wire aliases such as `threadId`, `turnId`, and `approvalPolicy`.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/tests/test_public_api_signatures.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/typescript/src/index.ts

## REQ-CODEX-SDK-API-0218 Keep async canonical and sync omitted
The SDK MUST NOT provide synchronous convenience wrappers for non-streaming operations.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md
  - C:/src/openai/codex/sdk/typescript/README.md

## REQ-CODEX-SDK-API-0219 Exclude direct OpenAI API dependency
The public API MUST treat direct OpenAI API access as out of scope and must not require the official OpenAI .NET library for Codex runtime operations.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/incursa/chatkit-dotnet/README.md
  - C:/src/incursa/openai-agents-dotnet/README.md
  - C:/src/openai/codex/sdk/python/README.md

## REQ-CODEX-SDK-API-0220 Preserve commentary-only completion semantics
`CodexRunResult` and the streaming event model MUST preserve the upstream rule that commentary-only turns can complete without a final response.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_run.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md
