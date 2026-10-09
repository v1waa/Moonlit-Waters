using System.Windows;
using MoonlitWaters.Models;

namespace MoonlitWaters.Windows;

public static class WindowNavigation
{
    public static void Open(string section, DateTime day)
    {
        if (!App.CurrentUser.IsAdmin && section is "prices" or "staff") return;
        var window = Application.Current.Windows.OfType<SectionWindow>().FirstOrDefault(w => w.Section == section);
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
            if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
        }
        window.Activate();
    }

    public static Task RefreshAllAsync() =>
        Task.WhenAll(Application.Current.Windows.OfType<SectionWindow>().Select(w => w.ReloadAsync()));

    public static async Task LogoutAsync()
    {
        await App.Database.CallAsync<bool>("mw_logout", new { p_token = App.Token });
        App.Token = "";
        App.CurrentUser = new Profile();
        var oldWindows = Application.Current.Windows.Cast<Window>().ToArray();
        var login = new LoginWindow();
        Application.Current.MainWindow = login;
        login.Show();
        foreach (var window in oldWindows) window.Close();
    }
}
