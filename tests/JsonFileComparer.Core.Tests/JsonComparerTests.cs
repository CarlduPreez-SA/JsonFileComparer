using System.Text.Json;
using JsonFileComparer.Core;
using JsonFileComparer.Core.Models;
using Xunit;

namespace JsonFileComparer.Core.Tests;

public class JsonComparerTests
{
    private static ComparisonResult CompareJson(string leftJson, string rightJson, JsonCompareOptions? options = null)
    {
        using var left = JsonDocument.Parse(leftJson);
        using var right = JsonDocument.Parse(rightJson);
        var comparer = new JsonComparer(options);
        return comparer.Compare(left, right);
    }

    [Fact]
    public void IdenticalObjects_AreEqual()
    {
        var result = CompareJson("""{"a":1,"b":"x"}""", """{"a":1,"b":"x"}""");

        Assert.True(result.AreEqual);
        Assert.Equal(0, result.AddedCount + result.RemovedCount + result.ChangedCount);
    }

    [Fact]
    public void MissingPropertyOnRight_IsReportedAsRemoved()
    {
        var result = CompareJson("""{"a":1,"b":2}""", """{"a":1}""");

        var removed = Assert.Single(result.Entries, e => e.Type == DiffType.Removed);
        Assert.Equal("$.b", removed.Path);
        Assert.Equal("2", removed.LeftValue);
    }

    [Fact]
    public void ExtraPropertyOnRight_IsReportedAsAdded()
    {
        var result = CompareJson("""{"a":1}""", """{"a":1,"c":3}""");

        var added = Assert.Single(result.Entries, e => e.Type == DiffType.Added);
        Assert.Equal("$.c", added.Path);
        Assert.Equal("3", added.RightValue);
    }

    [Fact]
    public void ChangedScalarValue_IsReportedAsChanged()
    {
        var result = CompareJson("""{"a":1}""", """{"a":2}""");

        var changed = Assert.Single(result.Entries, e => e.Type == DiffType.Changed);
        Assert.Equal("$.a", changed.Path);
        Assert.Equal("1", changed.LeftValue);
        Assert.Equal("2", changed.RightValue);
    }

    [Fact]
    public void DifferentValueKinds_AreReportedAsTypeChanged()
    {
        var result = CompareJson("""{"a":1}""", """{"a":"1"}""");

        var typeChanged = Assert.Single(result.Entries, e => e.Type == DiffType.TypeChanged);
        Assert.Equal(JsonValueKind.Number, typeChanged.LeftKind);
        Assert.Equal(JsonValueKind.String, typeChanged.RightKind);
    }

    [Fact]
    public void NestedObjects_ProduceDottedPaths()
    {
        var result = CompareJson(
            """{"a":{"b":{"c":1}}}""",
            """{"a":{"b":{"c":2}}}""");

        var changed = Assert.Single(result.Entries, e => e.Type == DiffType.Changed);
        Assert.Equal("$.a.b.c", changed.Path);
    }

    [Fact]
    public void ArraysCompareByIndex_WhenNoKeyFieldPresent()
    {
        var result = CompareJson("""{"a":[1,2,3]}""", """{"a":[1,9,3,4]}""");

        var changed = Assert.Single(result.Entries, e => e.Type == DiffType.Changed);
        Assert.Equal("$.a[1]", changed.Path);

        var added = Assert.Single(result.Entries, e => e.Type == DiffType.Added);
        Assert.Equal("$.a[3]", added.Path);
    }

    [Fact]
    public void ArraysCompareByKey_WhenIdFieldPresentOnBothSides_EvenIfReordered()
    {
        var left = """{"items":[{"id":1,"name":"a"},{"id":2,"name":"b"}]}""";
        var right = """{"items":[{"id":2,"name":"b-changed"},{"id":1,"name":"a"}]}""";

        var result = CompareJson(left, right);

        var changed = Assert.Single(result.Entries, e => e.Type == DiffType.Changed);
        Assert.Equal("$.items[id=2].name", changed.Path);
    }

    [Fact]
    public void ArraysCompareByKey_DetectsAddedAndRemovedElements()
    {
        var left = """{"items":[{"id":1},{"id":2}]}""";
        var right = """{"items":[{"id":2},{"id":3}]}""";

        var result = CompareJson(left, right);

        Assert.Contains(result.Entries, e => e.Type == DiffType.Removed && e.Path == "$.items[id=1]");
        Assert.Contains(result.Entries, e => e.Type == DiffType.Added && e.Path == "$.items[id=3]");
    }

    [Fact]
    public void ForcedIndexMode_IgnoresKeyFieldEvenWhenPresent()
    {
        var left = """{"items":[{"id":1,"v":"a"},{"id":2,"v":"b"}]}""";
        var right = """{"items":[{"id":2,"v":"b"},{"id":1,"v":"a"}]}""";

        var options = new JsonCompareOptions { ArrayComparisonMode = ArrayComparisonMode.Index };
        var result = CompareJson(left, right, options);

        Assert.Contains(result.Entries, e => e.Type == DiffType.Changed && e.Path == "$.items[0].id");
        Assert.Contains(result.Entries, e => e.Type == DiffType.Changed && e.Path == "$.items[0].v");
    }

    [Fact]
    public void NumericTolerance_TreatsCloseValuesAsEqual()
    {
        var options = new JsonCompareOptions { NumericTolerance = 0.01 };
        var result = CompareJson("""{"a":1.001}""", """{"a":1.002}""", options);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void CaseInsensitivePropertyNames_MatchDifferentlyCasedKeys()
    {
        var options = new JsonCompareOptions { CaseSensitivePropertyNames = false };
        var result = CompareJson("""{"Name":"x"}""", """{"name":"x"}""", options);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void TreatNullAsMissing_MakesNullAndAbsentEquivalent()
    {
        var options = new JsonCompareOptions { TreatNullAsMissing = true };
        var result = CompareJson("""{"a":null}""", """{}""", options);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void IncludeUnchangedFalse_OmitsUnchangedEntries()
    {
        var options = new JsonCompareOptions { IncludeUnchanged = false };
        var result = CompareJson("""{"a":1,"b":2}""", """{"a":1,"b":3}""", options);

        Assert.DoesNotContain(result.Entries, e => e.Type == DiffType.Unchanged);
        Assert.Single(result.Entries);
    }
}
