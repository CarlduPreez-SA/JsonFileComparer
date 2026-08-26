using Avalonia.Controls;
using Avalonia.Interactivity;

namespace JsonFileComparer.App.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string message, string confirmText = "Overwrite") : this()
    {
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);

    public static async System.Threading.Tasks.Task<bool> ShowAsync(Window owner, string message, string confirmText = "Overwrite")
    {
        var dialog = new ConfirmDialog(message, confirmText);
        return await dialog.ShowDialog<bool>(owner);
    }
}
