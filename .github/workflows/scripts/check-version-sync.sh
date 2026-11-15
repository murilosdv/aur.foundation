#!/usr/bin/env bash
# Inputs (environment): PACKAGE, BASE_SHA, HEAD_SHA, SKIP. Output: version.
# BASE_SHA must be a commit that exists (detect-changed-packages resolves it).
set -euo pipefail

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

if [ "$SRC_CHANGED" != "$VERSION_CHANGED" ]; then
  echo "::error::$PACKAGE: src/$PACKAGE changed=$SRC_CHANGED but $PROPERTY changed=$VERSION_CHANGED in Versions.props (${OLD_VERSION:-none} -> $NEW_VERSION). They must change together."
  exit 1
fi
