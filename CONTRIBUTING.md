# Contributing

This repository is a .NET SDK for orchestrating a local Codex runtime. Contributions should keep that scope clear: async .NET APIs, typed Codex subprocess contracts, stable public API baselines, and repository-native validation.

## Before You Start

1. Read [README.md](README.md) for the current support boundary.
2. Read [docs/usage-guide.md](docs/usage-guide.md) before changing client behavior, transport behavior, event mapping, or result handling.
3. Read [src/Incursa.OpenAI.Codex/README.md](src/Incursa.OpenAI.Codex/README.md) and [src/Incursa.OpenAI.Codex.Extensions/README.md](src/Incursa.OpenAI.Codex.Extensions/README.md) before changing package-facing APIs.
4. Check [tests/Incursa.OpenAI.Codex.Tests/README.md](tests/Incursa.OpenAI.Codex.Tests/README.md) for the relevant automated gate.
5. Follow [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) in project spaces.

## Contributor Agreement

All pull requests and code-bearing issue comments are accepted only under [CONTRIBUTOR-AGREEMENT.md](CONTRIBUTOR-AGREEMENT.md). Do not submit a contribution unless you can make the copyright assignment and grants described there.

If you are contributing on behalf of an employer or another organization, confirm that you have authority before opening the pull request. For corporate contribution questions, contact oss@incursa.com.

Pull requests are checked by the `Contributor Agreement` workflow. If the workflow asks you to sign, read the agreement and comment exactly:

```text
I have read the Incursa Contributor Agreement and I hereby assign my contribution rights as described.
```

Maintainers can re-run the check by commenting `recheck contributor agreement`. The automation setup is documented in [docs/contributor-agreement-automation.md](docs/contributor-agreement-automation.md).

## Local Setup

Required tools:

1. .NET SDK matching [global.json](global.json).
2. A local `codex` executable if you want to run live integration checks.
3. Local Codex authentication for any live test that talks to the real runtime.

Normal source workflow:

```powershell
dotnet restore Incursa.OpenAI.Codex.slnx
dotnet build Incursa.OpenAI.Codex.slnx -c Release
dotnet test Incursa.OpenAI.Codex.slnx -c Release --no-build
```

Run the sample app from source:

```powershell
dotnet run --project samples\Incursa.OpenAI.Codex.Sample -- --help
dotnet run --project samples\Incursa.OpenAI.Codex.Sample -- --mode quickstart --prompt "Summarize this repository."
```

Keep real local settings in user secrets, environment variables, or another secret store. Do not commit OpenAI keys, Codex auth state, private transcripts, local repository paths, or sample output copied from private repositories.

## Public API Baselines

This repository uses public API baseline files to make surface changes explicit.

If you intentionally change public SDK types or members, refresh the baseline and keep `PublicApiSnapshotTests` green:

```powershell
$env:UPDATE_PUBLIC_API_BASELINES = '1'
dotnet test tests\Incursa.OpenAI.Codex.Tests\Incursa.OpenAI.Codex.Tests.csproj -c Release --filter FullyQualifiedName~PublicApiSnapshotTests
Remove-Item Env:UPDATE_PUBLIC_API_BASELINES
```

Review the resulting `PublicAPI.Shipped.txt` changes before committing. Do not widen the public API accidentally to make tests pass.

## Validation

Run the smallest relevant validation while developing, then run the full local gate before handing off a release candidate.

Fast local checks:

```powershell
dotnet build Incursa.OpenAI.Codex.slnx -c Release
dotnet test tests\Incursa.OpenAI.Codex.Tests\Incursa.OpenAI.Codex.Tests.csproj -c Release --no-build
```

Formatter cleanup is tracked separately from the normal build/test gate. `dotnet format Incursa.OpenAI.Codex.slnx --verify-no-changes --no-restore` reports existing analyzer and formatting findings across source and tests; do not mix that broad cleanup into an unrelated feature, documentation, or release-prep change.

Full package-oriented gate:

```powershell
dotnet test Incursa.OpenAI.Codex.slnx -c Release --no-build -m:1
dotnet pack src\Incursa.OpenAI.Codex\Incursa.OpenAI.Codex.csproj -c Release --no-build
dotnet pack src\Incursa.OpenAI.Codex.Extensions\Incursa.OpenAI.Codex.Extensions.csproj -c Release --no-build
```

Live Codex integration checks are skipped unless explicitly enabled. Record the Codex CLI version, OS, backend selection, and any skipped live checks when claiming runtime behavior.

## Release Workflow

Cut releases with [scripts/release.ps1](scripts/release.ps1). Do not hand-edit the release tag or version when that script is available.

The release script is the semver source of truth. It compares the current public API baseline files against the latest `v*.*.*` tag, updates `Directory.Build.props`, runs the Release test suite, commits, tags, and pushes.

## Pull Request Expectations

Good pull requests include:

1. A focused change with a clear SDK, package, or maintainer-facing impact.
2. Tests or explicit evidence for behavior changes.
3. Documentation updates when setup, samples, public APIs, requirements, security posture, or release workflow changes.
4. Public API baseline updates for intentional surface changes.
5. No unrelated formatting churn.
6. A clear validation section with exact commands and results.

Protected changes to `main` must go through a pull request. The default Code Owner is listed in [.github/CODEOWNERS](.github/CODEOWNERS).

Pull requests must be current with `main`, pass the required CI status checks, resolve review threads, and merge by squash commit only. Merge commits and rebase merges are disabled for this repository.

PRs authored by `SamuelMcAravey` do not require a second-person approval. PRs authored by any other account must pass the `Maintainer Review` status check, which requires Samuel's approval on the current head commit.

For public-facing behavior, avoid claims that are broader than the evidence. If a flow has only been validated through fake transports or skipped live tests, describe that honestly.

## Security Hygiene

Treat these as sensitive:

1. OpenAI API keys.
2. Codex local auth state.
3. Private repository paths and transcripts.
4. Local subprocess logs that include private prompt, file, or command content.
5. Screenshots or sample output from non-demo repositories.

If you find a security issue, follow [SECURITY.md](SECURITY.md). Do not open a public issue that includes secrets, exploit details, private transcripts, or local credential paths.
