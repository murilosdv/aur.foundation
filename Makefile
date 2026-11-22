-include .env
export MSYS2_ARG_CONV_EXCL := *

SOLUTION_FILE := Foundation.slnx
SBX_DIR ?= ../.dev-sbx
SONAR_HOST ?= http://127.0.0.1:9000
SONAR_PROJECT_NAME ?= aur.foundation

.PHONY: sbx-up sbx-down sonar test test-local report clean

sbx-up:
	$(MAKE) -C $(SBX_DIR) docker-up

sbx-down:
	$(MAKE) -C $(SBX_DIR) docker-down

# Analyses the project with the SonarQube of the dev-sbx: starts the dev-sbx with SonarQube, begin, build, test with coverage, end, then removes SonarQube alone; the rest of the dev-sbx stays up. Needs SONAR_TOKEN in .env.
# "end" waits for the analysis to be processed (qualitygate.wait) because SonarQube is removed right after it.
sonar:
	$(MAKE) -C $(SBX_DIR) docker-up PROFILE=sonar
	rm -rf artifacts/test-results
	@dotnet sonarscanner begin /k:"$(SONAR_PROJECT_NAME)" /d:sonar.host.url="$(SONAR_HOST)" /d:sonar.token="$(SONAR_TOKEN)" /d:sonar.qualitygate.wait=true /d:sonar.cs.vscoveragexml.reportsPaths="artifacts/test-results/*.xml"
	dotnet build $(SOLUTION_FILE) --no-incremental
	dotnet test --solution $(SOLUTION_FILE) --coverage --coverage-output-format xml
	@dotnet sonarscanner end /d:sonar.token="$(SONAR_TOKEN)"

test:
	docker compose run --build --rm tests

test-local:
	dotnet format $(SOLUTION_FILE)
	dotnet test --solution $(SOLUTION_FILE)

report:
	cmd.exe /c start artifacts\coverage-report\index.html

clean:
	rm -rf artifacts
	find . -path './.git' -prune -o -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
