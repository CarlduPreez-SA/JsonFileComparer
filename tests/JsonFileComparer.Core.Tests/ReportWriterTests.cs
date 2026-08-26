using System.Text.Json;
using JsonFileComparer.Core;
using JsonFileComparer.Core.Models;
using JsonFileComparer.Core.Reporting;
using Xunit;

namespace JsonFileComparer.Core.Tests;

public class ReportWriterTests
{
    private static ComparisonResult BuildSampleResult()
    {
        using var left = JsonDocument.Parse("""{"a":1,"b":2}""");
        using var right = JsonDocument.Parse("""{"a":9,"c":3}""");
        return new JsonComparer().Compare(left, right);
    }

    [Fact]
    public void JsonReportWriter_ProducesParsableJsonWithSummaryAndDifferences()
    {
        var result = BuildSampleResult();

        var json = JsonReportWriter.ToJson(result);
        using var parsed = JsonDocument.Parse(json);

        var summary = parsed.RootElement.GetProperty("summary");
        Assert.False(summary.GetProperty("areEqual").GetBoolean());
        Assert.Equal(1, summary.GetProperty("added").GetInt32());
        Assert.Equal(1, summary.GetProperty("removed").GetInt32());
        Assert.Equal(1, summary.GetProperty("changed").GetInt32());

        var differences = parsed.RootElement.GetProperty("differences").EnumerateArray().ToList();
        Assert.Equal(3, differences.Count);
        Assert.Contains(differences, d => d.GetProperty("path").GetString() == "$.a" && d.GetProperty("type").GetString() == "Changed");
        Assert.Contains(differences, d => d.GetProperty("path").GetString() == "$.b" && d.GetProperty("type").GetString() == "Removed");
        Assert.Contains(differences, d => d.GetProperty("path").GetString() == "$.c" && d.GetProperty("type").GetString() == "Added");
    }

    [Fact]
    public void HtmlReportWriter_ProducesHtmlContainingPathsAndSummaryCounts()
    {
        var result = BuildSampleResult();

        var html = HtmlReportWriter.ToHtml(result, "left.json", "right.json");

        Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("left.json", html);
        Assert.Contains("right.json", html);
        Assert.Contains("$.a", html);
        Assert.Contains("$.b", html);
        Assert.Contains("$.c", html);
        Assert.Contains("Added: 1", html);
        Assert.Contains("Removed: 1", html);
        Assert.Contains("Changed: 1", html);
    }

    [Fact]
    public void HtmlReportWriter_HtmlEncodesValuesToPreventInjection()
    {
        using var left = JsonDocument.Parse("""{"a":"<script>"}""");
        using var right = JsonDocument.Parse("""{"a":"<img src=x>"}""");
        var result = new JsonComparer().Compare(left, right);

        var html = HtmlReportWriter.ToHtml(result, "l", "r");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void WriteToFile_WritesJsonReportToDisk()
    {
        var result = BuildSampleResult();
        var path = Path.Combine(Path.GetTempPath(), $"jfc-test-{Guid.NewGuid():N}.json");

        try
        {
            JsonReportWriter.WriteToFile(result, path);

            Assert.True(File.Exists(path));
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            Assert.True(doc.RootElement.TryGetProperty("summary", out _));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void WriteToFile_WritesHtmlReportToDisk()
    {
        var result = BuildSampleResult();
        var path = Path.Combine(Path.GetTempPath(), $"jfc-test-{Guid.NewGuid():N}.html");

        try
        {
            HtmlReportWriter.WriteToFile(result, "left.json", "right.json", path);

            Assert.True(File.Exists(path));
            Assert.Contains("<html", File.ReadAllText(path), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
