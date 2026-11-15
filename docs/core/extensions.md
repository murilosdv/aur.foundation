[Home](../../README.md) · [Documentation](../README.md) · [Core](./README.md)

# Extensions

Small helpers in `Aurore.Foundation.Core.Extensions`.

## Strings

| Method | Does | Example |
|---|---|---|
| `HasValue()` | True when not null, empty or whitespace (also tells the compiler it is not null). | `name.HasValue()` |
| `RemoveDiacritics()` | Removes accents. Null becomes empty. | `"Ação"` → `"Acao"` |
| `IsEqualTo(other)` | Case-insensitive, culture-independent comparison. | |
| `SplitWords()` | Runs of letters and digits; case changes also split. | `"BlogService"` → `["Blog", "Service"]` |
| `ToSentenceCase()` | Only the first word capitalized, acronyms kept. | |
| `ToTitleCase()` | Every word capitalized, acronyms kept. | `"blogService"` → `"Blog Service"` |
| `ToPascalCase()` | Words joined, each capitalized. | `"blog service"` → `"BlogService"` |
| `ToCamelCase()` | Like `System.Text.Json` camel case. | `"PageSize"` → `"pageSize"` |
| `ToSnakeCase(lowercase = true)` | Words joined by underscores, without accents. | `"BlogService"` → `"blog_service"` |
| `ToSlugCase(lowercase = true)` | Words joined by hyphens, without accents. | `"BlogService"` → `"blog-service"` |
| `ToPlural()` / `ToSingular()` | English, irregular ones included. | `"person"` → `"people"` |

Null input gives an empty string, never an exception.

## URLs

| Method | True when |
|---|---|
| `IsAbsoluteUrl()` | The text is an absolute URL. |
| `IsHttpsUrl()` | It is an absolute URL using HTTPS. |
| `IsLocalUrl()` | It points to a loopback address or a `*.localhost` host: addresses that only make sense on a developer machine. The production validators use it to reject development values. |

## Enums

| Method | Does |
|---|---|
| `ParseEnum<T>()` | Case-insensitive parse; **throws** on unknown values (including numbers that map to no member), so a bad configuration fails at startup. |
| `ParseEnumOrDefault(fallback)` | Case-insensitive parse that returns the fallback instead of throwing. |

## Paths and environment

| Method | Does |
|---|---|
| `Path.NormalizeRuntimePath(params string[] paths)` | Combines segments into a full path, translating Windows drive paths (`C:\x`) when running under WSL. |
| `OperatingSystem.IsWsl()` | Linux inside the Windows Subsystem for Linux. |
| `OperatingSystem.IsNativeLinux()` | Linux outside WSL. |
