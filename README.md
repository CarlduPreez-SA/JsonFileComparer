# JsonFileComparer

A .NET 10 desktop application for accurately comparing two config files side by side — see exactly what values changed, what was added, and what's missing. Supports both **JSON** (`appsettings.json`) and **XML** (`web.config`, `applicationHost.config`) files, making it well suited to comparing IIS application configs across environments.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)

## Features

- **JSON and XML support** — compare `appsettings.json` files, `web.config` files, or any mix of the two. The file format is auto-detected from the extension (`.json`, `.xml`, `.config`) or, failing that, by sniffing the content.
- **Accurate structural diffing** — recursively compares two documents and classifies every difference as **Added**, **Removed**, **Changed**, or **Type changed** (e.g. a number became a string).
- **Smart array comparison** — arrays of objects (or, in XML, repeated sibling elements like `<add key="..." value="..."/>`) are matched by an identifying key (`id`, `name`, `key`, or their XML attribute equivalents) when one is reliably present on both sides, so reordering or inserting an element doesn't produce a wall of false differences. Falls back to strict positional (index) comparison otherwise. The mode can be forced either way.
- **Configurable comparison rules**:
  - Case-sensitive or case-insensitive property name matching
  - Numeric tolerance (treat `1.000` and `1.0001` as equal)
  - Treat JSON `null` the same as a missing property
  - Optionally include unchanged values in the output
- **Side-by-side desktop UI** built with [Avalonia](https://avaloniaui.net/), showing every difference in a sortable, color-coded grid.
- **Text view** — switch to a Notepad-style side-by-side view of the raw file text, with changed/added/removed lines highlighted. Both panes are fully editable; edits can be saved straight back to the file (with a backup, same as merge).
- **Selective merge** — per difference, choose whether the left or right value should win, then apply your selections directly to one of the two files. A timestamped backup of the overwritten file is created automatically before every merge.
- **Exportable reports** — save the diff as a machine-readable JSON report or a self-contained, shareable HTML report.

## Project structure

```
JsonFileComparer/
├── src/
│   ├── JsonFileComparer.Core/    Comparison engine, merge engine, text-line diff, options, and report writers (no UI dependencies)
│   └── JsonFileComparer.App/     Avalonia MVVM desktop application
└── tests/
    └── JsonFileComparer.Core.Tests/   xUnit tests for the comparison, merge, and text-diff engines, and report writers
```

The comparison logic lives entirely in `JsonFileComparer.Core`, a plain class library with no UI dependencies — it can be reused from a CLI, a web API, or any other host. XML files are normalized into the same JSON tree shape before comparison (see [How XML is compared](#how-xml-is-compared) below), so the exact same diff engine and options apply to both formats.

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build

```bash
dotnet build
```

### Run the app

```bash
dotnet run --project src/JsonFileComparer.App
```

### Run the tests

```bash
dotnet test
```

## Using the app

1. Browse to (or type the path of) a **left** and a **right** config file — JSON and XML can be freely mixed.
2. Adjust comparison options if needed:
   - **Array mode** — `Auto` (default), `Index`, or `Key`
   - **Case-sensitive keys** — whether object property names must match case exactly
   - **Treat null as missing** — whether `"a": null` and a missing `"a"` count as equal
   - **Show unchanged** — include values that are identical in both files
   - **Numeric tolerance** — allowed absolute difference between two numbers before they're reported as changed
3. Click **Compare**. Differences appear in the grid, color-coded by type, with the JSON path, and both the left and right values.
4. Use **Export JSON...** or **Export HTML...** to save the report to disk.

### Merging selected values across files

1. Choose which file to **Overwrite** — Left or Right. Every row defaults to keeping that file's own current value (i.e. nothing changes until you say so).
2. On any row you want to change, flip its **Keep** toggle to the other side. This works the same way for changed values, values only present on one side (added/removed), and array elements matched by key.
3. Click **Apply Merge...** and confirm. The target file is overwritten with your selections applied on top of its own content; everything else is left untouched.
4. A backup of the target file (`<filename>.bak-<timestamp>`) is written alongside it before every merge, so a merge is always reversible.

If the target file is XML, the merged result is written back out as valid XML (see below) — never as JSON — and vice versa.

### Text view

Switch **View** to **Text** to see both files as plain text, side by side, with line numbers — like Notepad, but with differences highlighted:

- **Yellow** — the line changed between files
- **Green** — the line only exists on the right
- **Red** — the line only exists on the left

This is a separate, purely line-based diff (the same family of algorithm behind `diff`/`git diff`) — independent of the structural JSON/XML comparison used by the grid and merge views, so it reflects the files' literal text, not their parsed structure.

Both panes are editable. **Refresh Diff** recomputes the highlighting from your current edits without touching disk. **Save Left** / **Save Right** writes a pane's current text back to its file (with a confirmation and an automatic backup, same as merge), then re-runs the comparison so the grid, summary, and both views all stay in sync.

## How comparison paths work

Each difference is reported against a JSON-path-like location, rooted at `$`:

| Example path         | Meaning                                              |
|-----------------------|-------------------------------------------------------|
| `$.name`              | The top-level `name` property                        |
| `$.meta.created`      | A nested property                                     |
| `$.tags[2]`            | The 3rd element of the `tags` array (index-based)     |
| `$.items[id=3]`        | The array element of `items` whose `id` is `3` (key-based) |

## How XML is compared

XML files (`web.config`, `applicationHost.config`, etc.) are converted into the same JSON-like tree the JSON comparer already understands, using these rules:

- The root element becomes a single top-level property named after itself, e.g. `<configuration>` → `$.configuration`.
- Attributes become properties prefixed with `@` — `<add key="Foo" value="Bar" />` → `{"@key": "Foo", "@value": "Bar"}`.
- A leaf element with no attributes and no children becomes a plain string value.
- Repeated sibling elements with the same tag name (e.g. multiple `<add>` entries under `<appSettings>`) become an array, matched by key just like a JSON array of objects — so the default array-key candidates include `@key`, `@name`, and `@id` alongside their unprefixed JSON equivalents.

For example, comparing this `web.config` fragment:

```xml
<appSettings>
  <add key="Environment" value="Staging" />
  <add key="FeatureFlagX" value="false" />
</appSettings>
```

against:

```xml
<appSettings>
  <add key="Environment" value="Production" />
  <add key="FeatureFlagY" value="true" />
</appSettings>
```

correctly reports `Environment` as **Changed**, `FeatureFlagX` as **Removed**, and `FeatureFlagY` as **Added** — matched by the `key` attribute, not by position, so reordering `<add>` entries doesn't produce false differences.

## Publishing a standalone executable

Self-contained, single-file publish profiles are provided for Windows, Linux, and macOS:

```bash
dotnet publish src/JsonFileComparer.App -c Release -p:PublishProfile=win-x64
dotnet publish src/JsonFileComparer.App -c Release -p:PublishProfile=linux-x64
dotnet publish src/JsonFileComparer.App -c Release -p:PublishProfile=osx-x64
```

The output executable (no separate .NET runtime install required on the target machine) is written to `src/JsonFileComparer.App/bin/Release/net10.0/<rid>/publish/`.

## License

See [LICENSE](LICENSE).
