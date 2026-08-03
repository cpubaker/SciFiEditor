using System.Windows;
using System.Windows.Input;
using SciFiEditor.App.ViewModels;

namespace SciFiEditor.App.Views;

public partial class SearchWindow : Window
{
    public SearchWindow(SearchViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void QueryTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is SearchViewModel viewModel)
        {
            viewModel.SearchCommand.Execute(null);
        }
    }

    private void ResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SearchViewModel viewModel)
        {
            viewModel.OpenResultCommand.Execute(viewModel.SelectedResult);
        }
    }
}
