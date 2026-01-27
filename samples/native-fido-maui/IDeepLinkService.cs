namespace NativeFidoMaui;

/// <summary>
/// Service for handling deep link authentication flow
/// </summary>
public interface IDeepLinkService
{
    /// <summary>
    /// Parse the authentication callback URL from the system browser
    /// </summary>
    AuthCallbackResult ParseAuthCallback(string url);

    /// <summary>
    /// Build the URL to start passkey authentication in system browser
    /// </summary>
    string BuildAuthStartUrl();

    /// <summary>
    /// Build the URL to redeem the auth code in the WebView
    /// </summary>
    string BuildRedeemUrl(string code, string? state);
}

/// <summary>
/// Result of parsing an authentication callback URL
/// </summary>
public class AuthCallbackResult
{
    public bool IsValid { get; set; }
    public string? Code { get; set; }
    public string? State { get; set; }
    public string? Error { get; set; }
}
