[Home](../../README.md) · [Documentation](../README.md)

# Shared actions

Composite actions in `.github/actions`, used by this repository's workflows and by other repositories.

| Action | Does | Inputs |
|---|---|---|
| `conventional-commits` | Fails when a commit between two SHAs, or the pull request title when given, is not a Conventional Commit. Needs `fetch-depth: 0`. | `base-sha`, `head-sha`, `title` |
| `detect-changed-packages` | Lists the packages whose `src` folder or `Versions.props` line changed, as a JSON array for a matrix. Needs `fetch-depth: 0`. | `base-sha`, `head-sha` |
| `check-version-sync` | Reads a package's version and fails unless its `src` and version changed together and the changelog has the entry for that version. | `package-name`, `base-sha`, `head-sha`, `skip-check` |
| `start-services` | Starts `postgres` and/or `sqlserver` containers (dev-sbx ports and passwords) and waits until healthy. | `services` (empty starts nothing) |
| `build-test` | Installs the SDK, builds, runs a test project, optionally with Cobertura coverage. | `project`, `test-project`, `configuration`, `coverage-file`, `global-json-file` |
| `coverage-summary` | Writes a Cobertura report as a table in the job summary. | `name`, `file-name`, `class-filters` |
| `pack-package` | Packs a project into `./nupkg`. | `project`, `configuration` |
| `publish-package` | Pushes `./nupkg` to a NuGet feed (the owner's GitHub Packages by default). | `api-key`, `source` |
| `create-release` | Creates a GitHub release from the published records, with the changelog entry of each published package and version as the notes. | `records-dir`, `changelog`, `token` |

## Use

In this repository: `uses: ./.github/actions/start-services`. It runs the version of the commit being built, so a pull request tests its own change to an action.

From another repository:

```yaml
- uses: murilosdv/aur.foundation/.github/actions/start-services@main
  with:
    services: postgres
```

Pin a tag or commit instead of `main` to avoid changes under you. The repository must be public, or allow Actions access from the organization's repositories.

- `package-pipeline.yaml` cannot be called from another repository: a reusable workflow resolves `./.github/actions/...` against the caller's repository. Compose the actions in your own workflow.
- Keycloak is not in `start-services`; its realm needs the dev-sbx files.

## Scripts

No workflow or action holds more than one command of shell. Logic lives in `.github/workflows/scripts/`, one script per job, named for what it does. GitHub ignores subfolders there when loading workflows.
Actions reach them through `${{ github.action_path }}/../../workflows/scripts`, which also works from another repository because the whole repository is checked out at the ref.

Inputs arrive as environment variables, never interpolated into shell text, and each script lists them in its first comment, so it runs by hand:

```bash
BASE_SHA=<sha> HEAD_SHA=HEAD bash .github/workflows/scripts/validate-conventional-commits.sh
```

Scripts are linted with ShellCheck, workflows with actionlint.
