using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MoonlitWaters.Windows;

public partial class PasswordWindow : Window
{
    public PasswordWindow() { InitializeComponent(); SaveButton.Content = "Сохранить"; }
    private void CancelClick(object? sender, RoutedEventArgs e) => Close(false);
    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        if (NewBox.Text != RepeatBox.Text) { ErrorText.Text = "Новые пароли не совпадают."; return; }
        SaveButton.IsEnabled = false;
        try
        {
            await App.Database.CallAsync<bool>("mw_change_password", new { p_token = App.Token, p_old_password = OldBox.Text ?? "", p_new_password = NewBox.Text ?? "" });
            await new MessageWindow("Готово", "Пароль изменён.").ShowDialog<bool>(this);
            Close(true);
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { SaveButton.IsEnabled = true; }
    }
}
