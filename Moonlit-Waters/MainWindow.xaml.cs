using Moonlit_Waters.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Moonlit_Waters.Models;

namespace Moonlit_Waters;

public partial class MainWindow : Window
{
    public MainWindow() { InitializeComponent(); }

    private async void LoginClick(object? sender, RoutedEventArgs e) => await SignIn();
    private async void InputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; await SignIn(); }
    }

    private async Task SignIn()
    {
        if (!LoginButton.IsEnabled) return;
        ErrorText.Text = "";
        if (string.IsNullOrWhiteSpace(LoginBox.Text) || string.IsNullOrEmpty(PasswordBox.Password))
        { ErrorText.Text = "Введите логин и пароль."; return; }
        LoginButton.IsEnabled = false;
        try
        {
            var result = await App.Database.CallAsync<LoginResult>("mw_login", new { p_username = LoginBox.Text.Trim(), p_password = PasswordBox.Password });
            if (!result.Ok) { ErrorText.Text = result.Message; return; }
            App.Token = result.Token;
            App.CurrentUser = result.Profile;
            WindowNavigation.SelectedDay = App.MoscowNow.Date;
            var window = new DayWindow();
            Application.Current.MainWindow = window;
            window.Show();
            PasswordBox.Password = "";
            Close();
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { LoginButton.IsEnabled = true; }
    }
}
