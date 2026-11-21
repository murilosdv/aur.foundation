# syntax=docker/dockerfile:1

ARG SOLUTION=Foundation.slnx

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
ARG SOLUTION
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props Versions.props .editorconfig $SOLUTION ./
COPY --parents src/*/*.csproj tests/*/*.csproj ./
RUN dotnet restore "$SOLUTION"

RUN dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.*
ENV PATH="$PATH:/root/.dotnet/tools"

COPY src/ src/
COPY tests/ tests/

# The script is written here, not tracked: compose would re-split the reportgenerator arguments on the ';' if passed as an entrypoint override.
COPY <<'SCRIPT' run-tests.sh
#!/usr/bin/env bash
set -euo pipefail

for package in Core AspNetCore Telemetry OpenApi OpenApi.Scalar OpenApi.Swagger EntityFrameworkCore EntityFrameworkCore.Postgres; do
    dotnet test "tests/Tests.$package" --no-restore --configuration Release \
        --coverage --coverage-output-format cobertura --coverage-output "$package.cobertura.xml"
done

reportgenerator \
    "-reports:artifacts/test-results/*.cobertura.xml" \
    "-targetdir:artifacts/coverage-report" \
    "-reporttypes:Html;TextSummary" \
    "-assemblyfilters:-Aurore.Foundation.Tests.*" \
    "-classfilters:-Microsoft.AspNetCore.OpenApi.Generated;-System.Runtime.CompilerServices;-System.Text.RegularExpressions.Generated;-Microsoft.Extensions.Logging.Generated"

cat artifacts/coverage-report/Summary.txt
SCRIPT
RUN chmod +x run-tests.sh

FROM build AS test
ENTRYPOINT ["./run-tests.sh"]
