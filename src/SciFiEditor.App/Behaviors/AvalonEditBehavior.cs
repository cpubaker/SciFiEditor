using System.Windows;
using ICSharpCode.AvalonEdit;

namespace SciFiEditor.App.Behaviors;

public static class AvalonEditBehavior
{
    private static readonly HashSet<TextEditor> RegisteredEditors = new();

    public static readonly DependencyProperty BindableTextProperty =
        DependencyProperty.RegisterAttached(
            "BindableText",
            typeof(string),
            typeof(AvalonEditBehavior),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBindableTextChanged));

    public static string GetBindableText(DependencyObject obj) => (string)obj.GetValue(BindableTextProperty);

    public static void SetBindableText(DependencyObject obj, string value) => obj.SetValue(BindableTextProperty, value);

    private static void OnBindableTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextEditor editor)
        {
            return;
        }

        var newText = e.NewValue as string ?? string.Empty;
        if (editor.Text != newText)
        {
            editor.Text = newText;
        }

        if (RegisteredEditors.Add(editor))
        {
            editor.TextChanged += (_, _) => SetBindableText(editor, editor.Text);
        }
    }
}
