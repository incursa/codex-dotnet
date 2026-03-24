---
artifact_id: SPEC-CODEX-SDK-TRANSPORT
artifact_type: specification
title: Codex .NET SDK Transport and Protocol Requirements
domain: codex-sdk
capability: dotnet-sdk
status: approved
owner: sdk-platform
tags:
  - codex
  - dotnet
  - sdk
  - transport
  - protocol
  - json-rpc
related_artifacts:
  - SPEC-CODEX-SDK
  - SPEC-CODEX-SDK-API
  - SPEC-CODEX-SDK-DI
  - SPEC-CODEX-SDK-CATALOG
  - SPEC-CODEX-SDK-HELPERS
  - ARC-CODEX-SDK-0001
---

# SPEC-CODEX-SDK-TRANSPORT - Codex .NET SDK Transport and Protocol Requirements

## Purpose

Define how the .NET SDK launches the Codex runtime, serializes requests, routes notifications, handles errors, and bridges the CLI-backed and app-server-backed execution models.

## Scope

This specification covers the backend selection logic, process launch behavior, JSON-RPC and stdio transport, configuration translation, approval handling, output-schema lifecycle, notification preservation, retry behavior, and typed error mapping.

It does not define the public conversation object model, which is covered in the companion public API specification.

## Context

The TypeScript SDK exercises the CLI-backed `codex exec` flow with process-spawned JSONL events.

The Python SDK exercises the app-server JSON-RPC v2 flow with a persistent stdio transport, typed request methods, approval callbacks, and a bounded notification queue.

The .NET SDK should preserve both sets of semantics through an explicit transport abstraction so the public API can remain stable even when the backend changes.

## Backend Selection

## REQ-CODEX-SDK-TRANSPORT-0231 Provide a transport boundary
The SDK MUST provide a transport boundary that lets the high-level client target either the CLI-backed execution path or the app-server JSON-RPC path without duplicating conversation logic.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/async_client.py

## REQ-CODEX-SDK-TRANSPORT-0232 Expose backend capability selection
The SDK MUST expose backend selection and capability information so callers can distinguish exec-style and app-server-style behavior before invoking backend-specific methods.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## CLI Transport

## REQ-CODEX-SDK-TRANSPORT-0233 Launch the CLI backend with the upstream command shape
The CLI transport MUST launch the Codex runtime with the upstream `codex exec --experimental-json` command shape and stream newline-delimited JSON output.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0001
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/typescript/tests/exec.test.ts
  - C:/src/openai/codex/sdk/typescript/README.md

## REQ-CODEX-SDK-TRANSPORT-0234 Translate CLI configuration overrides deterministically
The CLI transport MUST flatten nested configuration overrides into deterministic dotted `--config` assignments and reject unsupported values before dispatch.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/typescript/tests/run.test.ts
  - C:/src/openai/codex/sdk/typescript/README.md

## REQ-CODEX-SDK-TRANSPORT-0235 Preserve CLI argument ordering
The CLI transport MUST place resume arguments before image flags and forward local images as repeated `--image` arguments.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/tests/exec.test.ts
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/typescript/README.md

## REQ-CODEX-SDK-TRANSPORT-0236 Honor CLI environment semantics
The CLI transport MUST apply an explicit environment map without leaking ambient variables when the caller requests isolation, while still injecting SDK-required variables such as the API key and originator marker.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/tests/exec.test.ts
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/typescript/README.md

## REQ-CODEX-SDK-TRANSPORT-0237 Honor cancellation and surface stderr tails
The CLI transport MUST honor cancellation tokens by aborting the child process promptly and include a recent stderr tail in process-exit failures.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/tests/abort.test.ts
  - C:/src/openai/codex/sdk/typescript/tests/exec.test.ts
  - C:/src/openai/codex/sdk/typescript/src/exec.ts

## App-Server Transport

## REQ-CODEX-SDK-TRANSPORT-0238 Launch the app-server backend with the upstream stdio shape
The app-server transport MUST launch `codex app-server --listen stdio://` and communicate over JSON-RPC v2 on stdio.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/async_client.py
  - C:/src/openai/codex/sdk/python/README.md

