using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SciFiEditor.App.Editor;
using SciFiEditor.App.ViewModels;
using SciFiEditor.Domain;

namespace SciFiEditor.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly TypewriterScrollController _typewriterScroll;
    private readonly FocusModeDimmer _focusModeDimmer;
    private bool _isFocusModeActive;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        Deactivated += MainWindow_Deactivated;
        Closing += MainWindow_Closing;

        FindReplaceBarControl.Attach(SceneEditor);
        FormattingToolbarControl.Attach(SceneEditor);
        _typewriterScroll = new TypewriterScrollController(SceneEditor);
        _focusModeDimmer = new FocusModeDimmer(SceneEditor);
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            FindReplaceBarControl.Open();
            e.Handled = true;
        }
        else if (e.Key == Key.F11)
        {
            _viewModel.IsFocusMode = !_viewModel.IsFocusMode;
            e.Handled = true;
        }
        else if (e.Key == Key.N && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            _viewModel.AddChapterCommand.Execute(CurrentFolder());
            e.Handled = true;
        }
        else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.AddSceneCommand.Execute(CurrentFolder());
            e.Handled = true;
        }
    }

    private void FindMenuItem_Click(object sender, RoutedEventArgs e)
    {
        FindReplaceBarControl.Open();
    }

    private BinderNodeViewModel? CurrentFolder()
    {
        var selected = _viewModel.SelectedNode;
        return selected is { NodeType: NodeType.Folder or NodeType.Chapter } ? selected : null;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.IsFocusMode))
        {
            return;
        }

        if (_viewModel.IsFocusMode && !_isFocusModeActive)
        {
            _isFocusModeActive = true;
            _typewriterScroll.Attach();
            _focusModeDimmer.Attach();
        }
        else if (!_viewModel.IsFocusMode && _isFocusModeActive)
        {
            _isFocusModeActive = false;
            _typewriterScroll.Detach();
            _focusModeDimmer.Detach();
        }
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
