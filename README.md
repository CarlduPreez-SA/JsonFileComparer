# JsonFileComparer

A .NET 10 desktop application for accurately comparing two JSON files side by side — see exactly what values changed, what was added, and what's missing.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)

## Features

- **Accurate structural diffing** — recursively compares two JSON documents and classifies every difference as **Added**, **Removed**, **Changed**, or **Type changed** (e.g. a number became a string).
- **Smart array comparison** — arrays of objects are matched by an identifying key (`id`, `name`, etc.) when one is reliably present on both sides, so reordering or inserting an element doesn't produce a wall of false differences. Falls back to strict positional (index) comparison otherwise. The mode can be forced either way.
- **Configurable comparison rules**:
  - Case-sensitive or case-insensitive property name matching
  - Numeric tolerance (treat `1.000` and `1.0001` as equal)
  - Treat JSON `null` the same as a missing property
  - Optionally include unchanged values in the output
- **Side-by-side desktop UI** built with [Avalonia](https://avaloniaui.net/), showing every difference in a sortable, color-coded grid.
- **Exportable reports** — save the diff as a machine-readable JSON report or a self-contained, shareable HTML report.

## Project structure

```
JsonFileComparer/
├── src/
│   ├── JsonFileComparer.Core/    Comparison engine, options, and report writers (no UI dependencies)
│   └── JsonFileComparer.App/     Avalonia MVVM desktop application
└── tests/
    └── JsonFileComparer.Core.Tests/   xUnit tests for the comparison engine and report writers
```

The comparison logic lives entirely in `JsonFileComparer.Core`, a plain class library with no UI dependencies — it can be reused from a CLI, a web API, or any other host.

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

1. Browse to (or type the path of) a **left** and a **right** JSON file.
2. Adjust comparison options if needed:
   - **Array mode** — `Auto` (default), `Index`, or `Key`
   - **Case-sensitive keys** — whether object property names must match case exactly
   - **Treat null as missing** — whether `"a": null` and a missing `"a"` count as equal
   - **Show unchanged** — include values that are identical in both files
   - **Numeric tolerance** — allowed absolute difference between two numbers before they're reported as changed
3. Click **Compare**. Differences appear in the grid, color-coded by type, with the JSON path, and both the left and right values.
4. Use **Export JSON...** or **Export HTML...** to save the report to disk.

## How comparison paths work

Each difference is reported against a JSON-path-like location, rooted at `$`:

| Example path         | Meaning                                              |
|-----------------------|-------------------------------------------------------|
| `$.name`              | The top-level `name` property                        |
| `$.meta.created`      | A nested property                                     |
| `$.tags[2]`            | The 3rd element of the `tags` array (index-based)     |
| `$.items[id=3]`        | The array element of `items` whose `id` is `3` (key-based) |

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
