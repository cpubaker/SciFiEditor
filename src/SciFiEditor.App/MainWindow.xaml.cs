using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SciFiEditor.App.ViewModels;

namespace SciFiEditor.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        Deactivated += MainWindow_Deactivated;
        Closing += MainWindow_Closing;
    }

    private async void BinderTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        await _viewModel.OnBinderSelectionChangedAsync(e.NewValue as BinderNodeViewModel);
    }

    private async void MainWindow_Deactivated(object? sender, EventArgs e)
    {
        await _viewModel.FlushAutosaveAsync();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        Task.Run(() => _viewModel.FlushAutosaveAsync()).Wait();
    }

    private void RenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox { IsVisible: true } textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }

    private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: BinderNodeViewModel node })
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            node.CommitRename();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            node.CancelRename();
            e.Handled = true;
        }
    }

    private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: BinderNodeViewModel node })
        {
            node.CommitRename();
        }
    }

    private void InspectorField_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BinderNodeViewModel node })
        {
            node.CommitInspector();
        }
    }

    private void InspectorField_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BinderNodeViewModel node })
        {
            node.CommitInspector();
        }
    }
}
