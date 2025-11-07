using Plugin.Firebase.CloudMessaging;
namespace MauiPwaShell;

public partial class MainPage : ContentPage
{
    private const string PwaUrl = "http://10.0.2.2:5000/";
    public MainPage() { InitializeComponent(); PwaView.Navigated += PwaView_Navigated; PwaView.Source = PwaUrl; }
    private async void PwaView_Navigated(object? sender, WebNavigatedEventArgs e) { try { var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync(); if (!string.IsNullOrEmpty(token)) await InjectTokenIntoWebAsync(token); } catch (Exception ex) { Console.WriteLine("[MAUI] Error getting token on navigated: " + ex); } }
    public async Task InjectTokenIntoWebAsync(string token) { try { var escaped = token.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n"); var js = $@"(function(){{ try{{ window.nativeFcmToken = '{escaped}'; localStorage.setItem('nativeFcmToken','{escaped}'); window.dispatchEvent(new CustomEvent('nativeTokenReady', {{ detail: '{escaped}' }})); }}catch(e){{console.error('inject token failed', e);}} }})();"; await MainThread.InvokeOnMainThreadAsync(async () => { await PwaView.EvaluateJavaScriptAsync(js); }); Console.WriteLine("[MAUI] Injected token into webview"); } catch (Exception ex) { Console.WriteLine("[MAUI] InjectTokenIntoWebAsync error: " + ex); } }
}
