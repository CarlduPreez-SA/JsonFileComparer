using System.Text.Json;

namespace JsonFileComparer.Core.Models;

/// <summary>
/// A single difference (or match) found at a specific JSON path when comparing two documents.
/// </summary>
public sealed record DiffEntry
{
    public required string Path { get; init; }

    public required DiffType Type { get; init; }

    public JsonValueKind? LeftKind { get; init; }

    public JsonValueKind? RightKind { get; init; }

    /// <summary>Raw text of the left-hand value, or null if absent.</summary>
    public string? LeftValue { get; init; }

    /// <summary>Raw text of the right-hand value, or null if absent.</summary>
    public string? RightValue { get; init; }
}
