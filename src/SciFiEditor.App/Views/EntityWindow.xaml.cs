using System.Windows;
using SciFiEditor.App.ViewModels;

namespace SciFiEditor.App.Views;

public partial class EntityWindow : Window
{
    public EntityWindow(EntityViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
