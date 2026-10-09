using System.Net.Http;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Moonlit_Waters.Services;

// All database calls are in one place. The key in appsettings is a public key.
public class SupabaseService : IDisposable
{
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(25) };
    private readonly JsonSerializerOptions json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public SupabaseService()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        using var settings = JsonDocument.Parse(File.ReadAllText(path));
        string url = settings.RootElement.GetProperty("SupabaseUrl").GetString()!;
        string key = settings.RootElement.GetProperty("PublishableKey").GetString()!;
        http.BaseAddress = new Uri(url.TrimEnd('/') + "/rest/v1/rpc/");
        http.DefaultRequestHeaders.Add("apikey", key);
    }

    public async Task<T> CallAsync<T>(string function, object parameters)
    {
        try
        {
            using var body = new StringContent(JsonSerializer.Serialize(parameters, json), Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(function, body);
            string text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                string message = "Не удалось выполнить запрос к базе данных.";
                try
                {
                    using var error = JsonDocument.Parse(text);
                    if (error.RootElement.TryGetProperty("message", out var value)) message = value.GetString() ?? message;
                }
                catch (JsonException) { }
                throw new InvalidOperationException(message);
            }
            return JsonSerializer.Deserialize<T>(text, json) ?? throw new InvalidOperationException("Пустой ответ базы данных.");
        }
        catch (HttpRequestException) { throw new InvalidOperationException("Нет соединения с Supabase. Проверьте интернет и повторите попытку."); }
        catch (TaskCanceledException) { throw new InvalidOperationException("База данных не ответила вовремя. Повторите попытку."); }
    }

    public void Dispose() => http.Dispose();
}
