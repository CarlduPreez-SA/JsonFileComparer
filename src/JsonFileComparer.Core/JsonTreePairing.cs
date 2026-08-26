using System.Text.Json;

namespace JsonFileComparer.Core;

/// <summary>
/// Shared object/array child-pairing logic used by both <see cref="JsonComparer"/> and <see cref="JsonMerger"/>,
/// so the two always agree on which left/right elements correspond to which path — critical since a merge
/// resolution is looked up by the exact path string the diff view showed the user.
/// </summary>
internal static class JsonTreePairing
{
    internal readonly record struct Pair(string Path, JsonElement? Left, JsonElement? Right);

    internal static IEnumerable<Pair> PairObjectProperties(string basePath, JsonElement left, JsonElement right, JsonCompareOptions options)
    {
        var comparer = options.PropertyNameComparer;
        var leftProps = left.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, comparer);
        var rightProps = right.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, comparer);

        var allNames = new HashSet<string>(leftProps.Keys, comparer);
        allNames.UnionWith(rightProps.Keys);

        foreach (var name in allNames.OrderBy(n => n, comparer))
        {
            JsonElement? l = leftProps.TryGetValue(name, out var lv) ? lv : null;
            JsonElement? r = rightProps.TryGetValue(name, out var rv) ? rv : null;
            yield return new Pair($"{basePath}.{name}", l, r);
        }
    }

    internal static IEnumerable<Pair> PairArrayElements(string basePath, JsonElement left, JsonElement right, JsonCompareOptions options)
    {
        string? keyField = options.ArrayComparisonMode switch
        {
            ArrayComparisonMode.Key => DetermineKeyField(left, right, options),
            ArrayComparisonMode.Auto => DetermineKeyField(left, right, options),
            _ => null
        };

        return keyField is not null
            ? PairArrayElementsByKey(basePath, left, right, keyField)
            : PairArrayElementsByIndex(basePath, left, right);
    }

    private static IEnumerable<Pair> PairArrayElementsByIndex(string basePath, JsonElement left, JsonElement right)
    {
        var leftItems = left.EnumerateArray().ToList();
        var rightItems = right.EnumerateArray().ToList();
        var max = Math.Max(leftItems.Count, rightItems.Count);

        for (var i = 0; i < max; i++)
        {
            JsonElement? l = i < leftItems.Count ? leftItems[i] : null;
            JsonElement? r = i < rightItems.Count ? rightItems[i] : null;
            yield return new Pair($"{basePath}[{i}]", l, r);
        }
    }

    private static IEnumerable<Pair> PairArrayElementsByKey(string basePath, JsonElement left, JsonElement right, string keyField)
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
            JsonElement? l = leftMap.TryGetValue(key, out var lv) ? lv : null;
            JsonElement? r = rightMap.TryGetValue(key, out var rv) ? rv : null;
            yield return new Pair($"{basePath}[{keyField}={key}]", l, r);
        }
    }

    /// <summary>
    /// Finds the first candidate key field (from <see cref="JsonCompareOptions.ArrayKeyFieldNames"/>) that is
    /// present, scalar and unique on every element of both arrays — making key-based matching safe to use.
    /// Returns null (meaning: fall back to index-based comparison) if no such field exists, or either array
    /// is empty or contains non-object elements.
    /// </summary>
    private static string? DetermineKeyField(JsonElement left, JsonElement right, JsonCompareOptions options)
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

        foreach (var field in options.ArrayKeyFieldNames)
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
}
