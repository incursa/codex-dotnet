---
artifact_id: SPEC-CODEX-SDK
artifact_type: specification
title: Codex .NET SDK Requirements
domain: codex-sdk
capability: dotnet-sdk
status: approved
owner: sdk-platform
tags:
  - codex
  - dotnet
  - sdk
  - cli
  - app-server
  - json-rpc
related_artifacts:
  - SPEC-CODEX-SDK-API
  - SPEC-CODEX-SDK-TRANSPORT
  - SPEC-CODEX-SDK-DI
  - SPEC-CODEX-SDK-STRUCTURE
  - SPEC-CODEX-SDK-LIFECYCLE
  - SPEC-CODEX-SDK-CATALOG
  - SPEC-CODEX-SDK-HELPERS
  - ARC-CODEX-SDK-0001
  - VER-CODEX-SDK-0002
---

# SPEC-CODEX-SDK - Codex .NET SDK Requirements

## Purpose

Define the requirements for a .NET SDK that can drive both the CLI-backed `codex exec` flow and the app-server JSON-RPC v2 flow while preserving the upstream behavior of the TypeScript and Python SDKs.

This document is the top-level umbrella. The class-by-class public API requirements, transport and protocol requirements, dependency-injection requirements, and architecture live in companion documents in this directory and the architecture folder.

## Scope

This specification covers the public SDK surface, transport launch and shutdown, configuration translation, thread and turn lifecycle, streaming, turn controls, multimodal input normalization, output schema handling, error mapping, retry behavior, generated model fidelity, and package-level governance.

It does not define the Codex runtime itself, model behavior, or the upstream protocol schemas.

## Context

The upstream TypeScript SDK is a CLI wrapper around `codex exec` with thread/run streaming and output-schema support.

The upstream Python SDK is a richer app-server client with typed thread and turn handles, generated wire models, retry helpers, and explicit notification handling.

A .NET SDK that combines both sources needs to preserve the behavior of each surface without silently dropping capabilities that exist in only one upstream SDK.

## REQ-CODEX-SDK-0001 Expose a high-level conversation API
The SDK MUST expose a high-level conversation API for creating new Codex sessions and resuming existing ones.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/README.md
  - C:/src/openai/codex/sdk/python/docs/api-reference.md
  - C:/src/openai/codex/sdk/python/docs/getting-started.md

## REQ-CODEX-SDK-0002 Expose a lower-level transport API
The SDK MUST expose a lower-level transport API that can issue protocol requests and notifications against the underlying Codex runtime.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/async_client.py

## REQ-CODEX-SDK-0003 Provide asynchronous I/O coverage
The SDK MUST provide asynchronous members for every operation that can block on process or network I/O.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/async_client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-0004 Omit synchronous convenience wrappers
The SDK MUST NOT provide synchronous convenience wrappers for the high-level conversation API.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## REQ-CODEX-SDK-0005 Preserve idiomatic .NET names and wire aliases
The SDK MUST keep its public .NET member names idiomatic while serializing and deserializing to the upstream wire aliases.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/tests/test_public_api_signatures.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/typescript/src/index.ts

## REQ-CODEX-SDK-0006 Release runtime resources on asynchronous dispose
The SDK MUST release the underlying runtime process or transport when the client is disposed asynchronously.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/typescript/src/exec.ts

## REQ-CODEX-SDK-0007 Fail fast on launch or initialization failure
The SDK MUST fail fast when the runtime cannot be launched or initialized and close partially initialized resources before surfacing the error.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/typescript/src/codex.ts

## REQ-CODEX-SDK-0008 Expose runtime metadata after startup
The SDK MUST expose runtime metadata such as user-agent, server name, and server version after startup.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_signatures.py

## REQ-CODEX-SDK-0009 Reject incomplete identity metadata
The SDK MUST fail initialization when the runtime omits required identity metadata unless that identity can be reconstructed from the user-agent.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_signatures.py

