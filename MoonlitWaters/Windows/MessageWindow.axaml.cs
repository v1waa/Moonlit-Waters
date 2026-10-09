using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MoonlitWaters.Windows;

public partial class MessageWindow : Window
{
    public MessageWindow() { InitializeComponent(); }
    public MessageWindow(string title, string message, bool confirmation = false) : this()
    {
        Heading.Text = title;
        Message.Text = message;
        CopyButton.IsVisible = !confirmation;
        CancelButton.IsVisible = confirmation;
    }
    private void OkClick(object? sender, RoutedEventArgs e) => Close(true);
    private void CancelClick(object? sender, RoutedEventArgs e) => Close(false);
    private async void CopyClick(object? sender, RoutedEventArgs e)
    {
        if (Clipboard is not null) { await Clipboard.SetTextAsync(Message.Text); CopyButton.Content = "Скопировано"; }
    }
}
