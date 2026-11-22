[Home](../../README.md) · [Documentation](../README.md)

# Code style

The analyzers in `.editorconfig` catch part of this; the rest is on whoever writes the code. When a rule does not fit a case, ask before bending it.

## Files

The rule is **cohesion**: what exists only for one thing lives in that thing's file; what other code uses stands alone.

- One public type per file. Together stay a generic type and its non-generic twin (`Result` and `Result<T>`, `Feature` and `Feature<,>`, `SortField`, `IEntity`, `EntityConfigurationBase`) and a type with the private pieces it is made of (`SortMap` and `SortStep`).
- In tests, a helper used only inside one test file stays in it; a fixture or stub that several tests use is its own file (`Tests.Shared`, or a `Support` folder).
- Namespaces follow the folders.

## Code

- **Pattern matching wherever it applies:** a `switch` for an `if` / `else if` / `else` on one value, and `is` / `is not` against constants (`Count is 0`, `x is not "*"`). Not possible against `static readonly` values (`Uri.UriSchemeHttps`) or inside an EF expression tree. A negative test is `is false`, never `!`: `Contains(x) is false`. SonarQube rule S1125 (unnecessary Boolean literal) would flag these, so it is off in `.editorconfig`.
- **No braces around a body of one line** (`if`, `else`, `foreach`, `while`); braces when it spans lines. Keep the braces on every branch of an `if` / `else` chain that has a braced branch. IDE0011 only asks for missing braces, so leaving redundant ones out is on you.
- **A declaration longer than 120 characters puts one parameter per line**, the closing parenthesis after the last one and the base list (`: Base<...>`) on its own indented line.
- Check with `dotnet format Foundation.slnx --verify-no-changes`.

## Project files, Makefiles and Dockerfiles

The same order everywhere, top-down, so any file of these kinds reads like the others.

- **Project file (`.csproj`):** one `PropertyGroup` (`Version` when the project has one, `AssemblyName`, `RootNamespace`, then the rest), then one `ItemGroup` each for `FrameworkReference`, `InternalsVisibleTo`, `PackageReference`, `ProjectReference`, files (`Content`) and `Using`. Items inside a group are in alphabetical order. A comment only where the reason is not obvious.
- **Makefile:** `-include` first, `export` next, then the variables grouped under a one-line comment (project, dev-sbx, SonarQube, migrations), then `.PHONY`, then the targets in sections headed `## Section ##` (dev-sbx, SonarQube, tests, clean, migrations, database). A command that repeats is a variable.
- **Dockerfile:** `syntax` line and `ARG` first, then the stages in order, each headed `## Stage: what it does ##` (restore, build, test, publish, runtime). The test stage does not run as root.

## Documentation

Everything public has XML documentation ([XML documentation](./xml-docs.md)).
