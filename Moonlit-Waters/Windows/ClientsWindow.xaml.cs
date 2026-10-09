using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Moonlit_Waters.Models;

namespace Moonlit_Waters.Windows;

public partial class ClientsWindow : Window
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

    public ClientsWindow()
    {
        InitializeComponent();
        UserNameText.Text = App.CurrentUser.FullName;
        UserRoleText.Text = TopRoleText.Text = App.CurrentUser.RoleText;
        PricesNav.Visibility = StaffNav.Visibility = App.CurrentUser.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        DayPicker.SelectedDate = WindowNavigation.SelectedDay;
        AddClientButton.Visibility = (App.CurrentUser.IsAdmin) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RenderData() => FilterClients();

    private void FilterClients()
    {
        Guid? selected = (ClientList.SelectedItem as Client)?.Id;
        string query = ClientSearch.Text?.Trim() ?? "";
        string digits = new(query.Where(char.IsDigit).ToArray());
        var list = data.Clients.Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || c.Phone.Contains(query, StringComparison.OrdinalIgnoreCase) || (digits.Length > 0 && new string(c.Phone.Where(char.IsDigit).ToArray()).Contains(digits))).ToList();
        ClientList.ItemsSource = list;
        NoClients.Visibility = (list.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
        ClientList.SelectedItem = list.FirstOrDefault(c => c.Id == selected) ?? list.FirstOrDefault();
        ShowClient();
    }

    private void ShowClient()
    {
        var c = ClientList.SelectedItem as Client;
        ClientNameText.Text = c?.Name ?? "Выберите клиента";
        ClientPhoneText.Text = c?.Phone ?? "";
        ClientEmailText.Text = c?.Email ?? "";
        ClientLastService.Text = c?.LastService ?? "Пока нет завершённых визитов";
        ClientLastVisit.Text = c?.LastVisit is null ? "" : c.LastVisitText + " · " + c.LastMaster;
        ClientBookingButton.IsEnabled = c is not null;
    }

    private async void ClientBookingClick(object? sender, RoutedEventArgs e)
    { if (ClientList.SelectedItem is Client c) await OpenForm(new AppointmentWindow(data, SelectedDay, client: c)); }
    private async void AddClientClick(object? sender, RoutedEventArgs e) => await OpenForm(new ClientWindow());
    private async void ClientDoubleTapped(object? sender, MouseButtonEventArgs e)
    { if (App.CurrentUser.IsAdmin && ClientList.SelectedItem is Client c) await OpenForm(new ClientWindow(c)); }
    private void ClientSelected(object? sender, SelectionChangedEventArgs e) { if (ready) ShowClient(); }
    private void ClientSearchChanged(object? sender, TextChangedEventArgs e) { if (ready) FilterClients(); }
}
