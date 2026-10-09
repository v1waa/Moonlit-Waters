using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MoonlitWaters.Models;
using MoonlitWaters.Services;
using MoonlitWaters.Windows;

namespace MoonlitWaters;

public partial class App : Application
{
    public static SupabaseService Database { get; } = new();
    public static string Token { get; set; } = "";
    public static Profile CurrentUser { get; set; } = new();
    public static DateTime MoscowNow => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).DateTime;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new LoginWindow();
            desktop.Exit += (_, _) => Database.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
