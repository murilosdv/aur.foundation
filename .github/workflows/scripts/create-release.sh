#!/usr/bin/env bash
# Inputs (environment): RECORDS_DIR, CHANGELOG, GH_TOKEN. Each record is a file "<name> <version>".
# The notes are the changelog entries of the packages published in this run, one section each.
set -euo pipefail

source "$(dirname "${BASH_SOURCE[0]}")/changelog.sh"

if ! ls "$RECORDS_DIR"/*.txt > /dev/null 2>&1; then
  echo "Nothing was published in this run, no release." >> "$GITHUB_STEP_SUMMARY"
  exit 0
fi

LIST=""
NOTES=""
COUNT=0
VERSIONS=()
ALL_PRERELEASE=true
while read -r NAME VERSION; do
  COUNT=$((COUNT + 1))
  VERSIONS+=("$VERSION")
  LIST="${LIST}${NAME} v${VERSION}, "
  case "$VERSION" in *-*) ;; *) ALL_PRERELEASE=false ;; esac

  ENTRY="$(changelog_entry "$NAME" "$VERSION" "$CHANGELOG")"
  if [ -z "$ENTRY" ]; then
    ENTRY="No entry for $NAME-$VERSION in [$CHANGELOG](https://github.com/$GITHUB_REPOSITORY/blob/main/$CHANGELOG)."
  fi
  NOTES="${NOTES}## ${NAME} v${VERSION}"$'\n\n'"${ENTRY}"$'\n\n'
done < <(cat "$RECORDS_DIR"/*.txt)
LIST="${LIST%, }"

# The title stays short: the packages when there are few, otherwise the shared version or the date. The full list is in the notes.
if [ "$COUNT" -le 3 ]; then
  TITLE="Release: $LIST"
elif [ "$(printf '%s\n' "${VERSIONS[@]}" | sort -u | wc -l)" -eq 1 ]; then
  TITLE="Release v${VERSIONS[0]} ($COUNT packages)"
else
  TITLE="Release $(date -u +%F) ($COUNT packages, several versions)"
fi

TAG="release-$(git rev-parse --short HEAD)"
if gh release view "$TAG" > /dev/null 2>&1; then
  echo "Release $TAG already exists, skipping."
  exit 0
fi

{
  printf '%s' "$NOTES"
  echo "---"
  echo "Published: $LIST"
  echo "Commit: $GITHUB_SHA"
} > release-notes.md

FLAGS=()
if [ "$ALL_PRERELEASE" = "true" ]; then FLAGS+=(--prerelease); fi

gh release create "$TAG" --title "$TITLE" --notes-file release-notes.md --target "$GITHUB_SHA" "${FLAGS[@]}"