## REQ-CODEX-SDK-0010 Resolve the runtime path deterministically
The SDK MUST resolve the Codex runtime from an explicit override before falling back to installed runtime discovery.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-0011 Support the upstream launch modes
The SDK MUST support launching the CLI-backed runtime for `exec` workflows and the app-server runtime for JSON-RPC workflows with the upstream command-line switches.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/docs/getting-started.md

## REQ-CODEX-SDK-0012 Isolate caller-supplied environment variables
The SDK MUST apply caller-supplied environment variables to the runtime process without inheriting ambient variables when an explicit environment map is supplied.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/typescript/tests/exec.test.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-0013 Flatten configuration overrides deterministically
The SDK MUST flatten nested configuration overrides into deterministic dotted `--config` assignments and reject unsupported values before dispatch.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/typescript/tests/run.test.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-0014 Honor cancellation requests
The SDK MUST honor cancellation tokens for long-running runtime operations and stop waiting promptly when cancellation is requested.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/typescript/tests/abort.test.ts

## REQ-CODEX-SDK-0015 Translate runtime errors into typed exceptions
The SDK MUST translate runtime process failures and JSON-RPC errors into typed exceptions that preserve the underlying code, message, and diagnostic detail.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/errors.py
  - C:/src/openai/codex/sdk/python/examples/10_error_handling_and_retry/async.py

## REQ-CODEX-SDK-0016 Classify retryable overload conditions
The SDK MUST classify transient overload conditions so callers can retry them without parsing raw error strings.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/errors.py
  - C:/src/openai/codex/sdk/python/examples/10_error_handling_and_retry/async.py

## REQ-CODEX-SDK-0017 Preserve diagnostic tails on failure
The SDK SHOULD retain recent stderr or equivalent diagnostic output and include it in transport failures.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py

## REQ-CODEX-SDK-0018 Allow only one active turn consumer
The SDK MUST reject a second active turn consumer on the same client instance until the first consumer finishes or is disposed.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## REQ-CODEX-SDK-0019 Expose model discovery
The SDK MUST expose model discovery with pagination metadata on the app-server surface.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## REQ-CODEX-SDK-0020 Expose typed runtime request methods
The SDK MUST expose typed request methods for thread start, thread resume, thread list, thread read, thread fork, thread archive, thread unarchive, thread rename, thread compact, turn start, turn steer, turn interrupt, and model list when the underlying runtime supports those operations.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/async_client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-0021 Handle runtime-initiated requests
The SDK MUST allow the host application to answer runtime-initiated requests through a configurable approval handler.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/errors.py

## REQ-CODEX-SDK-0022 Preserve unknown notifications
The SDK MUST preserve unknown or invalid notifications as structured fallback payloads rather than discarding them.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py
  - C:/src/openai/codex/sdk/python/tests/test_client_rpc_methods.py

## REQ-CODEX-SDK-0023 Serialize request parameters by alias
The SDK MUST serialize request parameters by omitting unset members and applying the upstream wire aliases.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/tests/test_client_rpc_methods.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py

## REQ-CODEX-SDK-0030 Distinguish threads from turns
The SDK MUST model a thread as persistent conversation state and a turn as a single execution within that thread.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/README.md
  - C:/src/openai/codex/sdk/python/docs/faq.md
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## REQ-CODEX-SDK-0031 Support start and resume identity
The SDK MUST support starting a new thread, resuming an existing thread by identifier, and preserving the thread identifier across subsequent turns.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/README.md
  - C:/src/openai/codex/sdk/typescript/src/codex.ts
  - C:/src/openai/codex/sdk/python/docs/getting-started.md

## REQ-CODEX-SDK-0032 Support thread management operations
The SDK MUST support listing, reading, forking, archiving, unarchiving, renaming, and compacting threads when the runtime exposes those operations.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py

## REQ-CODEX-SDK-0033 Expose turn handles
The SDK MUST expose turn handles that can stream notifications, steer an active turn, interrupt an active turn, and return the canonical completed turn model.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md
  - C:/src/openai/codex/sdk/python/examples/14_turn_controls/async.py

