# WebAuthn/Passkeys in MAUI WebView

## Overview

This document explains how passkeys and WebAuthn work when running a Progressive Web App (PWA) inside a .NET MAUI WebView shell application.

## The Challenge

WebAuthn (Web Authentication) and passkeys rely on browser APIs (`navigator.credentials`) that are typically available in standard web browsers but may have **limited or no support in WebView controls** depending on the platform and configuration.

### Key Issues
1. **WebView ≠ Full Browser**: WebViews are embedded browser engines that may not have all features of standalone browsers
2. **Platform Differences**: Android, iOS, and other platforms handle WebViews and credential management differently
3. **Security Context**: WebAuthn requires a secure context (HTTPS) and proper origin handling
4. **Platform Credentials**: Passkeys are typically managed at the OS level, requiring platform-specific integrations

## Platform-Specific Status

### Android (WebView/Chrome)

**Status**: ✅ **Supported with configuration**

Android WebView is based on Chrome and supports WebAuthn with proper configuration:

#### Requirements:
1. **Target Android 9.0 (API 28) or higher** for full WebAuthn support
2. **Enable JavaScript** in WebView settings
3. **Enable DOM storage** for credential storage
4. **Use HTTPS** or configure localhost as secure
5. **Configure WebView client** to handle credential requests

#### Implementation Details:
- Android WebView supports `navigator.credentials` API
- Uses Google Play Services for FIDO2/WebAuthn
- Can access device biometrics (fingerprint, face unlock)
- Supports platform authenticators (device passkeys)

#### Limitations:
- Requires Google Play Services on the device
- May not work on custom ROM builds without Google services
- Minimum Android version requirement (API 28+)

### iOS (WKWebView)

**Status**: ⚠️ **Partially Supported**

iOS WKWebView has limited WebAuthn support:

#### Requirements:
1. **iOS 14.0 or higher** for basic WebAuthn support
2. **iOS 15.0 or higher** recommended for better support
3. **Associated Domains** entitlement configured
4. **Proper Info.plist configuration**

#### Implementation Details:
- WKWebView supports WebAuthn starting iOS 14
- Uses Face ID/Touch ID for user verification
- Stores credentials in iCloud Keychain (if enabled)
- Requires app to be properly signed and provisioned

#### Limitations:
- Requires associated domains configuration (apple-app-site-association)
- May not support all WebAuthn features
- Cross-domain credential sharing requires additional setup
- Simulator testing may have limitations

### Windows (WebView2)

**Status**: ✅ **Supported**

Windows WebView2 (Edge-based) has good WebAuthn support:

#### Requirements:
1. **WebView2 Runtime** installed
2. **Windows Hello** configured for biometrics
3. **HTTPS** or localhost

#### Implementation Details:
- Full WebAuthn API support
- Uses Windows Hello for authentication
- Supports security keys and platform authenticators

### macOS (WKWebView/Catalyst)

**Status**: ⚠️ **Partially Supported**

Similar to iOS with Mac Catalyst:

#### Requirements:
1. **macOS 11.0 or higher**
2. **Touch ID** or other authentication method configured

## Best Practices

### 1. Feature Detection

Always check if WebAuthn is available before using it:

```javascript
if (window.PublicKeyCredential) {
    // WebAuthn is available
    console.log('WebAuthn supported');
} else {
    // Fallback to traditional authentication
    console.log('WebAuthn not supported - using fallback');
}
```

### 2. Secure Context

Ensure your PWA is served over HTTPS or from localhost:

```csharp
// For development, Android emulator
private const string PwaUrl = "http://10.0.2.2:5000/";

// For production
private const string PwaUrl = "https://your-domain.com/";
```

### 3. Origin Configuration

Configure your FIDO2 settings to match the WebView's origin:

```json
{
  "Fido2": {
    "ServerDomain": "your-domain.com",
    "ServerName": "Your App Name",
    "Origin": "https://your-domain.com",
    "TimestampDriftTolerance": 300000
  }
}
```

### 4. Error Handling

Implement graceful fallbacks when WebAuthn fails:

