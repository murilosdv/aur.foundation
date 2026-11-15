#!/usr/bin/env bash
# Inputs (environment): RECORDS_DIR, CHANGELOG, GH_TOKEN. Each record is a file "<name> <version>".
set -euo pipefail

if ! ls "$RECORDS_DIR"/*.txt > /dev/null 2>&1; then
  echo "Nothing was published in this run, no release." >> "$GITHUB_STEP_SUMMARY"
  exit 0
fi

LIST=""
ALL_PRERELEASE=true
while read -r NAME VERSION; do
  LIST="${LIST}${NAME} v${VERSION}, "
  case "$VERSION" in *-*) ;; *) ALL_PRERELEASE=false ;; esac
done < <(cat "$RECORDS_DIR"/*.txt)
LIST="${LIST%, }"

TAG="release-$(git rev-parse --short HEAD)"
if gh release view "$TAG" > /dev/null 2>&1; then
  echo "Release $TAG already exists, skipping."
  exit 0
fi

NOTES="$(awk '/^## \[Unreleased\]/{flag=1; next} /^## \[/{if (flag) exit} flag' "$CHANGELOG" | sed '/^---$/d')"
if [ -z "$(echo "$NOTES" | tr -d '[:space:]')" ]; then
  NOTES="No entry under **Unreleased** at release time. See [$CHANGELOG](https://github.com/$GITHUB_REPOSITORY/blob/main/$CHANGELOG)."
fi

{
  echo "$NOTES"
  echo ""
  echo "---"
  echo "Published: $LIST"
  echo "Commit: $GITHUB_SHA"
} > release-notes.md

FLAGS=()
if [ "$ALL_PRERELEASE" = "true" ]; then FLAGS+=(--prerelease); fi

gh release create "$TAG" --title "Release: $LIST" --notes-file release-notes.md --target "$GITHUB_SHA" "${FLAGS[@]}"
