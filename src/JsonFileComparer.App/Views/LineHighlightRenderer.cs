using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using JsonFileComparer.Core.TextDiff;

namespace JsonFileComparer.App.Views;

/// <summary>Paints a translucent background behind lines flagged as Added/Removed/Changed in an AvaloniaEdit editor.</summary>
public sealed class LineHighlightRenderer : IBackgroundRenderer
{
    private static readonly IBrush AddedBrush = new SolidColorBrush(Color.FromArgb(60, 46, 160, 67));
    private static readonly IBrush RemovedBrush = new SolidColorBrush(Color.FromArgb(60, 220, 53, 69));
    private static readonly IBrush ChangedBrush = new SolidColorBrush(Color.FromArgb(60, 230, 190, 30));

    private readonly Dictionary<int, LineDiffType> _lines = new();

    public KnownLayer Layer => KnownLayer.Background;

    public void SetHighlights(IEnumerable<TextLine> lines)
    {
        _lines.Clear();
        foreach (var line in lines)
        {
            if (line.Type != LineDiffType.Unchanged)
            {
                _lines[line.LineNumber] = line.Type;
            }
        }
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_lines.Count == 0 || textView.Document is null)
        {
            return;
        }

        foreach (var visualLine in textView.VisualLines)
        {
            var lineNumber = visualLine.FirstDocumentLine.LineNumber;
            if (!_lines.TryGetValue(lineNumber, out var type))
            {
                continue;
            }

            var brush = type switch
            {
                LineDiffType.Added => AddedBrush,
                LineDiffType.Removed => RemovedBrush,
                LineDiffType.Changed => ChangedBrush,
                _ => null
            };
            if (brush is null)
            {
                continue;
            }

            var y = visualLine.VisualTop - textView.ScrollOffset.Y;
            var rect = new Rect(0, y, textView.Bounds.Width, visualLine.Height);
            drawingContext.FillRectangle(brush, rect);
        }
    }
}
