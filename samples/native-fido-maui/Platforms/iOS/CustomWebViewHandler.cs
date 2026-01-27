using Foundation;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using WebKit;

namespace NativeFidoMaui.Platforms.iOS;

/// <summary>
/// Custom WebView handler for iOS that enables WebAuthn/passkeys support
/// and intercepts native bridge calls
/// </summary>
public class CustomWebViewHandler : WebViewHandler
{
    protected override WKWebView CreatePlatformView()
    {
        var config = new WKWebViewConfiguration();
        
        // Enable modern web features
        config.Preferences.SetValueForKey(NSNumber.FromBoolean(true), new NSString("javaScriptEnabled"));
        
        // Create custom navigation delegate
        var webView = new WKWebView(CoreGraphics.CGRect.Empty, config)
        {
            NavigationDelegate = new CustomNavigationDelegate(this)
        };

        return webView;
    }

    private class CustomNavigationDelegate : WKNavigationDelegate
    {
        private readonly CustomWebViewHandler _handler;

        public CustomNavigationDelegate(CustomWebViewHandler handler)
        {
            _handler = handler;
        }

        public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
        {
            var url = navigationAction?.Request?.Url?.ToString();

            if (!string.IsNullOrEmpty(url))
            {
                // Intercept native bridge calls
                if (url.StartsWith("native://auth/start-passkey-login"))
                {
                    // Cancel WebView navigation
                    decisionHandler(WKNavigationActionPolicy.Cancel);

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
                            Console.WriteLine($"[iOS WebView] Error starting passkey login: {ex.Message}");
                        }
                    });

                    return;
                }
            }

            // Allow normal navigation
            decisionHandler(WKNavigationActionPolicy.Allow);
        }
    }
}
