using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Moonlit_Waters.Models;

namespace Moonlit_Waters.Windows;

public partial class ClientsWindow : SectionWindow
{
    public ClientsWindow()
    {
        InitializeComponent();
        InitializeSection("clients");
        AddClientButton.Visibility = (App.CurrentUser.IsAdmin) ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void RenderData() => FilterClients();

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
