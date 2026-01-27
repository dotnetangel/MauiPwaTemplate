using System.Web;

namespace NativeFidoMaui;

/// <summary>
/// Service for building and parsing deep link URLs for passkey authentication flow
/// </summary>
public class DeepLinkService : IDeepLinkService
{
    // Configuration - Replace with your actual domain
    private const string PwaDomain = "https://yourdomain.com";
    private const string CustomScheme = "mauipwa";
    
    // Server endpoints (adjust to match your backend)
    private const string AuthStartPath = "/auth/webauthn/deeplink/start";
    private const string AuthBrowserPath = "/auth/webauthn/browser";
    private const string RedeemPath = "/auth/webauthn/redeem";
    
    // Deep link callback scheme and path
    private const string CallbackScheme = CustomScheme + "://";
    private const string CallbackPath = "auth/return";

    /// <summary>
    /// Build the URL to start passkey authentication in system browser
    /// This endpoint should redirect to the actual WebAuthn ceremony
    /// </summary>
    public string BuildAuthStartUrl()
    {
        // Generate a state parameter for CSRF protection
        var state = GenerateState();
        
        // Build callback URL that server will redirect to after auth
        var callbackUrl = $"{CallbackScheme}{CallbackPath}";
        
        // Build the start URL with callback and state
        var startUrl = $"{PwaDomain}{AuthStartPath}?callback={Uri.EscapeDataString(callbackUrl)}&state={state}";
        
        return startUrl;
    }

    /// <summary>
    /// Parse the authentication callback URL from deep link
    /// Expected format: mauipwa://auth/return?code=xxx&state=yyy
    /// </summary>
    public AuthCallbackResult ParseAuthCallback(string url)
    {
        try
        {
            var uri = new Uri(url);
            
            // Validate scheme and path
            if (!url.StartsWith($"{CallbackScheme}{CallbackPath}", StringComparison.OrdinalIgnoreCase))
            {
                return new AuthCallbackResult
                {
                    IsValid = false,
                    Error = "Invalid callback URL scheme or path"
                };
            }

            // Parse query string
            var query = HttpUtility.ParseQueryString(uri.Query);
            var code = query["code"];
            var state = query["state"];
            var error = query["error"];

            if (!string.IsNullOrEmpty(error))
            {
                return new AuthCallbackResult
                {
                    IsValid = false,
                    Error = error
                };
            }

            if (string.IsNullOrEmpty(code))
            {
                return new AuthCallbackResult
                {
                    IsValid = false,
                    Error = "Missing authorization code"
                };
            }

            // TODO: Validate state parameter matches what we sent
            // In production, store state in secure storage and verify it here

            return new AuthCallbackResult
            {
                IsValid = true,
                Code = code,
                State = state
            };
        }
        catch (Exception ex)
        {
            return new AuthCallbackResult
            {
                IsValid = false,
                Error = $"Failed to parse callback: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Build the URL to redeem the auth code in the WebView
    /// This sets the session cookie inside the WebView
    /// </summary>
    public string BuildRedeemUrl(string code, string? state)
    {
        var url = $"{PwaDomain}{RedeemPath}?code={Uri.EscapeDataString(code)}";
        
        if (!string.IsNullOrEmpty(state))
        {
            url += $"&state={Uri.EscapeDataString(state)}";
        }
        
        return url;
    }

    /// <summary>
    /// Generate a random state parameter for CSRF protection
    /// </summary>
    private string GenerateState()
    {
        // In production, store this securely and verify it on callback
        return Guid.NewGuid().ToString("N");
    }
}
