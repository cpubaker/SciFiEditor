using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using SciFiEditor.App.Editor;
using SciFiEditor.App.Views;

namespace SciFiEditor.App.Controls;

public partial class FormattingToolbar : UserControl
{
    private RichTextBox? _editor;
    private bool _isSyncingFromSelection;

    public FormattingToolbar()
    {
        InitializeComponent();
    }

    public void Attach(RichTextBox editor)
    {
        _editor = editor;
        _editor.SelectionChanged += (_, _) => SyncToolbarState();
        SyncToolbarState();
    }

    private void HeadingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingFromSelection || _editor is null)
        {
            return;
        }

        if (_editor.CaretPosition.Paragraph is not { } paragraph)
        {
            return;
        }

        var level = HeadingComboBox.SelectedItem is ComboBoxItem { Tag: string tagText } && int.TryParse(tagText, out var parsed)
            ? parsed
            : 0;

        if (level == 0)
        {
            paragraph.Tag = null;
            paragraph.FontWeight = FontWeights.Normal;
            paragraph.FontSize = _editor.FontSize;
        }
        else
        {
            paragraph.Tag = level;
            paragraph.FontWeight = FontWeights.Bold;
            paragraph.FontSize = level switch { 1 => 26, 2 => 22, 3 => 18, _ => 16 };
        }

        _editor.Focus();
    }

    private void ListButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu })
        {
            menu.PlacementTarget = ListButton;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    private void BulletList_Click(object sender, RoutedEventArgs e)
    {
        if (_editor is null)
        {
            return;
        }

        EditingCommands.ToggleBullets.Execute(null, _editor);
        _editor.Focus();
    }

    private void NumberedList_Click(object sender, RoutedEventArgs e)
    {
        if (_editor is null)
        {
            return;
        }

        EditingCommands.ToggleNumbering.Execute(null, _editor);
        _editor.Focus();
    }

    private void BoldButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editor is null)
        {
            return;
        }

        EditingCommands.ToggleBold.Execute(null, _editor);
        _editor.Focus();
    }

    private void ItalicButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editor is null)
        {
            return;
        }

        EditingCommands.ToggleItalic.Execute(null, _editor);
        _editor.Focus();
    }

    private void StrikethroughButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editor is null || _editor.Selection.IsEmpty)
        {
            return;
        }

        var selection = _editor.Selection;
        var isStrike = IsStrikethrough(selection.GetPropertyValue(Inline.TextDecorationsProperty));
        selection.ApplyPropertyValue(Inline.TextDecorationsProperty, isStrike ? null : TextDecorations.Strikethrough);
        _editor.Focus();
    }

    private void LinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editor is null)
        {
            return;
        }

        var selectedText = _editor.Selection.Text;
        var dialog = new InsertLinkWindow(selectedText) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var hyperlink = new Hyperlink(new Run(string.IsNullOrEmpty(dialog.LinkText) ? dialog.Url : dialog.LinkText));
        if (Uri.TryCreate(dialog.Url, UriKind.RelativeOrAbsolute, out var uri))
        {
            hyperlink.NavigateUri = uri;
        }

        if (!_editor.Selection.IsEmpty)
        {
            _editor.Selection.Text = string.Empty;
        }

        var insertionPoint = _editor.CaretPosition.Paragraph ?? _editor.Document.Blocks.LastBlock as Paragraph;
        insertionPoint?.Inlines.Add(hyperlink);
        _editor.Focus();
    }

    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu })
        {
            menu.PlacementTarget = MoreButton;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    private void Blockquote_Click(object sender, RoutedEventArgs e)
    {
        if (_editor?.CaretPosition.Paragraph is not { } paragraph)
        {
            return;
        }

        if (Equals(paragraph.Tag, "Quote"))
        {
            paragraph.Tag = null;
            paragraph.BorderThickness = new Thickness(0);
            paragraph.Padding = new Thickness(0);
            paragraph.FontStyle = FontStyles.Normal;
        }
        else
        {
            paragraph.Tag = "Quote";
            paragraph.BorderBrush = MarkdownFlowDocumentConverter.QuoteBorderBrush;
            paragraph.BorderThickness = new Thickness(4, 0, 0, 0);
            paragraph.Padding = new Thickness(12, 2, 0, 2);
            paragraph.FontStyle = FontStyles.Italic;
        }

        _editor.Focus();
    }

    private void Code_Click(object sender, RoutedEventArgs e)
    {
        if (_editor is null || _editor.Selection.IsEmpty)
        {
            return;
        }

        var selection = _editor.Selection;
        var currentFont = selection.GetPropertyValue(TextElement.FontFamilyProperty) as FontFamily;
        var isCode = currentFont == MarkdownFlowDocumentConverter.MonospaceFont;

        if (isCode)
        {
            selection.ApplyPropertyValue(TextElement.FontFamilyProperty, _editor.FontFamily);
            selection.ApplyPropertyValue(TextElement.BackgroundProperty, null);
        }
        else
        {
            selection.ApplyPropertyValue(TextElement.FontFamilyProperty, MarkdownFlowDocumentConverter.MonospaceFont);
            selection.ApplyPropertyValue(TextElement.BackgroundProperty, MarkdownFlowDocumentConverter.CodeBackgroundBrush);
        }

        _editor.Focus();
    }

    private void HorizontalRule_Click(object sender, RoutedEventArgs e)
    {
        if (_editor is null)
        {
            return;
        }

        var border = new Border
        {
            BorderBrush = MarkdownFlowDocumentConverter.HrBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Margin = new Thickness(0, 8, 0, 8),
            Tag = "Hr"
        };
        var container = new BlockUIContainer(border);

        if (_editor.CaretPosition.Paragraph is { } paragraph)
        {
            _editor.Document.Blocks.InsertAfter(paragraph, container);
        }
        else
        {
            _editor.Document.Blocks.Add(container);
        }

        _editor.Focus();
    }

    private void SyncToolbarState()
    {
        if (_editor is null)
        {
            return;
        }

        _isSyncingFromSelection = true;
        try
        {
            var headingLevel = _editor.CaretPosition.Paragraph?.Tag is int level ? level : 0;
            HeadingComboBox.SelectedIndex = headingLevel is >= 1 and <= 3 ? headingLevel : 0;

            var selection = _editor.Selection;
            BoldButton.IsChecked = Equals(selection.GetPropertyValue(TextElement.FontWeightProperty), FontWeights.Bold);
            ItalicButton.IsChecked = Equals(selection.GetPropertyValue(TextElement.FontStyleProperty), FontStyles.Italic);
            StrikethroughButton.IsChecked = IsStrikethrough(selection.GetPropertyValue(Inline.TextDecorationsProperty));
        }
        finally
        {
            _isSyncingFromSelection = false;
        }
    }

    private static bool IsStrikethrough(object? value) =>
        value is TextDecorationCollection decorations &&
        decorations.Any(d => d.Location == TextDecorationLocation.Strikethrough);
}
