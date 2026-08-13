namespace SciFiEditor.Core.Compile;

public static class MarkdownPreviewService
{
    private const string HtmlHead = """
        <html><head><meta charset="utf-8" />
        <style>
        body {
            font-family: 'Times New Roman', Georgia, serif;
            font-size: 14pt;
            line-height: 1.6;
            color: #1a1a1a;
            background: #ffffff;
            max-width: 680px;
            margin: 32px auto;
            padding: 0 24px;
        }
        p {
            margin: 0;
            text-indent: 1.25cm;
            text-align: justify;
        }
        body > p:first-child,
        h1 + p, h2 + p, h3 + p, h4 + p, h5 + p, h6 + p {
            text-indent: 0;
        }
        h1, h2, h3, h4, h5, h6 {
            text-align: center;
            font-weight: 700;
            margin: 1.5em 0 0.75em;
        }
        blockquote {
            margin: 1em 2.5cm;
            font-style: italic;
            color: #333333;
        }
        </style>
        </head><body>
        """;

    public static string ToHtml(string? markdown)
    {
        var body = Markdig.Markdown.ToHtml(markdown ?? string.Empty, MarkdownPipelineFactory.Create());
        return HtmlHead + body + "</body></html>";
    }
}
