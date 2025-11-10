# Android Push Notifications Registration Flow (FCM)

This document describes the complete push notification registration flow for the **Android** platform using Firebase Cloud Messaging (FCM) in a .NET MAUI application.

## Overview

Android push notifications use Firebase Cloud Messaging (FCM) for delivering messages. The flow involves:
1. Firebase initialization
2. Permission request (Android 13+)
3. FCM token retrieval
4. Token registration with the backend server
5. Token injection into the WebView

## Architecture Components

### Android Components
- **.NET MAUI App**: Cross-platform mobile application framework
- **Plugin.Firebase**: NuGet package for Firebase integration
- **Firebase SDK**: Native Android Firebase libraries
- **Google Play Services**: Required for FCM functionality

### Backend Components
- **ASP.NET Core Server**: Backend API server
- **RegisterPushController**: Endpoint for token registration
- **Firebase Console**: For sending test notifications and managing the project

### Configuration Files
- **google-services.json**: Firebase project configuration
- **AndroidManifest.xml**: App permissions and metadata
- **MauiProgram.cs**: Firebase initialization code

## Detailed Registration Flow

```
┌────────────────┐      ┌─────────────────┐      ┌──────────────┐      ┌───────────────┐
│   MAUI App     │      │  Firebase SDK   │      │ FCM Service  │      │ ASP.NET Server│
│   (Android)    │      │    (Native)     │      │  (Google)    │      │   (Backend)   │
└────────┬───────┘      └────────┬────────┘      └──────┬───────┘      └───────┬───────┘
         │                       │                       │                      │
         │ 1. App launches       │                       │                      │
         │───────────────────┐   │                       │                      │
         │                   │   │                       │                      │
         │<──────────────────┘   │                       │                      │
         │                       │                       │                      │
         │ 2. Initialize Firebase│                       │                      │
         │   (MauiProgram.cs)    │                       │                      │
         │───────────────────────>                       │                      │
         │                       │                       │                      │
         │                       │ 3. Load google-       │                      │
         │                       │    services.json      │                      │
         │                       │───────────┐           │                      │
         │                       │           │           │                      │
         │                       │<──────────┘           │                      │
         │                       │                       │                      │
         │ 4. Request permission │                       │                      │
         │   (Android 13+)       │                       │                      │
         │───────────────────────>                       │                      │
         │                       │                       │                      │
         │ 5. User grants        │                       │                      │
         │<───────────────────────                       │                      │
         │                       │                       │                      │
         │ 6. Check if valid     │                       │                      │
         │───────────────────────>                       │                      │
         │                       │                       │                      │
         │ 7. Valid response     │                       │                      │
         │<───────────────────────                       │                      │
         │                       │                       │                      │
         │ 8. Get FCM token      │                       │                      │
         │───────────────────────>                       │                      │
         │                       │                       │                      │
         │                       │ 9. Request token      │                      │
         │                       │       from FCM        │                      │
         │                       │───────────────────────>                      │
         │                       │                       │                      │
         │                       │ 10. Generate token    │                      │
         │                       │       (device ID +    │                      │
         │                       │        app ID)        │                      │
         │                       │<───────────────────────                      │
         │                       │                       │                      │
         │ 11. Return token      │                       │                      │
         │<───────────────────────                       │                      │
         │                       │                       │                      │
         │ 12. POST token to     │                       │                      │
         │     server (10.0.2.2) │                       │                      │
         │───────────────────────────────────────────────────────────────────>  │
         │                       │                       │                      │
         │                       │                       │                      │ 13. Store token
         │                       │                       │                      │─────────┐
         │                       │                       │                      │         │
         │                       │                       │                      │<────────┘
         │                       │                       │                      │
         │ 14. Success response  │                       │                      │
         │<───────────────────────────────────────────────────────────────────  │
         │                       │                       │                      │
         │ 15. Inject token      │                       │                      │
         │     into WebView      │                       │                      │
         │───────────┐           │                       │                      │
         │           │           │                       │                      │
         │<──────────┘           │                       │                      │
         │                       │                       │                      │
         │ 16. WebView can now   │                       │                      │
         │     access token via  │                       │                      │
         │     localStorage      │                       │                      │
         │───────────┐           │                       │                      │
         │           │           │                       │                      │
         │<──────────┘           │                       │                      │
         │                       │                       │                      │
```

## Step-by-Step Process

### Step 1: App Launch
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

When the MAUI application launches, it builds the app and immediately calls `InitializeFirebase()`.

### Step 2-3: Initialize Firebase
**Location**: `MauiPwaShell/MauiProgram.cs` - Lines 23-52

