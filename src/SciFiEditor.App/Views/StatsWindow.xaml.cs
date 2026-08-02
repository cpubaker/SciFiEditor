using System.Windows;
using SciFiEditor.App.ViewModels;

namespace SciFiEditor.App.Views;

public partial class StatsWindow : Window
{
    public StatsWindow(StatsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
