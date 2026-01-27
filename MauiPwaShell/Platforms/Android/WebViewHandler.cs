using Android.Webkit;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace MauiPwaShell.Platforms.Android;

/// <summary>
/// Custom WebView handler for Android to enable WebAuthn/FIDO2 support
/// </summary>
public class CustomWebViewHandler : WebViewHandler
{
    protected override void ConnectHandler(global::Android.Webkit.WebView platformView)
    {
        base.ConnectHandler(platformView);
        ConfigureWebViewForWebAuthn(platformView);
    }

    private static void ConfigureWebViewForWebAuthn(global::Android.Webkit.WebView webView)
    {
        var settings = webView.Settings;

        // Enable JavaScript (required for WebAuthn)
        settings.JavaScriptEnabled = true;

        // Enable DOM storage (required for credential storage)
        settings.DomStorageEnabled = true;

        // Enable database storage
        settings.DatabaseEnabled = true;

        // Allow mixed content for development (HTTPS PWA with HTTP resources)
        // In production, ensure all resources are HTTPS
        settings.MixedContentMode = MixedContentHandling.CompatibilityMode;

        // Enable zoom controls (improves UX)
        settings.SetSupportZoom(true);
        settings.BuiltInZoomControls = true;
        settings.DisplayZoomControls = false;

        // Enable modern web features
        settings.AllowFileAccess = false; // Security: disable file access
        settings.AllowContentAccess = true;
        settings.MediaPlaybackRequiresUserGesture = false;

        // Enable caching for better performance
        settings.CacheMode = CacheModes.Default;

        // Set user agent to ensure compatibility
        var userAgent = settings.UserAgentString;
        if (!string.IsNullOrEmpty(userAgent))
        {
            // Add MAUI identifier to user agent for server-side detection
            settings.UserAgentString = $"{userAgent} MauiPwaShell/1.0";
        }

        System.Diagnostics.Debug.WriteLine("[Android WebView] Configured for WebAuthn support");
    }
}
