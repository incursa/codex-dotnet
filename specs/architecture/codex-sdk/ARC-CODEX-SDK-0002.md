---
artifact_id: ARC-CODEX-SDK-0002
artifact_type: architecture
title: Codex .NET SDK Plan Mode Object Model
domain: codex-sdk
status: draft
owner: sdk-platform
satisfies:
  - REQ-CODEX-SDK-API-0202
  - REQ-CODEX-SDK-API-0205
related_artifacts:
  - ARC-CODEX-SDK-0001
  - SPEC-CODEX-SDK
  - SPEC-CODEX-SDK-API
  - SPEC-CODEX-SDK-HELPERS
  - SPEC-CODEX-SDK-TRANSPORT
---

# ARC-CODEX-SDK-0002 - Codex .NET SDK Plan Mode Object Model

## Purpose

Define the first-class Plan mode surface for the Codex .NET SDK as a typed, configuration-oriented object model that stays idiomatic in C#, preserves backend-neutral semantics, and avoids inventing a separate runtime handle hierarchy for a mode that is fundamentally a configuration concern.

## Scope

This design covers the public plan-mode options object, how it is bound and serialized, and how it sits relative to thread-goal handling and turn-level overrides.

It does not introduce a goal-mode API. Goal behavior remains represented by the existing thread goal records and methods on [`CodexThread`](../../src/Incursa.OpenAI.Codex/CodexClient.cs).

It does not define new transport methods or new plan-mode runtime events unless the upstream Codex runtime adds them.

## Context

The SDK already models [`CodexClientOptions`](../../src/Incursa.OpenAI.Codex/Options.cs) as the mutable configuration exception and keeps live handles (`CodexClient`, `CodexThread`, `CodexTurn`) separate from immutable records.

Upstream Codex already recognizes a plan-mode-specific reasoning-effort config key, `plan_mode_reasoning_effort`, in the CLI/config path. A separate plan-mode model override, `plan_mode_model`, has been requested upstream but is not yet a stable SDK surface.

That means the .NET SDK should treat Plan mode as a mode-scoped configuration bag, not as a new transport, client, or interface hierarchy.

Official upstream references:

