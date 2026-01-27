using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace NativeFidoMaui.Platforms.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
// Deep link intent filter for custom scheme
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "mauipwa",
    DataHost = "auth",
    DataPathPrefix = "/return")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Handle deep link if app was launched via deep link
        HandleIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        
        // Handle deep link when app is already running
        if (intent != null)
        {
            Intent = intent;
            HandleIntent(intent);
        }
    }

    private void HandleIntent(Intent? intent)
    {
        if (intent?.Data != null)
        {
            var deepLinkUrl = intent.Data.ToString();
            
            if (!string.IsNullOrEmpty(deepLinkUrl))
            {
                Console.WriteLine($"[Android] Received deep link: {deepLinkUrl}");
                
                // Pass to MainPage to handle the auth callback
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        // Get the MainPage instance
                        if (Application.Current?.MainPage is AppShell shell &&
                            shell.CurrentPage is MainPage mainPage)
                        {
                            await mainPage.HandleDeepLinkAsync(deepLinkUrl);
                        }
                        else
                        {
                            Console.WriteLine("[Android] MainPage not ready for deep link");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Android] Error handling deep link: {ex.Message}");
                    }
                });
            }
        }
        // Check for native bridge trigger (internal navigation from WebView)
        else if (intent?.DataString?.StartsWith("native://auth/start-passkey-login") == true)
        {
            Console.WriteLine("[Android] Native bridge triggered");
            
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (Application.Current?.MainPage is AppShell shell &&
                        shell.CurrentPage is MainPage mainPage)
                    {
                        await mainPage.StartPasskeyLoginAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Android] Error starting passkey login: {ex.Message}");
                }
            });
        }
    }
}
