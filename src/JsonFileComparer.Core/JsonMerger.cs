using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonFileComparer.Core;

/// <summary>
/// Builds a merged JSON tree from two documents, resolving each differing path to either the left or right
/// side's value. Uses the exact same object/array pairing as <see cref="JsonComparer"/> (via
/// <see cref="JsonTreePairing"/>), so a resolution keyed by a path from a <see cref="JsonComparer"/> diff
/// always lands on the correct node here.
/// </summary>
public sealed class JsonMerger
{
    private readonly JsonCompareOptions _options;

    public JsonMerger(JsonCompareOptions? options = null)
    {
        _options = options ?? JsonCompareOptions.Default;
    }

    /// <summary>
    /// Produces the merged tree for <paramref name="target"/>: starts from <paramref name="target"/>'s own
    /// content, and for every path present in <paramref name="resolutions"/> whose chosen side differs from
    /// <paramref name="target"/>, substitutes that path's value (or presence/absence) from the other side.
    /// Paths not present in <paramref name="resolutions"/> default to <paramref name="target"/> (i.e. no change).
    /// </summary>
    public JsonNode? Merge(JsonDocument left, JsonDocument right, MergeSide target, IReadOnlyDictionary<string, MergeSide> resolutions)
    {
        var outcome = MergeElements("$", left.RootElement, right.RootElement, target, resolutions);
        return outcome.Value;
    }

    private MergeOutcome MergeElements(string path, JsonElement? leftOpt, JsonElement? rightOpt, MergeSide target, IReadOnlyDictionary<string, MergeSide> resolutions)
    {
        if (_options.TreatNullAsMissing)
        {
            if (leftOpt is { ValueKind: JsonValueKind.Null }) leftOpt = null;
            if (rightOpt is { ValueKind: JsonValueKind.Null }) rightOpt = null;
        }

        if (leftOpt is null && rightOpt is null)
        {
            return MergeOutcome.Missing;
        }

        if (leftOpt is null)
        {
            var choice = resolutions.GetValueOrDefault(path, target);
            return choice == MergeSide.Right ? MergeOutcome.Of(ToJsonNode(rightOpt!.Value)) : MergeOutcome.Missing;
        }

        if (rightOpt is null)
        {
            var choice = resolutions.GetValueOrDefault(path, target);
            return choice == MergeSide.Left ? MergeOutcome.Of(ToJsonNode(leftOpt.Value)) : MergeOutcome.Missing;
        }

        var left = leftOpt.Value;
        var right = rightOpt.Value;

        if (left.ValueKind != right.ValueKind)
        {
            var choice = resolutions.GetValueOrDefault(path, target);
            return MergeOutcome.Of(ToJsonNode(choice == MergeSide.Left ? left : right));
        }

        return left.ValueKind switch
        {
            JsonValueKind.Object => MergeObject(path, left, right, target, resolutions),
            JsonValueKind.Array => MergeArray(path, left, right, target, resolutions),
            _ => MergeScalar(path, left, right, target, resolutions)
        };
    }

    private MergeOutcome MergeObject(string path, JsonElement left, JsonElement right, MergeSide target, IReadOnlyDictionary<string, MergeSide> resolutions)
    {
        var obj = new JsonObject();

        foreach (var pair in JsonTreePairing.PairObjectProperties(path, left, right, _options))
        {
            var outcome = MergeElements(pair.Path, pair.Left, pair.Right, target, resolutions);
            if (outcome.Present)
            {
                var name = pair.Path[(path.Length + 1)..];
                obj[name] = outcome.Value;
            }
        }

        return MergeOutcome.Of(obj);
    }

    private MergeOutcome MergeArray(string path, JsonElement left, JsonElement right, MergeSide target, IReadOnlyDictionary<string, MergeSide> resolutions)
    {
        var array = new JsonArray();

        foreach (var pair in JsonTreePairing.PairArrayElements(path, left, right, _options))
        {
            var outcome = MergeElements(pair.Path, pair.Left, pair.Right, target, resolutions);
            if (outcome.Present)
            {
                array.Add(outcome.Value);
            }
        }

        return MergeOutcome.Of(array);
    }

    private MergeOutcome MergeScalar(string path, JsonElement left, JsonElement right, MergeSide target, IReadOnlyDictionary<string, MergeSide> resolutions)
    {
        var choice = resolutions.GetValueOrDefault(path, target);
        return MergeOutcome.Of(ToJsonNode(choice == MergeSide.Left ? left : right));
    }

    private static JsonNode? ToJsonNode(JsonElement element) => JsonNode.Parse(element.GetRawText());

    private readonly struct MergeOutcome
    {
        public bool Present { get; }
        public JsonNode? Value { get; }

        private MergeOutcome(bool present, JsonNode? value)
        {
            Present = present;
            Value = value;
        }

        public static MergeOutcome Missing { get; } = new(false, null);

        public static MergeOutcome Of(JsonNode? value) => new(true, value);
    }
}
