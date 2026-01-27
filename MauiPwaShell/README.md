# MAUI PWA Shell - WebAuthn/Passkey Support

## Overview

This MAUI application serves as a native wrapper for a Progressive Web App (PWA) that includes WebAuthn/passkey authentication support. The app uses a WebView to render the PWA while providing native platform integration.

## WebAuthn/Passkey Support

### Does it work?

**✅ Yes, with platform-specific considerations:**

- **Android 9.0+**: Full support via Google Play Services FIDO2 API
- **iOS 14.0+**: Partial support via WKWebView WebAuthn implementation  
- **Windows**: Full support via WebView2 and Windows Hello
- **macOS**: Partial support via Catalyst

### How it works

```
┌─────────────────────────────────────────┐
│      MAUI Application Shell             │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │  Custom WebView Handler           │  │
│  │  - Enables JavaScript             │  │
│  │  - Enables DOM Storage            │  │
│  │  - Configures security settings   │  │
│  └───────────────────────────────────┘  │
│              ▼                          │
│  ┌───────────────────────────────────┐  │
│  │      WebView Control              │  │
│  │  Renders PWA with WebAuthn        │  │
│  └───────────────────────────────────┘  │
│              ▼                          │
│  ┌───────────────────────────────────┐  │
│  │   Platform WebView Engine         │  │
│  │   - Android: Chrome WebView       │  │
│  │   - iOS: WKWebView               │  │
│  │   - Windows: WebView2 (Edge)     │  │
│  └───────────────────────────────────┘  │
│              ▼                          │
│  ┌───────────────────────────────────┐  │
│  │  Platform Authenticator           │  │
│  │  - Android: Fingerprint/Face      │  │
│  │  - iOS: Touch ID/Face ID         │  │
│  │  - Windows: Windows Hello        │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## Platform-Specific Implementation

### Android

**File**: `Platforms/Android/WebViewHandler.cs`

The custom Android WebView handler:
- Enables JavaScript (required for WebAuthn API)
- Enables DOM storage (for credential caching)
- Configures mixed content handling
- Sets proper user agent

**Permissions**: `AndroidManifest.xml`
- `USE_BIOMETRIC` - Access biometric sensors
- `USE_FINGERPRINT` - Legacy fingerprint support
- Hardware features declared as optional (not required)

**Requirements**:
- Android 9.0 (API 28) minimum
- Google Play Services installed
- Biometric hardware configured on device

### iOS

**File**: `Platforms/iOS/WebViewHandler.cs`

The custom iOS WKWebView handler:
- Enables JavaScript
- Configures modern web features
- Sets custom user agent for server detection

**Privacy**: `Info.plist`
- `NSFaceIDUsageDescription` - Explains why Face ID is needed

**Requirements**:
- iOS 14.0 minimum (iOS 15.0+ recommended)
- Touch ID or Face ID configured
- Test on physical device (simulator has limitations)

### Configuration

**File**: `MauiProgram.cs`

The custom handlers are registered at app startup:

```csharp
builder.ConfigureMauiHandlers(handlers =>
{
#if ANDROID
    handlers.AddHandler<Microsoft.Maui.Controls.WebView, Platforms.Android.CustomWebViewHandler>();
#elif IOS
    handlers.AddHandler<Microsoft.Maui.Controls.WebView, Platforms.iOS.CustomWebViewHandler>();
#endif
});
```

## Testing

### Android Testing

1. **Build for Android**:
   ```bash
   dotnet build -t:Run -f net8.0-android
   ```

2. **Prerequisites**:
   - Android device with biometrics set up
   - Google Play Services installed
   - Developer mode enabled

3. **Test Flow**:
   - Launch the app
   - Navigate to Passkey section
   - Click "Register Passkey"
   - Follow biometric prompt
   - Verify credential creation

### iOS Testing

1. **Build for iOS** (requires macOS with Xcode):
   ```bash
   dotnet build -t:Run -f net8.0-ios
   ```

2. **Prerequisites**:
   - iOS device with Touch ID/Face ID
   - Developer certificate and provisioning profile
   - Face ID configured in device settings

3. **Test Flow**:
   - Launch the app
   - Check WebAuthn status display
   - Attempt passkey registration
   - Verify Face ID/Touch ID prompt appears

### Emulator/Simulator Limitations

⚠️ **Important**: WebAuthn testing is limited in emulators/simulators:

- **Android Emulator**: May not have Google Play Services or biometrics
- **iOS Simulator**: Limited biometric simulation
- **Always test on physical devices** for accurate results

## Troubleshooting

### "WebAuthn is not supported"

**Cause**: WebView doesn't have necessary APIs enabled

**Solutions**:
1. Verify custom WebView handler is registered
2. Check platform version meets minimum requirements
3. Ensure JavaScript is enabled in WebView
4. Verify app has necessary permissions

### Android: "NotAllowedError"

**Possible Causes**:
- Google Play Services not installed/updated
- Biometrics not configured on device
- User cancelled biometric prompt

**Solutions**:
1. Update Google Play Services
2. Configure fingerprint/face unlock in Settings
3. Check app permissions in Android settings

### iOS: No biometric prompt appears

**Possible Causes**:
- Info.plist missing Face ID usage description
- Touch ID/Face ID not configured
- Testing on simulator instead of device

**Solutions**:
1. Verify `NSFaceIDUsageDescription` in Info.plist
2. Configure Face ID/Touch ID in iOS Settings
3. Test on physical device

### Credentials work in browser but not in app

**Expected Behavior**: This is normal and by design.

- WebAuthn credentials are **origin-scoped**
- WebView has different origin than browser
- Credentials are isolated between contexts
- Users must register separately in app

## Security Considerations

### Origin Configuration

The PWA backend must be configured to accept requests from the WebView origin:

**File**: `PwaWeb/appsettings.json`
```json
{
  "Fido2": {
    "ServerDomain": "your-domain.com",
    "Origin": "https://your-domain.com"
  }
}
```

### HTTPS Requirement

- **Development**: `http://10.0.2.2:5000` (Android emulator) treated as secure
- **Production**: **Must use HTTPS** - WebAuthn requires secure context

