namespace JsonFileComparer.Core.TextDiff;

/// <summary>One line of a file's raw text, tagged with how it relates to the other file's content.</summary>
public sealed record TextLine
{
    /// <summary>1-based line number within its own file.</summary>
    public required int LineNumber { get; init; }

    public required string Text { get; init; }

    public required LineDiffType Type { get; init; }
}
