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
                foreach (var pair in JsonTreePairing.PairObjectProperties(path, left, right, _options))
                {
                    CompareElements(pair.Path, pair.Left, pair.Right, entries);
                }
                break;
            case JsonValueKind.Array:
                foreach (var pair in JsonTreePairing.PairArrayElements(path, left, right, _options))
                {
                    CompareElements(pair.Path, pair.Left, pair.Right, entries);
                }
                break;
            default:
                CompareScalars(path, left, right, entries);
                break;
        }
    }

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
