[Home](../README.md) · [Documentation](./README.md)

# Installing

The packages are on [nuget.org](https://www.nuget.org/profiles/aurorelabs), so there is nothing to configure: no registry, no token. Every version is a preview for now, so a version or `--prerelease` is required:

```
dotnet add package Aurore.Foundation.Core --prerelease
```

Which package to pick: [Getting started](./getting-started.md#packages).

## Versions before `1.0.0-preview.3`

`1.0.0-preview.1` and `1.0.0-preview.2` of every package were on GitHub Packages, which needed a token even to read, and were never on nuget.org. Those versions were removed; use `1.0.0-preview.3` or later.
