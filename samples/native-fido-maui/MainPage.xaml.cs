namespace NativeFidoMaui;

public partial class MainPage : ContentPage
{
    // Replace this with your actual PWA URL (must be HTTPS for production)
    private const string PwaUrl = "https://yourdomain.com/";
    private readonly IDeepLinkService _deepLinkService;

    public MainPage(IDeepLinkService deepLinkService)
    {
        InitializeComponent();
        _deepLinkService = deepLinkService;
        
        // Set initial PWA URL
        PwaView.Source = PwaUrl;
        
        // Listen for navigating events to inject bridge code
        PwaView.Navigating += PwaView_Navigating;
    }

    /// <summary>
    /// Inject JavaScript bridge that allows the PWA to trigger system browser login
    /// </summary>
    private async void PwaView_Navigating(object? sender, WebNavigatingEventArgs e)
    {
        // Only inject on main domain navigations
        if (e.Url?.StartsWith(PwaUrl) == true)
        {
            await Task.Delay(500); // Give page time to load
            await InjectAuthBridgeAsync();
        }
    }

    /// <summary>
    /// Injects JavaScript that provides a bridge for the PWA to request passkey login
    /// The PWA can call: window.nativeAuthBridge.startPasskeyLogin()
    /// </summary>
    private async Task InjectAuthBridgeAsync()
    {
        try
        {
            var js = @"
(function() {
    if (window.nativeAuthBridge) return; // Already injected
    
    window.nativeAuthBridge = {
        // Called by PWA when user clicks 'Login with Passkey'
        startPasskeyLogin: function() {
            // Post message that MAUI will intercept
            window.location.href = 'native://auth/start-passkey-login';
        }
    };
    
    console.log('[Native Bridge] Auth bridge injected');
})();
";
            await PwaView.EvaluateJavaScriptAsync(js);
            Console.WriteLine("[MAUI] Injected auth bridge into WebView");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MAUI] Failed to inject auth bridge: {ex.Message}");
        }
    }

    /// <summary>
    /// Called by platform-specific code when the app receives a deep link callback
    /// </summary>
    public async Task HandleDeepLinkAsync(string url)
    {
        try
        {
            Console.WriteLine($"[MAUI] Handling deep link: {url}");
            
            var result = _deepLinkService.ParseAuthCallback(url);
            
            if (result.IsValid && !string.IsNullOrEmpty(result.Code))
            {
                // Redeem the code by navigating WebView to the redeem endpoint
                // This sets the session cookie inside the WebView
                var redeemUrl = _deepLinkService.BuildRedeemUrl(result.Code, result.State);
                
                Console.WriteLine($"[MAUI] Navigating to redeem URL: {redeemUrl}");
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    PwaView.Source = redeemUrl;
                });
            }
            else
            {
                Console.WriteLine($"[MAUI] Invalid deep link callback: {result.Error}");
                // Optionally show error to user
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MAUI] Error handling deep link: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens the system browser for passkey authentication
    /// Called when PWA triggers the native bridge
    /// </summary>
    public async Task StartPasskeyLoginAsync()
    {
        try
        {
            var authUrl = _deepLinkService.BuildAuthStartUrl();
            Console.WriteLine($"[MAUI] Opening system browser for passkey login: {authUrl}");
            
            await Launcher.OpenAsync(new Uri(authUrl));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MAUI] Error opening browser: {ex.Message}");
        }
    }
}
