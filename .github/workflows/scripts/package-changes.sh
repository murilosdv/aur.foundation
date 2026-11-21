#!/usr/bin/env bash
# Library, sourced by detect-changed-packages.sh and check-version-sync.sh. Decides what counts as a change of a package besides its own src folder:
# a dependency of the package that got another version in Directory.Packages.props. Needs the full history (fetch-depth: 0).
# Reads one <PackageVersion .../> and one <PackageReference .../> per line, which is how the files are written.

# package_version_pins <revision>: "Name=Version" for every PackageVersion of Directory.Packages.props at that revision, sorted. Nothing when the file does not exist there.
package_version_pins() {
  git show "$1:Directory.Packages.props" 2>/dev/null \
    | sed -nE 's/.*<PackageVersion[^>]*Include="([^"]+)"[^>]*Version="([^"]+)".*/\1=\2/p' \
    | LC_ALL=C sort || true
}

# package_references <revision> <package>: the names of the packages that src/<package>/<package>.csproj references at that revision, sorted.
package_references() {
  git show "$1:src/$2/$2.csproj" 2>/dev/null \
    | sed -nE 's/.*<PackageReference[^>]*Include="([^"]+)".*/\1/p' \
    | LC_ALL=C sort -u || true
}

# changed_dependencies <base> <head> <package>: the packages that <package> references and whose version (added, changed or removed) differs between the two revisions.
# Nothing when the base has no Directory.Packages.props: the versions were in the projects then, and that is a change under src.
changed_dependencies() {
  local base="$1" head="$2" package="$3" changed

  git cat-file -e "$base:Directory.Packages.props" 2>/dev/null || return 0

  changed="$(comm -3 <(package_version_pins "$base") <(package_version_pins "$head") | sed -E 's/^[[:space:]]+//; s/=.*//' | LC_ALL=C sort -u)"
  comm -12 <(printf '%s\n' "$changed") <(package_references "$head" "$package")
}
