using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MoonlitWaters;
using MoonlitWaters.Models;
using MoonlitWaters.Windows;

internal static class Program
{
    // Проверка настоящих WPF-контролов на Windows. Данные только в памяти,
    // окна не показываются пользователю и запросы в Supabase не выполняются.
    [STAThread]
    private static int Main()
    {
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
            App.CurrentUser = new Profile { FullName = "Администратор", Role = "admin" };
            var client = new Client { Id = Guid.NewGuid(), Name = "Тестовый клиент", Phone = "+7 900 000-00-00" };
            var service = new Service { Id = Guid.NewGuid(), Name = "Маникюр", Category = "Маникюр", DurationMinutes = 60, Price = 1500 };
            var master = new Master { Id = Guid.NewGuid(), FullName = "Тестовый мастер", Specialization = "Маникюр", WorkStart = TimeSpan.FromHours(10), WorkEnd = TimeSpan.FromHours(19) };
            var appointment = new Appointment { Id = Guid.NewGuid(), ClientId = client.Id, ClientName = client.Name, ClientPhone = client.Phone, ServiceId = service.Id, ServiceName = service.Name, MasterId = master.Id, MasterName = master.FullName, StartsAt = DateTimeOffset.UtcNow.AddHours(1), EndsAt = DateTimeOffset.UtcNow.AddHours(2), Price = 1500 };
            var data = new SalonData { Clients = new() { client }, Services = new() { service }, Staff = new() { master }, Appointments = new() { appointment } };
            Directory.CreateDirectory("wpf-screenshots");

            foreach (var window in new Window[]
            {
                new LoginWindow(), new DayWindow(), new ClientsWindow(), new PricesWindow(), new StaffWindow(),
                new ClientWindow(client), new ServiceWindow(data, "Маникюр", service), new MasterWindow(master),
                new AppointmentWindow(data, App.MoscowNow.Date, appointment), new PasswordWindow(),
                new MessageWindow("Проверка", "Сообщение и кнопки работают")
            })
            {
                if (window is SectionWindow section)
                {
                    typeof(SectionWindow).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(section, data);
                    typeof(SectionWindow).GetField("ready", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(section, true);
                    section.GetType().GetMethod("RenderData", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(section, null);
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

            App.CurrentUser = new Profile { Id = master.Id, FullName = master.FullName, Role = "master" };
            var masterDay = new DayWindow();
            if (((Button)masterDay.FindName("PricesNav")).Visibility != Visibility.Collapsed ||
                ((Button)masterDay.FindName("StaffNav")).Visibility != Visibility.Collapsed)
                throw new Exception("Admin navigation is visible to a master");
            masterDay.Close();
            Console.WriteLine("PASS master permissions; 11 WPF windows rendered without database writes.");
            app.Shutdown();
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
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
        var bitmap = new RenderTargetBitmap((int)width, (int)Math.Ceiling(height), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine("wpf-screenshots", window.GetType().Name + ".png"));
        png.Save(file);
    }
}
