# syntax=docker/dockerfile:1

ARG SOLUTION=Foundation.slnx

## Restore: packages and tools, in a layer that only changes with the project files ##
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS restore
ARG SOLUTION
WORKDIR /src

# Outside /root, so the user of the test stage can read them.
ENV NUGET_PACKAGES=/opt/nuget
ENV PATH="$PATH:/opt/tools"

COPY global.json Directory.Build.props Directory.Packages.props Versions.props .editorconfig $SOLUTION ./
COPY --parents src/*/*.csproj tests/*/*.csproj ./
RUN dotnet restore "$SOLUTION"
RUN dotnet tool install --tool-path /opt/tools dotnet-reportgenerator-globaltool --version 5.*

## Build: the sources ##
FROM restore AS build
COPY src/ src/
COPY tests/ tests/

## Test: every test project, then the coverage report; not as root ##
FROM build AS test

# Written here, not tracked: compose would split the reportgenerator arguments on ';' if this were an entrypoint override.
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

# uid 1000 is the user of the machine, so it can write to the mounted artifacts folder.
RUN chmod +x run-tests.sh && chown -R 1000:1000 /src /opt/nuget
ENV DOTNET_CLI_HOME=/tmp
USER 1000:1000

ENTRYPOINT ["./run-tests.sh"]
