# Upstream Parity Review

Status: updates found after the latest Python and TypeScript upstream comparison.

## Current Read

- Upstream repo: `/home/runner/work/codex-dotnet/codex-dotnet/.upstream/openai-codex`
- Reviewed baseline commit: `319d03056e9b345fe9d129873c3a808c5df783df`
- Upstream head commit: `ed2f985a26eee9a59cde0fdefd20f69b45bc25f5`
- The Python SDK is the primary source of truth for this comparison.
- The TypeScript SDK is still checked because both SDKs live in the same upstream monorepo.

## Compared Paths

### Python SDK

- Tracked path: `sdk/python`
- Baseline commit: `319d03056e9b345fe9d129873c3a808c5df783df`
- Latest commit range: `319d03056e9b345fe9d129873c3a808c5df783df..ed2f985a26eee9a59cde0fdefd20f69b45bc25f5`
- Commit count: 5
- Changed file count: 16

#### Commit Lines

- 61dc1d97f6 Add MCP client conformance regression gates (#36810)
- cf7e9cfe6a Support self-serve Business ProLite accounts (#35785)
- 9970cd706f Support alpha hotfix release versions (#34463)
- 3f74f00295 Prepare Python SDK 0.144.4 stable release (#33213)
- 3f61570044 [codex] bundle code mode host in release packages (#30202)

#### Changed Files

- sdk/python/README.md
- sdk/python/_runtime_setup.py
- sdk/python/docs/api-reference.md
- sdk/python/docs/faq.md
- sdk/python/docs/getting-started.md
- sdk/python/examples/README.md
- sdk/python/pyproject.toml
- sdk/python/release_version.py
- sdk/python/scripts/update_sdk_artifacts.py
- sdk/python/src/openai_codex/generated/notification_registry.py
- sdk/python/src/openai_codex/generated/v2_all.py
- sdk/python/tests/test_artifact_workflow_and_binaries.py
- ... (4 more)

### TypeScript SDK

- Tracked path: `sdk/typescript`
- Baseline commit: `319d03056e9b345fe9d129873c3a808c5df783df`
- Latest commit range: `319d03056e9b345fe9d129873c3a808c5df783df..ed2f985a26eee9a59cde0fdefd20f69b45bc25f5`
- Commit count: 2
- Changed file count: 7

#### Commit Lines

- 61dc1d97f6 Add MCP client conformance regression gates (#36810)
- 2edad72de3 Track prompt cache write token usage (#33454)

#### Changed Files

- sdk/typescript/package.json
- sdk/typescript/samples/basic_streaming.ts
- sdk/typescript/src/events.ts
- sdk/typescript/src/thread.ts
- sdk/typescript/tests/mcpConformance.test.ts
- sdk/typescript/tests/run.test.ts
- sdk/typescript/tests/runStreamed.test.ts

## Next Steps

- Review the Python SDK diff first.
- Apply the matching .NET changes in `src/Incursa.OpenAI.Codex` and `tests/Incursa.OpenAI.Codex.Tests`.
- Refresh `quality/upstream-parity.json` after the .NET implementation is updated.
- Re-run this review once the local branch has caught up.

