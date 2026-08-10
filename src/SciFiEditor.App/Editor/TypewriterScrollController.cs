using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SciFiEditor.App.Editor;

// Keeps the caret vertically centered while typing (Focus Mode). Uses the caret's real
// pixel rect via TextPointer.GetCharacterRect, which -- unlike the previous AvalonEdit
// version -- correctly accounts for wrapped paragraphs instead of assuming one visual
// line per document line.
public sealed class TypewriterScrollController
{
    private readonly RichTextBox _editor;
    private bool _isActive;

    public TypewriterScrollController(RichTextBox editor)
    {
        _editor = editor;
    }

    public void Attach()
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        _editor.SelectionChanged += OnSelectionChanged;
    }

    public void Detach()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        _editor.SelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(object? sender, RoutedEventArgs e)
    {
        var caretRect = _editor.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
        var target = _editor.VerticalOffset + caretRect.Top - (_editor.ActualHeight / 2) + (caretRect.Height / 2);
        _editor.ScrollToVerticalOffset(Math.Max(0, target));
    }
}
