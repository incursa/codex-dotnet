---
artifact_id: SPEC-CODEX-SDK-LIFECYCLE
artifact_type: specification
title: Codex .NET SDK Thread and Turn Lifecycle Requirements
domain: codex-sdk
capability: dotnet-sdk
status: approved
owner: sdk-platform
tags:
  - codex
  - dotnet
  - sdk
  - lifecycle
  - threading
  - turn-control
related_artifacts:
  - SPEC-CODEX-SDK
  - SPEC-CODEX-SDK-API
  - SPEC-CODEX-SDK-TRANSPORT
  - SPEC-CODEX-SDK-DI
  - SPEC-CODEX-SDK-STRUCTURE
  - SPEC-CODEX-SDK-CATALOG
  - SPEC-CODEX-SDK-HELPERS
  - ARC-CODEX-SDK-0001
---

# SPEC-CODEX-SDK-LIFECYCLE - Codex .NET SDK Thread and Turn Lifecycle Requirements

## Purpose

Define how thread handles, turn handles, streaming, steering, interruption, and sequential turn reuse should behave in the .NET SDK.

## Scope

This specification covers conversation lifecycle, per-thread state, per-turn state, concurrency rules, and the relationship between convenience run methods and low-level turn handles.

It assumes the namespace, construction, transport, and packaging decisions have already been fixed by the companion specifications.

## Context

The TypeScript SDK models a thread as a reusable conversation handle over CLI execution.

The Python SDK models a thread as a reusable handle over a persistent app-server thread id.

The .NET SDK should preserve both behaviors while making the rules around turn exclusivity and event ordering explicit.

## Thread Lifecycle

## REQ-CODEX-SDK-LIFECYCLE-0286 Treat thread handles as reusable conversation state
The SDK MUST treat `CodexThread` as a reusable conversation state object that can be used for multiple sequential turns.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-LIFECYCLE-0287 Keep thread identity stable after assignment
The SDK MUST keep a thread's identity stable after it is assigned by the runtime.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## REQ-CODEX-SDK-LIFECYCLE-0288 Serialize thread mutations through the transport
The SDK MUST serialize thread mutation operations such as starting a turn, setting a name, and compacting a thread so they do not race each other on the same client instance.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## Turn Lifecycle

## REQ-CODEX-SDK-LIFECYCLE-0289 Treat turns as ephemeral execution scopes
The SDK MUST treat `CodexTurn` as an ephemeral execution scope for one active generation cycle.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## REQ-CODEX-SDK-LIFECYCLE-0290 Enforce single-consumer streaming on a turn
The SDK MUST allow only one active consumer of a turn stream at a time.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-LIFECYCLE-0291 Release the turn consumer slot when iteration ends
The SDK MUST release the active turn consumer slot when stream iteration completes, faults, is canceled, or is disposed.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-LIFECYCLE-0292 Keep streaming events ordered
The SDK MUST deliver streamed turn events in the order they are emitted by the runtime for the active turn.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## Run and Stream Methods

## REQ-CODEX-SDK-LIFECYCLE-0293 Implement `RunAsync` as the convenience path
The SDK MUST implement `CodexThread.RunAsync` as the convenience path that collects the streamed turn into a `CodexRunResult`.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_run.py

## REQ-CODEX-SDK-LIFECYCLE-0294 Implement `RunStreamedAsync` as the progress path
The SDK MUST implement `CodexThread.RunStreamedAsync` as the progress path that exposes every streamed event for the active turn.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0001
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/typescript/README.md

## REQ-CODEX-SDK-LIFECYCLE-0295 Expose `StartTurnAsync` for low-level turn control
The SDK MUST expose `StartTurnAsync` so callers can hold a `CodexTurn` and decide later whether to stream, run, steer, or interrupt it.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0001
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-LIFECYCLE-0296 Keep steering and interruption turn-scoped
The SDK MUST restrict `SteerAsync` and `InterruptAsync` to the turn they were created for.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## Thread Operations

## REQ-CODEX-SDK-LIFECYCLE-0297 Expose read-only thread snapshots
The SDK MUST expose `ReadAsync` as a read-only snapshot operation that does not mutate local thread state.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-LIFECYCLE-0298 Expose thread naming and compaction as thread-scoped operations
The SDK MUST expose `SetNameAsync` and `CompactAsync` as thread-scoped operations that operate on the current thread identity.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-LIFECYCLE-0299 Keep sequential turn reuse deterministic
The SDK MUST allow a thread to be reused for a later turn after the prior turn has completed, failed, or been interrupted.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/tests/run.test.ts
  - C:/src/openai/codex/sdk/typescript/tests/runStreamed.test.ts
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## REQ-CODEX-SDK-LIFECYCLE-0300 Preserve backend-specific state handling
The SDK MUST preserve backend-specific state handling so the CLI transport can replay local history while the app-server transport can reuse a remote thread id.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
