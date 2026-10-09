using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MoonlitWaters.Models;

namespace MoonlitWaters.Windows;

public partial class ClientWindow : DialogWindow
{
    private readonly Guid? clientId;
    public ClientWindow() { InitializeComponent(); }
    public ClientWindow(Client? client) : this()
    {
        if (client is null) return;
        clientId = client.Id;
        FormTitle.Text = "Изменить клиента";
        SaveButton.Content = "Сохранить";
        NameBox.Text = client.Name;
        PhoneBox.Text = client.Phone;
        EmailBox.Text = client.Email;
    }
    private void CancelClick(object? sender, RoutedEventArgs e) => Close(false);
    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        ErrorText.Text = "";
        try
        {
            await App.Database.CallAsync<Guid>("mw_save_client", new { p_token = App.Token, p_id = clientId, p_name = NameBox.Text ?? "", p_phone = PhoneBox.Text ?? "", p_email = EmailBox.Text ?? "" });
            Close(true);
        }
        catch (Exception ex) { ErrorText.Text = ex.Message; }
        finally { SaveButton.IsEnabled = true; }
    }
}