- [config/mod.rs](https://github.com/openai/codex/blob/main/codex-rs/core/src/config/mod.rs)
- [config.schema.json](https://github.com/openai/codex/blob/main/codex-rs/core/config.schema.json)
- [Feature request: add `plan_mode_model` config override for Plan mode](https://github.com/openai/codex/issues/19343)

## Design Summary

[`CodexClientOptions.PlanMode`](../../src/Incursa.OpenAI.Codex/Options.cs) should remain a nested, binding-friendly options object of sealed-record shape (`CodexPlanModeOptions`).

That record should hold plan-mode-only defaults, starting with `ReasoningEffort`, and any future plan-mode-only keys should be added there instead of creating parallel mode-specific option trees.

The object model should remain intentionally shallow:

- [`CodexClientOptions`](../../src/Incursa.OpenAI.Codex/Options.cs) owns the Plan mode configuration boundary.
- [`CodexConfigSerialization`](../../src/Incursa.OpenAI.Codex/CodexConfigSerialization.cs) maps typed values to config override keys.
- [`CodexExecTransport`](../../src/Incursa.OpenAI.Codex/ExecTransport.cs) and [`CodexAppServerTransport`](../../src/Incursa.OpenAI.Codex/AppServerTransport.cs) apply the same serialized override set when launching or configuring Codex.
- [`CodexThreadOptions`](../../src/Incursa.OpenAI.Codex/Options.cs) and [`CodexTurnOptions`](../../src/Incursa.OpenAI.Codex/Options.cs) remain execution-scoped and do not become Plan mode wrappers.
- Goal behavior remains in [`CodexThread`](../../src/Incursa.OpenAI.Codex/CodexClient.cs) plus the goal methods and [`CodexThreadGoal`](../../src/Incursa.OpenAI.Codex/CoreTypes.cs) record already exposed by the SDK.

This keeps the public API idiomatic C#:

- records for passive data
- classes for live handles
- interfaces only where behavior varies across transports or lifetime boundaries

### Why not a Plan-mode interface?

Plan mode does not introduce polymorphic behavior. It is a passive configuration bag. A sealed record gives binding, cloning, equality, and public surface stability without adding an inheritance tree that would not buy any dispatch flexibility.

### Why not a separate Plan-mode client?

The client already owns runtime selection, config binding, transport startup, and capability discovery. Splitting Plan mode into a separate client would fragment a single runtime into multiple public entry points even though the underlying conversation semantics remain shared.

### Why not a Goal-mode sibling?

Goal mode is not a second configuration bag. The SDK already has explicit goal methods and a `CodexThreadGoal` record, so the right abstraction for goal behavior is thread state and thread methods, not a parallel top-level options object.

## Key Components

- [`CodexClientOptions`](../../src/Incursa.OpenAI.Codex/Options.cs)
- [`CodexPlanModeOptions`](../../src/Incursa.OpenAI.Codex/Options.cs)
- [`CodexConfigSerialization`](../../src/Incursa.OpenAI.Codex/CodexConfigSerialization.cs)
- [`CodexExecTransport`](../../src/Incursa.OpenAI.Codex/ExecTransport.cs)
- [`CodexAppServerTransport`](../../src/Incursa.OpenAI.Codex/AppServerTransport.cs)
- [`CodexThreadOptions`](../../src/Incursa.OpenAI.Codex/Options.cs)
- [`CodexTurnOptions`](../../src/Incursa.OpenAI.Codex/Options.cs)
- [`CodexThread`](../../src/Incursa.OpenAI.Codex/CodexClient.cs)

## Data and State Considerations

`CodexPlanModeOptions` should stay immutable as a record, while `CodexClientOptions.PlanMode` remains a mutable property so configuration binding and manual setup both stay straightforward.

Plan mode should be treated as client/profile-level configuration, not as per-turn runtime state. The SDK should apply its values when it serializes configuration or launches the backend, and then let the normal thread/turn surfaces carry the actual conversation state.

Plan-mode defaults should inherit cleanly:

- `PlanMode == null` means "use existing behavior"
- `ReasoningEffort == null` means "no explicit Plan-mode override"
- backend-specific launch paths should serialize the same effective value if it is present

If upstream Codex adds more plan-only keys later, they should extend the same record unless a materially different lifecycle emerges.

## Edge Cases and Constraints

- Unsupported or unknown plan-mode fields should not be invented on the .NET side just to fill an API gap.
- `plan_mode_reasoning_effort` should serialize consistently across backends so Plan mode behaves the same whether the SDK is driving `exec` or `app-server`.
- `CodexThreadOptions` and `CodexTurnOptions` should not absorb plan-mode defaults unless the runtime introduces a turn-local plan override concept.
- Goal tracking must remain separate from plan-mode configuration so that a thread can have a goal without being in Plan mode and vice versa.
- The SDK should not expose stringly-typed mode selectors when a typed record can describe the supported config cleanly.

## Alternatives Considered

- Separate `CodexPlanClient` or `CodexPlanThread` classes.
  - Rejected because it fragments the runtime model and creates a parallel API surface for what is really a configuration overlay.
- Interface-based plan-mode configuration.
  - Rejected because the object is passive data, not a polymorphic behavior contract.
- Pushing plan-mode settings into `CodexThreadOptions`.
  - Rejected because those options are execution-scoped and would blur the boundary between client/profile defaults and a particular turn or thread start.
- Exposing raw configuration dictionaries only.
  - Rejected because that would undo the point of a typed SDK and make the plan-mode surface harder to discover and validate.

## Risks

- Upstream Codex may add `plan_mode_model` or other plan-only settings later, so the SDK should keep the record open for extension without introducing a second plan-mode tree.
- Desktop and CLI may consume the same config with different fidelity, so the SDK should keep the serialization contract transport-neutral and visible in tests.
- Plan mode and goal mode can be conflated in documentation if the API surface is not explicit about which is configuration and which is thread state.

## Open Questions

- If `plan_mode_model` becomes stable upstream, should it be added as another property on `CodexPlanModeOptions` or as a sibling nested record?
- If mode switching ever becomes an explicit runtime concept in the SDK, should that live on `CodexThread` or a separate mode controller?
- Should plan-mode settings ever be surfaced back in runtime metadata, or remain write-only configuration?
