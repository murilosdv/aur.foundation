#!/usr/bin/env bash
# Inputs (environment): BASE_SHA, HEAD_SHA.
# Outputs: packages (JSON array of the folder names under src), any (true/false), base-sha (the commit really compared against).
# A package is changed when its folder under src changed or its version in Versions.props did.
set -euo pipefail

ZERO_SHA="0000000000000000000000000000000000000000"
EMPTY_TREE="4b825dc642cb6eb9a060e54bf8d69288fbee4904"

if [ -z "$BASE_SHA" ] || [ "$BASE_SHA" = "$ZERO_SHA" ] || ! git cat-file -e "$BASE_SHA" 2>/dev/null; then
  BASE_SHA="$(git rev-parse "$HEAD_SHA^" 2>/dev/null || echo "$EMPTY_TREE")"
fi

version_of() {
  grep -oP "(?<=<$1>)[^<]+" || true
}

CHANGED=()
for DIRECTORY in src/*/; do
  PACKAGE="$(basename "$DIRECTORY")"
  PROPERTY="${PACKAGE//./}Version"

  if git diff --quiet "$BASE_SHA" "$HEAD_SHA" -- "src/$PACKAGE"; then SRC_CHANGED=false; else SRC_CHANGED=true; fi

  OLD_VERSION="$(git show "$BASE_SHA:Versions.props" 2>/dev/null | version_of "$PROPERTY" || true)"
  NEW_VERSION="$(git show "$HEAD_SHA:Versions.props" 2>/dev/null | version_of "$PROPERTY" || true)"

  if [ "$SRC_CHANGED" = "true" ] || [ "$OLD_VERSION" != "$NEW_VERSION" ]; then
    CHANGED+=("$PACKAGE")
  fi
done

JSON="["
for PACKAGE in "${CHANGED[@]}"; do
  JSON="${JSON}\"${PACKAGE}\","
done
JSON="${JSON%,}]"

{
  echo "packages=$JSON"
  echo "any=$([ ${#CHANGED[@]} -gt 0 ] && echo true || echo false)"
  echo "base-sha=$BASE_SHA"
} >> "$GITHUB_OUTPUT"

echo "Changed packages: ${CHANGED[*]:-none}" >> "$GITHUB_STEP_SUMMARY"
