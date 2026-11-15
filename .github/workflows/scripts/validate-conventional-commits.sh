#!/usr/bin/env bash
# Inputs (environment): BASE_SHA, HEAD_SHA.
set -euo pipefail

PATTERN='^(bug|build|chore|ci|docs|feat|fix|infra|perf|refactor|style|test)(\([a-zA-Z0-9_./-]+\))?!?: .+$'

FAILED=0
while IFS= read -r SHA; do
  MESSAGE="$(git log -1 --format=%s "$SHA")"
  if ! printf '%s' "$MESSAGE" | grep -qP "$PATTERN"; then
    echo "::error::Commit $SHA ('$MESSAGE') is not a Conventional Commit: '<type>(<scope>)?!?: <description>', type one of bug, build, chore, ci, docs, feat, fix, infra, perf, refactor, style, test."
    FAILED=1
  fi
done < <(git rev-list --no-merges "$BASE_SHA..$HEAD_SHA")

exit $FAILED
