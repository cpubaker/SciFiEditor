using System.ComponentModel;
using System.Diagnostics;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Compile;

public sealed class ExportService
{
    private readonly string _pandocExecutable;

    public ExportService(string pandocExecutable = "pandoc")
    {
        _pandocExecutable = pandocExecutable;
    }

    public async Task ExportAsync(string markdown, string outputPath, ExportFormat format)
    {
        if (format == ExportFormat.Markdown)
        {
            await File.WriteAllTextAsync(outputPath, markdown);
            return;
        }

        var tempMarkdownPath = Path.Combine(Path.GetTempPath(), $"scifieditor-compile-{Guid.NewGuid():N}.md");
        try
        {
            await File.WriteAllTextAsync(tempMarkdownPath, markdown);
            await RunPandocAsync(tempMarkdownPath, outputPath);
        }
        finally
        {
            if (File.Exists(tempMarkdownPath))
            {
                File.Delete(tempMarkdownPath);
            }
        }
    }

    private async Task RunPandocAsync(string inputPath, string outputPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _pandocExecutable,
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(outputPath);

        Process process;
        try
        {
            process = Process.Start(startInfo) ?? throw new PandocNotFoundException();
        }
        catch (Win32Exception)
        {
            throw new PandocNotFoundException();
        }

        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"pandoc exited with code {process.ExitCode}: {stderr}");
        }
    }
}

public sealed class PandocNotFoundException : Exception
{
    public PandocNotFoundException()
        : base("pandoc.exe not found on PATH. Install pandoc from https://pandoc.org and ensure it is on PATH.")
    {
    }
}
