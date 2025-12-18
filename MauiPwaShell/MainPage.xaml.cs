using Plugin.Firebase.CloudMessaging;
using MauiPwaShell.Services;

namespace MauiPwaShell;

public partial class MainPage : ContentPage
{
    private const string PwaUrl = "http://10.0.2.2:5000/";
    private readonly NativeBridge _nativeBridge;

    public MainPage()
    {
        InitializeComponent();
        
        // Initialize native services and bridge
        var nativeService = new NativeService();
        _nativeBridge = new NativeBridge(nativeService);
        
        PwaView.Navigated += PwaView_Navigated;
        PwaView.Source = PwaUrl;
    }

    private async void PwaView_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        try
        {
            // Inject FCM token
            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
                await InjectTokenIntoWebAsync(token);
            
            // Setup native bridge
            await SetupNativeBridgeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MAUI] Error on navigated: {ex.Message}");
        }
    }

    public async Task InjectTokenIntoWebAsync(string token)
    {
        try
        {
            var escaped = token.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n");
            var js = $@"
(function() {{
    try {{
        window.nativeFcmToken = '{escaped}';
        localStorage.setItem('nativeFcmToken', '{escaped}');
        window.dispatchEvent(new CustomEvent('nativeTokenReady', {{ detail: '{escaped}' }}));
    }} catch(e) {{
        console.error('inject token failed', e);
    }}
}})();";

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await PwaView.EvaluateJavaScriptAsync(js);
            });

            Console.WriteLine("[MAUI] Injected FCM token into webview");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MAUI] InjectTokenIntoWebAsync error: {ex.Message}");
        }
    }

    private async Task SetupNativeBridgeAsync()
    {
        try
        {
            var js = @"
(function() {
    // Define the native bridge interface
    window.nativeBridge = {
        // Send a message to the native layer
        sendMessage: async function(action, data) {
            return new Promise((resolve, reject) => {
                try {
                    const request = JSON.stringify({ action: action, data: data });
                    const callbackName = 'nativeCallback_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
                    
                    // Create a temporary callback function
                    window[callbackName] = function(response) {
                        delete window[callbackName];
                        const result = JSON.parse(response);
                        if (result.success) {
                            resolve(result.data);
                        } else {
                            reject(new Error(result.error || 'Unknown error'));
                        }
                    };
                    
                    // Call the native handler with callback name
                    window.location = 'nativebridge://call?callback=' + callbackName + '&message=' + encodeURIComponent(request);
                } catch (e) {
                    reject(e);
                }
            });
        },
        
        // Convenience methods
        initialize: function(apiKey) {
            return this.sendMessage('initialize', { apiKey: apiKey });
        },
        performOperation: function(input) {
            return this.sendMessage('performOperation', { input: input });
        },
        getDeviceInfo: function() {
            return this.sendMessage('getDeviceInfo', {});
        },
        isInitialized: function() {
            return this.sendMessage('isInitialized', {});
        }
    };
    
    // Dispatch event to notify that the bridge is ready
    window.dispatchEvent(new CustomEvent('nativeBridgeReady'));
    console.log('[PWA] Native bridge initialized');
})();
";

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await PwaView.EvaluateJavaScriptAsync(js);
            });

            // Register the native message handler
            PwaView.Navigating += OnWebViewNavigating;

            Console.WriteLine("[MAUI] Native bridge setup complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MAUI] SetupNativeBridgeAsync error: {ex.Message}");
        }
    }

    private async void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        // Intercept native bridge calls
        if (e.Url.StartsWith("nativebridge://call"))
        {
            e.Cancel = true;
            await HandleNativeBridgeCallAsync(e.Url);
        }
    }

    private async Task HandleNativeBridgeCallAsync(string url)
    {
        try
        {
            var uri = new Uri(url);
            var query = uri.Query.TrimStart('?');
            var parameters = ParseQueryString(query);
            
            if (!parameters.TryGetValue("callback", out var callbackName) || 
                !parameters.TryGetValue("message", out var message))
            {
                Console.WriteLine("[MAUI] Invalid native bridge call");
                return;
            }

            if (string.IsNullOrEmpty(callbackName) || string.IsNullOrEmpty(message))
            {
                Console.WriteLine("[MAUI] Invalid native bridge call - empty parameters");
                return;
            }

            // Process the message through the native bridge
            var response = await _nativeBridge.HandleMessageAsync(message);

            // Call back to JavaScript with the response
            var escapedResponse = response.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n");
            var callbackJs = $"if (typeof window['{callbackName}'] === 'function') {{ window['{callbackName}']('{escapedResponse}'); }}";

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await PwaView.EvaluateJavaScriptAsync(callbackJs);
            });

            Console.WriteLine($"[MAUI] Handled native bridge call: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MAUI] HandleNativeBridgeCallAsync error: {ex.Message}");
        }
    }

    private Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query))
            return result;

        var pairs = query.Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
        }
        return result;
    }
}
