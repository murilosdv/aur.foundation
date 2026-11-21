#!/usr/bin/env bash
# Inputs (environment): PACKAGE, BASE_SHA, HEAD_SHA, SKIP, CHANGELOG (default CHANGELOG.md). Output: version.
# BASE_SHA must be a commit that exists (detect-changed-packages resolves it).
# Fails unless the package changed (its src, or a dependency of it in Directory.Packages.props, see package-changes.sh) exactly when its version did, and the changelog has the entry for that version (see changelog.sh).
# A dependency-only change needs no src: bump the version, write the entry.
set -euo pipefail

source "$(dirname "${BASH_SOURCE[0]}")/changelog.sh"
source "$(dirname "${BASH_SOURCE[0]}")/package-changes.sh"

PROPERTY="${PACKAGE//./}Version"
NEW_VERSION="$(grep -oP "(?<=<$PROPERTY>)[^<]+" Versions.props)"
echo "version=$NEW_VERSION" >> "$GITHUB_OUTPUT"

if [ "$SKIP" = "true" ]; then
  echo "::notice::$PACKAGE: version-sync check bypassed manually."
  exit 0
fi

if git diff --quiet "$BASE_SHA" "$HEAD_SHA" -- "src/$PACKAGE"; then SRC_CHANGED=false; else SRC_CHANGED=true; fi

OLD_VERSION="$(git show "$BASE_SHA:Versions.props" 2>/dev/null | grep -oP "(?<=<$PROPERTY>)[^<]+" || true)"
if [ "$OLD_VERSION" = "$NEW_VERSION" ]; then VERSION_CHANGED=false; else VERSION_CHANGED=true; fi

DEPENDENCIES="$(changed_dependencies "$BASE_SHA" "$HEAD_SHA" "$PACKAGE" | paste -sd, -)"
if [ "$SRC_CHANGED" = "true" ] || [ -n "$DEPENDENCIES" ]; then CHANGED=true; else CHANGED=false; fi

if [ "$CHANGED" = "true" ] && [ "$VERSION_CHANGED" = "false" ]; then
  echo "::error::$PACKAGE: src/$PACKAGE changed=$SRC_CHANGED, dependencies changed=${DEPENDENCIES:-none}, but $PROPERTY did not change in Versions.props ($NEW_VERSION). Bump it and add its changelog entry."
  exit 1
fi

if [ "$CHANGED" = "false" ] && [ "$VERSION_CHANGED" = "true" ]; then
  echo "::error::$PACKAGE: $PROPERTY changed in Versions.props (${OLD_VERSION:-none} -> $NEW_VERSION) but neither src/$PACKAGE nor a dependency of it in Directory.Packages.props changed. Revert the version, or make the change it is for."
  exit 1
fi

check_changelog "$PACKAGE" "$NEW_VERSION" "${CHANGELOG:-CHANGELOG.md}"
