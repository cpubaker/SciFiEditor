using System.Windows;
using Microsoft.Web.WebView2.Wpf;

namespace SciFiEditor.App.Behaviors;

public static class WebView2Behavior
{
    public static readonly DependencyProperty BindableHtmlProperty = DependencyProperty.RegisterAttached(
        "BindableHtml", typeof(string), typeof(WebView2Behavior), new PropertyMetadata(string.Empty, OnBindableHtmlChanged));

    public static string GetBindableHtml(DependencyObject obj) => (string)obj.GetValue(BindableHtmlProperty);

    public static void SetBindableHtml(DependencyObject obj, string value) => obj.SetValue(BindableHtmlProperty, value);

    private static async void OnBindableHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WebView2 webView)
        {
            return;
        }

        var html = e.NewValue as string ?? string.Empty;

        try
        {
            if (webView.CoreWebView2 is null)
            {
                await webView.EnsureCoreWebView2Async();
            }

            webView.CoreWebView2?.NavigateToString(html);
        }
        catch (Exception)
        {
            // Best-effort preview: swallow WebView2-runtime/initialization failures rather than crashing the app.
        }
    }
}
