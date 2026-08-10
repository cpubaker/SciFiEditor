using System.Windows;

namespace SciFiEditor.App.Views;

public partial class InsertLinkWindow : Window
{
    public InsertLinkWindow(string initialText)
    {
        InitializeComponent();
        LinkTextBox.Text = initialText;
        LinkTextBox.IsEnabled = string.IsNullOrEmpty(initialText);
    }

    public string LinkText => LinkTextBox.Text.Trim();
    public string Url => UrlTextBox.Text.Trim();

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            return;
        }

        DialogResult = true;
    }
}
