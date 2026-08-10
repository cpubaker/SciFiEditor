using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using SciFiEditor.App.Editor;

namespace SciFiEditor.App.Controls;

public partial class FindReplaceBar : UserControl
{
    private readonly List<Match> _matches = new();
    private RichTextBox? _editor;
    private int _currentMatchIndex = -1;

    public FindReplaceBar()
    {
        InitializeComponent();
    }

    public void Attach(RichTextBox editor)
    {
        _editor = editor;
    }

    public void Open()
    {
        Visibility = Visibility.Visible;
        FindTextBox.Focus();
        FindTextBox.SelectAll();
        RecomputeMatches();
    }

    public void CloseBar()
    {
        Visibility = Visibility.Collapsed;
        _editor?.Focus();
    }

    private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e) => RecomputeMatches();

    private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            FindNext();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseBar();
            e.Handled = true;
        }
    }

    private void FindNextButton_Click(object sender, RoutedEventArgs e) => FindNext();

    private void FindPreviousButton_Click(object sender, RoutedEventArgs e) => FindPrevious();

    private void ReplaceButton_Click(object sender, RoutedEventArgs e) => ReplaceCurrent();

    private void ReplaceAllButton_Click(object sender, RoutedEventArgs e) => ReplaceAll();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseBar();

    private void RecomputeMatches()
    {
        _matches.Clear();
        _currentMatchIndex = -1;

        if (_editor is null || string.IsNullOrEmpty(FindTextBox.Text))
        {
            UpdateMatchCountText();
            return;
        }

        var query = FindTextBox.Text;
        foreach (var paragraph in RichTextNavigationHelper.EnumerateParagraphs(_editor.Document))
        {
            var text = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;
            var index = 0;
            while ((index = text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                _matches.Add(new Match(paragraph, index, query.Length));
                index += query.Length;
            }
        }

        UpdateMatchCountText();
    }

    private void UpdateMatchCountText()
    {
        MatchCountText.Text = _matches.Count == 0
            ? "0/0"
            : $"{Math.Max(1, _currentMatchIndex + 1)}/{_matches.Count}";
    }

    private void FindNext()
    {
        if (_editor is null)
        {
            return;
        }

        if (_matches.Count == 0)
        {
            RecomputeMatches();
        }

        if (_matches.Count == 0)
        {
            return;
        }

        _currentMatchIndex = (_currentMatchIndex + 1) % _matches.Count;
        SelectMatch(_currentMatchIndex);
    }

    private void FindPrevious()
    {
        if (_editor is null || _matches.Count == 0)
        {
            return;
        }

        _currentMatchIndex = (_currentMatchIndex - 1 + _matches.Count) % _matches.Count;
        SelectMatch(_currentMatchIndex);
    }

    private void SelectMatch(int index)
    {
        if (_editor is null || index < 0 || index >= _matches.Count)
        {
            return;
        }

        var match = _matches[index];
        var start = match.Paragraph.ContentStart.GetPositionAtOffset(match.Offset);
        var end = start?.GetPositionAtOffset(match.Length);
        if (start is null || end is null)
        {
            return;
        }

        _editor.Selection.Select(start, end);
        ScrollIntoView(start);
        UpdateMatchCountText();
    }

    private void ScrollIntoView(TextPointer pointer)
    {
        if (_editor is null)
        {
            return;
        }

        var rect = pointer.GetCharacterRect(LogicalDirection.Forward);
        if (rect.Top < 0 || rect.Bottom > _editor.ActualHeight)
        {
            var target = _editor.VerticalOffset + rect.Top - (_editor.ActualHeight / 2) + (rect.Height / 2);
            _editor.ScrollToVerticalOffset(Math.Max(0, target));
        }
    }

    private void ReplaceCurrent()
    {
        if (_editor is null || _currentMatchIndex < 0 || _currentMatchIndex >= _matches.Count)
        {
            return;
        }

        var match = _matches[_currentMatchIndex];
        var start = match.Paragraph.ContentStart.GetPositionAtOffset(match.Offset);
        var end = start?.GetPositionAtOffset(match.Length);
        if (start is null || end is null)
        {
            return;
        }

        new TextRange(start, end).Text = ReplaceTextBox.Text;
        RecomputeMatches();

        if (_matches.Count > 0)
        {
            _currentMatchIndex = Math.Min(_currentMatchIndex, _matches.Count - 1);
            SelectMatch(_currentMatchIndex);
        }
    }

    private void ReplaceAll()
    {
        if (_editor is null || string.IsNullOrEmpty(FindTextBox.Text))
        {
            return;
        }

        var markdown = MarkdownFlowDocumentConverter.ToMarkdown(_editor.Document);
        var replaced = ReplaceAllIgnoreCase(markdown, FindTextBox.Text, ReplaceTextBox.Text);
        _editor.Document = MarkdownFlowDocumentConverter.ToFlowDocument(replaced);
        RecomputeMatches();
    }

    private static string ReplaceAllIgnoreCase(string text, string query, string replacement)
    {
        var builder = new StringBuilder();
        var index = 0;
        int found;
        while ((found = text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            builder.Append(text, index, found - index);
            builder.Append(replacement);
            index = found + query.Length;
        }

        builder.Append(text, index, text.Length - index);
        return builder.ToString();
    }

    private readonly record struct Match(Paragraph Paragraph, int Offset, int Length);
}
