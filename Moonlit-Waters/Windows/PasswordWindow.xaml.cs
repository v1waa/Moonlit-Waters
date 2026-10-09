using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Moonlit_Waters.Windows;

public partial class PasswordWindow : DialogWindow
{
    public PasswordWindow() { InitializeComponent(); SaveButton.Content = "Сохранить"; }
    private void CancelClick(object? sender, RoutedEventArgs e) => Close(false);
    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        if (NewBox.Password != RepeatBox.Password) { ErrorText.Text = "Новые пароли не совпадают."; return; }
        SaveButton.IsEnabled = false;
        try
        {
            await App.Database.CallAsync<bool>("mw_change_password", new { p_token = App.Token, p_old_password = OldBox.Password ?? "", p_new_password = NewBox.Password ?? "" });
            await new MessageWindow("Готово", "Пароль изменён.").ShowDialogAsync(this);
            Close(true);
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { SaveButton.IsEnabled = true; }
    }
}
