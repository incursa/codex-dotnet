---
artifact_id: SPEC-CODEX-SDK-STRUCTURE
artifact_type: specification
title: Codex .NET SDK Namespace, Construction, and Initialization Requirements
domain: codex-sdk
capability: dotnet-sdk
status: approved
owner: sdk-platform
tags:
  - codex
  - dotnet
  - sdk
  - namespaces
  - initialization
  - public-api
related_artifacts:
  - SPEC-CODEX-SDK
  - SPEC-CODEX-SDK-API
  - SPEC-CODEX-SDK-TRANSPORT
  - SPEC-CODEX-SDK-DI
  - SPEC-CODEX-SDK-CATALOG
  - SPEC-CODEX-SDK-HELPERS
  - ARC-CODEX-SDK-0001
---

# SPEC-CODEX-SDK-STRUCTURE - Codex .NET SDK Namespace, Construction, and Initialization Requirements

## Purpose

Pin down the .NET-specific type layout, package split, construction model, initialization semantics, and public type shapes so the SDK can move from requirements into implementation without ambiguity.

## Scope

This specification covers namespace conventions, assembly split, construction and initialization behavior, cancellation placement, public type mutability, and the visibility of generated protocol models.

It does not redefine the higher-level conversation, transport, or DI requirements already covered in companion specifications.

## Context

The upstream TypeScript and Python SDKs differ in startup style, but the .NET SDK should present a single idiomatic shape to consumers.

The .NET design should keep construction cheap, initialization explicit when desired, and transport details hidden behind a stable public surface.

## Namespaces and Assemblies

## REQ-CODEX-SDK-STRUCTURE-0271 Use a stable Codex namespace root
The SDK MUST expose its public runtime types under the `Incursa.OpenAI.Codex` namespace root.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents/README.md
  - C:/src/incursa/chatkit-dotnet/src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs

## REQ-CODEX-SDK-STRUCTURE-0272 Use a separate namespace for DI extensions
The SDK MUST expose its optional dependency-injection extensions under the `Incursa.OpenAI.Codex.Extensions` namespace root.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents.Extensions/ServiceCollectionExtensions.cs
  - C:/src/incursa/chatkit-dotnet/src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs

## REQ-CODEX-SDK-STRUCTURE-0273 Keep generated protocol models out of the public runtime namespace
The SDK MUST keep generated protocol models internal by default and expose only curated wrapper types in the public `Incursa.OpenAI.Codex` surface.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## Construction and Startup

## REQ-CODEX-SDK-STRUCTURE-0274 Keep client construction side-effect free
The `CodexClient` constructor MUST be side-effect free and avoid starting, connecting to, or initializing the runtime process or stdio transport.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/codex.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-STRUCTURE-0275 Expose explicit initialization
The SDK MUST expose an explicit `InitializeAsync` path so callers can fail fast before the first conversational operation.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-STRUCTURE-0276 Make initialization concurrency-safe
The SDK MUST ensure that concurrent initialization attempts on the same client instance collapse to a single runtime startup.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## Backend Selection

## REQ-CODEX-SDK-STRUCTURE-0277 Default backend selection to Auto
`CodexClientOptions` MUST default backend selection to `Auto`.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/codexOptions.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-STRUCTURE-0278 Prefer the app-server backend when Auto resolves
When backend selection resolves from `Auto`, the SDK MUST prefer the app-server backend when the installed runtime can initialize it successfully.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/README.md
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-STRUCTURE-0279 Fall back to exec for simple runtime paths
When the app-server backend is unavailable and the caller only requests exec-compatible conversation behavior, the SDK SHOULD fall back to the CLI-backed exec transport.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/README.md
  - C:/src/openai/codex/sdk/typescript/src/exec.ts

## Public Type Shapes

## REQ-CODEX-SDK-STRUCTURE-0280 Expose live handles as sealed classes
The SDK MUST expose `CodexClient`, `CodexThread`, and `CodexTurn` as sealed classes rather than as inheritable base classes.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/typescript/src/thread.ts

## REQ-CODEX-SDK-STRUCTURE-0281 Expose polymorphic families as abstract records
The SDK MUST expose `CodexThreadEvent`, `CodexThreadItem`, and `CodexException` as abstract record-style base types with sealed concrete derived types.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/events.ts
  - C:/src/openai/codex/sdk/typescript/src/items.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/errors.py

## REQ-CODEX-SDK-STRUCTURE-0282 Expose options and result objects with a mutable client options class
The SDK MUST expose `CodexClientOptions` as a mutable options class and `CodexThreadOptions`, `CodexTurnOptions`, `CodexRunResult`, and the metadata/result wrapper types as immutable records with init-only properties.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/codexOptions.ts
  - C:/src/openai/codex/sdk/typescript/src/threadOptions.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py

## REQ-CODEX-SDK-STRUCTURE-0283 Represent structured output schema with JSON DOM types
The SDK MUST represent structured output schema inputs with a .NET JSON DOM type rather than with `object` or a custom schema DSL.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/outputSchemaFile.ts
  - C:/src/openai/codex/sdk/typescript/src/thread.ts

## REQ-CODEX-SDK-STRUCTURE-0284 Use cancellation tokens on methods, not on transport options
The SDK MUST expose cancellation through standard `CancellationToken` parameters on public async methods rather than through serializable option objects.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/turnOptions.ts
  - C:/src/openai/codex/sdk/typescript/tests/abort.test.ts

## REQ-CODEX-SDK-STRUCTURE-0285 Keep `Id` readable but SDK-controlled
The SDK MUST expose `Id` on thread and turn handles as a readable property whose mutation is controlled by the SDK rather than by public callers.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
