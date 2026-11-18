SOLUTION_FILE := Foundation.slnx

.PHONY: sbx-up sbx-down test test-local report clean

# The dev-sbx (.dev-sbx next to this repository) runs on its own; these call its Makefile. The tests need both databases.
#   make sbx-up   |   make sbx-up DB=postgres   |   make sbx-down
DB ?= all

sbx-up:
	$(MAKE) -C ../.dev-sbx docker-up DB=$(DB)

sbx-down:
	$(MAKE) -C ../.dev-sbx docker-down

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
