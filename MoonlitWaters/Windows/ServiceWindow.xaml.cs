using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MoonlitWaters.Models;
using System.Globalization;

namespace MoonlitWaters.Windows;

public partial class ServiceWindow : DialogWindow
{
    private readonly Guid? serviceId;
    public ServiceWindow() { InitializeComponent(); }
    public ServiceWindow(SalonData data, string category, Service? service = null) : this()
    {
        var masters = new List<Master> { new() { FullName = "Любой мастер" } };
        masters.AddRange(data.Staff);
        MasterBox.ItemsSource = masters;
        MasterBox.SelectedItem = masters.FirstOrDefault(m => m.Id == service?.MasterId) ?? masters[0];
        CategoryBox.ItemsSource = PricesWindow.Categories.Concat(data.Services.Select(s => s.Category)).Distinct().ToList();
        CategoryBox.SelectedItem = category;
        DurationBox.Text = "60";
        PriceBox.Text = "0";
        if (service is null) return;
        serviceId = service.Id;
        FormTitle.Text = "Изменить услугу";
        SaveButton.Content = "Сохранить";
        ArchiveButton.Visibility = Visibility.Visible;
        NameBox.Text = service.Name;
        DurationBox.Text = service.DurationMinutes.ToString();
        PriceBox.Text = service.Price.ToString(CultureInfo.InvariantCulture);
    }
    private void CancelClick(object? sender, RoutedEventArgs e) => Close(false);
    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        if (!int.TryParse(DurationBox.Text, out int minutes) || !decimal.TryParse(PriceBox.Text?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price))
        { ErrorText.Text = "Время и стоимость должны быть числами."; return; }
        Guid? masterId = (MasterBox.SelectedItem as Master)?.Id;
        if (masterId == Guid.Empty) masterId = null;
        SaveButton.IsEnabled = false;
        try
        {
            await App.Database.CallAsync<Guid>("mw_save_service", new { p_token = App.Token, p_id = serviceId, p_name = NameBox.Text ?? "", p_category = CategoryBox.SelectedItem?.ToString() ?? "", p_duration = minutes, p_price = price, p_master_id = masterId });
            Close(true);
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { SaveButton.IsEnabled = true; }
    }
    private async void ArchiveClick(object? sender, RoutedEventArgs e)
    {
        if (!await new MessageWindow("Убрать услугу?", "Услуга исчезнет из прайс-листа. Уже созданные записи сохранятся.", true).ShowDialogAsync(this)) return;
        ArchiveButton.IsEnabled = false;
        try { await App.Database.CallAsync<bool>("mw_archive", new { p_token = App.Token, p_kind = "service", p_id = serviceId }); Close(true); }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { ArchiveButton.IsEnabled = true; }
    }
}
