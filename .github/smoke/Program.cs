using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Moonlit_Waters;
using Moonlit_Waters.Models;
using Moonlit_Waters.Windows;

internal static class Program
{
    // Настоящие WPF-контролы на Windows. Тестовая копия настроек направляет
    // запросы открываемых окон на закрытый локальный порт, не в Supabase.
    [STAThread]
    private static int Main()
    {
        var settings = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var original = File.ReadAllText(settings);
        File.WriteAllText(settings, """{"SupabaseUrl":"http://127.0.0.1:1","PublishableKey":"ui-test"}""");
        try { return RunChecks(); }
        finally { File.WriteAllText(settings, original); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunChecks()
    {
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            var app = new App();
            app.InitializeComponent();
            app.StartupUri = null;
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            App.CurrentUser = new Profile { FullName = "Администратор", Role = "admin" };
            var client = new Client { Id = Guid.NewGuid(), Name = "Тестовый клиент", Phone = "+7 900 000-00-00" };
            var service = new Service { Id = Guid.NewGuid(), Name = "Маникюр", Category = "Маникюр", DurationMinutes = 60, Price = 1500 };
            var master = new Master { Id = Guid.NewGuid(), FullName = "Тестовый мастер", Specialization = "Маникюр", WorkStart = TimeSpan.FromHours(10), WorkEnd = TimeSpan.FromHours(19) };
            var appointment = new Appointment { Id = Guid.NewGuid(), ClientId = client.Id, ClientName = client.Name, ClientPhone = client.Phone, ServiceId = service.Id, ServiceName = service.Name, MasterId = master.Id, MasterName = master.FullName, StartsAt = DateTimeOffset.UtcNow.AddHours(1), EndsAt = DateTimeOffset.UtcNow.AddHours(2), Price = 1500 };
            var data = new SalonData { Clients = new() { client }, Services = new() { service }, Staff = new() { master }, Appointments = new() { appointment } };
            Directory.CreateDirectory("wpf-screenshots");

            foreach (var window in new Window[]
            {
                new MainWindow(), new DayWindow(), new ClientsWindow(), new PricesWindow(), new StaffWindow(),
                new ClientWindow(client), new ServiceWindow(data, "Маникюр", service), new MasterWindow(master),
                new AppointmentWindow(data, App.MoscowNow.Date, appointment),
                new MessageWindow("Проверка", "Сообщение и кнопки работают")
            })
            {
                if (IsSection(window))
                {
                    var type = window.GetType();
                    type.GetField("data", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, data);
                    type.GetField("ready", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
                    type.GetMethod("RenderData", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
                    if (window.FindName("RefreshButton") is not null || window.FindName("PasswordButton") is not null)
                        throw new Exception("Removed actions remain in a section");
                }
                Render(window);
                if (window is ClientsWindow clients)
                {
                    ((TextBox)clients.FindName("ClientSearch")).Text = "Несуществующий клиент";
                    if (((ListBox)clients.FindName("ClientList")).Items.Count != 0)
                        throw new Exception("Client search did not filter the list");
                }
                if (window is PricesWindow prices && ((ListBox)prices.FindName("ServiceList")).Items.Count != 1)
                    throw new Exception("Service category did not load");
                window.Close();
                Console.WriteLine("PASS " + window.GetType().Name);
            }

            CheckCalendar();
            CheckNavigation(app);

            App.CurrentUser = new Profile { Id = master.Id, FullName = master.FullName, Role = "master" };
            var masterDay = new DayWindow();
            if (((Button)masterDay.FindName("PricesNav")).Visibility != Visibility.Collapsed ||
                ((Button)masterDay.FindName("StaffNav")).Visibility != Visibility.Collapsed)
                throw new Exception("Admin navigation is visible to a master");
            masterDay.Close();
            WindowNavigation.Switch(masterDay, "staff", App.MoscowNow.Date);
            if (app.Windows.OfType<StaffWindow>().Any())
                throw new Exception("Master can open an admin window");
            Console.WriteLine("PASS master permissions; 10 WPF windows rendered without database writes.");
            app.Shutdown();
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static bool IsSection(Window window) => window is DayWindow or ClientsWindow or PricesWindow or StaffWindow;
    private static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

    private static void CheckCalendar()
    {
        var window = new DayWindow();
        window.Show();
        Drain();
        var picker = (DatePicker)window.FindName("DayPicker");
        picker.ApplyTemplate();
        var text = (DatePickerTextBox)picker.Template.FindName("PART_TextBox", picker);
        if (text.ActualWidth < 90 || text.ActualHeight < 14)
            throw new Exception("Date text is clipped");
        var button = (Button)picker.Template.FindName("PART_Button", picker);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Drain();
        var popup = (Popup)picker.Template.FindName("PART_Popup", picker);
        if (!picker.IsDropDownOpen || !popup.IsOpen || popup.Child is not System.Windows.Controls.Calendar calendar)
            throw new Exception("Calendar button does not open the calendar");
        if (calendar.FirstDayOfWeek != DayOfWeek.Monday || calendar.Language.IetfLanguageTag != "ru-RU")
            throw new Exception("Calendar culture is incorrect");
        calendar.SelectedDate = new DateTime(2026, 10, 15);
        Drain();
        if (picker.SelectedDate != new DateTime(2026, 10, 15) || text.Text != "15.10.2026")
            throw new Exception("Calendar selection does not update date text");
        RenderContent(calendar, Brushes.White, calendar.ActualWidth, calendar.ActualHeight, "CalendarPopup");
        picker.IsDropDownOpen = false;
        picker.Text = "16.10.2026";
        if (picker.SelectedDate != new DateTime(2026, 10, 16))
            throw new Exception("Typed Russian date was not parsed");
        picker.Text = "31.02.2026";
        if (picker.SelectedDate != new DateTime(2026, 10, 16) || picker.Text != "16.10.2026")
            throw new Exception("Invalid date did not restore the previous value");
        window.Close();
        Console.WriteLine("PASS calendar popup, selection, Russian input and invalid date handling");
    }

    private static void CheckNavigation(App app)
    {
        WindowNavigation.SelectedDay = new DateTime(2026, 10, 20);
        Window current = new DayWindow { WindowStartupLocation = WindowStartupLocation.Manual, Left = 40, Top = 45, Width = 1280, Height = 800 };
        app.MainWindow = current;
        current.Show();
        Drain();
        foreach (var route in new[] { ("ClientsNav", typeof(ClientsWindow)), ("PricesNav", typeof(PricesWindow)), ("StaffNav", typeof(StaffWindow)), ("DayNav", typeof(DayWindow)) })
        {
            var bounds = current.RestoreBounds;
            var previous = current;
            ((Button)current.FindName(route.Item1)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Drain();
            current = app.MainWindow;
            if (current.GetType() != route.Item2 || previous.IsVisible || app.Windows.Cast<Window>().Count(IsSection) != 1)
                throw new Exception("Navigation did not replace the previous section: " + route.Item1);
            if (((DatePicker)current.FindName("DayPicker")).SelectedDate != WindowNavigation.SelectedDay ||
                Math.Abs(current.RestoreBounds.Left - bounds.Left) > 2 || Math.Abs(current.RestoreBounds.Top - bounds.Top) > 2 ||
                Math.Abs(current.RestoreBounds.Width - bounds.Width) > 2 || Math.Abs(current.RestoreBounds.Height - bounds.Height) > 2)
                throw new Exception("Navigation lost the date or window bounds");
            ((Button)current.FindName(route.Item1)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (app.MainWindow != current || app.Windows.Cast<Window>().Count(IsSection) != 1)
                throw new Exception("Active section creates a duplicate window");
        }
        current.WindowState = WindowState.Maximized;
        Drain();
        ((Button)current.FindName("ClientsNav")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Drain();
        if (app.MainWindow.WindowState != WindowState.Maximized)
            throw new Exception("Navigation lost the maximized state");
        app.MainWindow.Close();
        Console.WriteLine("PASS section switching, date, position, size and maximized state");
    }

    private static void Render(Window window)
    {
        var content = (FrameworkElement)window.Content;
        double width = window.Width;
        double height = double.IsNaN(window.Height) ? double.PositiveInfinity : window.Height;
        content.Measure(new Size(width, height));
        if (double.IsInfinity(height)) height = content.DesiredSize.Height;
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
        if (height <= 0) throw new Exception("Window has no content");
        RenderContent(content, window.Background, width, height, window.GetType().Name);
    }

    private static void RenderContent(FrameworkElement content, Brush background, double width, double height, string name)
    {
        var bitmap = new RenderTargetBitmap((int)width, (int)Math.Ceiling(height), 96, 96, PixelFormats.Pbgra32);
        var drawing = new DrawingVisual();
        using (var context = drawing.RenderOpen())
        {
            context.DrawRectangle(background, null, new Rect(0, 0, width, height));
            context.DrawRectangle(new VisualBrush(content), null, new Rect(0, 0, width, height));
        }
        bitmap.Render(drawing);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine("wpf-screenshots", name + ".png"));
        png.Save(file);
    }
}
