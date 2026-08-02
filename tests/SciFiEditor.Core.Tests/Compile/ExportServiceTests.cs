using FluentAssertions;
using SciFiEditor.Core.Compile;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Tests.Compile;

public class ExportServiceTests
{
    [Fact]
    public async Task ExportAsync_Markdown_WritesFileDirectly_WithoutInvokingPandoc()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "manuscript.md");
        var service = new ExportService();

        await service.ExportAsync("# Title\n\nBody text.", outputPath, ExportFormat.Markdown);

        File.Exists(outputPath).Should().BeTrue();
        (await File.ReadAllTextAsync(outputPath)).Should().Be("# Title\n\nBody text.");
    }

    [Fact]
    public async Task ExportAsync_NonMarkdownFormat_WhenPandocMissing_ThrowsPandocNotFoundException()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "manuscript.docx");
        var service = new ExportService("this-executable-does-not-exist-12345");

        var act = async () => await service.ExportAsync("content", outputPath, ExportFormat.Docx);

        await act.Should().ThrowAsync<PandocNotFoundException>();
    }
}