## REQ-CODEX-SDK-TRANSPORT-0239 Perform initialize and metadata normalization
The app-server transport MUST perform the `initialize` and `initialized` handshake, normalize server metadata, and reject incomplete identity information unless the user-agent can supply the missing pieces.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_signatures.py

## REQ-CODEX-SDK-TRANSPORT-0240 Expose typed request methods for the app-server protocol
The app-server transport MUST expose typed request methods for thread start, thread resume, thread list, thread read, thread fork, thread archive, thread unarchive, thread rename, thread compact, turn start, turn steer, turn interrupt, and model list.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/async_client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py

## REQ-CODEX-SDK-TRANSPORT-0241 Preserve unknown notifications
The app-server transport MUST preserve unknown or invalid notifications as structured fallback payloads rather than discarding them.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/models.py
  - C:/src/openai/codex/sdk/python/tests/test_client_rpc_methods.py

## REQ-CODEX-SDK-TRANSPORT-0242 Enforce single active turn consumer
The app-server transport MUST reject a second active turn consumer on the same client instance until the first consumer finishes or is disposed.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/tests/test_public_api_runtime_behavior.py

## REQ-CODEX-SDK-TRANSPORT-0243 Support approval handling for runtime-initiated requests
The app-server transport MUST allow a configurable approval handler for runtime-initiated requests and return a JSON object response for each handled request.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/errors.py

## Serialization and Schema

## REQ-CODEX-SDK-TRANSPORT-0244 Serialize requests by alias and omission rules
The transport layer MUST serialize request parameters by omitting unset members and applying upstream wire aliases.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/tests/test_client_rpc_methods.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/generated/v2_all.py

## REQ-CODEX-SDK-TRANSPORT-0245 Create and clean up temporary output-schema files
The transport layer MUST create temporary output-schema files under the system temp directory and delete them after the run finishes or fails.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/outputSchemaFile.ts
  - C:/src/openai/codex/sdk/typescript/src/thread.ts
  - C:/src/openai/codex/sdk/typescript/tests/runStreamed.test.ts

## Error Handling and Retry

## REQ-CODEX-SDK-TRANSPORT-0246 Map transport failures into typed exceptions
The SDK MUST translate process failures and JSON-RPC errors into typed exceptions that preserve the underlying code, message, and diagnostic data.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/typescript/src/exec.ts
  - C:/src/openai/codex/sdk/python/src/codex_app_server/errors.py
  - C:/src/openai/codex/sdk/python/examples/10_error_handling_and_retry/async.py

## REQ-CODEX-SDK-TRANSPORT-0247 Classify retryable overload conditions
The SDK MUST classify transient overload conditions so callers can retry them without parsing raw error strings.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/errors.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/retry.py
  - C:/src/openai/codex/sdk/python/examples/10_error_handling_and_retry/async.py

## REQ-CODEX-SDK-TRANSPORT-0248 Provide a bounded retry helper
The SDK SHOULD provide a retry helper or policy for overload-classified operations with exponential backoff and jitter.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0002
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/retry.py
  - C:/src/openai/codex/sdk/python/examples/10_error_handling_and_retry/async.py

## Capability and Compatibility

## REQ-CODEX-SDK-TRANSPORT-0249 Gate unsupported methods by capability
The transport layer MUST expose capability probing for model discovery, thread listing, thread lifecycle, and turn steering, with unsupported methods failing with a capability-oriented exception.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/src/codex_app_server/api.py
  - C:/src/openai/codex/sdk/python/src/codex_app_server/client.py
  - C:/src/openai/codex/sdk/python/docs/api-reference.md

## REQ-CODEX-SDK-TRANSPORT-0250 Keep direct OpenAI API usage out of the transport layer
The transport layer MUST not route Codex runtime operations through the official OpenAI .NET library or any direct OpenAI API client.

Trace:
- Satisfied By:
  - ARC-CODEX-SDK-0001
- Verified By:
  - VER-CODEX-SDK-0003
- Source Refs:
  - C:/src/openai/codex/sdk/python/README.md
  - C:/src/openai/codex/sdk/typescript/README.md
  - C:/src/incursa/chatkit-dotnet/README.md
