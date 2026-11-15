#!/usr/bin/env bash
# Inputs (environment): TEST_PROJECT, CONFIGURATION, COVERAGE_FILE.
set -euo pipefail

if [ -z "$TEST_PROJECT" ] || ! ls "$TEST_PROJECT"/*.csproj > /dev/null 2>&1; then
  echo "::notice::No test project to run."
  exit 0
fi

ARGS=()
if [ -n "$COVERAGE_FILE" ]; then
  ARGS+=(--coverage --coverage-output-format cobertura --coverage-output "$COVERAGE_FILE")
fi

dotnet test "$TEST_PROJECT" --configuration "$CONFIGURATION" "${ARGS[@]}"
