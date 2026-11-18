## Summary

<!-- What does this change, and why? -->

## Checklist

- [ ] If this touches `src/<Package>/`, the matching version in `Versions.props` was changed in this same PR (CI fails the package otherwise); a preview is a version like `0.2.0-preview.1`.
- [ ] For every package changed: its version in `Versions.props` was bumped and `CHANGELOG.md` has the entry `## [<Package>-<version>]` for it.
- [ ] Tests were added or updated for the behavior being changed.
- [ ] `dotnet format Foundation.slnx --verify-no-changes` passes locally.
