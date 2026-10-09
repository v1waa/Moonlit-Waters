using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Moonlit_Waters.Models;
using System.Globalization;

namespace Moonlit_Waters.Windows;

public partial class DayWindow : Window
{

    private SalonData data = new();
    private bool ready;
    private bool dialogOpen;
    private int loadVersion;
    private DateTime SelectedDay => (DayPicker.SelectedDate ?? WindowNavigation.SelectedDay).Date;

    private async void WindowLoaded(object sender, RoutedEventArgs e)
    {
        ready = true;
        await ReloadAsync();
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        ready = false;
        loadVersion++;
    }

    private void NavigateClick(object sender, RoutedEventArgs e)
    {
        if (!dialogOpen && sender is Button button)
            WindowNavigation.Switch(this, button.Tag?.ToString() ?? "day", SelectedDay);
    }

    private async void DayChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready) return;
        if (DayPicker.SelectedDate is null)
        {
            DayPicker.SelectedDate = WindowNavigation.SelectedDay;
            return;
        }
        WindowNavigation.SelectedDay = SelectedDay;
        await ReloadAsync();
    }

    private void DateValidationError(object sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        DayPicker.Text = SelectedDay.ToString("dd.MM.yyyy");
    }

    private async void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5 && !dialogOpen)
        {
            e.Handled = true;
            await ReloadAsync();
        }
    }

    private async void LogoutClick(object sender, RoutedEventArgs e)
    {
        LogoutButton.IsEnabled = false;
        try { await WindowNavigation.LogoutAsync(this); }
        catch (Exception ex) { PageError.Text = ex.Message; PageError.Visibility = Visibility.Visible; }
        finally { LogoutButton.IsEnabled = true; }
    }

    private async Task ReloadAsync()
    {
        int version = ++loadVersion;
        PageError.Visibility = Visibility.Collapsed;
        try
        {
            var result = await App.Database.CallAsync<SalonData>("mw_load",
                new { p_token = App.Token, p_day = SelectedDay.ToString("yyyy-MM-dd") });
            if (version != loadVersion) return;
            data = result;
            RenderData();
        }
        catch (Exception ex)
        {
            if (version != loadVersion) return;
            data = new SalonData();
            RenderData();
            PageError.Text = ex.Message;
            PageError.Visibility = Visibility.Visible;
        }
    }

    private async Task OpenForm(DialogWindow form)
    {
        if (dialogOpen) return;
        dialogOpen = true;
        DialogShade.Visibility = Visibility.Visible;
        form.KeyDown += (_, e) => { if (e.Key == Key.Escape) form.Close(false); };
        try { if (await form.ShowDialogAsync(this)) await ReloadAsync(); }
        finally { dialogOpen = false; DialogShade.Visibility = Visibility.Collapsed; }
    }

    public DayWindow()
    {
        InitializeComponent();
        UserNameText.Text = App.CurrentUser.FullName;
        UserRoleText.Text = TopRoleText.Text = App.CurrentUser.RoleText;
        PricesNav.Visibility = StaffNav.Visibility = App.CurrentUser.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        DayPicker.SelectedDate = WindowNavigation.SelectedDay;
    }

    private void RenderData()
    {
        AppointmentList.ItemsSource = data.Appointments;
        NoAppointments.Visibility = (data.Appointments.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
        var active = data.Appointments.Where(a => a.Status != "cancelled").ToList();
        CountText.Text = active.Count.ToString();
        RevenueText.Text = active.Where(a => a.Status == "completed").Sum(a => a.Price).ToString("N0", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";
        FreeText.Text = CountFreeWindows().ToString();
        var next = active.FirstOrDefault(a => (a.Status is "confirmed" or "waiting") && a.StartsAt >= DateTimeOffset.UtcNow);
        NextTime.Text = next?.TimeText ?? "—";
        NextClient.Text = next?.ClientName ?? "Ближайших записей нет";
        NextService.Text = next?.ServiceName ?? "";
        NextMaster.Text = next is null ? "" : "Мастер: " + next.ShortMasterName;
    }

    private int CountFreeWindows()
    {
        int count = 0;
        foreach (var m in data.Staff.Where(m => !m.IsDayOff))
        {
            var start = new DateTimeOffset(DateTime.SpecifyKind(SelectedDay + m.WorkStart, DateTimeKind.Unspecified), TimeSpan.FromHours(3));
            var end = new DateTimeOffset(DateTime.SpecifyKind(SelectedDay + m.WorkEnd, DateTimeKind.Unspecified), TimeSpan.FromHours(3));
            while (start.AddHours(1) <= end)
            {
                if (start >= DateTimeOffset.UtcNow && !data.Appointments.Any(a => a.MasterId == m.Id && a.Status != "cancelled" && a.StartsAt < start.AddHours(1) && a.EndsAt > start))
                { count++; start = start.AddHours(1); }
                else start = start.AddMinutes(30);
            }
        }
        return count;
    }

    private async void NewAppointmentClick(object? sender, RoutedEventArgs e) => await OpenForm(new AppointmentWindow(data, SelectedDay));
    private async void EditAppointmentClick(object? sender, RoutedEventArgs e)
    { if ((sender as Button)?.Tag is Appointment a) await OpenForm(new AppointmentWindow(data, SelectedDay, a)); }
    private async void AppointmentDoubleTapped(object? sender, MouseButtonEventArgs e)
    { if (AppointmentList.SelectedItem is Appointment a) await OpenForm(new AppointmentWindow(data, SelectedDay, a)); }
}
