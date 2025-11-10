# iOS Push Notifications Registration Flow (FCM with APNs)

This document describes the complete push notification registration flow for the **iOS** platform using Firebase Cloud Messaging (FCM) integrated with Apple Push Notification service (APNs) in a .NET MAUI application.

## Overview

iOS push notifications use a two-tier system:
1. **APNs (Apple Push Notification service)**: Apple's native push infrastructure
2. **FCM (Firebase Cloud Messaging)**: Intermediary that translates and routes messages

The registration flow involves:
1. APNs permission request
2. APNs device token retrieval
3. FCM initialization and token generation
4. Token registration with backend server
5. Token injection into WebView

## Architecture Components

### iOS Components
- **.NET MAUI App**: Cross-platform mobile application
- **Plugin.Firebase**: NuGet package for Firebase integration
- **Firebase iOS SDK**: Native Firebase libraries
- **APNs**: Apple's push notification service
- **User Notifications Framework**: iOS notification APIs

### Backend Components
- **ASP.NET Core Server**: Backend API
- **RegisterPushController**: Token registration endpoint
- **Firebase Console**: Message routing and management
- **Apple Developer Portal**: Certificate and key management

### Configuration Files
- **GoogleService-Info.plist**: Firebase project configuration
- **Info.plist**: App capabilities and permissions
- **Entitlements.plist**: Push notification entitlements
- **Provisioning Profile**: Must include Push Notifications capability

## Detailed Registration Flow

```
┌────────────┐   ┌─────────────┐   ┌────────────┐   ┌───────────┐   ┌────────────┐
│  MAUI App  │   │Firebase SDK │   │   APNs     │   │    FCM    │   │   Server   │
│   (iOS)    │   │  (Native)   │   │  (Apple)   │   │  (Google) │   │  (Backend) │
└─────┬──────┘   └──────┬──────┘   └─────┬──────┘   └─────┬─────┘   └─────┬──────┘
      │                 │                 │                │               │
      │ 1. App launches │                 │                │               │
      │─────────────┐   │                 │                │               │
      │             │   │                 │                │               │
      │<────────────┘   │                 │                │               │
      │                 │                 │                │               │
      │ 2. Initialize   │                 │                │               │
      │    Firebase     │                 │                │               │
      │─────────────────>                 │                │               │
      │                 │                 │                │               │
      │                 │ 3. Load         │                │               │
      │                 │    GoogleService│                │               │
      │                 │    -Info.plist  │                │               │
      │                 │────────┐        │                │               │
      │                 │        │        │                │               │
      │                 │<───────┘        │                │               │
      │                 │                 │                │               │
      │ 4. Request      │                 │                │               │
      │    notification │                 │                │               │
      │    permission   │                 │                │               │
      │─────────────────>                 │                │               │
      │                 │                 │                │               │
      │ 5. Show iOS     │                 │                │               │
      │    permission   │                 │                │               │
      │    dialog       │                 │                │               │
      │─────────┐       │                 │                │               │
      │         │       │                 │                │               │
      │<────────┘       │                 │                │               │
      │                 │                 │                │               │
      │ 6. User grants  │                 │                │               │
      │<─────────────────                 │                │               │
      │                 │                 │                │               │
      │ 7. Register for │                 │                │               │
      │    remote       │                 │                │               │
      │    notifications│                 │                │               │
      │─────────────────>                 │                │               │
      │                 │                 │                │               │
      │                 │ 8. Request APNs │                │               │
      │                 │    device token │                │               │
      │                 │─────────────────>                │               │
      │                 │                 │                │               │
      │                 │                 │ 9. Generate    │               │
      │                 │                 │    device token│               │
      │                 │                 │    (device ID +│               │
      │                 │                 │     bundle ID) │               │
      │                 │<─────────────────                │               │
      │                 │                 │                │               │
      │                 │ 10. APNs token  │                │               │
      │<─────────────────                 │                │               │
      │                 │                 │                │               │
      │                 │ 11. Send APNs   │                │               │
      │                 │     token to FCM│                │               │
      │                 │─────────────────────────────────>                │
      │                 │                 │                │               │
      │                 │                 │                │ 12. Generate  │
      │                 │                 │                │     FCM token │
      │                 │                 │                │     (map APNs │
      │                 │                 │                │      to FCM)  │
      │                 │<─────────────────────────────────                │
      │                 │                 │                │               │
      │ 13. Return FCM  │                 │                │               │
      │     token       │                 │                │               │
      │<─────────────────                 │                │               │
      │                 │                 │                │               │
      │ 14. POST token  │                 │                │               │
      │     to server   │                 │                │               │
      │─────────────────────────────────────────────────────────────────> │
      │                 │                 │                │               │
      │                 │                 │                │               │ 15. Store
      │                 │                 │                │               │─────┐
      │                 │                 │                │               │     │
      │                 │                 │                │               │<────┘
      │                 │                 │                │               │
      │ 16. Success     │                 │                │               │
      │<─────────────────────────────────────────────────────────────────  │
      │                 │                 │                │               │
      │ 17. Inject      │                 │                │               │
      │     token into  │                 │                │               │
      │     WebView     │                 │                │               │
      │─────────┐       │                 │                │               │
      │         │       │                 │                │               │
      │<────────┘       │                 │                │               │
      │                 │                 │                │               │
```

