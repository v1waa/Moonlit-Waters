using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MoonlitWaters.Models;
using System.Globalization;

namespace MoonlitWaters.Windows;

public partial class DayWindow : SectionWindow
{
    public DayWindow()
    {
        InitializeComponent();
        InitializeSection("day");
    }

    protected override void RenderData()
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
