using System.Text.Json;
using JsonFileComparer.Core.Models;

namespace JsonFileComparer.Core;

/// <summary>
/// Recursively compares two JSON documents and produces a flat list of differences
/// (added, removed, changed, type-changed and, optionally, unchanged values) keyed by JSON path.
/// </summary>
public sealed class JsonComparer
{
    private readonly JsonCompareOptions _options;

    public JsonComparer(JsonCompareOptions? options = null)
    {
        _options = options ?? JsonCompareOptions.Default;
    }

    public ComparisonResult Compare(JsonDocument left, JsonDocument right)
    {
        var entries = new List<DiffEntry>();
        CompareElements("$", left.RootElement, right.RootElement, entries);
        return new ComparisonResult { Entries = entries };
    }

    private void CompareElements(string path, JsonElement? leftOpt, JsonElement? rightOpt, List<DiffEntry> entries)
    {
        if (_options.TreatNullAsMissing)
        {
            if (leftOpt is { ValueKind: JsonValueKind.Null }) leftOpt = null;
            if (rightOpt is { ValueKind: JsonValueKind.Null }) rightOpt = null;
        }

        if (leftOpt is null && rightOpt is null)
        {
            return;
        }

        if (leftOpt is null)
        {
            entries.Add(new DiffEntry
            {
                Path = path,
                Type = DiffType.Added,
                RightKind = rightOpt!.Value.ValueKind,
                RightValue = rightOpt.Value.GetRawText()
            });
            return;
        }

        if (rightOpt is null)
        {
            entries.Add(new DiffEntry
            {
                Path = path,
                Type = DiffType.Removed,
                LeftKind = leftOpt!.Value.ValueKind,
                LeftValue = leftOpt.Value.GetRawText()
            });
            return;
        }

        var left = leftOpt.Value;
        var right = rightOpt.Value;

        if (left.ValueKind != right.ValueKind)
        {
            entries.Add(new DiffEntry
            {
                Path = path,
                Type = DiffType.TypeChanged,
                LeftKind = left.ValueKind,
                RightKind = right.ValueKind,
                LeftValue = left.GetRawText(),
                RightValue = right.GetRawText()
            });
            return;
        }

        switch (left.ValueKind)
        {
            case JsonValueKind.Object:
                CompareObjects(path, left, right, entries);
                break;
            case JsonValueKind.Array:
                CompareArrays(path, left, right, entries);
                break;
            default:
                CompareScalars(path, left, right, entries);
                break;
        }
    }

