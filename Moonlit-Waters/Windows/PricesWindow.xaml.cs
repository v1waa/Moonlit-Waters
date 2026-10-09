using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Moonlit_Waters.Models;

namespace Moonlit_Waters.Windows;

public partial class PricesWindow : Window
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

    public static readonly string[] Categories = { "Маникюр", "Волосы", "Лицо", "Массаж", "Брови и ресницы" };
    private string category = "Маникюр";

    public PricesWindow()
    {
        InitializeComponent();
        UserNameText.Text = App.CurrentUser.FullName;
        UserRoleText.Text = TopRoleText.Text = App.CurrentUser.RoleText;
        PricesNav.Visibility = StaffNav.Visibility = App.CurrentUser.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        DayPicker.SelectedDate = WindowNavigation.SelectedDay;
    }

    private void RenderData() { BuildCategories(); FilterServices(); }

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
