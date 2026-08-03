using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SciFiEditor.App.Resources;
using SciFiEditor.Core.Snapshots;
using SciFiEditor.Domain;

namespace SciFiEditor.App.ViewModels;

public sealed partial class SnapshotViewModel : ObservableObject
{
    private readonly SnapshotService _snapshotService;
    private readonly Guid _nodeId;

    public SnapshotViewModel(SnapshotService snapshotService, Guid nodeId)
    {
        _snapshotService = snapshotService;
        _nodeId = nodeId;
        LoadHistory();
    }

    public ObservableCollection<SceneSnapshotInfo> History { get; } = new();

    public string? RestoredContent { get; private set; }

    [ObservableProperty]
    private SceneSnapshotInfo? _selectedSnapshot;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [RelayCommand]
    private async Task TakeSnapshotAsync()
    {
        await _snapshotService.TakeSnapshotAsync(_nodeId, SnapshotService.ManualLabel);
        LoadHistory();
        StatusMessage = Strings.SnapshotTaken;
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (SelectedSnapshot is null)
        {
            return;
        }

        var result = MessageBox.Show(
            Strings.SnapshotRestoreConfirm,
            Strings.SnapshotsWindowTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        RestoredContent = await _snapshotService.RestoreSnapshotAsync(_nodeId, SelectedSnapshot.Id);
        StatusMessage = Strings.SnapshotRestored;
    }

    private void LoadHistory()
    {
        History.Clear();
        foreach (var snapshot in _snapshotService.GetHistory(_nodeId))
        {
            History.Add(snapshot);
        }
    }
}