```csharp
private static async void InitializeFirebase()
{
    try
    {
        await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
        await CrossFirebaseCloudMessaging.Current.RequestPermissionAsync();

        var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
        Console.WriteLine($"[MAUI] Initial FCM token: {token}");
        
        // Register for token changes
        CrossFirebaseCloudMessaging.Current.OnTokenChanged += async (s, newToken) =>
        {
            Console.WriteLine($"[MAUI] FCM token changed: {newToken}");
            await SendTokenToServer(newToken);
        };

        if (!string.IsNullOrEmpty(token))
        {
            await SendTokenToServer(token);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MAUI] Firebase initialization error: {ex.Message}");
    }
}
```

**Firebase Configuration**: `Platforms/Android/google-services.json`

This file contains your Firebase project configuration from the Firebase Console. It includes:
- **project_info**: Project ID and name
- **client**: App package name and OAuth credentials
- **api_key**: Firebase API key
- **fcm_sender_id**: Used for FCM registration

**Example Structure**:
```json
{
  "project_info": {
    "project_number": "123456789",
    "project_id": "your-project-id",
    "storage_bucket": "your-project.appspot.com"
  },
  "client": [
    {
      "client_info": {
        "mobilesdk_app_id": "1:123456789:android:abc123...",
        "android_client_info": {
          "package_name": "com.example.mauipwashell"
        }
      },
      "api_key": [
        {
          "current_key": "AIzaSy..."
        }
      ]
    }
  ]
}
```

### Step 4-5: Request Notification Permission (Android 13+)
**Location**: Plugin.Firebase handles this internally

For Android 13 (API level 33) and above, runtime permission is required:

**Manifest Permission**: `Platforms/Android/AndroidManifest.xml`

```xml
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<uses-permission android:name="android.permission.INTERNET" />
```

The `RequestPermissionAsync()` call shows a system dialog asking the user to allow notifications.

**Permission States**:
- **Granted**: User allowed notifications
- **Denied**: User blocked notifications
- **Not Determined**: User hasn't decided (Android < 13 defaults to granted)

### Step 6-7: Validate Firebase Setup
```csharp
await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
```

This verifies:
- Google Play Services is available
- Firebase is properly configured
- The app can communicate with FCM

### Step 8-11: Retrieve FCM Token
```csharp
var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
```

The FCM token is a unique identifier for this app installation on this device. It's generated by combining:
- Device identifier
- App package name
- Firebase sender ID

**Token Format**: Typically 152+ characters, e.g.:
```
fE8xB3...long_string...zKpQ:APA91b...even_longer_string
```

**Token Characteristics**:
- Unique per app installation
- Can change if app is reinstalled
- Can be refreshed by Firebase
- Required for sending push notifications to this device

### Step 12-14: Register Token with Backend
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
            Platform = DeviceInfo.Platform.ToString()
        };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Use 10.0.2.2 for Android emulator to reach host machine's localhost
        var serverUrl = "http://10.0.2.2:5000/api/registerpush";
        var response = await client.PostAsync(serverUrl, content);

        Console.WriteLine($"[MAUI] Sent FCM token to server: {response.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MAUI] Failed to send token to server: {ex.Message}");
    }
}
```

**Important Network Notes**:
- **Android Emulator**: Use `10.0.2.2` to access host machine's `localhost`
- **Physical Device**: Use your computer's IP address (e.g., `192.168.1.100:5000`)
- **Production**: Use your actual server URL (e.g., `https://api.example.com`)

**Backend Endpoint**: Same as web push registration
The server stores the FCM token alongside (or instead of) web push subscriptions.

### Step 15-16: Inject Token into WebView
**Location**: `MauiPwaShell/MainPage.xaml.cs` - Lines 30-57

```csharp
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
```

This JavaScript injection makes the token available to the PWA in three ways:
1. **window.nativeFcmToken**: Global variable
2. **localStorage.nativeFcmToken**: Persistent storage
3. **nativeTokenReady event**: Custom event for real-time notification

The PWA can then use this token for analytics, user identification, or server-side targeting.

## Handling Token Changes

FCM tokens can change in several scenarios:
- App is restored on a new device
- User clears app data
- Firebase automatically refreshes token for security

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

This event handler ensures the server and WebView always have the latest token.

## Receiving Push Notifications

### Foreground Notifications
**Location**: `Platforms/Android/MainActivity.cs` - Lines 14-17

```csharp
CrossFirebaseCloudMessaging.Current.OnNotificationReceived += (s, e) =>
{
    System.Console.WriteLine($"[Android] Notification received: {e.Notification.Title}");
};
```

