[Home](../../README.md) · [Documentation](../README.md) · [Changelog](../../CHANGELOG.md)

# Changelog

One changelog for every package. Each release of a package is one entry, and the pipeline checks it.

- The heading is `## [<Package>-<version>] - <date>`: the package as in the folder `src/<Package>` (`EntityFrameworkCore.Postgres`), the version as in [`Versions.props`](../../Versions.props). The date is `YYYY-MM-DD` and the file is one timeline, newest first: reading top-down, no entry is dated later than the one above it, whichever package it is. The pipeline fails otherwise. The same date many times is fine.
- A pull request that changes a package bumps its version in `Versions.props` ([publishing](./publishing.md)) and adds the entry for that version **in the same pull request**. A new package is the same: its first version is its first entry.
- The pipeline fails a changed package unless its **first** entry (top-down) is the version in `Versions.props` and that version has exactly one entry. Entries of other packages in between, on the same date or not, do not matter. Older entries of the package stay below.
- A release uses the entries of the packages it published as its notes, so write the entry for someone reading only that package.
- Write the impact for the reader, never copy commit messages.
- Only notable changes. Leave out refactors, renames and small fixes.
- The changelog is the history of the packages. Changes to the pipeline, the docs or the repository itself are not entries: that is what the commits are for.

```markdown
## [AspNetCore-1.1.0] - 2026-07-02

### Added

- Support for OpenTelemetry tracing.

---

## [Core-1.0.1] - 2026-07-01

### Fixed

- Updated dependencies to address known vulnerabilities.

---

## [AspNetCore-1.0.0] - 2026-06-20

### Added

- First release.
```

The check is in [`changelog.sh`](../../.github/workflows/scripts/changelog.sh).
