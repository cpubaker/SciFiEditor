using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Markdig.Syntax;
using SciFiEditor.Core.Compile;
using MarkdigInlines = Markdig.Syntax.Inlines;
using WpfBlock = System.Windows.Documents.Block;

namespace SciFiEditor.App.Editor;

// Bidirectional Markdown <-> FlowDocument conversion, scoped to exactly what the
// formatting toolbar can produce (headings, bold/italic/strikethrough, bullet/numbered
// lists, blockquote, inline/fenced code, horizontal rule, links). Anything the parser
// recognizes but this converter doesn't have a dedicated mapping for is rendered as its
// raw source text instead of being dropped, so opening a hand-edited .md file never
// loses content even if some formatting doesn't round-trip perfectly.
public static class MarkdownFlowDocumentConverter
{
    // Shared with FormattingToolbar so toolbar-applied formatting and parsed-from-markdown
    // formatting look identical and are detected the same way when saving back to markdown.
    internal static readonly FontFamily MonospaceFont = new("Consolas");
    internal static readonly SolidColorBrush QuoteBorderBrush = FrozenBrush(0x80, 0x80, 0x80, 0x80);
    internal static readonly SolidColorBrush CodeBackgroundBrush = FrozenBrush(0x30, 0x80, 0x80, 0x80);
    internal static readonly SolidColorBrush HrBrush = FrozenBrush(0x60, 0x80, 0x80, 0x80);

    public static FlowDocument ToFlowDocument(string? markdown)
    {
        var source = markdown ?? string.Empty;
        var document = new FlowDocument();
        var parsed = Markdig.Markdown.Parse(source, MarkdownPipelineFactory.Create());

        foreach (var block in parsed)
        {
            document.Blocks.Add(BuildBlock(block, source));
        }

        if (document.Blocks.Count == 0)
        {
            document.Blocks.Add(new Paragraph());
        }

        return document;
    }

    public static string ToMarkdown(FlowDocument document)
    {
        var writer = new StringBuilder();
        var blocks = document.Blocks.ToList();
        for (var i = 0; i < blocks.Count; i++)
        {
            if (i > 0)
            {
                writer.Append('\n');
            }

            WriteBlock(writer, blocks[i]);
        }

        return writer.ToString().TrimEnd('\n');
    }

    // ----- Markdown -> FlowDocument -----

    private static WpfBlock BuildBlock(Markdig.Syntax.Block block, string source) => block switch
    {
        HeadingBlock heading => BuildHeadingParagraph(heading, source),
        Markdig.Syntax.ParagraphBlock paragraph => BuildParagraph(paragraph.Inline?.FirstChild, source),
        QuoteBlock quote => BuildQuoteParagraph(quote, source),
        CodeBlock code => BuildCodeBlockParagraph(code),
        ThematicBreakBlock => BuildHorizontalRule(),
        ListBlock list => BuildList(list, source),
        _ => new Paragraph(new Run(SourceSpanText(block.Span, source)))
    };

    private static Paragraph BuildHeadingParagraph(HeadingBlock heading, string source)
    {
        var level = Math.Clamp(heading.Level, 1, 6);
        var paragraph = new Paragraph
        {
            Tag = level,
            FontWeight = FontWeights.Bold,
            FontSize = level switch { 1 => 26, 2 => 22, 3 => 18, _ => 16 },
            Margin = new Thickness(0, 12, 0, 6)
        };

        if (heading.Inline is not null)
        {
            // Baseline weight is Normal (not Bold) even though the heading paragraph
            // itself renders bold via FontWeight above -- otherwise every Run would also
            // get an explicit local Bold, which WriteFormattedRun would then see and
            // wrap in a redundant "**...**" on save.
            AppendInlines(paragraph.Inlines, heading.Inline.FirstChild, FontWeights.Normal, FontStyles.Normal, false, source);
        }

        return paragraph;
    }

    private static Paragraph BuildParagraph(MarkdigInlines.Inline? rootInline, string source)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
        if (rootInline is not null)
        {
            AppendInlines(paragraph.Inlines, rootInline, FontWeights.Normal, FontStyles.Normal, false, source);
        }