## Step-by-Step Process

### Step 1: App Launch and Firebase Initialization
**Location**: `MauiPwaShell/MauiProgram.cs` - Lines 8-21

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder.UseMauiApp<App>();

#if DEBUG
    builder.Logging.AddDebug();
#endif

    var app = builder.Build();
    InitializeFirebase();
    return app;
}
```

**iOS Delegate**: `Platforms/iOS/AppDelegate.cs` - Lines 1-19

```csharp
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIKit.UIApplication application, NSDictionary launchOptions)
    {
        CrossFirebaseCloudMessaging.Current.OnNotificationReceived += (s, e) =>
        {
            System.Console.WriteLine($"[iOS] Notification received: {e.Notification.Title}");
        };
        return base.FinishedLaunching(application, launchOptions);
    }
}
```

### Step 2-3: Load Firebase Configuration
**Location**: `Platforms/iOS/GoogleService-Info.plist`

This file contains Firebase project configuration downloaded from Firebase Console:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>API_KEY</key>
    <string>AIzaSy...</string>
    
    <key>GCM_SENDER_ID</key>
    <string>123456789</string>
    
    <key>GOOGLE_APP_ID</key>
    <string>1:123456789:ios:abc123...</string>
    
    <key>BUNDLE_ID</key>
    <string>com.example.mauipwashell</string>
    
    <key>PROJECT_ID</key>
    <string>your-project-id</string>
    
    <key>CLIENT_ID</key>
    <string>123456789-abc123.apps.googleusercontent.com</string>
</dict>
</plist>
```

### Step 4-6: Request Notification Permission
**Location**: `MauiPwaShell/MauiProgram.cs` - Line 28

```csharp
await CrossFirebaseCloudMessaging.Current.RequestPermissionAsync();
```

This triggers the iOS permission dialog with three buttons:
- **Allow**: Enables all notification types
- **Don't Allow**: Blocks notifications completely
- **Options**: Shows detailed permission settings (iOS 15+)

**Permission Options** (Detailed):
- Alerts: Visual notifications
- Sounds: Notification sounds
- Badges: App icon badge count
- Lock Screen: Show on lock screen
- Notification Center: Show in notification center
- Banners: Show as banners

**iOS Info.plist Configuration**: `Platforms/iOS/Info.plist`

```xml
<key>UIBackgroundModes</key>
<array>
    <string>remote-notification</string>
</array>

<key>FirebaseAppDelegateProxyEnabled</key>
<false/>
```

### Step 7-10: Register for Remote Notifications and Get APNs Token

When permission is granted, iOS automatically:
1. Registers the app with APNs
2. Generates a device token
3. Returns the token to the app

**APNs Token Characteristics**:
- Unique per device + app combination
- 32 bytes (64 hex characters)
- Can change when:
  - App is restored on new device
  - User reinstalls iOS
  - User restores from backup to new device

**Example APNs Token**:
```
740f4707bebcf74f9b7c25d48e3358945f6aa01da5ddb387462c7eaf61bb78ad
```

### Step 11-13: Exchange APNs Token for FCM Token
**Location**: Plugin.Firebase handles this internally

The Plugin.Firebase SDK:
1. Takes the APNs token
2. Sends it to Firebase servers
3. Receives back an FCM token
4. Returns the FCM token to the app

**Why two tokens?**
- **APNs token**: Used by Apple's servers only
- **FCM token**: Used by Firebase to route messages to Apple's servers
- FCM acts as a unified interface for multi-platform apps

