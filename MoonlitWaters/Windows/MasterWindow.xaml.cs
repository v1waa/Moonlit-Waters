using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MoonlitWaters.Models;

namespace MoonlitWaters.Windows;

public partial class MasterWindow : DialogWindow
{
    private readonly Guid? masterId;
    public MasterWindow()
    {
        InitializeComponent();
        StartPicker.Text = "10:00";
        EndPicker.Text = "19:00";
    }
    public MasterWindow(Master master) : this()
    {
        masterId = master.Id;
        FormTitle.Text = "Изменить мастера";
        SaveButton.Content = "Сохранить";
        ArchiveButton.Visibility = Visibility.Visible;
        NameBox.Text = master.FullName;
        PhoneBox.Text = master.Phone;
        SpecializationBox.Text = master.Specialization;
        BankBox.Text = master.BankAccount;
        StartPicker.Text = master.WorkStart.ToString(@"hh\:mm");
        EndPicker.Text = master.WorkEnd.ToString(@"hh\:mm");
        DayOffBox.IsChecked = master.IsDayOff;
    }
    private void CancelClick(object? sender, RoutedEventArgs e) => Close(false);
    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        if (!TimeSpan.TryParseExact(StartPicker.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var start) ||
            !TimeSpan.TryParseExact(EndPicker.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var end))
        { ErrorText.Text = "Введите время в формате ЧЧ:ММ, например 10:30."; return; }
        SaveButton.IsEnabled = false;
        ErrorText.Text = "";
        try
        {
            var access = await App.Database.CallAsync<MasterAccess>("mw_save_master", new { p_token = App.Token, p_id = masterId, p_name = NameBox.Text ?? "", p_phone = PhoneBox.Text ?? "", p_specialization = SpecializationBox.Text ?? "", p_bank_account = BankBox.Text?.Trim() ?? "", p_work_start = start.ToString(@"hh\:mm"), p_work_end = end.ToString(@"hh\:mm"), p_day_off = DayOffBox.IsChecked == true });
            if (!string.IsNullOrEmpty(access.Password))
                await new MessageWindow("Мастер добавлен", $"Данные для входа:\n\nЛогин: {access.Username}\nПароль: {access.Password}\n\nСохраните пароль. После входа мастер может сменить его кнопкой «Пароль».").ShowDialogAsync(this);
            Close(true);
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { SaveButton.IsEnabled = true; }
    }
    private async void ArchiveClick(object? sender, RoutedEventArgs e)
    {
        if (!await new MessageWindow("Убрать мастера?", "Вход для мастера будет закрыт, история записей сохранится.", true).ShowDialogAsync(this)) return;
        ArchiveButton.IsEnabled = false;
        try { await App.Database.CallAsync<bool>("mw_archive", new { p_token = App.Token, p_kind = "master", p_id = masterId }); Close(true); }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { ArchiveButton.IsEnabled = true; }
    }
}
