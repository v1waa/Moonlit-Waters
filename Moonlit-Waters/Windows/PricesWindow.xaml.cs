using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Moonlit_Waters.Models;

namespace Moonlit_Waters.Windows;

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
            var button = new Button { Content = name, Tag = name, HorizontalAlignment = HorizontalAlignment.Stretch };
            button.Style = (Style)FindResource(category == name ? "ActiveCategoryButton" : "CategoryButton");
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
        NoServices.Visibility = (list.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void NewServiceClick(object? sender, RoutedEventArgs e) => await OpenForm(new ServiceWindow(data, category));
    private async void EditServiceClick(object? sender, RoutedEventArgs e)
    { if ((sender as Button)?.Tag is Service s) await OpenForm(new ServiceWindow(data, s.Category, s)); }
    private async void ServiceDoubleTapped(object? sender, MouseButtonEventArgs e)
    { if (ServiceList.SelectedItem is Service s) await OpenForm(new ServiceWindow(data, s.Category, s)); }
    private void ServiceSearchChanged(object? sender, TextChangedEventArgs e) { if (ready) FilterServices(); }
}
