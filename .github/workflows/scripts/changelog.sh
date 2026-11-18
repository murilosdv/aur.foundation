#!/usr/bin/env bash
# Functions to read the changelog, sourced by check-version-sync.sh, create-release.sh and the tests. Nothing runs when sourced.
#
# One changelog for every package; an entry is a heading "## [<Package>-<version>] - <date>" (Package as in the folder src/<Package>,
# so it can hold dots: EntityFrameworkCore.Postgres-1.0.0). Order is newest first, but only within a package: entries of other
# packages (or of the same date) in between do not matter.

# changelog_versions <package> <file>: the versions the package has an entry for, in file order (newest first).
# The package is matched literally and must be followed by "-" and a digit, so EntityFrameworkCore never matches EntityFrameworkCore.Postgres.
changelog_versions() {
  local package="$1" file="$2"
  grep -oP "^## \[\Q${package}\E-\K[0-9][^\]]*(?=\])" "$file" || true
}

# check_changelog_order <file>: every entry has a date (YYYY-MM-DD) and, read top-down, no date is newer than the one above it.
# Entries of any package count, so the file reads as one timeline, newest first. The same date many times is fine.
check_changelog_order() {
  local file="$1" line previous="" previous_heading="" date

  while IFS= read -r line; do
    line="${line%$'\r'}"

    if [[ ! "$line" =~ ^##\ \[[^]]+\]\ -\ ([0-9]{4}-[0-9]{2}-[0-9]{2})$ ]]; then
      echo "::error::$file: '$line' has no date. An entry heading is '## [<Package>-<version>] - YYYY-MM-DD'."
      return 1
    fi

    date="${BASH_REMATCH[1]}"

    if [ -n "$previous" ] && [[ "$date" > "$previous" ]]; then
      echo "::error::$file: '$line' is newer than '$previous_heading' above it. Entries go newest first."
      return 1
    fi

    previous="$date"
    previous_heading="$line"
  done < <(grep '^## \[' "$file" || true)
}

# check_changelog <package> <version> <file>: succeeds when the file is in order and the first entry of the package is for <version>, once.
check_changelog() {
  local package="$1" version="$2" file="$3"

  if [ ! -f "$file" ]; then
    echo "::error::$package: $file not found."
    return 1
  fi

  check_changelog_order "$file" || return 1

  local -a found
  mapfile -t found < <(changelog_versions "$package" "$file")

  if [ "${#found[@]}" -eq 0 ]; then
    echo "::error::$package: no entry in $file. Add '## [$package-$version] - <date>'."
    return 1
  fi

  if [ "${found[0]}" != "$version" ]; then
    echo "::error::$package: the latest entry in $file is $package-${found[0]} but Versions.props says $version. They must match."
    return 1
  fi

  local count=0 entry
  for entry in "${found[@]}"; do
    if [ "$entry" = "$version" ]; then count=$((count + 1)); fi
  done

  if [ "$count" -gt 1 ]; then
    echo "::error::$package: $file has $count entries for $package-$version, it must have exactly one."
    return 1
  fi
}

# changelog_entry <package> <version> <file>: the text of the entry, without its heading, the "---" separators or blank lines at the edges.
# Empty when there is no such entry.
changelog_entry() {
  local package="$1" version="$2" file="$3"

  awk -v heading="## [${package}-${version}]" '
    { sub(/\r$/, "") }
    /^## \[/ { if (inside) exit; inside = (index($0, heading) == 1); next }
    inside && $0 != "---" { lines[++count] = $0 }
    END {
      first = 1; last = count
      while (first <= last && lines[first] ~ /^[[:space:]]*$/) first++
      while (last >= first && lines[last] ~ /^[[:space:]]*$/) last--
      for (i = first; i <= last; i++) print lines[i]
    }
  ' "$file"
}
