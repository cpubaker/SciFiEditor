using System.Windows;
using System.Windows.Controls;
using SciFiEditor.App.ViewModels;
using SciFiEditor.Domain;

namespace SciFiEditor.App.Views;

public partial class StartupWindow : Window
{
    private readonly MainViewModel _viewModel;

    public StartupWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        RecentProjectsList.ItemsSource = _viewModel.RecentProjects;
        NoRecentProjectsText.Visibility = _viewModel.RecentProjects.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async void NewProjectButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.NewProjectCommand.ExecuteAsync(null);
        Close();
    }

    private void OpenProjectButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenProjectCommand.Execute(null);
        Close();
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RecentProjectsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (RecentProjectsList.SelectedItem is RecentProjectEntry entry)
        {
            _viewModel.OpenRecentProjectCommand.Execute(entry);
            Close();
        }
    }
}
