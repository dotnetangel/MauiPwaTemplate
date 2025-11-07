using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.Core;

namespace MauiPwaShell;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        var app = builder.Build();
        InitializeFirebase();
        return app;
    }

    private static async void InitializeFirebase()
    {
        try
        {
            await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
            await CrossFirebaseCloudMessaging.Current.RequestPermissionAsync();

            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            Console.WriteLine($"[MAUI] Initial token: {token}");

            CrossFirebaseCloudMessaging.Current.OnTokenChanged += async (s, newToken) =>
            {
                Console.WriteLine($"[MAUI] Token changed: {newToken}");
                await SendTokenToServer(newToken);
                if (Application.Current?.MainPage is MainPage mp)
                    await mp.InjectTokenIntoWebAsync(newToken);
            };

            if (!string.IsNullOrEmpty(token))
            {
                await SendTokenToServer(token);
                if (Application.Current?.MainPage is MainPage mp)
                    await mp.InjectTokenIntoWebAsync(token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[MAUI] Firebase init error: " + ex);
        }
    }

    private static async Task SendTokenToServer(string token)
    {
        try
        {
            using var client = new HttpClient();
            var payload = new { Token = token, Platform = DeviceInfo.Platform.ToString() };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var serverUrl = "http://10.0.2.2:5000/api/registerpush";
            var resp = await client.PostAsync(serverUrl, content);
            Console.WriteLine($"[MAUI] Sent token to server: {resp.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[MAUI] Failed to send token to server: " + ex);
        }
    }
}
