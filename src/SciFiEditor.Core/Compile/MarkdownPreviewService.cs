namespace SciFiEditor.Core.Compile;

public static class MarkdownPreviewService
{
    private const string HtmlHead = """
        <html><head><meta charset="utf-8" />
        <style>body { font-family: 'Segoe UI', sans-serif; margin: 16px; line-height: 1.5; }</style>
        </head><body>
        """;

    public static string ToHtml(string? markdown)
    {
        var body = Markdig.Markdown.ToHtml(markdown ?? string.Empty, MarkdownPipelineFactory.Create());
        return HtmlHead + body + "</body></html>";
    }
}
