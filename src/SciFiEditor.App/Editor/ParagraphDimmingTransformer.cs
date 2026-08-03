using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace SciFiEditor.App.Editor;

public sealed class ParagraphDimmingTransformer : DocumentColorizingTransformer
{
    private static readonly Brush DimBrush = CreateDimBrush();

    private int _currentParagraphStartLine = -1;
    private int _currentParagraphEndLine = -1;

    public void UpdateCurrentParagraph(TextDocument document, int caretLine)
    {
        bool IsBlank(int lineNumber)
        {
            if (lineNumber < 1 || lineNumber > document.LineCount)
            {
                return true;
            }

            return string.IsNullOrWhiteSpace(document.GetText(document.GetLineByNumber(lineNumber)));
        }

        var start = caretLine;
        while (!IsBlank(start - 1))
        {
            start--;
        }

        var end = caretLine;
        while (!IsBlank(end + 1))
        {
            end++;
        }

        _currentParagraphStartLine = start;
        _currentParagraphEndLine = end;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (line.LineNumber >= _currentParagraphStartLine && line.LineNumber <= _currentParagraphEndLine)
        {
            return;
        }

        ChangeLinePart(line.Offset, line.EndOffset, element => element.TextRunProperties.SetForegroundBrush(DimBrush));
    }

    private static Brush CreateDimBrush()
    {
        var brush = new SolidColorBrush(Color.FromArgb(0x80, 0x80, 0x80, 0x80));
        brush.Freeze();
        return brush;
    }
}
