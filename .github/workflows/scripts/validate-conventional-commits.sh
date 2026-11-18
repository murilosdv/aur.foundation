#!/usr/bin/env bash
# Inputs (environment): BASE_SHA, HEAD_SHA, and optionally TITLE (the pull request title, checked with the same rule).
set -euo pipefail

PATTERN='^(bug|build|chore|ci|docs|feat|fix|infra|perf|refactor|style|test)(\([a-zA-Z0-9_./-]+\))?!?: .+$'
RULE="'<type>(<scope>)?!?: <description>', type one of bug, build, chore, ci, docs, feat, fix, infra, perf, refactor, style, test."

FAILED=0

if [ -n "${TITLE:-}" ] && ! printf '%s' "$TITLE" | grep -qP "$PATTERN"; then
  echo "::error::The pull request title ('$TITLE') is not a Conventional Commit: $RULE"
  FAILED=1
fi

while IFS= read -r SHA; do
  MESSAGE="$(git log -1 --format=%s "$SHA")"
  if ! printf '%s' "$MESSAGE" | grep -qP "$PATTERN"; then
    echo "::error::Commit $SHA ('$MESSAGE') is not a Conventional Commit: $RULE"
    FAILED=1
  fi
done < <(git rev-list --no-merges "$BASE_SHA..$HEAD_SHA")

exit $FAILED
