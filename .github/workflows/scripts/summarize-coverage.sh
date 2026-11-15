#!/usr/bin/env bash
# Inputs (environment): NAME, FILE_NAME, CLASS_FILTERS.
set -euo pipefail

FILE="$(find artifacts/test-results -name "$FILE_NAME" 2>/dev/null | head -n 1)"
if [ -z "$FILE" ]; then
  echo "No coverage file produced for $NAME." >> "$GITHUB_STEP_SUMMARY"
  exit 0
fi

dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.* > /dev/null
export PATH="$PATH:$HOME/.dotnet/tools"
reportgenerator "-reports:$FILE" "-targetdir:artifacts/coverage-report" -reporttypes:MarkdownSummaryGithub "-classfilters:$CLASS_FILTERS"

{
  echo "### $NAME coverage (informational)"
  cat artifacts/coverage-report/SummaryGithub.md
} >> "$GITHUB_STEP_SUMMARY"
