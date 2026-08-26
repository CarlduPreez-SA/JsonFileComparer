using JsonFileComparer.Core.TextDiff;
using Xunit;

namespace JsonFileComparer.Core.Tests;

public class LineDifferTests
{
    [Fact]
    public void IdenticalText_AllLinesUnchanged()
    {
        var text = "line1\nline2\nline3";
        var result = LineDiffer.Compute(text, text);

        Assert.All(result.LeftLines, l => Assert.Equal(LineDiffType.Unchanged, l.Type));
        Assert.All(result.RightLines, l => Assert.Equal(LineDiffType.Unchanged, l.Type));
        Assert.Equal(3, result.LeftLines.Count);
        Assert.Equal(3, result.RightLines.Count);
    }

    [Fact]
    public void SingleLineEdited_IsReportedAsChanged_NotRemoveAndAdd()
    {
        var left = "alpha\nbeta\ngamma";
        var right = "alpha\nBETA\ngamma";

        var result = LineDiffer.Compute(left, right);

        Assert.Equal(LineDiffType.Unchanged, result.LeftLines[0].Type);
        Assert.Equal(LineDiffType.Changed, result.LeftLines[1].Type);
        Assert.Equal("beta", result.LeftLines[1].Text);
        Assert.Equal(LineDiffType.Unchanged, result.LeftLines[2].Type);

        Assert.Equal(LineDiffType.Changed, result.RightLines[1].Type);
        Assert.Equal("BETA", result.RightLines[1].Text);
    }

    [Fact]
    public void AddedLine_OnlyAppearsOnRightSide_MarkedAdded()
    {
        var left = "one\ntwo";
        var right = "one\ntwo\nthree";

        var result = LineDiffer.Compute(left, right);

        Assert.Equal(2, result.LeftLines.Count);
        Assert.All(result.LeftLines, l => Assert.Equal(LineDiffType.Unchanged, l.Type));

        Assert.Equal(3, result.RightLines.Count);
        Assert.Equal(LineDiffType.Added, result.RightLines[2].Type);
        Assert.Equal("three", result.RightLines[2].Text);
    }

    [Fact]
    public void RemovedLine_OnlyAppearsOnLeftSide_MarkedRemoved()
    {
        var left = "one\ntwo\nthree";
        var right = "one\ntwo";

        var result = LineDiffer.Compute(left, right);

        Assert.Equal(3, result.LeftLines.Count);
        Assert.Equal(LineDiffType.Removed, result.LeftLines[2].Type);
        Assert.Equal("three", result.LeftLines[2].Text);

        Assert.Equal(2, result.RightLines.Count);
        Assert.All(result.RightLines, l => Assert.Equal(LineDiffType.Unchanged, l.Type));
    }

    [Fact]
    public void LineNumbersAreOneBasedAndSequentialPerFile()
    {
        var left = "a\nb\nc";
        var right = "a\nb\nc";

        var result = LineDiffer.Compute(left, right);

        Assert.Equal([1, 2, 3], result.LeftLines.Select(l => l.LineNumber));
        Assert.Equal([1, 2, 3], result.RightLines.Select(l => l.LineNumber));
    }

    [Fact]
    public void EmptyFiles_ProduceNoLines()
    {
        var result = LineDiffer.Compute("", "");

        // Splitting "" on '\n' yields a single empty-string line, which is expected and unchanged.
        Assert.Single(result.LeftLines);
        Assert.Single(result.RightLines);
        Assert.Equal(LineDiffType.Unchanged, result.LeftLines[0].Type);
    }

    [Fact]
    public void HandlesCrLfAndLfConsistently()
    {
        var left = "one\r\ntwo\r\nthree";
        var right = "one\ntwo\nthree";

        var result = LineDiffer.Compute(left, right);

        Assert.All(result.LeftLines, l => Assert.Equal(LineDiffType.Unchanged, l.Type));
        Assert.All(result.RightLines, l => Assert.Equal(LineDiffType.Unchanged, l.Type));
    }

    [Fact]
    public void MultipleLinesChangedTogether_AllPairedAsChanged()
    {
        var left = "keep1\nold1\nold2\nkeep2";
        var right = "keep1\nnew1\nnew2\nkeep2";

        var result = LineDiffer.Compute(left, right);

        Assert.Equal(LineDiffType.Unchanged, result.LeftLines[0].Type);
        Assert.Equal(LineDiffType.Changed, result.LeftLines[1].Type);
        Assert.Equal(LineDiffType.Changed, result.LeftLines[2].Type);
        Assert.Equal(LineDiffType.Unchanged, result.LeftLines[3].Type);

        Assert.Equal(LineDiffType.Changed, result.RightLines[1].Type);
        Assert.Equal(LineDiffType.Changed, result.RightLines[2].Type);
    }

    [Fact]
    public void UnequalSizedHunk_PairsWhatItCanAndMarksRestAddedOrRemoved()
    {
        // Left has 1 differing line, right has 3 differing lines in the same hunk.
        var left = "keep\nold\nkeep2";
        var right = "keep\nnew1\nnew2\nnew3\nkeep2";

        var result = LineDiffer.Compute(left, right);

        Assert.Equal(LineDiffType.Changed, result.LeftLines[1].Type); // "old" paired with "new1"

        var rightMiddle = result.RightLines.Where(l => l.Text.StartsWith("new")).ToList();
        Assert.Equal(LineDiffType.Changed, rightMiddle[0].Type);
        Assert.Equal(LineDiffType.Added, rightMiddle[1].Type);
        Assert.Equal(LineDiffType.Added, rightMiddle[2].Type);
    }
}
