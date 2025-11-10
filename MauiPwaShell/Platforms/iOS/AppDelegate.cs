using Foundation;
using Plugin.Firebase.CloudMessaging;

namespace MauiPwaShell;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIKit.UIApplication application, NSDictionary launchOptions)
    {
        CrossFirebaseCloudMessaging.Current.OnNotificationReceived += (s, e) =>
        {
            System.Console.WriteLine($"[iOS] Notification received: {e.Notification.Title}");
        };
        return base.FinishedLaunching(application, launchOptions);
    }
}
