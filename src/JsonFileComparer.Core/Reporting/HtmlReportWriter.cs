using System.Net;
using System.Text;
using JsonFileComparer.Core.Models;

namespace JsonFileComparer.Core.Reporting;

/// <summary>Renders a <see cref="ComparisonResult"/> as a self-contained, side-by-side HTML report.</summary>
public static class HtmlReportWriter
{
    public static string ToHtml(ComparisonResult result, string leftLabel, string rightLabel)
    {
        var sb = new StringBuilder();
        sb.Append("""
                   <!DOCTYPE html>
                   <html lang="en">
                   <head>
                   <meta charset="utf-8">
                   <title>JSON Comparison Report</title>
                   <style>
                     body { font-family: Consolas, 'Cascadia Mono', monospace; background: #1e1e1e; color: #ddd; margin: 2rem; }
                     h1 { font-size: 1.2rem; }
                     table { border-collapse: collapse; width: 100%; font-size: 0.85rem; }
                     th, td { border: 1px solid #444; padding: 4px 8px; text-align: left; vertical-align: top; }
                     th { background: #2d2d2d; }
                     tr.added { background: #10331b; }
                     tr.removed { background: #3a1414; }
                     tr.changed { background: #3a3410; }
                     tr.typechanged { background: #33203a; }
                     .path { color: #9cdcfe; }
                     .summary span { display: inline-block; margin-right: 1.5rem; }
                   </style>
                   </head>
                   <body>
                   """);

        sb.Append($"<h1>JSON Comparison: {Enc(leftLabel)} vs {Enc(rightLabel)}</h1>");
        sb.Append("<div class=\"summary\">");
        sb.Append($"<span>Added: {result.AddedCount}</span>");
        sb.Append($"<span>Removed: {result.RemovedCount}</span>");
        sb.Append($"<span>Changed: {result.ChangedCount}</span>");
        sb.Append($"<span>Unchanged: {result.UnchangedCount}</span>");
        sb.Append("</div>");

        sb.Append("<table><thead><tr><th>Path</th><th>Type</th><th>").Append(Enc(leftLabel))
          .Append("</th><th>").Append(Enc(rightLabel)).Append("</th></tr></thead><tbody>");

        foreach (var entry in result.Entries.Where(e => e.Type != DiffType.Unchanged))
        {
            var cssClass = entry.Type.ToString().ToLowerInvariant();
            sb.Append($"<tr class=\"{cssClass}\">");
            sb.Append($"<td class=\"path\">{Enc(entry.Path)}</td>");
            sb.Append($"<td>{Enc(entry.Type.ToString())}</td>");
            sb.Append($"<td>{Enc(entry.LeftValue ?? "(missing)")}</td>");
            sb.Append($"<td>{Enc(entry.RightValue ?? "(missing)")}</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table></body></html>");
        return sb.ToString();
    }

    public static void WriteToFile(ComparisonResult result, string leftLabel, string rightLabel, string path)
    {
        File.WriteAllText(path, ToHtml(result, leftLabel, rightLabel));
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
