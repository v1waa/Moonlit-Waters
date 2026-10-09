using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using MoonlitWaters.Models;

namespace MoonlitWaters.Windows;

public partial class LoginWindow : Window
{
    public LoginWindow() { InitializeComponent(); }

    private async void LoginClick(object? sender, RoutedEventArgs e) => await SignIn();
    private async void InputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; await SignIn(); }
    }

    private async Task SignIn()
    {
        if (!LoginButton.IsEnabled) return;
        ErrorText.Text = "";
        if (string.IsNullOrWhiteSpace(LoginBox.Text) || string.IsNullOrEmpty(PasswordBox.Text))
        { ErrorText.Text = "Введите логин и пароль."; return; }
        LoginButton.IsEnabled = false;
        try
        {
            var result = await App.Database.CallAsync<LoginResult>("mw_login", new { p_username = LoginBox.Text.Trim(), p_password = PasswordBox.Text });
            if (!result.Ok) { ErrorText.Text = result.Message; return; }
            App.Token = result.Token;
            App.CurrentUser = result.Profile;
            var window = new DayWindow();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = window;
            window.Show();
            PasswordBox.Text = "";
            Close();
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { LoginButton.IsEnabled = true; }
    }
}
