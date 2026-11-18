[Home](../../README.md) · [Documentation](../README.md)

# Publishing

The version of a package is its line in [`Versions.props`](../../Versions.props), published as written. No pipeline bumps it.

- A change under `src/<Package>` goes with a change of that package's version in the same pull request, and the reverse. The pipeline fails otherwise.
- That version also needs its entry in the [changelog](./changelog.md): `## [<Package>-<version>]`, the first one of that package, and only one. The pipeline fails otherwise. This covers a new package too.
- A preview has a suffix: `0.2.0-preview.1`.
- A package is **changed** when its `src` folder or its `Versions.props` line changed. Only changed packages run, each on its own. Packages that depend on a changed one are not rebuilt.

## Pull request ([`pr-check.yaml`](../../.github/workflows/pr-check.yaml))

1. Conventional Commits on the pull request title and on every commit, and `dotnet format --verify-no-changes`. Editing the title re-runs only the Conventional Commits job.
2. Detect the changed packages.
3. Each changed package runs the pipeline without publishing, and CodeQL runs alongside (skipped when no package changed).

## Merge to `main` ([`publish.yaml`](../../.github/workflows/publish.yaml))

1. Detect the changed packages.
2. Each changed package runs the pipeline and publishes, and CodeQL runs alongside. Tests run again because a merge can combine changes never tested together.
3. One GitHub release lists what was published, with the changelog entry of each published package and version as the notes. The title lists the packages when there are up to three; with more it shows the shared version (`Release v1.0.0 (9 packages)`) or the date when the versions differ (`Release 2026-11-18 (9 packages, several versions)`). It is a pre-release when everything published is a preview. Nothing published, no release.

The manual run of `publish.yaml` can skip the version check (which includes the changelog entry), for a one-off fix such as a version corrected afterwards.

## Package pipeline ([`package-pipeline.yaml`](../../.github/workflows/package-pipeline.yaml))

- **verify:** read the version and check it against `src`, start the servers the tests need, build and test (coverage summary on pull requests), pack. When publishing, it uploads the `.nupkg`.
- **publish:** push that same `.nupkg` to GitHub Packages (`https://nuget.pkg.github.com/murilosdv/index.json`). A version already in the feed fails that package only.

## Decisions

- **Packages are independent after the merge.** The version is already on `main` and cannot be moved. If one failure held back the others, the good ones would be stranded: the next push compares only its own commits and would not detect them.
  A failed package burns its version; its fix bumps the next.
- **CodeQL does not gate publishing.** It fails only when the analysis cannot run; findings go to the Security tab. It also runs weekly.

## Cleanup

**Cleanup old package versions** (manual) keeps the newest N versions per package, stable and preview counted separately, and deletes the rest. It only lists unless dry-run is turned off.
