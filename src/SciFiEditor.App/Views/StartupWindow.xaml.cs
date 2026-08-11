using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SciFiEditor.App.Resources;
using SciFiEditor.App.ViewModels;
using SciFiEditor.Core.Stats;
using SciFiEditor.Domain;

namespace SciFiEditor.App.Views;

public partial class StartupWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly GlobalStatsService _globalStatsService;

    public StartupWindow(MainViewModel viewModel, GlobalStatsService globalStatsService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _globalStatsService = globalStatsService;

        RecentProjectsList.ItemsSource = _viewModel.RecentProjects;
        NoRecentProjectsText.Visibility = _viewModel.RecentProjects.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        LoadActivity();
    }

    private void LoadActivity()
    {
        ActivityHeatmap.Data = _globalStatsService.GetHeatmapData();
        StreakText.Text = string.Format(Strings.StartupStreakLabel, _globalStatsService.GetStreak());
        TotalDaysText.Text = string.Format(Strings.StartupTotalDaysLabel, _globalStatsService.GetTotalDaysWritten());

        // Deferred: the ScrollViewer hasn't measured/arranged the heatmap yet at construction time,
        // so ScrollableWidth is still 0 here. Run after layout completes instead.
        Dispatcher.BeginInvoke(() =>
        {
            // ScrollToRightEnd() lands at an arbitrary pixel offset, slicing the leftmost visible
            // week-column mid-cell. Snap down to the nearest column boundary so it renders whole.
            var snappedOffset = Math.Floor(HeatmapScrollViewer.ScrollableWidth / Controls.CalendarHeatmapControl.ColumnWidth)
                * Controls.CalendarHeatmapControl.ColumnWidth;
            HeatmapScrollViewer.ScrollToHorizontalOffset(snappedOffset);
        }, DispatcherPriority.Loaded);
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
