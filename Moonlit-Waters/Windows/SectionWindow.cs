using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Moonlit_Waters.Models;

namespace Moonlit_Waters.Windows;

// Общие действия окон: загрузка, дата, меню и диалог редактирования.
public class SectionWindow : Window
{
    protected SalonData data = new();
    protected bool ready;
    private bool dialogOpen;
    private int loadVersion;
    private DatePicker dayPicker = null!;
    private TextBlock pageError = null!;
    private Border dialogShade = null!;
    public string Section { get; private set; } = "";
    protected DateTime SelectedDay => (dayPicker.SelectedDate ?? App.MoscowNow).Date;

    protected void InitializeSection(string section)
    {
        Section = section;
        dayPicker = ((DatePicker)FindName("DayPicker"));
        pageError = ((TextBlock)FindName("PageError"));
        dialogShade = ((Border)FindName("DialogShade"));
        ((TextBlock)FindName("UserNameText")).Text = App.CurrentUser.FullName;
        ((TextBlock)FindName("UserRoleText")).Text = App.CurrentUser.RoleText;
        ((TextBlock)FindName("TopRoleText")).Text = App.CurrentUser.RoleText;
        dayPicker.SelectedDate = App.MoscowNow.Date;
        foreach (var name in new[] { "DayNav", "ClientsNav", "PricesNav", "StaffNav" })
        {
            var button = (Button)FindName(name);
            string target = button.Tag?.ToString() ?? "day";
            button.Visibility = (App.CurrentUser.IsAdmin || target is "day" or "clients") ? Visibility.Visible : Visibility.Collapsed;
            button.Click += (_, _) => WindowNavigation.Open(target, SelectedDay);
        }
        ((Button)FindName("RefreshButton")).Click += async (_, _) => await ReloadAsync();
        ((Button)FindName("PasswordButton")).Click += async (_, _) => await OpenForm(new PasswordWindow());
        ((Button)FindName("LogoutButton")).Click += async (_, _) =>
        {
            var button = ((Button)FindName("LogoutButton"));
            button.IsEnabled = false;
            try { await WindowNavigation.LogoutAsync(); }
            catch (Exception ex) { pageError.Text = ex.Message; pageError.Visibility = Visibility.Visible; }
            finally { button.IsEnabled = true; }
        };
        dayPicker.SelectedDateChanged += async (_, _) => { if (ready) await ReloadAsync(); };
        Loaded += async (_, _) => { ready = true; await ReloadAsync(); };
        Closed += (_, _) => { ready = false; loadVersion++; };
        KeyDown += async (_, e) =>
        {
            if (e.Key == Key.F5 && !dialogOpen) { e.Handled = true; await ReloadAsync(); }
        };
    }

    public void SelectDay(DateTime day) => dayPicker.SelectedDate = day.Date;

    public async Task ReloadAsync()
    {
        int version = ++loadVersion;
        pageError.Visibility = Visibility.Collapsed;
        try
        {
            var result = await App.Database.CallAsync<SalonData>("mw_load", new { p_token = App.Token, p_day = SelectedDay.ToString("yyyy-MM-dd") });
            if (version != loadVersion) return;
            data = result;
            RenderData();
        }
        catch (Exception ex)
        {
            if (version != loadVersion) return;
            data = new SalonData();
            RenderData();
            pageError.Text = ex.Message;
            pageError.Visibility = Visibility.Visible;
        }
    }

    protected virtual void RenderData() { }

    protected async Task OpenForm(DialogWindow form)
    {
        if (dialogOpen) return;
        dialogOpen = true;
        dialogShade.Visibility = Visibility.Visible;
        form.KeyDown += (_, e) => { if (e.Key == Key.Escape) form.Close(false); };
        try
        {
            if (await form.ShowDialogAsync(this)) await WindowNavigation.RefreshAllAsync();
        }
        finally { dialogOpen = false; dialogShade.Visibility = Visibility.Collapsed; }
    }
}