**FCM Token Format**: Similar to Android, 152+ characters
```
fE8xB3...long_string...zKpQ:APA91b...even_longer_string
```

### Step 14-16: Register Token with Backend
**Location**: `MauiPwaShell/MauiProgram.cs` - Lines 54-78

```csharp
private static async Task SendTokenToServer(string token)
{
    try
    {
        using var client = new HttpClient();
        var payload = new
        {
            Token = token,
            Platform = DeviceInfo.Platform.ToString() // Will be "iOS"
        };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // For iOS simulator, use localhost or your machine's IP
        var serverUrl = "http://localhost:5000/api/registerpush";
        var response = await client.PostAsync(serverUrl, content);

        Console.WriteLine($"[MAUI] Sent FCM token to server: {response.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MAUI] Failed to send token to server: {ex.Message}");
    }
}
```

**Network Configuration for iOS**:
- **iOS Simulator**: Can use `localhost` or `127.0.0.1`
- **Physical Device**: Must use computer's IP address (e.g., `192.168.1.100`)
- **Production**: Use your actual server URL with HTTPS

**Important**: iOS App Transport Security (ATS) requires HTTPS by default. For development, you can allow HTTP:

**Info.plist**:
```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>
    <!-- Or allow specific domains -->
    <key>NSExceptionDomains</key>
    <dict>
        <key>localhost</key>
        <dict>
            <key>NSExceptionAllowsInsecureHTTPLoads</key>
            <true/>
        </dict>
    </dict>
</dict>
```

### Step 17: Inject Token into WebView
**Location**: `MauiPwaShell/MainPage.xaml.cs` - Lines 30-57

Same as Android - token is injected into the WebView JavaScript context.

## Apple Developer Portal Setup

### 1. Enable Push Notifications Capability

**In Xcode or Apple Developer Portal**:
1. Go to Certificates, Identifiers & Profiles
2. Select your App ID
3. Enable "Push Notifications" capability
4. Click "Edit" next to Push Notifications
5. Create certificates for Development and/or Production

### 2. APNs Authentication Key (Recommended)

