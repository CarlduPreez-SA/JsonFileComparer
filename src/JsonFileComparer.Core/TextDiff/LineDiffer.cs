namespace JsonFileComparer.Core.TextDiff;

/// <summary>
/// Computes a line-level diff between two raw texts (independent of JSON/XML structure), for a plain
/// "Notepad-style" side-by-side text view. Uses a classic LCS (longest common subsequence) alignment over
/// lines — the same family of algorithm underlying `diff`/`git diff` — then pairs up adjacent runs of
/// removed/added lines as "Changed" where they line up 1:1, so a single edited line reads as one changed
/// line rather than a remove+add pair.
/// </summary>
public static class LineDiffer
{
    public static LineDiffResult Compute(string leftText, string rightText)
    {
        var leftLines = SplitLines(leftText);
        var rightLines = SplitLines(rightText);

        var ops = ComputeLcsOps(leftLines, rightLines);

        var left = new List<TextLine>();
        var right = new List<TextLine>();

        var i = 0;
        while (i < ops.Count)
        {
            if (ops[i].Kind == OpKind.Equal)
            {
                var (l, r) = ops[i].Indices;
                left.Add(new TextLine { LineNumber = l + 1, Text = leftLines[l], Type = LineDiffType.Unchanged });
                right.Add(new TextLine { LineNumber = r + 1, Text = rightLines[r], Type = LineDiffType.Unchanged });
                i++;
                continue;
            }

            // Collect a contiguous run of Delete/Insert ops (a single "hunk" of differing lines).
            var deletes = new List<int>();
            var inserts = new List<int>();
            while (i < ops.Count && ops[i].Kind != OpKind.Equal)
            {
                if (ops[i].Kind == OpKind.Delete)
                {
                    deletes.Add(ops[i].Indices.Left);
                }
                else
                {
                    inserts.Add(ops[i].Indices.Right);
                }
                i++;
            }

            var paired = Math.Min(deletes.Count, inserts.Count);
            for (var k = 0; k < paired; k++)
            {
                left.Add(new TextLine { LineNumber = deletes[k] + 1, Text = leftLines[deletes[k]], Type = LineDiffType.Changed });
                right.Add(new TextLine { LineNumber = inserts[k] + 1, Text = rightLines[inserts[k]], Type = LineDiffType.Changed });
            }
            for (var k = paired; k < deletes.Count; k++)
            {
                left.Add(new TextLine { LineNumber = deletes[k] + 1, Text = leftLines[deletes[k]], Type = LineDiffType.Removed });
            }
            for (var k = paired; k < inserts.Count; k++)
            {
                right.Add(new TextLine { LineNumber = inserts[k] + 1, Text = rightLines[inserts[k]], Type = LineDiffType.Added });
            }
        }

        return new LineDiffResult { LeftLines = left, RightLines = right };
    }

    private static string[] SplitLines(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

    private enum OpKind { Equal, Delete, Insert }

    private readonly record struct Op(OpKind Kind, (int Left, int Right) Indices);

    private static List<Op> ComputeLcsOps(string[] left, string[] right)
    {
        var n = left.Length;
        var m = right.Length;
        var dp = new int[n + 1, m + 1];

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                dp[i, j] = left[i - 1] == right[j - 1]
                    ? dp[i - 1, j - 1] + 1
                    : Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        var ops = new List<Op>();
        var x = n;
        var y = m;
        while (x > 0 && y > 0)
        {
            if (left[x - 1] == right[y - 1])
            {
                ops.Add(new Op(OpKind.Equal, (x - 1, y - 1)));
                x--;
                y--;
            }
            else if (dp[x - 1, y] >= dp[x, y - 1])
            {
                ops.Add(new Op(OpKind.Delete, (x - 1, 0)));
                x--;
            }
            else
            {
                ops.Add(new Op(OpKind.Insert, (0, y - 1)));
                y--;
            }
        }
        while (x > 0)
        {
            ops.Add(new Op(OpKind.Delete, (x - 1, 0)));
            x--;
        }
        while (y > 0)
        {
            ops.Add(new Op(OpKind.Insert, (0, y - 1)));
            y--;
        }

        ops.Reverse();
        return ops;
    }
}
