using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SciFiEditor.App.Behaviors;

namespace SciFiEditor.App.Editor;

// Replaces AvalonEdit's ParagraphDimmingTransformer (a per-render DocumentColorizingTransformer,
// which RichTextBox has no equivalent for). Dims every paragraph except the one containing the
// caret by setting Paragraph.Foreground directly -- child Runs never set their own Foreground in
// MarkdownFlowDocumentConverter, so they inherit it. Foreground mutations are wrapped in
// RichTextBoxMarkdownBehavior.SuppressChangeTracking so moving the caret around in Focus Mode
// never spuriously re-serializes the document or triggers autosave/word-count churn.
public sealed class FocusModeDimmer
{
    private static readonly Brush DimBrush = CreateDimBrush();

    private readonly RichTextBox _editor;
    private Paragraph? _currentParagraph;

    public FocusModeDimmer(RichTextBox editor)
    {
        _editor = editor;
    }

    public void Attach()
    {
        _editor.SelectionChanged += OnSelectionChanged;
        UpdateDimming();
    }

    public void Detach()
    {
        _editor.SelectionChanged -= OnSelectionChanged;
        RestoreAll();
    }

    private void OnSelectionChanged(object? sender, RoutedEventArgs e) => UpdateDimming();

    private void UpdateDimming()
    {
        var paragraph = _editor.CaretPosition.Paragraph;
        if (paragraph == _currentParagraph)
        {
            return;
        }

        RichTextBoxMarkdownBehavior.SuppressChangeTracking(_editor, true);
        try
        {
            foreach (var p in RichTextNavigationHelper.EnumerateParagraphs(_editor.Document))
            {
                if (p == paragraph)
                {
                    p.ClearValue(TextElement.ForegroundProperty);
                }
                else
                {
                    p.Foreground = DimBrush;
                }
            }
        }
        finally
        {
            RichTextBoxMarkdownBehavior.SuppressChangeTracking(_editor, false);
        }

        _currentParagraph = paragraph;
    }

    private void RestoreAll()
    {
        RichTextBoxMarkdownBehavior.SuppressChangeTracking(_editor, true);
        try
        {
            foreach (var p in RichTextNavigationHelper.EnumerateParagraphs(_editor.Document))
            {
                p.ClearValue(TextElement.ForegroundProperty);
            }
        }
        finally
        {
            RichTextBoxMarkdownBehavior.SuppressChangeTracking(_editor, false);
        }

        _currentParagraph = null;
    }

    private static Brush CreateDimBrush()
    {
        var brush = new SolidColorBrush(Color.FromArgb(0x80, 0x80, 0x80, 0x80));
        brush.Freeze();
        return brush;
    }
}
