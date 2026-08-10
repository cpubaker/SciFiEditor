using System.Windows.Documents;

namespace SciFiEditor.App.Editor;

// Small navigation helpers shared by FindReplaceBar and TypewriterScrollController now
// that the editor is a real RichTextBox/FlowDocument instead of AvalonEdit's flat-text
// TextDocument.
public static class RichTextNavigationHelper
{
    public static IEnumerable<Paragraph> EnumerateParagraphs(FlowDocument document) =>
        EnumerateParagraphs(document.Blocks);

    private static IEnumerable<Paragraph> EnumerateParagraphs(IEnumerable<Block> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case Paragraph paragraph:
                    yield return paragraph;
                    break;

                case System.Windows.Documents.List list:
                    foreach (var item in list.ListItems)
                    {
                        foreach (var paragraph in EnumerateParagraphs(item.Blocks))
                        {
                            yield return paragraph;
                        }
                    }

                    break;

                case Section section:
                    foreach (var paragraph in EnumerateParagraphs(section.Blocks))
                    {
                        yield return paragraph;
                    }

                    break;
            }
        }
    }
}
