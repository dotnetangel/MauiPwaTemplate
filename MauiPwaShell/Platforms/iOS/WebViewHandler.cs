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
        // Note: Must be set on main thread after WebView is loaded
        NSRunLoop.Main.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var userAgentResult = await webView.EvaluateJavaScriptAsync(
                    @"navigator.userAgent + ' MauiPwaShell/1.0'"
                );
                
                if (userAgentResult != null)
                {
                    var userAgent = userAgentResult.ToString();
                    if (!string.IsNullOrEmpty(userAgent))
                    {
                        webView.CustomUserAgent = userAgent;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[iOS WebView] Failed to set custom user agent: {ex.Message}");
                // Fallback: set a basic custom user agent
                webView.CustomUserAgent = "MauiPwaShell/1.0";
            }
        });

        System.Diagnostics.Debug.WriteLine("[iOS WebView] Configured for WebAuthn support");
    }
}
