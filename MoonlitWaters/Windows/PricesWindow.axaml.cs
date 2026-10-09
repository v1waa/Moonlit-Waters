using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MoonlitWaters.Models;

namespace MoonlitWaters.Windows;

public partial class PricesWindow : SectionWindow
{
    public static readonly string[] Categories = { "Маникюр", "Волосы", "Лицо", "Массаж", "Брови и ресницы" };
    private string category = "Маникюр";

    public PricesWindow()
    {
        InitializeComponent();
        InitializeSection("prices");
    }

    protected override void RenderData() { BuildCategories(); FilterServices(); }

    private void BuildCategories()
    {
        var names = Categories.Concat(data.Services.Select(s => s.Category)).Distinct().ToList();
        CategoryPanel.Children.Clear();
        foreach (string name in names)
        {
            var button = new Button { Content = name, Tag = name, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
            button.Classes.Add("app");
            button.Classes.Add("category");
            button.Classes.Set("active", category == name);
            button.Click += (_, _) => { category = name; BuildCategories(); FilterServices(); };
            CategoryPanel.Children.Add(button);
        }
    }

    private void FilterServices()
    {
        string query = ServiceSearch.Text?.Trim() ?? "";
        var list = data.Services.Where(s => (query.Length > 0 || s.Category == category) && s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        CategoryTitle.Text = query.Length > 0 ? "Результаты поиска" : category;
        ServiceList.ItemsSource = list;
        NoServices.IsVisible = list.Count == 0;
    }

    private async void NewServiceClick(object? sender, RoutedEventArgs e) => await OpenForm(new ServiceWindow(data, category));
    private async void EditServiceClick(object? sender, RoutedEventArgs e)
    { if ((sender as Button)?.Tag is Service s) await OpenForm(new ServiceWindow(data, s.Category, s)); }
    private async void ServiceDoubleTapped(object? sender, TappedEventArgs e)
    { if (ServiceList.SelectedItem is Service s) await OpenForm(new ServiceWindow(data, s.Category, s)); }
    private void ServiceSearchChanged(object? sender, TextChangedEventArgs e) { if (ready) FilterServices(); }
}