**Option A: APNs Auth Key** (Easier, doesn't expire)
1. Go to Keys section in Apple Developer Portal
2. Click "+" to create new key
3. Enable "Apple Push Notifications service (APNs)"
4. Download the `.p8` key file (keep it secure!)
5. Note the Key ID and Team ID

**Upload to Firebase**:
1. Go to Firebase Console > Project Settings
2. Click on your iOS app
3. Scroll to "Cloud Messaging"
4. Upload APNs Authentication Key
5. Enter Key ID and Team ID

**Option B: APNs Certificate** (Legacy, expires annually)
1. Create Certificate Signing Request (CSR) in Keychain Access
2. Upload CSR to Apple Developer Portal
3. Download the certificate
4. Install in Keychain
5. Export as `.p12` file
6. Upload to Firebase Console

### 3. Provisioning Profile

Create or update provisioning profile that includes:
- Push Notifications capability
- Your test devices (for development)
- Correct App ID

Download and install the provisioning profile in Xcode.

### 4. Entitlements Configuration

**Platforms/iOS/Entitlements.plist**:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>aps-environment</key>
    <string>development</string> <!-- or "production" -->
    
    <key>com.apple.developer.usernotifications.filtering</key>
    <true/>
</dict>
</plist>
```

**aps-environment values**:
- `development`: For testing with development certificates
- `production`: For App Store builds

## Firebase Project Setup for iOS

### 1. Add iOS App to Firebase Project
1. Go to Firebase Console
2. Click "Add app" > iOS icon
3. Enter iOS bundle ID: Must match your app's bundle identifier
4. Download `GoogleService-Info.plist`
5. Place in `MauiPwaShell/Platforms/iOS/`

### 2. Configure Bundle ID

Ensure bundle ID matches in:
- **Firebase Console**: App settings
- **GoogleService-Info.plist**: `BUNDLE_ID` key
- **Info.plist**: `CFBundleIdentifier` (usually `$(PRODUCT_BUNDLE_IDENTIFIER)`)
- **MauiPwaShell.csproj**: `<ApplicationId>com.example.mauipwashell</ApplicationId>`

### 3. Upload APNs Authentication

Upload your APNs key or certificate to Firebase Console as described in step 2 above.

## Handling Token Changes
**Location**: `MauiPwaShell/MauiProgram.cs` - Lines 33-39

```csharp
CrossFirebaseCloudMessaging.Current.OnTokenChanged += async (s, newToken) =>
{
    Console.WriteLine($"[MAUI] FCM token changed: {newToken}");
    await SendTokenToServer(newToken);
    if (Application.Current?.MainPage is MainPage mp)
        await mp.InjectTokenIntoWebAsync(newToken);
};
```

Token can change when:
- App is reinstalled
- User restores device from backup
- Firebase refreshes token for security
- APNs token changes

## Receiving Push Notifications

### Foreground Notifications
**Location**: `Platforms/iOS/AppDelegate.cs`

```csharp
CrossFirebaseCloudMessaging.Current.OnNotificationReceived += (s, e) =>
{
    System.Console.WriteLine($"[iOS] Notification received: {e.Notification.Title}");
    
    // Custom handling - show in-app alert, update UI, etc.
};
```

### Background Notifications

iOS handles background notifications automatically when:
1. App is in background or killed
2. Notification arrives via APNs
3. iOS displays the notification
4. User taps notification → app launches

**Handle notification launch**:
```csharp
public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
{
    if (launchOptions != null)
    {
        if (launchOptions.ContainsKey(UIApplication.LaunchOptionsRemoteNotificationKey))
        {
            var notification = launchOptions[UIApplication.LaunchOptionsRemoteNotificationKey] as NSDictionary;
            // Handle notification data
        }
    }
    
    return base.FinishedLaunching(application, launchOptions);
}
```

### Silent Notifications (Background Fetch)

For silent background updates:

**Notification Payload**:
```json
{
  "aps": {
    "content-available": 1
  },
  "data": {
    "custom_key": "custom_value"
  }
}
```

**Enable Background Modes**: `Info.plist`
```xml
<key>UIBackgroundModes</key>
<array>
    <string>remote-notification</string>
    <string>fetch</string>
</array>
```

## Sending Push Notifications to iOS

### Method 1: Firebase Console
Same as Android - Firebase Console can send to both platforms simultaneously.

### Method 2: Firebase Admin SDK (Server)
```csharp
var message = new Message
{
    Token = fcmToken, // iOS FCM token
    Notification = new Notification
    {
        Title = "Hello iOS",
        Body = "This is a test notification"
    },
    Apns = new ApnsConfig
    {
        Aps = new Aps
        {
            Alert = new ApsAlert
            {
                Title = "Hello iOS",
                Body = "This is a test notification"
            },
            Badge = 1,
            Sound = "default"
        },
        Headers = new Dictionary<string, string>
        {
            { "apns-priority", "10" },
            { "apns-push-type", "alert" }
        }
    }
};

var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
```

### Method 3: Direct APNs (Advanced)
For direct APNs integration without FCM:

```bash
curl -v \
  -d '{"aps":{"alert":"Hello","sound":"default"}}' \
  -H "apns-topic: com.example.mauipwashell" \
  -H "apns-priority: 10" \
  -H "authorization: bearer $JWT_TOKEN" \
  --http2 \
  https://api.sandbox.push.apple.com/3/device/DEVICE_TOKEN
```

## Push Notification Flow (Sending)

```
┌────────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌─────────┐
│Your Server │      │   FCM    │      │   APNs   │      │  iOS OS  │      │ MAUI App│
└─────┬──────┘      └─────┬────┘      └─────┬────┘      └─────┬────┘      └────┬────┘
      │                   │                  │                 │                │
      │ 1. Send push with │                  │                 │                │
      │    FCM token      │                  │                 │                │
      │───────────────────>                  │                 │                │
      │                   │                  │                 │                │
      │                   │ 2. Lookup APNs   │                 │                │
      │                   │    token mapping │                 │                │
      │                   │──────────┐       │                 │                │
      │                   │          │       │                 │                │
      │                   │<─────────┘       │                 │                │
      │                   │                  │                 │                │
      │                   │ 3. Forward to    │                 │                │
      │                   │    APNs with     │                 │                │
      │                   │    APNs token    │                 │                │
      │                   │──────────────────>                 │                │
      │                   │                  │                 │                │
      │                   │                  │ 4. Validate     │                │
      │                   │                  │    certificate  │                │
      │                   │                  │────────┐        │                │
      │                   │                  │        │        │                │
      │                   │                  │<───────┘        │                │
      │                   │                  │                 │                │
      │                   │                  │ 5. Route to     │                │
      │                   │                  │    device       │                │
      │                   │                  │─────────────────>                │
      │                   │                  │                 │                │
      │                   │                  │                 │ 6. Deliver to  │
      │                   │                  │                 │    app         │
      │                   │                  │                 │────────────────>
      │                   │                  │                 │                │
      │                   │                  │ 7. Acknowledge  │                │
      │                   │                  │<─────────────────                │
      │                   │                  │                 │                │
      │                   │ 8. Success       │                 │                │
      │                   │<──────────────────                 │                │
      │                   │                  │                 │                │
      │ 9. Return result  │                  │                 │                │
      │<───────────────────                  │                 │                │
      │                   │                  │                 │                │
```

## Notification Payload Structure

### Basic Notification
```json
{
  "message": {
    "token": "FCM_TOKEN",
    "notification": {
      "title": "Hello iOS",
      "body": "This is a notification"
    },
    "apns": {
      "payload": {
        "aps": {
          "alert": {
            "title": "Hello iOS",
            "body": "This is a notification"
          },
          "badge": 1,
          "sound": "default"
        }
      }
    }
  }
}
```

### Rich Notification with Image
```json
{
  "message": {
    "token": "FCM_TOKEN",
    "notification": {
      "title": "Photo Update",
      "body": "Check out this photo!"
    },
    "apns": {
      "payload": {
        "aps": {
          "alert": {
            "title": "Photo Update",
            "body": "Check out this photo!"
          },
          "mutable-content": 1
        }
      },
      "fcm_options": {
        "image": "https://example.com/image.jpg"
      }
    }
  }
}
```

### Silent Background Notification
```json
{
  "message": {
    "token": "FCM_TOKEN",
    "apns": {
      "payload": {
        "aps": {
          "content-available": 1
        }
      }
    },
    "data": {
      "update_type": "silent_refresh",
      "data_id": "12345"
    }
  }
}
```

## Common Issues and Solutions

### Issue 1: "Unable to retrieve APNs token"
**Symptoms**: Token is null, permission granted but no token

**Solutions**:
- Verify Push Notifications capability is enabled in App ID
- Check provisioning profile includes Push Notifications
- Ensure `aps-environment` is set in Entitlements.plist
- Test on physical device (simulator has limitations)
- Check Xcode automatic signing is working

### Issue 2: "Failed to send push notification"
**Symptoms**: FCM returns error when sending

**Solutions**:
- Verify APNs key/certificate is uploaded to Firebase
- Check `aps-environment` matches certificate type (dev/prod)
- Ensure bundle ID matches across all configurations
- Verify token hasn't expired or been invalidated

### Issue 3: Notifications not received
**Symptoms**: Token registered but notifications don't arrive

**Solutions**:
- Check device has internet connection
- Verify device notifications are enabled in Settings
- Test with Firebase Console test message first
- Check APNs certificate is valid and not expired
- Ensure app is not in Do Not Disturb mode
- Verify Background App Refresh is enabled

### Issue 4: Certificate signing errors
**Symptoms**: Build fails with provisioning profile errors

**Solutions**:
- Clean build folder (Product > Clean Build Folder in Xcode)
- Delete derived data
- Re-download provisioning profile
- Check bundle ID matches exactly
- Ensure development team is selected

### Issue 5: "GoogleService-Info.plist not found"
**Symptoms**: Firebase initialization fails

**Solutions**:
- Verify file is in `Platforms/iOS/` folder
- Check file is marked as `BundleResource` in properties
- Ensure file name is exactly `GoogleService-Info.plist`
- Clean and rebuild project

## Testing on iOS

### iOS Simulator Testing
**Limitations**:
- Cannot receive remote notifications (APNs tokens not issued)
- Can test UI and local notifications
- Use for development of UI flow only

**For full testing, use physical device**

### Physical Device Testing

**Prerequisites**:
1. Apple Developer Account
2. Registered device in Apple Developer Portal
3. Development provisioning profile
4. Valid code signing certificate

**Setup**:
1. Connect device via USB
2. Trust device in Xcode
3. Select device as target
4. Build and deploy

### Testing Checklist
- [ ] App builds without errors
- [ ] App launches on device
- [ ] Firebase initializes successfully
- [ ] Permission dialog appears
- [ ] User grants permission
- [ ] APNs token is retrieved
- [ ] FCM token is generated
- [ ] Token is sent to server
- [ ] Token appears in server logs
- [ ] Test notification sent from Firebase Console
- [ ] Notification received in foreground
- [ ] Notification received in background
- [ ] Notification received when app is killed
- [ ] Tapping notification opens app
- [ ] Token injected into WebView
- [ ] Badge count updates
- [ ] Sound plays

## iOS Notification Best Practices

### 1. Permission Strategy
- Don't ask immediately on first launch
- Explain value proposition first
- Use provisional authorization (iOS 12+) for tentative notifications
- Respect user's choice if denied

### 2. Notification Content
- Keep titles under 40 characters
- Keep bodies under 120 characters (longer truncates)
- Use relevant badges (show unread count)
- Choose appropriate sounds
- Include relevant images for rich notifications

### 3. Timing and Frequency
- Don't send notifications too frequently
- Respect time zones
- Consider "Quiet Hours" (evening/night)
- Allow users to customize frequency

### 4. User Experience
- Provide notification settings in-app
- Allow users to opt-out of specific categories
- Show value before requesting permission
- Test thoroughly on different iOS versions

### 5. Performance
- Use silent notifications sparingly (limited budget)
- Implement efficient background processing
- Handle token refresh properly
- Clean up old tokens from server

## iOS Version Compatibility

| iOS Version | Features | Notes |
|------------|----------|-------|
| iOS 17 | Latest features | Live Activities, Focus filters |
| iOS 16 | Focus modes enhanced | Customizable lock screen |
| iOS 15 | Notification summary | Interruption levels |
| iOS 14 | App Clips | Better notification management |
| iOS 13 | Dark mode | Better notification UI |
| iOS 12 | Provisional auth | Tentative notifications |
| iOS 11 | Basic support | Modern notification framework |
| iOS 10 | Rich notifications | Images, videos, actions |

**Minimum Supported**: iOS 11.0 as per MAUI requirements

## APNs vs FCM Comparison

| Aspect | APNs (Direct) | FCM (via Firebase) |
|--------|---------------|-------------------|
| Token Management | APNs device token only | FCM token + APNs token |
| Server Integration | Complex (HTTP/2, JWT) | Simple (Firebase SDK) |
| Multi-platform | iOS only | iOS + Android + Web |
| Certificate Management | Required | Handled by Firebase |
| Token Refresh | Manual handling | Automatic |
| Analytics | Manual | Built-in Firebase Analytics |
| Testing | Complex | Firebase Console |
| Cost | Free (via Apple) | Free (Firebase tier) |

**Recommendation**: Use FCM for multi-platform apps, direct APNs for iOS-only apps with specific requirements.

## Security Considerations

### 1. APNs Key Security
- Store `.p8` key file securely (never commit to repo)
- Limit access to key file
- Rotate keys periodically
- Use different keys for dev/staging/prod

### 2. Certificate Management
- Never commit certificates to version control
- Use CI/CD secrets for automated builds
- Set expiration reminders
- Revoke compromised certificates immediately

### 3. Token Protection
- Encrypt tokens in transit (HTTPS)
- Store tokens securely on server
- Associate tokens with authenticated users
- Implement token validation

### 4. Notification Content
- Don't include sensitive data in notification body
- Use notification actions to fetch secure data
- Implement proper authentication in notification handlers
- Validate all notification data before processing

## Debugging Tips

### Enable Detailed Logging
```csharp
#if DEBUG
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
#endif
```

### Check APNs Response
Monitor Xcode console for:
```
[Firebase/Messaging][I-FCM001000] APNs device token received: <740f4707...>
[Firebase/Messaging][I-FCM001001] FCM token: fE8xB3...
```

### Test APNs Connectivity
```bash
# Test APNs sandbox (development)
openssl s_client -connect api.sandbox.push.apple.com:443

# Test APNs production
openssl s_client -connect api.push.apple.com:443
```

### Verify Certificate
```bash
# Check certificate expiration
openssl x509 -in certificate.pem -noout -dates

# Verify certificate with Apple
openssl s_client -connect api.push.apple.com:443 -cert certificate.pem -key key.pem
```

## References

- [Apple Push Notification Service](https://developer.apple.com/documentation/usernotifications)
- [Firebase Cloud Messaging for iOS](https://firebase.google.com/docs/cloud-messaging/ios/client)
- [APNs Provider API](https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server)
- [Plugin.Firebase Documentation](https://github.com/TobiasBuchholz/Plugin.Firebase)
- [.NET MAUI iOS Documentation](https://learn.microsoft.com/en-us/dotnet/maui/ios/)
- [UserNotifications Framework](https://developer.apple.com/documentation/usernotifications)