        return paragraph;
    }

    private static Paragraph BuildQuoteParagraph(QuoteBlock quote, string source)
    {
        var paragraph = new Paragraph
        {
            Tag = "Quote",
            BorderBrush = QuoteBorderBrush,
            BorderThickness = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(12, 2, 0, 2),
            Margin = new Thickness(0, 4, 0, 8),
            FontStyle = FontStyles.Italic
        };

        var isFirst = true;
        foreach (var inner in quote)
        {
            if (!isFirst)
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            isFirst = false;

            if (inner is Markdig.Syntax.ParagraphBlock innerParagraph && innerParagraph.Inline is not null)
            {
                // Baseline style is Normal even though the quote paragraph itself renders
                // italic via FontStyle above -- see the matching note in BuildHeadingParagraph.
                AppendInlines(paragraph.Inlines, innerParagraph.Inline.FirstChild, FontWeights.Normal, FontStyles.Normal, false, source);
            }
            else
            {
                paragraph.Inlines.Add(new Run(SourceSpanText(inner.Span, source)));
            }
        }

        return paragraph;
    }

    private static Paragraph BuildCodeBlockParagraph(CodeBlock code)
    {
        return new Paragraph(new Run(code.Lines.ToString()))
        {
            Tag = "CodeBlock",
            FontFamily = MonospaceFont,
            Background = CodeBackgroundBrush,
            Padding = new Thickness(8),
            Margin = new Thickness(0, 4, 0, 8)
        };
    }

    private static WpfBlock BuildHorizontalRule()
    {
        var border = new Border
        {
            BorderBrush = HrBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Margin = new Thickness(0, 8, 0, 8),
            Tag = "Hr"
        };

        return new BlockUIContainer(border);
    }

    private static System.Windows.Documents.List BuildList(ListBlock list, string source)
    {
        var wpfList = new System.Windows.Documents.List
        {
            MarkerStyle = list.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
            Margin = new Thickness(0, 0, 0, 8)
        };

        foreach (var item in list)
        {
            if (item is not ListItemBlock listItem)
            {
                continue;
            }

            var wpfItem = new ListItem();
            foreach (var child in listItem)
            {
                wpfItem.Blocks.Add(BuildBlock(child, source));
            }

            if (wpfItem.Blocks.Count == 0)
            {
                wpfItem.Blocks.Add(new Paragraph());
            }

            wpfList.ListItems.Add(wpfItem);
        }

        return wpfList;
    }

    private static void AppendInlines(
        InlineCollection target,
        MarkdigInlines.Inline? inline,
        FontWeight weight,
        FontStyle style,
        bool strike,
        string source)
    {
        for (var node = inline; node is not null; node = node.NextSibling)
        {
            switch (node)
            {
                case MarkdigInlines.LiteralInline literal:
                    target.Add(BuildRun(literal.Content.ToString(), weight, style, strike));
                    break;

                case MarkdigInlines.EmphasisInline emphasis:
                    var isStrike = emphasis.DelimiterChar == '~';
                    var isBold = !isStrike && emphasis.DelimiterCount is 2 or 3;
                    var isItalic = !isStrike && emphasis.DelimiterCount is 1 or 3;
                    AppendInlines(
                        target,
                        emphasis.FirstChild,
                        isBold ? FontWeights.Bold : weight,
                        isItalic ? FontStyles.Italic : style,
                        strike || isStrike,
                        source);
                    break;

                case MarkdigInlines.CodeInline code:
                    target.Add(BuildRun(code.Content, weight, style, strike, isCode: true));
                    break;

                case MarkdigInlines.LinkInline { IsImage: false } link:
                    target.Add(BuildHyperlink(link, weight, style, strike, source));
                    break;

                case MarkdigInlines.LineBreakInline:
                    target.Add(new LineBreak());
                    break;

                default:
                    // Unhandled inline (images, autolinks, raw html, footnotes, etc.) --
                    // never lose text, fall back to its raw source span.
                    var raw = SourceSpanText(node.Span, source);
                    if (!string.IsNullOrEmpty(raw))
                    {
                        target.Add(BuildRun(raw, weight, style, strike));
                    }

                    break;
            }
        }
    }

    private static Run BuildRun(string text, FontWeight weight, FontStyle style, bool strike, bool isCode = false)
    {
        var run = new Run(text) { FontWeight = weight, FontStyle = style };
        if (strike)
        {
            run.TextDecorations = TextDecorations.Strikethrough;
        }

        if (isCode)
        {
            run.FontFamily = MonospaceFont;
            run.Background = CodeBackgroundBrush;
        }

        return run;
    }

    private static Inline BuildHyperlink(
        MarkdigInlines.LinkInline link, FontWeight weight, FontStyle style, bool strike, string source)
    {
        var hyperlink = new Hyperlink();
        if (Uri.TryCreate(link.Url, UriKind.RelativeOrAbsolute, out var uri))
        {
            hyperlink.NavigateUri = uri;
        }

        if (link.FirstChild is not null)
        {
            AppendInlines(hyperlink.Inlines, link.FirstChild, weight, style, strike, source);
        }
        else
        {
            hyperlink.Inlines.Add(new Run(link.Url ?? string.Empty));
        }

        return hyperlink;
    }

    private static string SourceSpanText(SourceSpan span, string source)
    {
        if (span.Start < 0 || span.Length <= 0 || span.Start >= source.Length)
        {
            return string.Empty;
        }

        var length = Math.Min(span.Length, source.Length - span.Start);
        return source.Substring(span.Start, length);
    }

    // ----- FlowDocument -> Markdown -----

    private static void WriteBlock(StringBuilder writer, WpfBlock block)
    {
        switch (block)
        {
            case Paragraph { Tag: int headingLevel } paragraph when headingLevel is >= 1 and <= 6:
                writer.Append('#', headingLevel).Append(' ');
                WriteInlines(writer, paragraph.Inlines);
                writer.Append('\n');
                break;

            case Paragraph { Tag: "Quote" } paragraph:
                foreach (var line in InlinesToText(paragraph.Inlines).Split('\n'))
                {
                    writer.Append("> ").Append(line).Append('\n');
                }

                break;

            case Paragraph { Tag: "CodeBlock" } paragraph:
                writer.Append("```\n");
                writer.Append(new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.TrimEnd('\r', '\n'));
                writer.Append('\n').Append("```\n");
                break;

            case Paragraph paragraph:
                WriteInlines(writer, paragraph.Inlines);
                writer.Append('\n');
                break;

            case System.Windows.Documents.List list:
                WriteList(writer, list);
                break;

            case BlockUIContainer { Child: Border { Tag: "Hr" } }:
                writer.Append("---\n");
                break;
        }
    }

    private static void WriteList(StringBuilder writer, System.Windows.Documents.List list)
    {
        var ordered = list.MarkerStyle == TextMarkerStyle.Decimal;
        var number = 1;
        foreach (var block in list.ListItems)
        {
            if (block is not ListItem item)
            {
                continue;
            }

            var marker = ordered ? $"{number}. " : "- ";
            number++;

            var first = true;
            foreach (var itemBlock in item.Blocks)
            {
                var itemWriter = new StringBuilder();
                WriteBlock(itemWriter, itemBlock);
                var text = itemWriter.ToString().TrimEnd('\n');
                foreach (var line in text.Split('\n'))
                {
                    writer.Append(first ? marker : new string(' ', marker.Length));
                    first = false;
                    writer.Append(line).Append('\n');
                }
            }
        }
    }

    private static void WriteInlines(StringBuilder writer, InlineCollection inlines)
    {
        foreach (var inline in inlines)
        {
            WriteInline(writer, inline);
        }
    }

    private static string InlinesToText(InlineCollection inlines)
    {
        var builder = new StringBuilder();
        WriteInlines(builder, inlines);
        return builder.ToString();
    }

    private static void WriteInline(StringBuilder writer, Inline inline)
    {
        switch (inline)
        {
            case LineBreak:
                writer.Append('\n');
                break;

            case Hyperlink hyperlink:
                writer.Append('[');
                foreach (var child in hyperlink.Inlines)
                {
                    WriteInline(writer, child);
                }

                writer.Append("](").Append(hyperlink.NavigateUri?.ToString() ?? string.Empty).Append(')');
                break;

            case Run run:
                WriteFormattedRun(writer, run);
                break;

            case Span span:
                foreach (var child in span.Inlines)
                {
                    WriteInline(writer, child);
                }

                break;
        }
    }

    private static void WriteFormattedRun(StringBuilder writer, Run run)
    {
        var text = run.Text;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (run.FontFamily == MonospaceFont)
        {
            writer.Append('`').Append(text).Append('`');
            return;
        }

        var bold = run.FontWeight == FontWeights.Bold;
        var italic = run.FontStyle == FontStyles.Italic;
        var strike = run.TextDecorations is not null &&
                     run.TextDecorations.Any(d => d.Location == TextDecorationLocation.Strikethrough);

        var open = (strike ? "~~" : string.Empty) + (bold ? "**" : string.Empty) + (italic ? "*" : string.Empty);
        var close = (italic ? "*" : string.Empty) + (bold ? "**" : string.Empty) + (strike ? "~~" : string.Empty);

        writer.Append(open).Append(text).Append(close);
    }

    private static SolidColorBrush FrozenBrush(byte a, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }
}
