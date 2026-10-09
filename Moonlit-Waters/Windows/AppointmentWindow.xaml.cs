using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Moonlit_Waters.Models;

namespace Moonlit_Waters.Windows;

public partial class AppointmentWindow : DialogWindow
{
    private SalonData? data;
    private readonly Guid? appointmentId;
    private static readonly string[] StatusCodes = { "waiting", "confirmed", "in_progress", "completed", "cancelled" };
    public AppointmentWindow() { InitializeComponent(); }
    public AppointmentWindow(SalonData data, DateTime day, Appointment? appointment = null, Client? client = null) : this()
    {
        this.data = data;
        var services = data.Services.ToList();
        if (appointment is not null && services.All(s => s.Id != appointment.ServiceId))
            services.Add(new Service { Id = appointment.ServiceId, Name = appointment.ServiceName, Price = appointment.Price, DurationMinutes = (int)(appointment.EndsAt - appointment.StartsAt).TotalMinutes });
        ServiceBox.ItemsSource = services;
        StatusBox.ItemsSource = new[] { "Ожидает", "Подтверждена", "В работе", "Завершена", "Отменена" };
        StatusBox.SelectedIndex = 1;
        StatusPanel.Visibility = (appointment is not null) ? Visibility.Visible : Visibility.Collapsed;
        MasterPanel.Visibility = (App.CurrentUser.IsAdmin) ? Visibility.Visible : Visibility.Collapsed;
        DateBox.SelectedDate = day;
        TimeBox.Text = "10:00";
        if (client is not null)
        {
            NameBox.Text = client.Name;
            PhoneBox.Text = client.Phone;
            ServiceBox.SelectedItem = services.FirstOrDefault(s => s.Name == client.LastService);
        }
        if (appointment is not null)
        {
            appointmentId = appointment.Id;
            FormTitle.Text = "Изменить запись";
            SaveButton.Content = "Сохранить";
            NameBox.Text = appointment.ClientName;
            PhoneBox.Text = appointment.ClientPhone;
            DateBox.SelectedDate = appointment.StartsAt.ToOffset(TimeSpan.FromHours(3)).Date;
            TimeBox.Text = appointment.StartsAt.ToOffset(TimeSpan.FromHours(3)).ToString("HH:mm");
            ServiceBox.SelectedItem = services.FirstOrDefault(s => s.Id == appointment.ServiceId);
            StatusBox.SelectedIndex = Array.IndexOf(StatusCodes, appointment.Status);
        }
        if (ServiceBox.SelectedItem is null) ServiceBox.SelectedIndex = 0;
        UpdateMasters();
        if (appointment is not null) MasterBox.SelectedItem = (MasterBox.ItemsSource as IEnumerable<Master>)?.FirstOrDefault(m => m.Id == appointment.MasterId);
    }
    private void DateValidationError(object sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        DateBox.Text = (DateBox.SelectedDate ?? App.MoscowNow.Date).ToString("dd.MM.yyyy");
    }

    private void UpdateMasters()
    {
        if (data is null) return;
        var service = ServiceBox.SelectedItem as Service;
        var old = MasterBox.SelectedItem as Master;
        var list = data.Staff.Where(m => (App.CurrentUser.IsAdmin || m.Id == App.CurrentUser.Id) && (service?.MasterId is null || service.MasterId == m.Id)).ToList();
        MasterBox.ItemsSource = list;
        MasterBox.SelectedItem = list.FirstOrDefault(m => m.Id == old?.Id) ?? list.FirstOrDefault();
    }
    private void ServiceChanged(object? sender, SelectionChangedEventArgs e) => UpdateMasters();
    private void CancelClick(object? sender, RoutedEventArgs e) => Close(false);
    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        if (ServiceBox.SelectedItem is not Service service || MasterBox.SelectedItem is not Master master || DateBox.SelectedDate is not DateTime date || !TimeSpan.TryParseExact(TimeBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan time) || StatusBox.SelectedIndex < 0)
        { ErrorText.Text = "Выберите услугу, мастера, дату и время."; return; }
        var startsAt = new DateTimeOffset(DateTime.SpecifyKind(date.Date + time, DateTimeKind.Unspecified), TimeSpan.FromHours(3));
        SaveButton.IsEnabled = false;
        try
        {
            await App.Database.CallAsync<Guid>("mw_save_appointment", new { p_token = App.Token, p_id = appointmentId, p_client_name = NameBox.Text ?? "", p_phone = PhoneBox.Text ?? "", p_service_id = service.Id, p_master_id = master.Id, p_starts_at = startsAt, p_status = StatusCodes[StatusBox.SelectedIndex] });
            Close(true);
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { SaveButton.IsEnabled = true; }
    }
}
