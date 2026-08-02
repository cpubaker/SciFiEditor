using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SciFiEditor.App.Resources;
using SciFiEditor.Core.Compile;
using SciFiEditor.Domain;

namespace SciFiEditor.App.ViewModels;

public sealed partial class CompileViewModel : ObservableObject
{
    private readonly CompileService _compileService;
    private readonly ExportService _exportService;

    public CompileViewModel(CompileService compileService, ExportService exportService)
    {
        _compileService = compileService;
        _exportService = exportService;
    }

    public IReadOnlyList<ExportFormatOption> FormatOptions { get; } =
    [
        new(ExportFormat.Markdown, "Markdown (.md)"),
        new(ExportFormat.Docx, "Word (.docx)"),
        new(ExportFormat.Epub, "EPUB (.epub)"),
        new(ExportFormat.Pdf, "PDF (.pdf)")
    ];

    [ObservableProperty]
    private ExportFormat _selectedFormat = ExportFormat.Markdown;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [RelayCommand]
    private async Task ExportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = FilterFor(SelectedFormat),
            FileName = "manuscript" + ExtensionFor(SelectedFormat)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            StatusMessage = string.Empty;
            var markdown = await _compileService.BuildManuscriptMarkdownAsync();
            await _exportService.ExportAsync(markdown, dialog.FileName, SelectedFormat);
            StatusMessage = Strings.CompileSuccess;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private static string ExtensionFor(ExportFormat format) => format switch
    {
        ExportFormat.Markdown => ".md",
        ExportFormat.Docx => ".docx",
        ExportFormat.Epub => ".epub",
        ExportFormat.Pdf => ".pdf",
        _ => ".md"
    };

    private static string FilterFor(ExportFormat format) => format switch
    {
        ExportFormat.Markdown => "Markdown (*.md)|*.md",
        ExportFormat.Docx => "Word Document (*.docx)|*.docx",
        ExportFormat.Epub => "EPUB (*.epub)|*.epub",
        ExportFormat.Pdf => "PDF (*.pdf)|*.pdf",
        _ => "All files (*.*)|*.*"
    };
}
