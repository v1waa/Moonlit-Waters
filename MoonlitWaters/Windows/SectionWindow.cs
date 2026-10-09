using Avalonia.Controls;
using Avalonia.Input;
using MoonlitWaters.Models;

namespace MoonlitWaters.Windows;

// Общие действия окон: загрузка, дата, меню и диалог редактирования.
public class SectionWindow : Window
{
    protected SalonData data = new();
    protected bool ready;
    private bool dialogOpen;
    private int loadVersion;
    private CalendarDatePicker dayPicker = null!;
    private TextBlock pageError = null!;
    private Border dialogShade = null!;
    public string Section { get; private set; } = "";
    protected DateTime SelectedDay => (dayPicker.SelectedDate ?? App.MoscowNow).Date;

    protected void InitializeSection(string section)
    {
        Section = section;
        dayPicker = this.FindControl<CalendarDatePicker>("DayPicker")!;
        pageError = this.FindControl<TextBlock>("PageError")!;
        dialogShade = this.FindControl<Border>("DialogShade")!;
        this.FindControl<TextBlock>("UserNameText")!.Text = App.CurrentUser.FullName;
        this.FindControl<TextBlock>("UserRoleText")!.Text = App.CurrentUser.RoleText;
        this.FindControl<TextBlock>("TopRoleText")!.Text = App.CurrentUser.RoleText;
        dayPicker.SelectedDate = App.MoscowNow.Date;
        foreach (var name in new[] { "DayNav", "ClientsNav", "PricesNav", "StaffNav" })
        {
            var button = this.FindControl<Button>(name)!;
            string target = button.Tag?.ToString() ?? "day";
            button.IsVisible = App.CurrentUser.IsAdmin || target is "day" or "clients";
            button.Click += (_, _) => WindowNavigation.Open(target, SelectedDay);
        }
        this.FindControl<Button>("RefreshButton")!.Click += async (_, _) => await ReloadAsync();
        this.FindControl<Button>("PasswordButton")!.Click += async (_, _) => await OpenForm(new PasswordWindow());
        this.FindControl<Button>("LogoutButton")!.Click += async (_, _) =>
        {
            var button = this.FindControl<Button>("LogoutButton")!;
            button.IsEnabled = false;
            try { await WindowNavigation.LogoutAsync(); }
            catch (Exception ex) { pageError.Text = ex.Message; pageError.IsVisible = true; }
            finally { button.IsEnabled = true; }
        };
        dayPicker.SelectedDateChanged += async (_, _) => { if (ready) await ReloadAsync(); };
        Opened += async (_, _) => { ready = true; await ReloadAsync(); };
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
        pageError.IsVisible = false;
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
            pageError.IsVisible = true;
        }
    }

    protected virtual void RenderData() { }

    protected async Task OpenForm(Window form)
    {
        if (dialogOpen) return;
        dialogOpen = true;
        dialogShade.IsVisible = true;
        form.KeyDown += (_, e) => { if (e.Key == Key.Escape) form.Close(false); };
        try
        {
            if (await form.ShowDialog<bool>(this)) await WindowNavigation.RefreshAllAsync();
        }
        finally { dialogOpen = false; dialogShade.IsVisible = false; }
    }
}
