using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using Moonlit_Waters.Models;
using Moonlit_Waters.Services;
using Moonlit_Waters.Windows;

namespace Moonlit_Waters;

public partial class App : Application
{
    public static SupabaseService Database { get; } = new();
    public static string Token { get; set; } = "";
    public static Profile CurrentUser { get; set; } = new();
    public static DateTime MoscowNow => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).DateTime;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("ru-RU")));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Database.Dispose();
        base.OnExit(e);
    }
}
