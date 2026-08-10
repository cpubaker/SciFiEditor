using Markdig;

namespace SciFiEditor.Core.Compile;

public static class MarkdownPipelineFactory
{
    private static readonly MarkdownPipeline SharedPipeline = new MarkdownPipelineBuilder()
        .UseEmphasisExtras()
        .Build();

    public static MarkdownPipeline Create() => SharedPipeline;
}
