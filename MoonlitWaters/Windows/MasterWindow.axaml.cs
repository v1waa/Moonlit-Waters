using Avalonia.Controls;
using Avalonia.Interactivity;
using MoonlitWaters.Models;

namespace MoonlitWaters.Windows;

public partial class MasterWindow : Window
{
    private readonly Guid? masterId;
    public MasterWindow()
    {
        InitializeComponent();
        StartPicker.SelectedTime = TimeSpan.FromHours(10);
        EndPicker.SelectedTime = TimeSpan.FromHours(19);
    }
    public MasterWindow(Master master) : this()
    {
        masterId = master.Id;
        FormTitle.Text = "Изменить мастера";
        SaveButton.Content = "Сохранить";
        ArchiveButton.IsVisible = true;
        NameBox.Text = master.FullName;
        PhoneBox.Text = master.Phone;
        SpecializationBox.Text = master.Specialization;
        BankBox.Text = master.BankAccount;
        StartPicker.SelectedTime = master.WorkStart;
        EndPicker.SelectedTime = master.WorkEnd;
        DayOffBox.IsChecked = master.IsDayOff;
    }
    private void CancelClick(object? sender, RoutedEventArgs e) => Close(false);
    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        ErrorText.Text = "";
        try
        {
            var access = await App.Database.CallAsync<MasterAccess>("mw_save_master", new { p_token = App.Token, p_id = masterId, p_name = NameBox.Text ?? "", p_phone = PhoneBox.Text ?? "", p_specialization = SpecializationBox.Text ?? "", p_bank_account = BankBox.Text?.Trim() ?? "", p_work_start = StartPicker.SelectedTime?.ToString(@"hh\:mm"), p_work_end = EndPicker.SelectedTime?.ToString(@"hh\:mm"), p_day_off = DayOffBox.IsChecked == true });
            if (!string.IsNullOrEmpty(access.Password))
                await new MessageWindow("Мастер добавлен", $"Данные для входа:\n\nЛогин: {access.Username}\nПароль: {access.Password}\n\nСохраните пароль. После входа мастер может сменить его кнопкой «Пароль».").ShowDialog<bool>(this);
            Close(true);
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { SaveButton.IsEnabled = true; }
    }
    private async void ArchiveClick(object? sender, RoutedEventArgs e)
    {
        if (!await new MessageWindow("Убрать мастера?", "Вход для мастера будет закрыт, история записей сохранится.", true).ShowDialog<bool>(this)) return;
        ArchiveButton.IsEnabled = false;
        try { await App.Database.CallAsync<bool>("mw_archive", new { p_token = App.Token, p_kind = "master", p_id = masterId }); Close(true); }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { ArchiveButton.IsEnabled = true; }
    }
}
