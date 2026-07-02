# Upstream Parity Review

Status: updates found after the latest Python and TypeScript upstream comparison.

## Current Read

- Upstream repo: `/home/runner/work/codex-dotnet/codex-dotnet/.upstream/openai-codex`
- Reviewed baseline commit: `5e3ee5eddfa5333f2e0b011880abf0cbf92bd295`
- Upstream head commit: `129ea2aaf5fb426d8ba683ee53f290742f41dd31`
- The Python SDK is the primary source of truth for this comparison.
- The TypeScript SDK is still checked because both SDKs live in the same upstream monorepo.

## Compared Paths

### Python SDK

- Tracked path: `sdk/python`
- Baseline commit: `5e3ee5eddfa5333f2e0b011880abf0cbf92bd295`
- Latest commit range: `5e3ee5eddfa5333f2e0b011880abf0cbf92bd295..129ea2aaf5fb426d8ba683ee53f290742f41dd31`
- Commit count: 28
- Changed file count: 71

#### Commit Lines

- ff31ba8d0a Make formatter output quiet on success (#29467)
- 5a67d898a5 Allow ChatGPT accounts without email (#28991)
- 20431d49a0 [sdk/python] Stop advertising HTTP image URLs (#29464)
- 740c4f269d build: run buildifier from just fmt (#28125)
- 94427aaf46 Use uv as Python SDK build backend (#27901)
- c375deaf66 Use dependency groups for Python SDK tooling (#27538)
- 9316acf9b2 [2/4] Add private Python goal operations (#27111)
- 5a0f913426 [1/4] Add Python goal routing foundation (#27110)
- 10b408080a [codex] Pin Python SDK to runtime 0.137.0a4 (#26216)
- bc49677ec8 [codex] Pin Python SDK to glibc-compatible runtime (#25907)
- 747f1003dd [codex] Add comprehensive root formatting check (#25683)
- 281b416c44 Check root Python script formatting in CI (#25165)
- ... (16 more)

#### Changed Files

- sdk/python/README.md
- sdk/python/_runtime_setup.py
- sdk/python/docs/api-reference.md
- sdk/python/docs/faq.md
- sdk/python/docs/getting-started.md
- sdk/python/examples/02_turn_run/async.py
- sdk/python/examples/02_turn_run/sync.py
- sdk/python/examples/03_turn_stream_events/async.py
- sdk/python/examples/03_turn_stream_events/sync.py
- sdk/python/examples/04_models_and_metadata/async.py
- sdk/python/examples/04_models_and_metadata/sync.py
- sdk/python/examples/05_existing_thread/async.py
- ... (59 more)

### TypeScript SDK

- Tracked path: `sdk/typescript`
- Baseline commit: `5e3ee5eddfa5333f2e0b011880abf0cbf92bd295`
- Latest commit range: `5e3ee5eddfa5333f2e0b011880abf0cbf92bd295..129ea2aaf5fb426d8ba683ee53f290742f41dd31`
- Commit count: 3
- Changed file count: 5

#### Commit Lines

- 0b4f86095c sdk: launch packaged Codex runtimes (#23786)
- 6941f5c2c5 [codex] preserve MCP result meta in McpToolCallItemResult (#22946)
- 83decfa300 [codex] Remove unused legacy shell tools (#22246)

#### Changed Files

- sdk/typescript/src/exec.ts
- sdk/typescript/src/items.ts
- sdk/typescript/tests/abort.test.ts
- sdk/typescript/tests/exec.test.ts
- sdk/typescript/tests/responsesProxy.ts

## Next Steps

- Review the Python SDK diff first.
- Apply the matching .NET changes in `src/Incursa.OpenAI.Codex` and `tests/Incursa.OpenAI.Codex.Tests`.
- Refresh `quality/upstream-parity.json` after the .NET implementation is updated.
- Re-run this review once the local branch has caught up.

