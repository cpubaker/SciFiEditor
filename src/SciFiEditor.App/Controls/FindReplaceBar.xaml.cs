using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;

namespace SciFiEditor.App.Controls;

public partial class FindReplaceBar : UserControl
{
    private readonly List<int> _matchOffsets = new();
    private TextEditor? _editor;
    private int _currentMatchIndex = -1;

    public FindReplaceBar()
    {
        InitializeComponent();
    }

    public void Attach(TextEditor editor)
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
        _matchOffsets.Clear();
        _currentMatchIndex = -1;

        if (_editor is null || string.IsNullOrEmpty(FindTextBox.Text))
        {
            UpdateMatchCountText();
            return;
        }

        var text = _editor.Text;
        var query = FindTextBox.Text;
        var index = 0;
        while ((index = text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            _matchOffsets.Add(index);
            index += query.Length;
        }

        UpdateMatchCountText();
    }

    private void UpdateMatchCountText()
    {
        MatchCountText.Text = _matchOffsets.Count == 0
            ? "0/0"
            : $"{Math.Max(1, _currentMatchIndex + 1)}/{_matchOffsets.Count}";
    }

    private void FindNext()
    {
        if (_editor is null)
        {
            return;
        }

        if (_matchOffsets.Count == 0)
        {
            RecomputeMatches();
        }

        if (_matchOffsets.Count == 0)
        {
            return;
        }

        _currentMatchIndex = (_currentMatchIndex + 1) % _matchOffsets.Count;
        SelectMatch(_currentMatchIndex);
    }

    private void FindPrevious()
    {
        if (_editor is null || _matchOffsets.Count == 0)
        {
            return;
        }

        _currentMatchIndex = (_currentMatchIndex - 1 + _matchOffsets.Count) % _matchOffsets.Count;
        SelectMatch(_currentMatchIndex);
    }

    private void SelectMatch(int index)
    {
        if (_editor is null || index < 0 || index >= _matchOffsets.Count)
        {
            return;
        }

        var offset = _matchOffsets[index];
        var length = FindTextBox.Text.Length;
        _editor.Select(offset, length);
        _editor.ScrollTo(_editor.Document.GetLineByOffset(offset).LineNumber, 0);
        UpdateMatchCountText();
    }

    private void ReplaceCurrent()
    {
        if (_editor is null || _currentMatchIndex < 0 || _currentMatchIndex >= _matchOffsets.Count)
        {
            return;
        }

        var offset = _matchOffsets[_currentMatchIndex];
        var length = FindTextBox.Text.Length;
        _editor.Document.Replace(offset, length, ReplaceTextBox.Text);
        RecomputeMatches();

        if (_matchOffsets.Count > 0)
        {
            _currentMatchIndex = Math.Min(_currentMatchIndex, _matchOffsets.Count - 1);
            SelectMatch(_currentMatchIndex);
        }
    }

    private void ReplaceAll()
    {
        if (_editor is null || string.IsNullOrEmpty(FindTextBox.Text))
        {
            return;
        }

        _editor.Document.Text = ReplaceAllIgnoreCase(_editor.Text, FindTextBox.Text, ReplaceTextBox.Text);
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
}
