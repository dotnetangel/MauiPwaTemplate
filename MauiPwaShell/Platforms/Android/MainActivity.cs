using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.Firebase.CloudMessaging;

namespace MauiPwaShell;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        CrossFirebaseCloudMessaging.Current.OnNotificationReceived += (s, e) =>
        {
            System.Console.WriteLine($"[Android] Notification received: {e.Notification.Title}");
        };
    }
}
