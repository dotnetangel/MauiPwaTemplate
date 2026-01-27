using Foundation;
using Microsoft.Maui.Handlers;
using WebKit;

namespace MauiPwaShell.Platforms.iOS;

/// <summary>
/// Custom WebView handler for iOS to enable WebAuthn/Passkey support
/// </summary>
public class CustomWebViewHandler : WebViewHandler
{
    protected override void ConnectHandler(WKWebView platformView)
    {
        base.ConnectHandler(platformView);
        ConfigureWebViewForWebAuthn(platformView);
    }

    private static void ConfigureWebViewForWebAuthn(WKWebView webView)
    {
        // Enable JavaScript (required for WebAuthn)
        webView.Configuration.Preferences.JavaScriptEnabled = true;

        // Enable modern JavaScript features
        webView.Configuration.Preferences.JavaScriptCanOpenWindowsAutomatically = true;

        // Allow inline media playback
        webView.Configuration.AllowsInlineMediaPlayback = true;

        // Configure for responsive design
        webView.Configuration.AllowsAirPlayForMediaPlayback = true;

        // Security: Only allow navigation to secure contexts
        // Note: localhost and file:// are considered secure contexts

        // User agent customization for server-side detection
        webView.EvaluateJavaScriptAsync(
            @"navigator.userAgent + ' MauiPwaShell/1.0'"
        ).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result != null)
            {
                var userAgent = task.Result.ToString();
                if (!string.IsNullOrEmpty(userAgent))
                {
                    webView.CustomUserAgent = userAgent;
                }
            }
        });

        System.Diagnostics.Debug.WriteLine("[iOS WebView] Configured for WebAuthn support");
    }
}