### User Agent Detection

The app adds `MauiPwaShell/1.0` to the user agent string, allowing server-side detection of WebView vs browser access for analytics or conditional features.

## Feature Detection in PWA

The PWA includes `webauthn-detection.js` which:
- Detects if running in WebView vs browser
- Checks WebAuthn API availability
- Displays platform-specific information
- Provides graceful degradation

**Usage in PWA**:
```javascript
const availability = await window.webAuthnDetection.checkWebAuthnAvailability();
if (availability.available) {
    // Enable passkey features
} else {
    // Show fallback authentication
}
```

## Limitations

### Known Issues

1. **iOS WKWebView**:
   - Limited WebAuthn feature support compared to Safari
   - May not support all authenticator types
   - Requires associated domains for production apps

2. **Android WebView**:
   - Requires Google Play Services (not available on all ROMs)
   - Minimum API 28 requirement excludes older devices

3. **Cross-Platform**:
   - Credentials don't sync between platforms
   - Each platform maintains separate credential storage
   - No cross-device credential sharing (by design)

### Workarounds

**Hybrid Authentication Strategy**:
```javascript
// Feature detection
if (await isWebAuthnAvailable()) {
    // Offer passkey login
    showPasskeyButton();
} else {
    // Fall back to traditional auth
    showPasswordLogin();
}

// Always provide password fallback
showPasswordOption();
```

## Best Practices

### 1. Always Provide Fallback

Never rely solely on WebAuthn - always offer alternative authentication:
- Password-based login
- Magic link email
- OTP/SMS codes

### 2. Feature Detection

Check WebAuthn availability before offering passkey features:
```javascript
if (window.PublicKeyCredential) {
    // Show passkey UI
}
```

### 3. User Communication

Clearly explain to users:
- What passkeys are
- Which biometric will be used
- Why the app needs biometric permission
- Fallback options if passkeys don't work

### 4. Error Handling

Implement comprehensive error handling:
```javascript
try {
    await registerPasskey();
} catch (error) {
    if (error.name === 'NotAllowedError') {
        // User cancelled or timeout
    } else {
        // Other error - show fallback
    }
}
```

### 5. Testing Strategy

- Test on multiple device types
- Test with and without biometrics configured
- Test network failure scenarios
- Test timeout scenarios
- Verify error messages are user-friendly

## Production Deployment

### Checklist

- [ ] Use HTTPS for PWA backend
- [ ] Configure proper FIDO2 origin in appsettings
- [ ] Add associated domains (iOS)
- [ ] Test on physical devices (all target platforms)
- [ ] Implement fallback authentication
- [ ] Add comprehensive error handling
- [ ] Document minimum platform versions
- [ ] Set up monitoring/analytics for WebAuthn usage
- [ ] Provide user documentation

### Associated Domains (iOS)

For production iOS apps using WebAuthn:

1. Add associated domain entitlement
2. Create `apple-app-site-association` file
3. Host file at `https://yourdomain.com/.well-known/`
4. Configure in Xcode project

## Resources

### Documentation
- [WebAuthn Guide](https://webauthn.guide/)
- [FIDO Alliance](https://fidoalliance.org/)
- [MAUI WebView Docs](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/webview)

### Platform-Specific
- [Android FIDO2 API](https://developers.google.com/identity/fido)
- [Apple Passkeys](https://developer.apple.com/passkeys/)
- [Windows Hello](https://docs.microsoft.com/en-us/windows/security/identity-protection/hello-for-business/)

### This Repository
- [WebAuthn in WebView Guide](../docs/webauthn-passkeys-maui.md)
- [Push Notifications Overview](../docs/push-notifications-overview.md)

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review the detailed documentation in `/docs`
3. Check platform-specific logs (`adb logcat` for Android, Xcode console for iOS)
4. Open an issue on GitHub with:
   - Platform and version
   - Error messages
   - Steps to reproduce

## License

See LICENSE file in repository root.