## REQ-CODEX-SDK-0034 Capture the thread identifier on first use
The SDK MUST populate the thread identifier for newly created conversations from the runtime response or thread-start event as soon as it becomes available.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py

## REQ-CODEX-SDK-0035 Provide a convenience run result
The SDK MUST provide a convenience run path that collects completed items, token usage, and the final assistant text from a turn.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_run.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## REQ-CODEX-SDK-0036 Select the final assistant response deterministically
The convenience run result MUST prefer an explicit final-answer assistant message, otherwise use the last completed assistant message without a phase, and preserve an empty assistant message as empty text.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_run.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## REQ-CODEX-SDK-0037 Surface failed turns consistently
The SDK MUST surface the turn failure error when the runtime reports a failed turn and stop consuming the result stream after that failure is observed.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_run.py
  - C:/src/openai/codex/sdk/python/examples/10_error_handling_and_retry/async.py

## REQ-CODEX-SDK-0038 Expose turn events in runtime order
The SDK MUST expose turn events in runtime order and carry through thread-start, turn-start, item-completed, usage-update, turn-completed, and turn-failed events.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/events.ts
  - C:/src/openai/codex/sdk/typescript/tests/runStreamed.test.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py

## REQ-CODEX-SDK-0039 Complete only on the matching turn
The SDK MUST treat a turn as complete only when the runtime reports completion for the matching turn identifier.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/typescript/src/thread.ts

## REQ-CODEX-SDK-0040 Preserve usage data when present
The SDK MUST preserve token-usage data when the runtime provides it.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/events.ts
  - C:/src/openai/codex/sdk/typescript/tests/run.test.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py

## REQ-CODEX-SDK-0041 Honor working-directory semantics
The SDK MUST honor an explicit working directory or current-directory setting for thread and turn execution and allow bypassing the upstream Git-repository check when that bypass is requested.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/typescript/tests/run.test.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py

## REQ-CODEX-SDK-0042 Support the upstream multimodal input union
The SDK MUST accept text, remote image, local image, skill, and mention inputs on the app-server surface and serialize each input to the correct upstream discriminator.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_inputs.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/examples/07_image_and_text/async.py
  - C:/src/openai/codex/sdk/python/examples/08_local_image_and_text/async.py

## REQ-CODEX-SDK-0043 Merge CLI text segments predictably
The CLI-backed turn path MUST concatenate multiple text segments with blank lines before dispatch and forward local images separately.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/typescript/tests/run.test.ts

## REQ-CODEX-SDK-0044 Treat raw strings as text input
The SDK MUST treat a raw string input as a single text item rather than requiring callers to construct a text object explicitly.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/_inputs.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py
  - C:/src/openai/codex/sdk/typescript/src/thread.ts

## REQ-CODEX-SDK-0045 Accept per-turn output schemas
The SDK MUST accept an output-schema object per turn and reject non-object schemas before the runtime is invoked.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/outputSchemaFile.ts
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## REQ-CODEX-SDK-0046 Manage temporary schema files for CLI execution
The CLI-backed runtime path MUST write output schemas to a temporary file, pass the file path to the runtime, and delete the temporary file after the turn completes or fails.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/outputSchemaFile.ts
  - C:/src/openai/codex/sdk/typescript/tests/run.test.ts

## REQ-CODEX-SDK-0047 Translate CLI options deterministically
The CLI-backed path MUST translate model selection, sandbox mode, working directory, Git-check bypass, reasoning effort, network access, web-search settings, approval policy, base URL, API key, and additional directories into deterministic `codex exec` flags and config overrides.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/typescript/tests/run.test.ts
  - C:/src/openai/codex/sdk/typescript/tests/exec.test.ts

## REQ-CODEX-SDK-0048 Translate app-server options faithfully
The app-server-backed path MUST serialize approval policy, approval reviewer, base instructions, developer instructions, ephemeral flag, model, model provider, personality, sandbox, service tier, effort, summary, and output schema into the upstream JSON-RPC request fields.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## REQ-CODEX-SDK-0049 Expose typed model and policy unions
The SDK MUST expose the upstream reasoning, approval, sandbox, service-tier, thread-status, turn-status, thread-item, and notification domains as strongly typed models or discriminated unions.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/notification_registry.py
  - C:/src/openai/codex/sdk/typescript/src/items.ts
  - C:/src/openai/codex/sdk/typescript/src/events.ts

