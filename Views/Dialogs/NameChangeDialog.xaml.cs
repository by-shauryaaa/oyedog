using System.Windows;

namespace PixelDogReminders.Views.Dialogs;

public partial class NameChangeDialog : Window
{
    public string ResultDisplayName { get; private set; } = "Abhishek";

    public NameChangeDialog(string currentName)
    {
        InitializeComponent();
        ResultDisplayName = string.IsNullOrWhiteSpace(currentName) ? "Abhishek" : currentName.Trim();
        TxtDisplayName.Text = ResultDisplayName;
        TxtDisplayName.Focus();
        TxtDisplayName.SelectAll();
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        TxtDisplayName.Text = "Abhishek";
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var input = TxtDisplayName.Text.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            TxtValidationMsg.Text = "Please enter a valid display name.";
            TxtValidationMsg.Visibility = Visibility.Visible;
            return;
        }

        ResultDisplayName = input;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