```javascript
async function registerPasskey() {
    try {
        if (!window.PublicKeyCredential) {
            throw new Error('WebAuthn not supported');
        }
        // ... WebAuthn registration code
    } catch (error) {
        console.error('Passkey registration failed:', error);
        // Fallback to password-based auth
        showPasswordRegistration();
    }
}
```

### 5. Testing Strategy

Test on actual devices, not just emulators/simulators:

1. **Android**: Test on physical device with Google Play Services
2. **iOS**: Test on physical device with Touch ID/Face ID
3. **Test both registration and authentication flows**
4. **Test with and without existing credentials**

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    MAUI Application                          │
│                                                              │
│  ┌────────────────────────────────────────────────────┐     │
│  │              MainPage (XAML)                       │     │
│  │  ┌──────────────────────────────────────────┐     │     │
│  │  │         WebView Control                  │     │     │
│  │  │                                          │     │     │
│  │  │  ┌────────────────────────────────┐     │     │     │
│  │  │  │     PWA (index.html)          │     │     │     │
│  │  │  │                                │     │     │     │
│  │  │  │  JavaScript: navigator.        │     │     │     │
│  │  │  │  credentials.create()          │     │     │     │
│  │  │  └────────────────────────────────┘     │     │     │
│  │  │              ▲  ▼                        │     │     │
│  │  │  ┌────────────────────────────────┐     │     │     │
│  │  │  │  Platform WebView Engine       │     │     │     │
│  │  │  │  (WKWebView/Chrome)           │     │     │     │
│  │  │  └────────────────────────────────┘     │     │     │
│  │  └──────────────────────────────────────────┘     │     │
│  └────────────────────────────────────────────────────┘     │
│                       ▲  ▼                                   │
│  ┌────────────────────────────────────────────────────┐     │
│  │     Platform-Specific Handlers                     │     │
│  │  - Android: FIDO2 API via Play Services            │     │
│  │  - iOS: ASAuthorization / Keychain                 │     │
│  │  - Windows: Windows Hello                          │     │
│  └────────────────────────────────────────────────────┘     │
│                       ▲  ▼                                   │
└───────────────────────┼──┼───────────────────────────────────┘
                        │  │
                        │  │  HTTPS/HTTP
                        ▼  ▼
        ┌─────────────────────────────────────┐
        │    Backend Server (ASP.NET)         │
        │                                     │
        │  ┌──────────────────────────────┐   │
        │  │  FIDO2/WebAuthn Service      │   │
        │  │  - Credential registration   │   │
        │  │  - Authentication validation │   │
        │  └──────────────────────────────┘   │
        └─────────────────────────────────────┘
```

## Troubleshooting

### Problem: "WebAuthn is not supported"

**Possible Causes:**
- WebView doesn't have JavaScript enabled
- Platform version too old
- Not running in secure context (HTTP instead of HTTPS)

**Solutions:**
1. Check WebView JavaScript is enabled
2. Verify minimum platform version (Android 9+, iOS 14+)
3. Use HTTPS in production
4. For development, ensure localhost or 10.0.2.2 is treated as secure

### Problem: "NotAllowedError" during credential creation

**Possible Causes:**
- User cancelled the operation
- Timeout occurred
- Invalid origin configuration
- Security policy violation

**Solutions:**
1. Increase timeout in credential options
2. Verify origin matches FIDO2 configuration
3. Check user didn't cancel biometric prompt
4. Ensure app has necessary permissions

### Problem: Credentials work in browser but not in WebView

**Possible Causes:**
- Origin mismatch between browser and WebView
- WebView doesn't share credential storage with browser
- Platform-specific WebView limitations

**Solutions:**
1. Credentials are isolated per app/browser - this is expected
2. Each context (browser vs WebView) maintains separate credentials
3. Users need to register separately in each context

### Problem: iOS WebView shows no biometric prompt

**Possible Causes:**
- Missing entitlements
- WKWebView not properly configured
- iOS version too old

**Solutions:**
1. Add Face ID/Touch ID usage description to Info.plist
2. Configure associated domains if using app-specific credentials
3. Verify iOS 14+ is being used
4. Test on physical device, not just simulator

## Security Considerations

### 1. Origin Validation

Ensure your backend validates the origin of WebAuthn requests:

```csharp
builder.Services.AddFido2(options =>
{
    options.ServerDomain = configuration["Fido2:ServerDomain"];
    options.ServerName = configuration["Fido2:ServerName"];
    options.Origins = new HashSet<string> { configuration["Fido2:Origin"] };
    // Validate that the origin matches expected value
});
```

### 2. HTTPS Requirement

**Never disable HTTPS in production.** WebAuthn requires a secure context.

For development only:
- Android emulator: `http://10.0.2.2:5000` is treated as secure
- iOS simulator: `http://localhost:5000` is treated as secure

