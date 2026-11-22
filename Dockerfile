# syntax=docker/dockerfile:1

ARG SOLUTION=Foundation.slnx

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
ARG SOLUTION
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props Versions.props .editorconfig $SOLUTION ./
COPY --parents src/*/*.csproj tests/*/*.csproj ./
# The packages and the tool are not under /root, so the user of the test stage can read them.
ENV NUGET_PACKAGES=/opt/nuget
RUN dotnet restore "$SOLUTION"

RUN dotnet tool install --tool-path /opt/tools dotnet-reportgenerator-globaltool --version 5.*
ENV PATH="$PATH:/opt/tools"

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
# Not root: the tests need no privilege. uid 1000 (a user the image already has) is the user of the machine, so it can write to the mounted artifacts folder.
RUN chown -R 1000:1000 /src /opt/nuget
ENV DOTNET_CLI_HOME=/tmp
USER 1000:1000
ENTRYPOINT ["./run-tests.sh"]
