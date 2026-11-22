[Home](../../README.md) · [Documentation](../README.md)

# Publishing

The version of a package is its line in [`Versions.props`](../../Versions.props), published as written. No pipeline bumps it.

- A package **changes** when its folder `src/<Package>` changes, or when a package it references gets another version in [`Directory.Packages.props`](../../Directory.Packages.props) (a dependency of a test project does not count). A change goes with a change of that package's version in the same pull request, and the reverse: a version that changes with nothing behind it fails. A new version of a dependency alone needs no `src` change, only the bump and the changelog entry. The pipeline fails otherwise.
- That version also needs its entry in the [changelog](./changelog.md): `## [<Package>-<version>]`, the first one of that package, and only one. The pipeline fails otherwise. This covers a new package too.
- A preview has a suffix: `0.2.0-preview.1`.
- A package is **changed** when its `src` folder, a dependency of it in `Directory.Packages.props`, or its `Versions.props` line changed. Only changed packages run, each on its own. Packages that depend on a changed one are not rebuilt.

- Each package has its own `README.md` in its `src` folder: it is what nuget.org shows. Links in it are absolute (`https://github.com/murilosdv/aur.foundation/blob/main/...`), because relative ones do not work there. The description and the tags are in its project file; the authors, license, repository and release notes link are in `Directory.Build.props`.

## Pull request ([`pr-check.yaml`](../../.github/workflows/pr-check.yaml))

1. Conventional Commits on the pull request title and on every commit, and `dotnet format --verify-no-changes`. Editing the title re-runs only the Conventional Commits job.
2. Detect the changed packages.
3. Each changed package runs the pipeline without publishing, and CodeQL runs alongside (skipped when no package changed).

## Merge to `main` ([`publish.yaml`](../../.github/workflows/publish.yaml))

1. Detect the changed packages.
2. Each changed package runs the pipeline and publishes, and CodeQL runs alongside. Tests run again because a merge can combine changes never tested together.
3. One GitHub release lists what was published, with a link to each version on nuget.org and the changelog entry of each published package and version as the notes. The title lists the packages when there are up to three; with more it shows the shared version (`Release v1.0.0 (9 packages)`) or the date when the versions differ (`Release 2026-11-18 (9 packages, several versions)`). It is a pre-release when everything published is a preview. Nothing published, no release.

The manual run of `publish.yaml` can skip the version check (which includes the changelog entry), for a one-off fix such as a version corrected afterwards.

## Package pipeline ([`package-pipeline.yaml`](../../.github/workflows/package-pipeline.yaml))

- **verify:** read the version and check it against `src`, start the servers the tests need, build and test (coverage summary on pull requests), pack. When publishing, it uploads the `.nupkg`.
- **publish:** log in to nuget.org with [trusted publishing](#nugetorg-trusted-publishing) and push that same `.nupkg`. A version already on nuget.org fails that package only.

## NuGet versions

Every dependency version is in [`Directory.Packages.props`](../../Directory.Packages.props) (central package management); a project writes `<PackageReference Include="Name" />` with no version, and the test projects get the coverage extension from `Directory.Build.props`. To add or update a dependency, change its `PackageVersion` there. A new version there changes every package that references that dependency: the pipeline asks each of them for the version bump and the changelog entry, with no `src` change needed.

## Decisions

- **Packages are independent after the merge.** The version is already on `main` and cannot be moved. If one failure held back the others, the good ones would be stranded: the next push compares only its own commits and would not detect them.
  A failed package burns its version; its fix bumps the next.
- **CodeQL does not gate publishing.** It fails only when the analysis cannot run; findings go to the Security tab. It also runs weekly.

## nuget.org trusted publishing

No API key is stored. The `publish` job asks GitHub for an OpenID Connect token (`id-token: write`), `NuGet/login` swaps it for an API key that lasts a few minutes, and `dotnet nuget push` uses that key.

- **On nuget.org** (once): a trusted publishing policy owned by `aurorelabs` for GitHub repository `murilosdv/aur.foundation`, workflow file `publish.yaml`, environment `production`, scopes push and unlist, packages `Aurore.Foundation.*`. The workflow logs in as the nuget.org account `muriloalmeidadev` (`user` of `NuGet/login`), which is not a secret and is written in the workflow.
- **On GitHub:** the `publish` job runs in the environment `production` because the policy asks for it. GitHub creates it the first time it is used; protection rules on it (required reviewers, for example) would hold every publish until approved.
- Only `publish.yaml` publishes: the same job started by another workflow gets a token nuget.org refuses. `pr-check.yaml` grants `id-token` to the called workflow only because a called workflow may not ask for more than its caller, and its publish job never runs there.
- A version on nuget.org cannot be deleted, only unlisted (the policy allows it), so there is no cleanup of old versions. The versions that were on GitHub Packages are gone.
