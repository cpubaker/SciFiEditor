using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SciFiEditor.App.Editor;

namespace SciFiEditor.App.Behaviors;

public static class RichTextBoxMarkdownBehavior
{
    private static readonly Dictionary<RichTextBox, State> States = new();

    public static readonly DependencyProperty BindableMarkdownProperty =
        DependencyProperty.RegisterAttached(
            "BindableMarkdown",
            typeof(string),
            typeof(RichTextBoxMarkdownBehavior),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBindableMarkdownChanged));

    public static string GetBindableMarkdown(DependencyObject obj) => (string)obj.GetValue(BindableMarkdownProperty);

    public static void SetBindableMarkdown(DependencyObject obj, string value) => obj.SetValue(BindableMarkdownProperty, value);

    // Suppress flag other editor features (Focus Mode dimming) can use to mutate the
    // FlowDocument's formatting without triggering a markdown re-serialization / dirty
    // pipeline, the same way MainViewModel._isLoadingContent guards programmatic content
    // assignment.
    public static void SuppressChangeTracking(RichTextBox editor, bool suppress)
    {
        if (States.TryGetValue(editor, out var state))
        {
            state.IsSuppressed = suppress;
        }
    }

    private static void OnBindableMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox editor)
        {
            return;
        }

        if (!States.TryGetValue(editor, out var state))
        {
            state = new State();
            States[editor] = state;
            editor.TextChanged += (_, _) => OnEditorTextChanged(editor, state);
            state.DebounceTimer.Tick += (_, _) =>
            {
                state.DebounceTimer.Stop();
                PushEditorContentToSource(editor, state);
            };
        }

        var newMarkdown = e.NewValue as string ?? string.Empty;
        if (newMarkdown == state.LastKnownMarkdown)
        {
            return;
        }

        state.IsApplyingFromSource = true;
        try
        {
            editor.Document = MarkdownFlowDocumentConverter.ToFlowDocument(newMarkdown);
            state.LastKnownMarkdown = newMarkdown;
        }
        finally
        {
            state.IsApplyingFromSource = false;
        }
    }

    private static void OnEditorTextChanged(RichTextBox editor, State state)
    {
        if (state.IsApplyingFromSource || state.IsSuppressed)
        {
            return;
        }

        state.DebounceTimer.Stop();
        state.DebounceTimer.Start();
    }

    private static void PushEditorContentToSource(RichTextBox editor, State state)
    {
        var markdown = MarkdownFlowDocumentConverter.ToMarkdown(editor.Document);
        if (markdown == state.LastKnownMarkdown)
        {
            return;
        }

        state.LastKnownMarkdown = markdown;
        SetBindableMarkdown(editor, markdown);
    }

    private sealed class State
    {
        public string LastKnownMarkdown = string.Empty;
        public bool IsApplyingFromSource;
        public bool IsSuppressed;
        public readonly DispatcherTimer DebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    }
}