When the app is running (foreground), this event fires. You can:
- Display an in-app notification
- Update the UI
- Play a sound
- Show a dialog

### Background/Killed State Notifications
When the app is in the background or killed:
1. FCM receives the message
2. Android system displays the notification automatically
3. Tapping the notification launches the app
4. The notification data is available in the launch intent

## Sending Push Notifications to Android

### Method 1: Firebase Console
1. Go to Firebase Console > Cloud Messaging
2. Click "Send your first message"
3. Enter notification title and body
4. Select the app
5. Paste the FCM token in "Send test message"
6. Click "Test"

### Method 2: Firebase Admin SDK (Server)
```csharp
// Using FirebaseAdmin NuGet package
var message = new Message
{
    Token = fcmToken,
    Notification = new Notification
    {
        Title = "Hello from Server",
        Body = "This is a test notification"
    },
    Android = new AndroidConfig
    {
        Priority = Priority.High,
        Notification = new AndroidNotification
        {
            Icon = "notification_icon",
            Color = "#4CAF50"
        }
    }
};

var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
```

### Method 3: HTTP API (Direct FCM Call)
```bash
curl -X POST https://fcm.googleapis.com/v1/projects/YOUR_PROJECT_ID/messages:send \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "message": {
      "token": "FCM_TOKEN_HERE",
      "notification": {
        "title": "Test Notification",
        "body": "Hello from curl"
      }
    }
  }'
```

## Push Notification Flow (Sending)

```
┌────────────────┐         ┌──────────────┐         ┌──────────────┐         ┌────────────┐
│  Your Server   │         │ FCM Service  │         │ Android OS   │         │  MAUI App  │
└───────┬────────┘         └──────┬───────┘         └──────┬───────┘         └─────┬──────┘
        │                         │                        │                       │
        │ 1. Send notification    │                        │                       │
        │    with FCM token       │                        │                       │
        │─────────────────────────>                        │                       │
        │                         │                        │                       │
        │                         │ 2. Validate token      │                       │
        │                         │────────────┐           │                       │
        │                         │            │           │                       │
        │                         │<───────────┘           │                       │
        │                         │                        │                       │
        │                         │ 3. Route to device     │                       │
        │                         │────────────────────────>                       │
        │                         │                        │                       │
        │                         │                        │ 4. Wake up app        │
        │                         │                        │       (if sleeping)   │
        │                         │                        │───────────────────────>
        │                         │                        │                       │
        │                         │                        │ 5. Deliver message    │
        │                         │                        │       (via broadcast) │
        │                         │                        │───────────────────────>
        │                         │                        │                       │
        │                         │                        │                       │ 6. Handle
        │                         │                        │                       │    notification
        │                         │                        │                       │────────┐
        │                         │                        │                       │        │
        │                         │                        │                       │<───────┘
        │                         │                        │                       │
        │ 7. Return success       │                        │                       │
        │<─────────────────────────                        │                       │
        │                         │                        │                       │
```

## Firebase Project Setup

### 1. Create Firebase Project
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click "Add project"
3. Enter project name
4. Enable Google Analytics (optional)
5. Create project

### 2. Add Android App
1. Click "Add app" > Android icon
2. Enter Android package name: `com.example.mauipwashell` (must match `MauiPwaShell.csproj`)
3. Download `google-services.json`
4. Place in `MauiPwaShell/Platforms/Android/`

### 3. Configure Package Name
Ensure package name matches in:
- **Firebase Console**: App settings
- **google-services.json**: `client.client_info.android_client_info.package_name`
- **MauiPwaShell.csproj**: `<ApplicationId>com.example.mauipwashell</ApplicationId>`
- **AndroidManifest.xml**: `package="com.example.mauipwashell"`

## Android Manifest Configuration

**Location**: `Platforms/Android/AndroidManifest.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application android:allowBackup="true" 
                 android:icon="@mipmap/appicon" 
                 android:roundIcon="@mipmap/appicon_round"
                 android:supportsRtl="true">
    </application>
    
    <!-- Required permissions -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
    <uses-permission android:name="com.google.android.c2dm.permission.RECEIVE" />
    
    <!-- Wake lock for background notifications -->
    <uses-permission android:name="android.permission.WAKE_LOCK" />
</manifest>
```

## Security Considerations

### 1. Secure Token Storage
- Never expose FCM tokens in client-side code
- Store tokens securely on the server
- Implement token validation before sending notifications

### 2. Authentication
- Verify user identity before storing tokens
- Associate tokens with authenticated user accounts
- Implement token refresh mechanisms

