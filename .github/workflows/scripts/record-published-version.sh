#!/usr/bin/env bash
# Inputs (environment): PACKAGE, VERSION. Leaves "<package> <version>" in published/<package>.txt, written when the package is verified and uploaded as the record of the release only after the push succeeds.
set -euo pipefail

mkdir -p published
echo "$PACKAGE $VERSION" > "published/$PACKAGE.txt"
