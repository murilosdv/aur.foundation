#!/usr/bin/env bash
# Inputs (environment): OWNER, TOKEN, PACKAGE_ID, KEEP, DRY_RUN.
# Keeps the newest KEEP versions per channel (stable and preview counted separately) and deletes the rest, unless DRY_RUN is "true".
set -euo pipefail

API="https://api.github.com/orgs/$OWNER/packages/nuget/$PACKAGE_ID/versions"

PAGES_FILE="$(mktemp)"
PAGE=1
while :; do
  RESPONSE="$(curl -sf \
    -H "Authorization: Bearer $TOKEN" \
    -H "Accept: application/vnd.github+json" \
    -H "X-GitHub-Api-Version: 2022-11-28" \
    "$API?per_page=100&page=$PAGE" || echo "[]")"
  COUNT="$(echo "$RESPONSE" | jq 'length')"
  [ "$COUNT" -eq 0 ] && break
  echo "$RESPONSE" >> "$PAGES_FILE"
  [ "$COUNT" -lt 100 ] && break
  PAGE=$((PAGE + 1))
done

ALL="$(jq -s 'add // []' "$PAGES_FILE")"

REMOVE="$(echo "$ALL" | jq -r --argjson keep "$KEEP" '
  ([.[] | select(.name | contains("-") | not)] | sort_by(.created_at) | reverse | .[$keep:]) +
  ([.[] | select(.name | contains("-"))] | sort_by(.created_at) | reverse | .[$keep:])
  | .[] | "\(.id) \(.name)"')"

while IFS= read -r LINE; do
  [ -z "$LINE" ] && continue
  VERSION_ID="${LINE%% *}"
  VERSION_NAME="${LINE#* }"

  if [ "$DRY_RUN" = "true" ]; then
    echo "Would delete $PACKAGE_ID $VERSION_NAME" >> "$GITHUB_STEP_SUMMARY"
  else
    curl -sf -X DELETE \
      -H "Authorization: Bearer $TOKEN" \
      -H "Accept: application/vnd.github+json" \
      -H "X-GitHub-Api-Version: 2022-11-28" \
      "$API/$VERSION_ID"
    echo "Deleted $PACKAGE_ID $VERSION_NAME" >> "$GITHUB_STEP_SUMMARY"
  fi
done <<< "$REMOVE"
