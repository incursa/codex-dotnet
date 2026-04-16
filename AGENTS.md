# Repository Guidance

- Cut releases with [`scripts/release.ps1`](scripts/release.ps1); do not hand-edit the release tag or version when that script is available.
- The release script is the semver source of truth. It compares the current public API baseline files against the latest `v*.*.*` tag to choose `major`, `minor`, or `patch`, then updates `Directory.Build.props`, runs the Release test suite, commits, tags, and pushes.
- Keep `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` current before running the release script. If the public API changed, refresh the baselines and keep `PublicApiSnapshotTests` green first.