For production:
- Always use `https://your-domain.com`
- Configure proper SSL certificates
- Use certificate pinning for additional security

### 3. Credential Storage

Credentials are stored by the platform:
- **Android**: Encrypted by Google Play Services
- **iOS**: iCloud Keychain (if enabled) or device-only
- **Windows**: Windows Hello secure enclave

Your app **never has direct access** to private keys - this is by design for security.

### 4. Privacy

WebAuthn is privacy-preserving:
- No credential information shared across origins
- Credentials are origin-scoped
- Users control which credentials to use

## Limitations Summary

| Platform | WebAuthn Support | Minimum Version | Biometric Support | Notes |
|----------|-----------------|-----------------|-------------------|-------|
| Android  | ✅ Full         | API 28 (9.0)    | ✅ Yes            | Requires Play Services |
| iOS      | ⚠️ Partial      | iOS 14.0        | ✅ Yes            | Limited features |
| Windows  | ✅ Full         | WebView2        | ✅ Yes            | Windows Hello |
| macOS    | ⚠️ Partial      | macOS 11.0      | ✅ Yes            | Touch ID |

## Recommendations

### For Production Apps

1. **Hybrid Approach**: Offer both passkey and traditional authentication
2. **Feature Detection**: Check WebAuthn availability before offering passkey option
3. **Clear UX**: Explain to users what passkeys are and how they work
4. **Fallback**: Always provide alternative authentication methods
5. **Testing**: Test extensively on target devices with actual biometrics

### For Development

1. **Use HTTPS**: Even in development, use self-signed certificates for testing
2. **Test on Devices**: Emulators/simulators have limitations
3. **Enable Logging**: Add comprehensive logging for debugging
4. **Version Control**: Document minimum supported platform versions

## Alternative Approaches

If WebView WebAuthn support is insufficient, consider:

### 1. Native Biometric Integration

Instead of relying on WebAuthn in the WebView, implement native biometric authentication:

```csharp
// Use MAUI native biometric APIs
// Store credentials on device
// Communicate with PWA via JavaScript injection
```

### 2. Hybrid Authentication

- Use WebAuthn where supported (newer devices)
- Fall back to native biometric where WebAuthn is limited
- Always provide password fallback

### 3. Platform-Specific Implementation

Create custom handlers for each platform that bridge WebAuthn calls to native APIs:

```csharp
// Android: Use Google FIDO2 API directly
// iOS: Use ASAuthorizationController
// Bridge to JavaScript layer
```

## Further Reading

- [W3C WebAuthn Specification](https://www.w3.org/TR/webauthn-2/)
- [FIDO Alliance](https://fidoalliance.org/)
- [Android FIDO2 API](https://developers.google.com/identity/fido)
- [Apple Passkeys](https://developer.apple.com/passkeys/)
- [WebView Best Practices](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/webview)

## Conclusion

**Can passkeys work in MAUI WebView?**

✅ **Yes, with caveats:**
- Android 9+ has good support
- iOS 14+ has partial support
- Requires proper configuration
- May have platform-specific limitations
- Always implement fallback authentication

**Recommended Approach:**
1. Configure WebView properly for each platform
2. Implement feature detection in your PWA
3. Provide fallback authentication methods
4. Test thoroughly on target devices
5. Document limitations for users
