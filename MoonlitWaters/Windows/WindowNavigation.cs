using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MoonlitWaters.Models;

namespace MoonlitWaters.Windows;

public static class WindowNavigation
{
    private static IClassicDesktopStyleApplicationLifetime Desktop =>
        (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

    public static void Open(string section, DateTime day)
    {
        if (!App.CurrentUser.IsAdmin && section is "prices" or "staff") return;
        var window = Desktop.Windows.OfType<SectionWindow>().FirstOrDefault(w => w.Section == section);
        if (window is null)
        {
            window = section switch
            {
                "clients" => new ClientsWindow(),
                "prices" => new PricesWindow(),
                "staff" => new StaffWindow(),
                _ => new DayWindow()
            };
            window.SelectDay(day);
            window.Show();
        }
        else
        {
            window.SelectDay(day);
            if (window.WindowState == Avalonia.Controls.WindowState.Minimized)
                window.WindowState = Avalonia.Controls.WindowState.Normal;
        }
        window.Activate();
    }

    public static Task RefreshAllAsync() =>
        Task.WhenAll(Desktop.Windows.OfType<SectionWindow>().Select(w => w.ReloadAsync()));

    public static async Task LogoutAsync()
    {
        await App.Database.CallAsync<bool>("mw_logout", new { p_token = App.Token });
        App.Token = "";
        App.CurrentUser = new Profile();
        var oldWindows = Desktop.Windows.ToArray();
        var login = new LoginWindow();
        Desktop.MainWindow = login;
        login.Show();
        foreach (var window in oldWindows) window.Close();
    }
}
