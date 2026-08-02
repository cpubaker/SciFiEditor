using FluentAssertions;
using SciFiEditor.Core.Compile;

namespace SciFiEditor.Core.Tests.Compile;

public class MarkdownPreviewServiceTests
{
    [Fact]
    public void ToHtml_ConvertsBasicMarkdown()
    {
        var html = MarkdownPreviewService.ToHtml("# Hello\n\nWorld");

        html.Should().Contain("<h1>Hello</h1>");
        html.Should().Contain("<p>World</p>");
    }

    [Fact]
    public void ToHtml_NullInput_ReturnsValidEmptyDocument()
    {
        var html = MarkdownPreviewService.ToHtml(null);

        html.Should().Contain("<html>");
        html.Should().Contain("<body>");
    }

    [Fact]
    public void ToHtml_WrapsInStyledHtmlShell()
    {
        var html = MarkdownPreviewService.ToHtml("text");

        html.Should().Contain("<style>");
        html.Should().StartWith("<html>");
        html.Should().EndWith("</html>");
    }
}
