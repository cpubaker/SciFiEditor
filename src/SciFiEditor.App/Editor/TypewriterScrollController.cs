using ICSharpCode.AvalonEdit;

namespace SciFiEditor.App.Editor;

// Best-effort vertical centering: assumes one visual line per document line, so it drifts on
// wrapped paragraphs. AvalonEdit doesn't expose cheap pixel-accurate wrapped-line geometry.
public sealed class TypewriterScrollController
{
    private readonly TextEditor _editor;
    private bool _isActive;

    public TypewriterScrollController(TextEditor editor)
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
        _editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
    }

    public void Detach()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        _editor.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        var textView = _editor.TextArea.TextView;
        var caretLine = _editor.Document.GetLineByOffset(_editor.CaretOffset).LineNumber;
        var lineHeight = textView.DefaultLineHeight;
        var visualTop = (caretLine - 1) * lineHeight;
        var target = visualTop - (_editor.ViewportHeight / 2) + (lineHeight / 2);
        _editor.ScrollToVerticalOffset(Math.Max(0, target));
    }
}
