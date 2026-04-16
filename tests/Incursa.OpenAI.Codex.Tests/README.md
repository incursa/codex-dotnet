# Incursa.OpenAI.Codex.Tests

`Incursa.OpenAI.Codex.Tests` is the xUnit test project for the repository.

## Run

```bash
dotnet test tests/Incursa.OpenAI.Codex.Tests/Incursa.OpenAI.Codex.Tests.csproj -c Release
```

## Test Structure

- Requirement-linked tests should use `Trait("Requirement", "REQ-...")` so traceability can map evidence back to canonical requirement IDs.
- Positive and negative behavioral checks should be labeled with `CoverageType(RequirementCoverageType.Positive)` or `CoverageType(RequirementCoverageType.Negative)` when that helps downstream filtering.
- Property tests should use `[Property]` plus a `Category` trait such as `Trait("Category", "Property")`.
- Fuzz harnesses and benchmarks live in the separate `fuzz/` and `benchmarks/` projects.

## Tooling

- Run `dotnet tool restore` from the repository root before using mutation or fuzz tooling.
- Use `dotnet-stryker` for mutation testing.
- Use `sharpfuzz` for the fuzz harnesses.

See `../../README.md`, `../../fuzz/README.md`, `../../benchmarks/README.md`, and `../../quality/testing-intent.yaml` for the repo-level testing model.
