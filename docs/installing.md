[Home](../README.md) · [Documentation](./README.md)

# Installing

The packages are on the GitHub Packages NuGet registry of `murilosdv`, which needs a token even to read.

1. Create a personal access token (classic) with the `read:packages` scope.
2. Add the registry once per machine:

```
dotnet nuget source add https://nuget.pkg.github.com/murilosdv/index.json --name aurore --username <github-user> --password <token> --store-password-in-clear-text
```

`--store-password-in-clear-text` is needed on Linux and macOS, where NuGet cannot encrypt the password. On Windows it can be left out.

3. Add a package. Every version is a preview for now, so a version or `--prerelease` is required:

```
dotnet add package Aurore.Foundation.Core --prerelease
```

In GitHub Actions, add the registry with the workflow token (the job needs `packages: read`):

```
dotnet nuget source add https://nuget.pkg.github.com/murilosdv/index.json --name aurore --username ${{ github.actor }} --password ${{ secrets.GITHUB_TOKEN }} --store-password-in-clear-text
```

Which package to pick: [Getting started](./getting-started.md#packages).
