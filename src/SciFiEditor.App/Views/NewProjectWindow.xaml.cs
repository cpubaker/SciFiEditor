using System.Windows;
using Microsoft.Win32;
using SciFiEditor.App.Resources;

namespace SciFiEditor.App.Views;

public partial class NewProjectWindow : Window
{
    public NewProjectWindow()
    {
        InitializeComponent();
    }

    public string ProjectName => NameTextBox.Text.Trim();
    public string ProjectLocation => LocationTextBox.Text.Trim();

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Strings.DialogNewProjectLocation };
        if (dialog.ShowDialog(this) == true)
        {
            LocationTextBox.Text = dialog.FolderName;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProjectName) || string.IsNullOrWhiteSpace(ProjectLocation))
        {
            MessageBox.Show(this, Strings.DialogNewProjectName + " / " + Strings.DialogNewProjectLocation,
                Strings.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
