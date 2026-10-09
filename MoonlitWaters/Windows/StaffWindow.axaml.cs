using Avalonia.Controls;
using Avalonia.Interactivity;
using MoonlitWaters.Models;

namespace MoonlitWaters.Windows;

public partial class StaffWindow : SectionWindow
{
    public StaffWindow()
    {
        InitializeComponent();
        InitializeSection("staff");
    }

    protected override void RenderData()
    {
        foreach (var master in data.Staff)
            master.AppointmentCount = data.Appointments.Count(a => a.MasterId == master.Id && a.Status != "cancelled");
        StaffList.ItemsSource = data.Staff;
    }

    private async void AddMasterClick(object? sender, RoutedEventArgs e) => await OpenForm(new MasterWindow());
    private async void EditMasterClick(object? sender, RoutedEventArgs e)
    { if ((sender as Button)?.Tag is Master m) await OpenForm(new MasterWindow(m)); }
}
