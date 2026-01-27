using Foundation;
using UIKit;

namespace NativeFidoMaui.Platforms.iOS;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    /// <summary>
    /// Handle deep link URL when app is launched or brought to foreground
    /// </summary>
    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
    {
        HandleDeepLink(url);
        return true;
    }

    /// <summary>
    /// Handle universal links (HTTPS URLs associated with the app)
    /// Note: Requires Associated Domains capability and apple-app-site-association file on server
    /// </summary>
    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
    {
        if (userActivity.ActivityType == NSUserActivityType.BrowsingWeb && userActivity.WebPageUrl != null)
        {
            HandleDeepLink(userActivity.WebPageUrl);
            return true;
        }
        
        return base.ContinueUserActivity(application, userActivity, completionHandler);
    }

    private void HandleDeepLink(NSUrl url)
    {
        if (url == null) return;

        var deepLinkUrl = url.ToString();
        
        if (!string.IsNullOrEmpty(deepLinkUrl))
        {
            Console.WriteLine($"[iOS] Received deep link: {deepLinkUrl}");
            
            // Pass to MainPage to handle the auth callback
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Give the app a moment to fully initialize if it was just launched
                    await Task.Delay(100);
                    
                    if (Application.Current?.MainPage is AppShell shell &&
                        shell.CurrentPage is MainPage mainPage)
                    {
                        await mainPage.HandleDeepLinkAsync(deepLinkUrl);
                    }
                    else
                    {
                        Console.WriteLine("[iOS] MainPage not ready for deep link");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[iOS] Error handling deep link: {ex.Message}");
                }
            });
        }
    }
}
