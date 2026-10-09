using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MoonlitWaters.Windows;

public partial class MessageWindow : DialogWindow
{
    public MessageWindow() { InitializeComponent(); }
    public MessageWindow(string title, string message, bool confirmation = false) : this()
    {
        Heading.Text = title;
        Message.Text = message;
        CopyButton.Visibility = (!confirmation) ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Visibility = (confirmation) ? Visibility.Visible : Visibility.Collapsed;
    }
    private void OkClick(object? sender, RoutedEventArgs e) => Close(true);
    private void CancelClick(object? sender, RoutedEventArgs e) => Close(false);
    private void CopyClick(object? sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(Message.Text); CopyButton.Content = "Скопировано"; }
        catch (System.Runtime.InteropServices.ExternalException) { CopyButton.Content = "Повторить копирование"; }
    }
}