    private void CompareObjects(string path, JsonElement left, JsonElement right, List<DiffEntry> entries)
    {
        var comparer = _options.PropertyNameComparer;
        var leftProps = left.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, comparer);
        var rightProps = right.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, comparer);

        var allNames = new HashSet<string>(leftProps.Keys, comparer);
        allNames.UnionWith(rightProps.Keys);

        foreach (var name in allNames.OrderBy(n => n, comparer))
        {
            var childPath = $"{path}.{name}";
            JsonElement? l = leftProps.TryGetValue(name, out var lv) ? lv : null;
            JsonElement? r = rightProps.TryGetValue(name, out var rv) ? rv : null;
            CompareElements(childPath, l, r, entries);
        }
    }

    private void CompareArrays(string path, JsonElement left, JsonElement right, List<DiffEntry> entries)
    {
        string? keyField = _options.ArrayComparisonMode switch
        {
            ArrayComparisonMode.Key => DetermineKeyField(left, right),
            ArrayComparisonMode.Auto => DetermineKeyField(left, right),
            _ => null
        };

        if (keyField is not null)
        {
            CompareArraysByKey(path, left, right, keyField, entries);
        }
        else
        {
            CompareArraysByIndex(path, left, right, entries);
        }
    }

    private void CompareArraysByIndex(string path, JsonElement left, JsonElement right, List<DiffEntry> entries)
    {
        var leftItems = left.EnumerateArray().ToList();
        var rightItems = right.EnumerateArray().ToList();
        var max = Math.Max(leftItems.Count, rightItems.Count);

        for (var i = 0; i < max; i++)
        {
            var childPath = $"{path}[{i}]";
            JsonElement? l = i < leftItems.Count ? leftItems[i] : null;
            JsonElement? r = i < rightItems.Count ? rightItems[i] : null;
            CompareElements(childPath, l, r, entries);
        }
    }

    private void CompareArraysByKey(string path, JsonElement left, JsonElement right, string keyField, List<DiffEntry> entries)
    {
        var leftItems = left.EnumerateArray().ToList();
        var rightItems = right.EnumerateArray().ToList();

        var leftMap = leftItems.ToDictionary(item => KeyToString(item.GetProperty(keyField)), StringComparer.Ordinal);
        var rightMap = rightItems.ToDictionary(item => KeyToString(item.GetProperty(keyField)), StringComparer.Ordinal);

        var orderedKeys = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in leftItems)
        {
            var k = KeyToString(item.GetProperty(keyField));
            if (seen.Add(k)) orderedKeys.Add(k);
        }
        foreach (var item in rightItems)
        {
            var k = KeyToString(item.GetProperty(keyField));
            if (seen.Add(k)) orderedKeys.Add(k);
        }

        foreach (var key in orderedKeys)
        {
            var childPath = $"{path}[{keyField}={key}]";
            JsonElement? l = leftMap.TryGetValue(key, out var lv) ? lv : null;
            JsonElement? r = rightMap.TryGetValue(key, out var rv) ? rv : null;
            CompareElements(childPath, l, r, entries);
        }
    }

    /// <summary>
    /// Finds the first candidate key field (from <see cref="JsonCompareOptions.ArrayKeyFieldNames"/>) that is
    /// present, scalar and unique on every element of both arrays — making key-based matching safe to use.
    /// Returns null (meaning: fall back to index-based comparison) if no such field exists, or either array
    /// is empty or contains non-object elements.
    /// </summary>
    private string? DetermineKeyField(JsonElement left, JsonElement right)
    {
        var leftItems = left.EnumerateArray().ToList();
        var rightItems = right.EnumerateArray().ToList();

        if (leftItems.Count == 0 || rightItems.Count == 0)
        {
            return null;
        }

        if (leftItems.Any(e => e.ValueKind != JsonValueKind.Object) ||
            rightItems.Any(e => e.ValueKind != JsonValueKind.Object))
        {
            return null;
        }

        foreach (var field in _options.ArrayKeyFieldNames)
        {
            if (HasUniqueScalarKey(leftItems, field) && HasUniqueScalarKey(rightItems, field))
            {
                return field;
            }
        }

        return null;
    }

    private static bool HasUniqueScalarKey(List<JsonElement> items, string field)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (!item.TryGetProperty(field, out var keyEl))
            {
                return false;
            }

            if (keyEl.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False))
            {
                return false;
            }

            if (!seen.Add(KeyToString(keyEl)))
            {
                return false;
            }
        }

        return true;
    }

    private static string KeyToString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? string.Empty,
        _ => element.GetRawText()
    };

    private void CompareScalars(string path, JsonElement left, JsonElement right, List<DiffEntry> entries)
    {
        var equal = left.ValueKind switch
        {
            JsonValueKind.String => string.Equals(left.GetString(), right.GetString(), StringComparison.Ordinal),
            JsonValueKind.Number => NumbersEqual(left, right),
            JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null => true,
            _ => left.GetRawText() == right.GetRawText()
        };

        if (!equal)
        {
            entries.Add(new DiffEntry
            {
                Path = path,
                Type = DiffType.Changed,
                LeftKind = left.ValueKind,
                RightKind = right.ValueKind,
                LeftValue = left.GetRawText(),
                RightValue = right.GetRawText()
            });
        }
        else if (_options.IncludeUnchanged)
        {
            entries.Add(new DiffEntry
            {
                Path = path,
                Type = DiffType.Unchanged,
                LeftKind = left.ValueKind,
                RightKind = right.ValueKind,
                LeftValue = left.GetRawText(),
                RightValue = right.GetRawText()
            });
        }
    }

    private bool NumbersEqual(JsonElement left, JsonElement right)
    {
        if (left.GetRawText() == right.GetRawText())
        {
            return true;
        }

        if (_options.NumericTolerance > 0)
        {
            return Math.Abs(left.GetDouble() - right.GetDouble()) <= _options.NumericTolerance;
        }

        if (left.TryGetDecimal(out var ld) && right.TryGetDecimal(out var rd))
        {
            return ld == rd;
        }

        return left.GetDouble().Equals(right.GetDouble());
    }
}
