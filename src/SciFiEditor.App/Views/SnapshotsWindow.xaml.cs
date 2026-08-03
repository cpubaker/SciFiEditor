using System.Windows;
using SciFiEditor.App.ViewModels;

namespace SciFiEditor.App.Views;

public partial class SnapshotsWindow : Window
{
    public SnapshotsWindow(SnapshotViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