### 3. Google Services Security
- Keep `google-services.json` private (don't commit to public repos)
- Use different Firebase projects for dev/staging/production
- Rotate Firebase API keys regularly

### 4. Notification Content
- Don't send sensitive data in notification payloads
- Use data messages to trigger secure data fetching
- Implement end-to-end encryption for sensitive notifications

## Common Issues and Solutions

### Issue 1: "Google Play Services not available"
**Symptoms**: `CheckIfValidAsync()` fails

**Solutions**:
- Update Google Play Services on device/emulator
- Use an emulator with Google APIs (not AOSP)
- For physical device, ensure Play Store is installed

### Issue 2: Token is null or empty
**Symptoms**: `GetTokenAsync()` returns null

**Solutions**:
- Check `google-services.json` is in correct location
- Verify package name matches across all configurations
- Ensure internet permission is granted
- Check Firebase project is active

### Issue 3: "Unable to reach server at 10.0.2.2"
**Symptoms**: HTTP POST to server fails

**Solutions**:
- **Emulator**: Use `10.0.2.2` for localhost
- **Physical Device**: Use computer's IP (e.g., `192.168.1.100`)
- Ensure backend server is running
- Check firewall allows incoming connections

### Issue 4: Notifications not received
**Symptoms**: Token registered but no notifications arrive

**Solutions**:
- Verify token is correctly stored on server
- Check FCM project credentials
- Ensure notification priority is set to "high"
- Check device is not in battery saver/doze mode
- Verify app is not force-stopped

### Issue 5: Permission denied (Android 13+)
**Symptoms**: User denies notification permission

**Solutions**:
- Explain why notifications are needed before requesting
- Provide in-app settings to re-request permission
- Handle denial gracefully without breaking app functionality

## Testing on Android

### Emulator Setup
1. Create AVD with Google APIs (not AOSP)
2. Ensure Play Store is available
3. Sign in with Google account
4. Update Google Play Services

### Physical Device Setup
1. Enable Developer Options
2. Enable USB Debugging
3. Connect via USB or WiFi debugging
4. Install via Visual Studio or `dotnet build -t:Run`

### Testing Checklist
- [ ] App builds without errors
- [ ] Firebase initializes successfully
- [ ] Permission dialog appears (Android 13+)
- [ ] FCM token is retrieved and logged
- [ ] Token is sent to server successfully
- [ ] Token appears in server logs/database
- [ ] Test notification arrives when sent from Firebase Console
- [ ] Notification displays correctly in foreground
- [ ] Notification displays correctly in background
- [ ] Tapping notification opens the app
- [ ] Token is injected into WebView

## Best Practices

### 1. Token Management
- Store tokens with user identifiers
- Implement token cleanup for uninstalled apps
- Handle token refresh events properly
- Remove tokens when user logs out

### 2. Notification Design
- Keep titles under 40 characters
- Keep body text under 120 characters
- Use clear, actionable language
- Include relevant icons and images
- Test on different Android versions

### 3. Performance
- Don't block UI thread during initialization
- Cache token locally to avoid repeated requests
- Batch server updates when possible
- Handle errors gracefully without crashing

### 4. User Experience
- Request permission at appropriate times
- Explain value of notifications before requesting
- Provide notification settings in-app
- Allow users to customize notification preferences
- Respect user's notification settings

## Android Version Compatibility

| Android Version | API Level | FCM Support | Notes |
|----------------|-----------|-------------|-------|
| Android 14 | 34 | Full | Latest features |
| Android 13 | 33 | Full | Runtime permission required |
| Android 12 | 31-32 | Full | Material You support |
| Android 11 | 30 | Full | Conversation bubbles |
| Android 10 | 29 | Full | Dark mode support |
| Android 9 | 28 | Full | Display cutout support |
| Android 8 | 26-27 | Full | Notification channels required |
| Android 7 | 24-25 | Full | Multi-window support |
| Android 6 | 23 | Full | Runtime permissions introduced |
| Android 5 | 21-22 | Full | Material Design |

**Minimum Supported**: Android 5.0 (API 21) as per MAUI requirements

## References

- [Firebase Cloud Messaging Documentation](https://firebase.google.com/docs/cloud-messaging)
- [Plugin.Firebase GitHub](https://github.com/TobiasBuchholz/Plugin.Firebase)
- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [Android Notifications Guide](https://developer.android.com/develop/ui/views/notifications)
- [FCM HTTP v1 API](https://firebase.google.com/docs/reference/fcm/rest/v1/projects.messages)