## REQ-CODEX-SDK-0050 Preserve thread-list filters
The SDK MUST preserve thread-list filters for archived state, cursor, cwd, limit, model providers, search terms, sort keys, and source kinds.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## REQ-CODEX-SDK-0051 Preserve pagination cursors
The SDK MUST preserve pagination cursors on model and thread listing operations so callers can continue result sets without restarting the query.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/typescript/README.md

## REQ-CODEX-SDK-0100 Keep generated artifacts drift-free
The repository MUST keep the checked-in protocol models, notification registries, and API wrappers aligned with the canonical upstream schema or source of truth and keep those checked-in artifacts drift-free.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/tests/test_contract_generation.py
  - C:/src/openai/codex/sdk/python/tests/test_artifact_workflow_and_binaries.py
  - C:/src/openai/codex/sdk/python/scripts/update_sdk_artifacts.py

## REQ-CODEX-SDK-0101 Track the public API surface
The repository MUST track the public .NET API surface with a baseline or analyzer so accidental breaking changes are detected.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/incursa/chatkit-dotnet/src/Incursa.OpenAI.ChatKit/PublicAPI.Shipped.txt
  - C:/src/incursa/chatkit-dotnet/src/Incursa.OpenAI.ChatKit/PublicAPI.Unshipped.txt
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents/PublicAPI.Shipped.txt
  - C:/src/incursa/openai-agents-dotnet/src/Incursa.OpenAI.Agents/PublicAPI.Unshipped.txt

## REQ-CODEX-SDK-0102 Include behavioral tests for the core contract
The repository SHOULD include behavioral tests that cover transport launch, config serialization, turn streaming, error mapping, and notification coercion.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/tests/run.test.ts
  - C:/src/openai/codex/sdk/typescript/tests/runStreamed.test.ts
  - C:/src/openai/codex/sdk/typescript/tests/exec.test.ts
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py
  - C:/src/openai/codex/sdk/python/tests/test_client_rpc_methods.py

## REQ-CODEX-SDK-0103 Include runnable examples for the major flows
The repository SHOULD include runnable examples covering quickstart, streaming, structured output, image input, error handling, and turn controls.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/samples/basic_streaming.ts
  - C:/src/openai/codex/sdk/typescript/samples/structured_output.ts
  - C:/src/openai/codex/sdk/python/examples/README.md
  - C:/src/openai/codex/sdk/python/examples/01_quickstart_constructor/async.py
  - C:/src/openai/codex/sdk/python/examples/03_turn_stream_events/async.py
  - C:/src/openai/codex/sdk/python/examples/07_image_and_text/async.py
  - C:/src/openai/codex/sdk/python/examples/10_error_handling_and_retry/async.py
  - C:/src/openai/codex/sdk/python/examples/14_turn_controls/async.py

## REQ-CODEX-SDK-0104 Pin the expected runtime version
The published package SHOULD pin or otherwise reproducibly identify the runtime version it expects so SDK and runtime releases stay in lockstep.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/README.md
  - C:/src/openai/codex/sdk/python/examples/README.md
  - C:/src/openai/codex/sdk/python/pyproject.toml
  - C:/src/openai/codex/sdk/typescript/package.json

## REQ-CODEX-SDK-0105 Target a supported .NET runtime baseline
The package MUST target a currently supported .NET runtime that provides async streams and cancellation support.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/incursa/chatkit-dotnet/global.json
  - C:/src/incursa/openai-agents-dotnet/global.json

## REQ-CODEX-SDK-0106 Document the backend capability split
The repository SHOULD document which capabilities are provided by the CLI-backed surface and which are provided only by the app-server-backed surface.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/README.md
  - C:/src/openai/codex/sdk/python/docs/api-reference.md
  - C:/src/openai/codex/sdk/python/docs/faq.md


