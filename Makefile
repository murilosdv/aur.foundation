-include .env

# Git Bash would turn the /k: and /d: arguments of the scanner into paths
export MSYS2_ARG_CONV_EXCL := *

# Project
SOLUTION := Foundation.slnx
ARTIFACTS := artifacts
TEST_RESULTS := $(ARTIFACTS)/test-results
COVERAGE_REPORT := $(ARTIFACTS)/coverage-report/index.html

# Dev-sbx
SBX_DIR ?= ../.dev-sbx
SBX := $(MAKE) -C $(SBX_DIR)

# SonarQube (SONAR_TOKEN is a secret: .env)
SONAR_HOST ?= http://127.0.0.1:9000
SONAR_PROJECT_NAME ?= aur.foundation
SONAR_COVERAGE := $(TEST_RESULTS)/*.xml
SONAR_BEGIN := dotnet sonarscanner begin /k:"$(SONAR_PROJECT_NAME)" /d:sonar.host.url="$(SONAR_HOST)" /d:sonar.token="$(SONAR_TOKEN)" /d:sonar.cs.vscoveragexml.reportsPaths="$(SONAR_COVERAGE)"
SONAR_END := dotnet sonarscanner end /d:sonar.token="$(SONAR_TOKEN)"
XML_COVERAGE := --coverage --coverage-output-format xml

.PHONY: sbx-up sbx-down sonar test test-local report clean

## Dev-sbx ##

sbx-up:
	$(SBX) docker-up

sbx-down:
	$(SBX) docker-down

## SonarQube: starts the dev-sbx with SonarQube and leaves it running; make sonar-down (dev-sbx) removes it ##

sonar:
	$(SBX) docker-up PROFILE=sonar
	rm -rf $(TEST_RESULTS)
	@$(SONAR_BEGIN)
	dotnet build $(SOLUTION) --no-incremental
	dotnet test --solution $(SOLUTION) $(XML_COVERAGE)
	@$(SONAR_END)

## Tests ##

test:
	docker compose run --build --rm tests

test-local:
	dotnet format $(SOLUTION)
	dotnet test --solution $(SOLUTION)

report:
	cmd.exe /c start $(subst /,\,$(COVERAGE_REPORT))

## Clean ##

clean:
	rm -rf $(ARTIFACTS)
	find . -path './.git' -prune -o -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
