using Android.Webkit;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace NativeFidoMaui.Platforms.Android;

/// <summary>
/// Custom WebView handler for Android that enables WebAuthn/passkeys support
/// and intercepts native bridge calls
/// </summary>
public class CustomWebViewHandler : WebViewHandler
{
    protected override void ConnectHandler(global::Android.Webkit.WebView platformView)
    {
        base.ConnectHandler(platformView);

        // Configure WebView settings for WebAuthn support
        var settings = platformView.Settings;
        if (settings != null)
        {
            settings.JavaScriptEnabled = true;
            settings.DomStorageEnabled = true;
            settings.DatabaseEnabled = true;
            settings.SetGeolocationEnabled(true);
            
            // Enable modern web features
            settings.MixedContentMode = MixedContentHandling.AlwaysAllow; // Only for dev - use CompatibilityMode for production
        }

        // Set a custom WebViewClient to intercept navigation
        platformView.SetWebViewClient(new CustomWebViewClient(this));
    }

    private class CustomWebViewClient : MauiWebViewClient
    {
        private readonly CustomWebViewHandler _handler;

        public CustomWebViewClient(CustomWebViewHandler handler) : base(handler)
        {
            _handler = handler;
        }

        public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView? view, IWebResourceRequest? request)
        {
            var url = request?.Url?.ToString();
            
            if (!string.IsNullOrEmpty(url))
            {
                // Intercept native bridge calls
                if (url.StartsWith("native://auth/start-passkey-login"))
                {
                    // Trigger passkey login in system browser
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            if (Microsoft.Maui.Controls.Application.Current?.MainPage is AppShell shell &&
                                shell.CurrentPage is MainPage mainPage)
                            {
                                await mainPage.StartPasskeyLoginAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Android WebView] Error starting passkey login: {ex.Message}");
                        }
                    });
                    
                    return true; // Don't navigate WebView
                }
            }

            return base.ShouldOverrideUrlLoading(view, request);
        }
    }
}
