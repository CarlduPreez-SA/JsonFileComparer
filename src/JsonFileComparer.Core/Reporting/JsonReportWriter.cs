using System.Text.Json;
using JsonFileComparer.Core.Models;

namespace JsonFileComparer.Core.Reporting;

/// <summary>Serializes a <see cref="ComparisonResult"/> to a machine-readable JSON diff report.</summary>
public static class JsonReportWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static string ToJson(ComparisonResult result)
    {
        var report = new
        {
            summary = new
            {
                areEqual = result.AreEqual,
                added = result.AddedCount,
                removed = result.RemovedCount,
                changed = result.ChangedCount,
                unchanged = result.UnchangedCount
            },
            differences = result.Entries
                .Where(e => e.Type != DiffType.Unchanged)
                .Select(e => new
                {
                    path = e.Path,
                    type = e.Type.ToString(),
                    leftValue = e.LeftValue,
                    rightValue = e.RightValue
                })
        };

        return JsonSerializer.Serialize(report, SerializerOptions);
    }

    public static void WriteToFile(ComparisonResult result, string path)
    {
        File.WriteAllText(path, ToJson(result));
    }
}
