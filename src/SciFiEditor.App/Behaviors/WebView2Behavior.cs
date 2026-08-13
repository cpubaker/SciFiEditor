using System.Windows;
using Microsoft.Web.WebView2.Wpf;

namespace SciFiEditor.App.Behaviors;

public static class WebView2Behavior
{
    public static readonly DependencyProperty BindableHtmlProperty = DependencyProperty.RegisterAttached(
        "BindableHtml", typeof(string), typeof(WebView2Behavior), new PropertyMetadata(string.Empty, OnBindableHtmlChanged));

    public static string GetBindableHtml(DependencyObject obj) => (string)obj.GetValue(BindableHtmlProperty);

    public static void SetBindableHtml(DependencyObject obj, string value) => obj.SetValue(BindableHtmlProperty, value);

    private static void OnBindableHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WebView2 webView)
        {
            return;
        }

        if (!webView.IsLoaded)
        {
            // A WebView2 on an unselected TabItem isn't part of the visual tree yet, so it has no
            // HWND to initialize CoreWebView2 against. Defer navigation until it actually loads,
            // and always read the latest bound value at that point rather than the one from now.
            webView.Loaded -= WebView_Loaded;
            webView.Loaded += WebView_Loaded;
            return;
        }

        _ = NavigateAsync(webView);
    }

    private static void WebView_Loaded(object sender, RoutedEventArgs e)
    {
        var webView = (WebView2)sender;
        webView.Loaded -= WebView_Loaded;
        _ = NavigateAsync(webView);
    }

    private static async Task NavigateAsync(WebView2 webView)
    {
        try
        {
            if (webView.CoreWebView2 is null)
            {
                await webView.EnsureCoreWebView2Async();
            }

            webView.CoreWebView2?.NavigateToString(GetBindableHtml(webView));
        }
        catch (Exception)
        {
            // Best-effort preview: swallow WebView2-runtime/initialization failures rather than crashing the app.
        }
    }
}
