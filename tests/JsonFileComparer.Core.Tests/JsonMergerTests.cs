using System.Text.Json;
using System.Text.Json.Nodes;
using JsonFileComparer.Core;
using Xunit;

namespace JsonFileComparer.Core.Tests;

public class JsonMergerTests
{
    private static JsonNode? Merge(string leftJson, string rightJson, MergeSide target, IReadOnlyDictionary<string, MergeSide> resolutions, JsonCompareOptions? options = null)
    {
        using var left = JsonDocument.Parse(leftJson);
        using var right = JsonDocument.Parse(rightJson);
        return new JsonMerger(options).Merge(left, right, target, resolutions);
    }

    [Fact]
    public void NoResolutions_TargetLeft_ProducesLeftUnchanged()
    {
        var result = Merge("""{"a":1,"b":2}""", """{"a":9,"c":3}""", MergeSide.Left, new Dictionary<string, MergeSide>());

        Assert.Equal("""{"a":1,"b":2}""", result!.ToJsonString());
    }

    [Fact]
    public void NoResolutions_TargetRight_ProducesRightUnchanged()
    {
        var result = Merge("""{"a":1,"b":2}""", """{"a":9,"c":3}""", MergeSide.Right, new Dictionary<string, MergeSide>());

        Assert.Equal("""{"a":9,"c":3}""", result!.ToJsonString());
    }

    [Fact]
    public void ChangedValue_ResolvedToRight_PullsRightValueIntoLeftTarget()
    {
        var resolutions = new Dictionary<string, MergeSide> { ["$.a"] = MergeSide.Right };
        var result = Merge("""{"a":1,"b":2}""", """{"a":9,"b":2}""", MergeSide.Left, resolutions);

        Assert.Equal(9, result!["a"]!.GetValue<int>());
        Assert.Equal(2, result["b"]!.GetValue<int>());
    }

    [Fact]
    public void AddedProperty_ResolvedToRight_AddsItToLeftTarget()
    {
        var resolutions = new Dictionary<string, MergeSide> { ["$.c"] = MergeSide.Right };
        var result = Merge("""{"a":1}""", """{"a":1,"c":3}""", MergeSide.Left, resolutions);

        Assert.Equal(3, result!["c"]!.GetValue<int>());
    }

    [Fact]
    public void AddedProperty_DefaultResolution_DoesNotAddToLeftTarget()
    {
        var result = Merge("""{"a":1}""", """{"a":1,"c":3}""", MergeSide.Left, new Dictionary<string, MergeSide>());

        Assert.False(((JsonObject)result!).ContainsKey("c"));
    }

    [Fact]
    public void RemovedProperty_ResolvedToRight_RemovesItFromLeftTarget()
    {
        var resolutions = new Dictionary<string, MergeSide> { ["$.b"] = MergeSide.Right };
        var result = Merge("""{"a":1,"b":2}""", """{"a":1}""", MergeSide.Left, resolutions);

        Assert.False(((JsonObject)result!).ContainsKey("b"));
    }

    [Fact]
    public void RemovedProperty_DefaultResolution_KeepsItInLeftTarget()
    {
        var result = Merge("""{"a":1,"b":2}""", """{"a":1}""", MergeSide.Left, new Dictionary<string, MergeSide>());

        Assert.Equal(2, result!["b"]!.GetValue<int>());
    }

    [Fact]
    public void NestedObjectValue_ResolvedToRight_OnlyChangesThatLeaf()
    {
        var resolutions = new Dictionary<string, MergeSide> { ["$.meta.env"] = MergeSide.Right };
        var left = """{"meta":{"env":"staging","owner":"team-a"}}""";
        var right = """{"meta":{"env":"production","owner":"team-b"}}""";

        var result = Merge(left, right, MergeSide.Left, resolutions);

        Assert.Equal("production", result!["meta"]!["env"]!.GetValue<string>());
        Assert.Equal("team-a", result["meta"]!["owner"]!.GetValue<string>());
    }

    [Fact]
    public void ArrayElement_KeyMatched_ResolvedToRight_UpdatesOnlyThatElement()
    {
        var left = """{"items":[{"id":1,"v":"a"},{"id":2,"v":"b"}]}""";
        var right = """{"items":[{"id":1,"v":"a"},{"id":2,"v":"b-updated"}]}""";
        var resolutions = new Dictionary<string, MergeSide> { ["$.items[id=2].v"] = MergeSide.Right };

        var result = Merge(left, right, MergeSide.Left, resolutions);

        var items = result!["items"]!.AsArray();
        Assert.Equal("a", items[0]!["v"]!.GetValue<string>());
        Assert.Equal("b-updated", items[1]!["v"]!.GetValue<string>());
    }

    [Fact]
    public void ArrayElement_AddedByKey_ResolvedToRight_AppendsElement()
    {
        var left = """{"items":[{"id":1}]}""";
        var right = """{"items":[{"id":1},{"id":2}]}""";
        var resolutions = new Dictionary<string, MergeSide> { ["$.items[id=2]"] = MergeSide.Right };

        var result = Merge(left, right, MergeSide.Left, resolutions);

        var items = result!["items"]!.AsArray();
        Assert.Equal(2, items.Count);
        Assert.Equal(2, items[1]!["id"]!.GetValue<int>());
    }

    [Fact]
    public void ArrayElement_RemovedByKey_ResolvedToRight_DropsElement()
    {
        var left = """{"items":[{"id":1},{"id":2}]}""";
        var right = """{"items":[{"id":1}]}""";
        var resolutions = new Dictionary<string, MergeSide> { ["$.items[id=2]"] = MergeSide.Right };

        var result = Merge(left, right, MergeSide.Left, resolutions);

        var items = result!["items"]!.AsArray();
        Assert.Single(items);
        Assert.Equal(1, items[0]!["id"]!.GetValue<int>());
    }

    [Fact]
    public void JsonNull_IsPreservedAsExplicitNull_NotOmitted()
    {
        var resolutions = new Dictionary<string, MergeSide> { ["$.a"] = MergeSide.Right };
        var result = Merge("""{"a":1}""", """{"a":null}""", MergeSide.Left, resolutions);

        Assert.True(((JsonObject)result!).ContainsKey("a"));
        Assert.Equal(JsonValueKind.Null, result!["a"]?.GetValueKind() ?? JsonValueKind.Null);
    }

    [Fact]
    public void TypeChangedValue_ResolvedToRight_TakesRightsKindAndValue()
    {
        var resolutions = new Dictionary<string, MergeSide> { ["$.a"] = MergeSide.Right };
        var result = Merge("""{"a":1}""", """{"a":"one"}""", MergeSide.Left, resolutions);

        Assert.Equal("one", result!["a"]!.GetValue<string>());
    }
}
