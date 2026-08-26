namespace JsonFileComparer.Core.TextDiff;

/// <summary>
/// The result of a line-level text diff: every line of the left file and every line of the right file,
/// each independently tagged Unchanged/Changed/Added/Removed. Both lists are complete (no lines omitted),
/// so each side can be rendered as its own full, accurate document with per-line highlighting.
/// </summary>
public sealed record LineDiffResult
{
    public required IReadOnlyList<TextLine> LeftLines { get; init; }

    public required IReadOnlyList<TextLine> RightLines { get; init; }
}
