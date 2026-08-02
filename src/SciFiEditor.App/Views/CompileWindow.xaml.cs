using System.Windows;
using SciFiEditor.App.ViewModels;

namespace SciFiEditor.App.Views;

public partial class CompileWindow : Window
{
    public CompileWindow(CompileViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
