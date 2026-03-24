---
artifact_id: SPEC-CODEX-SDK-DI
artifact_type: specification
title: Codex .NET SDK Dependency Injection and Packaging Requirements
domain: codex-sdk
capability: dotnet-sdk
status: approved
owner: sdk-platform
tags:
  - codex
  - dotnet
  - sdk
  - dependency-injection
  - packaging
  - public-api
related_artifacts:
  - SPEC-CODEX-SDK
  - SPEC-CODEX-SDK-API
  - SPEC-CODEX-SDK-TRANSPORT
  - SPEC-CODEX-SDK-CATALOG
  - SPEC-CODEX-SDK-HELPERS
  - ARC-CODEX-SDK-0001
---

# SPEC-CODEX-SDK-DI - Codex .NET SDK Dependency Injection and Packaging Requirements

## Purpose

Define the optional DI surface, package split, and public API governance rules for the Codex .NET SDK.

## Scope

This specification covers how the SDK should integrate with `IServiceCollection`, how the core assembly should stay DI-agnostic, how backend-specific concerns should be packaged, and how the public surface should be tracked.

It does not define the transport protocol itself or the public conversation object model, which are covered in companion specifications.

## Context

The translated ChatKit and agents libraries show the common .NET pattern: a core package with concrete runtime types, plus an optional extension package that registers those types through the Microsoft dependency-injection abstractions.

Those projects are reference shapes only. The Codex SDK should borrow the idiom without importing unrelated OpenAI API surface area or cross-library assumptions.

## Core Package Boundary

## REQ-CODEX-SDK-DI-0260 Keep the core package DI-agnostic
The core Codex package MUST remain usable without `Microsoft.Extensions.DependencyInjection` and without any host-level service container.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents/README.md
  - C:/src/incursa/chatkit-dotnet/src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs
  - C:/src/openai/codex/sdk/python/README.md

## REQ-CODEX-SDK-DI-0261 Keep backend and protocol concerns out of the DI contract
The DI surface MUST register the public client and its supporting options without requiring consumers to know about the internal transport implementation classes.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents.Extensions/ServiceCollectionExtensions.cs
  - C:/src/incursa/chatkit-dotnet/src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs

## Service Collection Extensions

## REQ-CODEX-SDK-DI-0262 Expose IServiceCollection extension methods
The SDK MUST provide a dedicated DI extension package with `IServiceCollection` extension methods for registering the Codex client and its options.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents.Extensions/ServiceCollectionExtensions.cs
  - C:/src/incursa/chatkit-dotnet/src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs

## REQ-CODEX-SDK-DI-0263 Support both delegate-based and configuration-based registration
The DI package SHOULD support both `Action<CodexClientOptions>` registration and `IConfiguration` binding for the Codex client options.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents.Extensions/ServiceCollectionExtensions.cs
  - C:/src/incursa/chatkit-dotnet/src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs

## REQ-CODEX-SDK-DI-0264 Register the root client as a long-lived service
The DI package MUST register the root Codex client as a singleton or an equivalent long-lived service so that one runtime instance owns one backend process or connection.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents.Extensions/ServiceCollectionExtensions.cs
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-DI-0265 Delay runtime startup until first use
The DI package MUST not start the Codex runtime during service registration.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents.Extensions/ServiceCollectionExtensions.cs
  - C:/src/incursa/chatkit-dotnet/src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs

## Backend Registration

## REQ-CODEX-SDK-DI-0266 Support explicit backend selection in the DI package
The DI package MUST provide a way to select the CLI-backed or app-server-backed runtime when registering Codex services.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-DI-0267 Register approval and notification collaborators as regular services
The DI package SHOULD register approval handlers, notification observers, and serializer collaborators as normal services so hosts can replace them without custom factories.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents.Extensions/ServiceCollectionExtensions.cs

## Packaging and Governance

## REQ-CODEX-SDK-DI-0268 Split the SDK into clear assemblies
The Codex SDK MUST separate the core runtime package from the optional dependency-injection package so hosts that do not use DI do not pay for it.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/incursa/chatkit-dotnet/src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents.Extensions/ServiceCollectionExtensions.cs

## REQ-CODEX-SDK-DI-0269 Maintain public API baseline files for shipped and unshipped surface area
The SDK MUST maintain public API baseline files for every shipped assembly and treat them as part of the release gate.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents/PublicAPI.Shipped.txt
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents/PublicAPI.Unshipped.txt
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents.Extensions/PublicAPI.Shipped.txt

## REQ-CODEX-SDK-DI-0270 Avoid direct OpenAI API coupling in the package graph
The SDK package graph MUST avoid a hard dependency on the official OpenAI .NET library for Codex runtime operations.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/incursa/chatkit-dotnet/README.md
  - C:/src/incursa/openai-agents-dotnet/README.md
  - C:/src/openai/codex/sdk/python/README.md
