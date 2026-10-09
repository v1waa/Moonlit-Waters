using System.Windows;
using Moonlit_Waters.Models;

namespace Moonlit_Waters.Windows;

public static class WindowNavigation
{
    public static DateTime SelectedDay { get; set; } = App.MoscowNow.Date;

    public static void Switch(Window current, string section, DateTime day)
    {
        if (!App.CurrentUser.IsAdmin && section is "prices" or "staff") return;
        if (section == "day" && current is DayWindow ||
            section == "clients" && current is ClientsWindow ||
            section == "prices" && current is PricesWindow ||
            section == "staff" && current is StaffWindow) return;

        SelectedDay = day.Date;
        Window? next = section switch
        {
            "day" => new DayWindow(),
            "clients" => new ClientsWindow(),
            "prices" => new PricesWindow(),
            "staff" => new StaffWindow(),
            _ => null
        };
        if (next is null) return;

        // Новый раздел появляется на месте предыдущего, включая развёрнутый режим.
        var bounds = current.RestoreBounds;
        if (!bounds.IsEmpty)
        {
            next.WindowStartupLocation = WindowStartupLocation.Manual;
            next.Left = bounds.Left;
            next.Top = bounds.Top;
            next.Width = bounds.Width;
            next.Height = bounds.Height;
        }
        next.WindowState = current.WindowState;
        Application.Current.MainWindow = next;
        next.Show();
        current.Close();
    }

    public static async Task LogoutAsync(Window current)
    {
        await App.Database.CallAsync<bool>("mw_logout", new { p_token = App.Token });
        App.Token = "";
        App.CurrentUser = new Profile();
        var login = new MainWindow();
        Application.Current.MainWindow = login;
        login.Show();
        current.Close();
    }
}
