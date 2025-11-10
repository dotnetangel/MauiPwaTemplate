# Push Notifications Registration Flows - Overview

This document provides a high-level overview of push notification registration across all supported platforms in the MauiPwaTemplate solution.

## Supported Platforms

The MauiPwaTemplate supports push notifications on three platforms:

1. **Web (Browser)** - Using VAPID (Voluntary Application Server Identification)
2. **Android** - Using Firebase Cloud Messaging (FCM)
3. **iOS** - Using FCM with Apple Push Notification service (APNs)

## Platform-Specific Documentation

For detailed platform-specific flows, see:
- [Web Push Notifications (VAPID)](./push-notifications-web.md)
- [Android Push Notifications (FCM)](./push-notifications-android.md)
- [iOS Push Notifications (FCM + APNs)](./push-notifications-ios.md)

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Push Notification System                         │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                ┌───────────────────┼───────────────────┐
                │                   │                   │
                ▼                   ▼                   ▼
        ┌───────────┐       ┌───────────┐      ┌───────────┐
        │    Web    │       │  Android  │      │    iOS    │
        │ (Browser) │       │   (FCM)   │      │ (FCM+APNs)│
        └─────┬─────┘       └─────┬─────┘      └─────┬─────┘
              │                   │                   │
              │                   │                   │
        ┌─────▼─────┐       ┌─────▼─────┐      ┌─────▼─────┐
        │  Service  │       │ Firebase  │      │  Firebase │
        │  Worker   │       │    SDK    │      │    SDK    │
        └─────┬─────┘       └─────┬─────┘      └─────┬─────┘
              │                   │                   │
        ┌─────▼─────┐             │              ┌───▼────┐
        │  Browser  │             │              │  APNs  │
        │   Push    │             │              │ (Apple)│
        │  Service  │             │              └───┬────┘
        └─────┬─────┘             │                  │
              │                   ▼                  │
              │             ┌───────────┐            │
              │             │    FCM    │            │
              │             │  Service  │◄───────────┘
              │             │ (Google)  │
              │             └─────┬─────┘
              │                   │
              └───────────┬───────┘
                          │
                          ▼
                ┌──────────────────┐
                │  ASP.NET Server  │
                │    (Backend)     │
                └──────────────────┘
```

## Registration Flow Comparison

### High-Level Steps Across Platforms

| Step | Web (VAPID) | Android (FCM) | iOS (FCM + APNs) |
|------|-------------|---------------|------------------|
| **1. Initialize** | Service Worker | Firebase SDK | Firebase SDK |
| **2. Request Permission** | Browser dialog | System dialog (Android 13+) | iOS system dialog |
| **3. Generate Token** | Browser Push Service | FCM service | APNs → FCM |
| **4. Register with Server** | POST subscription | POST FCM token | POST FCM token |
| **5. Store on Server** | Store subscription | Store token | Store token |
| **6. Confirm** | Update UI | Inject into WebView | Inject into WebView |

### Detailed Comparison

#### 1. Permission Model

**Web (VAPID)**:
- User-initiated (button click recommended)
- Three states: granted, denied, default
- Can be revoked in browser settings
- Requires HTTPS (except localhost)

**Android (FCM)**:
- Automatic on Android < 13
- Runtime permission on Android 13+
- Can be revoked in system settings
- Background restrictions can affect delivery

**iOS (FCM + APNs)**:
- Always requires user permission
- Detailed permission options (alerts, sounds, badges)
- Can be revoked in system settings
- Do Not Disturb affects delivery

#### 2. Token/Subscription Format

**Web (VAPID)**:
```json
{
  "endpoint": "https://fcm.googleapis.com/fcm/send/...",
  "expirationTime": null,
  "keys": {
    "p256dh": "BL8...",
    "auth": "YKD..."
  }
}
```

**Android (FCM)**:
```
fE8xB3...152+ characters...zKpQ
```

**iOS (FCM)**:
```
fE8xB3...152+ characters...zKpQ
(FCM token, internally mapped to APNs token)
```

#### 3. Background Handling

**Web (VAPID)**:
- Service Worker runs in background
- Can show notifications when browser is closed
- Limited by browser (Chrome must be running)
- Event-driven architecture

**Android (FCM)**:
- System-level delivery
- Works when app is closed
- Handles notifications automatically
- Battery optimization can affect delivery

**iOS (FCM + APNs)**:
- System-level delivery via APNs
- Works when app is closed
- iOS handles display automatically
- Always reliable when connected

## Common Backend Integration

All platforms register with the same backend endpoint:

### Registration Endpoint
```
POST /api/registerpush
Content-Type: application/json

{
  "endpoint": "...",      // Web only
  "keys": {...},          // Web only
  "expirationTime": null, // Web only
  "Token": "...",         // Mobile only
  "Platform": "..."       // Mobile only
}
```

### Server-Side Storage

**Web Subscriptions**:
```csharp
public class PushSubscription
{
    public string Endpoint { get; set; }
    public string? ExpirationTime { get; set; }
    public PushSubscriptionKeys Keys { get; set; }
}
```

**Mobile Tokens**:
```csharp
public class DeviceToken
{
    public string Token { get; set; }
    public string Platform { get; set; } // "Android" or "iOS"
    public DateTime RegisteredAt { get; set; }
}
```

## Sending Notifications

### Web (VAPID)

**Method**: Direct Web Push API
```csharp
var notification = new PushMessage(JsonSerializer.Serialize(new
{
    title = "Hello Web",
    body = "This is a web push notification"
}));

await _pushService.SendNotificationAsync(subscription, notification);
```

**Protocol**: Web Push Protocol with VAPID authentication

### Android (FCM)

**Method**: Firebase Admin SDK
```csharp
var message = new Message
{
    Token = androidToken,
    Notification = new Notification
    {
        Title = "Hello Android",
        Body = "This is an Android notification"
    },
    Android = new AndroidConfig
    {
        Priority = Priority.High
    }
};

await FirebaseMessaging.DefaultInstance.SendAsync(message);
```

**Protocol**: FCM HTTP v1 API

### iOS (FCM + APNs)

**Method**: Firebase Admin SDK with APNs config
```csharp
var message = new Message
{
    Token = iosToken,
    Notification = new Notification
    {
        Title = "Hello iOS",
        Body = "This is an iOS notification"
    },
    Apns = new ApnsConfig
    {
        Aps = new Aps
        {
            Alert = new ApsAlert
            {
                Title = "Hello iOS",
                Body = "This is an iOS notification"
            },
            Badge = 1,
            Sound = "default"
        }
    }
};

await FirebaseMessaging.DefaultInstance.SendAsync(message);
```

**Protocol**: FCM → APNs bridge

## Unified Multi-Platform Flow

```
┌──────────────┐
│   User App   │
│  (Any Device)│
└──────┬───────┘
       │ 1. Register
       │    for push
       ▼
┌──────────────────────────────────┐
│   Platform-Specific Handler      │
├──────────────┬──────────┬────────┤
│     Web      │ Android  │  iOS   │
│   (VAPID)    │  (FCM)   │ (APNs) │
└──────┬───────┴────┬─────┴───┬────┘
       │            │         │
       │ 2. Get     │         │
       │    token/  │         │
       │    subscription       │
       │            │         │
       └────────────┴─────────┘
                    │
       ┌────────────▼───────────┐
       │  POST /api/registerpush│
       │   (Backend Server)     │
       └────────────┬───────────┘
                    │
       ┌────────────▼───────────┐
       │   Store in Database    │
       │  (or in-memory cache)  │
       └────────────┬───────────┘
                    │
       ┌────────────▼───────────┐
       │  Return success        │
       │  Notification ready    │
       └────────────────────────┘
```

## Setup Requirements by Platform

### Web Setup
- [ ] Generate VAPID keys
- [ ] Configure `appsettings.json` with VAPID keys
- [ ] Serve over HTTPS (production)
- [ ] Implement service worker
- [ ] Handle permission request

### Android Setup
- [ ] Create Firebase project
- [ ] Add Android app to Firebase
- [ ] Download `google-services.json`
- [ ] Configure package name
- [ ] Add to `Platforms/Android/`
- [ ] Configure AndroidManifest.xml

### iOS Setup
- [ ] Add iOS app to Firebase
- [ ] Download `GoogleService-Info.plist`
- [ ] Add to `Platforms/iOS/`
- [ ] Enable Push Notifications in App ID
- [ ] Create/upload APNs certificate or key to Firebase
- [ ] Configure bundle identifier
- [ ] Set up provisioning profile
- [ ] Configure Entitlements.plist

## Testing Checklist

### Pre-Flight Checks
- [ ] Backend server is running
- [ ] VAPID keys are configured (Web)
- [ ] Firebase project is set up (Mobile)
- [ ] google-services.json is in place (Android)
- [ ] GoogleService-Info.plist is in place (iOS)
- [ ] APNs key/certificate uploaded to Firebase (iOS)

### Web Testing
- [ ] Open app in Chrome/Firefox/Edge
- [ ] Click "Register for Push"
- [ ] Grant permission in browser dialog
- [ ] Verify subscription stored on server
- [ ] Send test notification from server
- [ ] Verify notification received

### Android Testing
- [ ] Build and deploy to emulator/device
- [ ] Grant permission (Android 13+)
- [ ] Verify FCM token in logs
- [ ] Verify token sent to server
- [ ] Send test notification from Firebase Console
- [ ] Verify notification received in all app states

### iOS Testing
- [ ] Build and deploy to physical device
- [ ] Grant permission in iOS dialog
- [ ] Verify APNs token in logs
- [ ] Verify FCM token in logs
- [ ] Verify token sent to server
- [ ] Send test notification from Firebase Console
- [ ] Verify notification received in all app states

## Common Integration Patterns

### Pattern 1: User-Specific Notifications

**Registration**:
```csharp
[HttpPost("register")]
public async Task<IActionResult> RegisterToken([FromBody] TokenRegistration request)
{
    var userId = GetAuthenticatedUserId(); // Your auth logic
    
    await _tokenStore.SaveTokenAsync(new DeviceToken
    {
        UserId = userId,
        Token = request.Token,
        Platform = request.Platform,
        RegisteredAt = DateTime.UtcNow
    });
    
    return Ok();
}
```

**Sending**:
```csharp
public async Task NotifyUserAsync(string userId, string title, string body)
{
    var tokens = await _tokenStore.GetTokensByUserIdAsync(userId);
    
    foreach (var token in tokens)
    {
        if (token.Platform == "Web")
        {
            await SendWebPushAsync(token, title, body);
        }
        else if (token.Platform == "Android" || token.Platform == "iOS")
        {
            await SendFcmAsync(token, title, body);
        }
    }
}
```

### Pattern 2: Broadcast Notifications

```csharp
public async Task BroadcastAsync(string title, string body)
{
    // Get all subscriptions
    var webSubscriptions = _pushService.GetAllSubscriptions();
    var mobileTokens = await _tokenStore.GetAllTokensAsync();
    
    // Send to web
    foreach (var sub in webSubscriptions)
    {
        await SendWebPushAsync(sub, title, body);
    }
    
    // Send to mobile (batch for efficiency)
    var messages = mobileTokens.Select(token => CreateMessage(token, title, body));
    await FirebaseMessaging.DefaultInstance.SendAllAsync(messages);
}
```

### Pattern 3: Topic-Based Notifications

```csharp
// Subscribe users to topics
await FirebaseMessaging.DefaultInstance.SubscribeToTopicAsync(
    new List<string> { token1, token2 },
    "news-updates"
);

// Send to topic
var message = new Message
{
    Topic = "news-updates",
    Notification = new Notification
    {
        Title = "Breaking News",
        Body = "Important update!"
    }
};

await FirebaseMessaging.DefaultInstance.SendAsync(message);
```

## Error Handling

### Common Errors and Solutions

| Error | Platform | Cause | Solution |
|-------|----------|-------|----------|
| Permission Denied | All | User blocked notifications | Provide in-app settings to re-request |
| Invalid Token | Mobile | Token expired/invalid | Remove from database, request re-registration |
| Invalid Subscription | Web | Subscription expired | Remove from database, prompt re-registration |
| Network Error | All | No internet connection | Retry with exponential backoff |
| Authentication Error | iOS | APNs certificate invalid | Update APNs certificate in Firebase |
| Service Unavailable | All | Push service down | Queue notifications, retry later |

### Retry Strategy

```csharp
public async Task<bool> SendWithRetryAsync(string token, Notification notification, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await SendNotificationAsync(token, notification);
            return true;
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unavailable)
        {
            if (i == maxRetries - 1) throw;
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); // Exponential backoff
        }
        catch (FirebaseMessagingException ex) when (
            ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
            ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            // Token is invalid, remove from database
            await _tokenStore.RemoveTokenAsync(token);
            throw;
        }
    }
    
    return false;
}
```

## Security Best Practices

### 1. Token Storage
- Encrypt tokens at rest
- Use secure database with access controls
- Implement token expiration and cleanup
- Associate tokens with authenticated users only

### 2. Authentication
```csharp
[Authorize] // Require authentication
[HttpPost("register")]
public async Task<IActionResult> RegisterToken([FromBody] TokenRegistration request)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();
    
    // Validate token format
    if (!IsValidToken(request.Token))
        return BadRequest("Invalid token format");
    
    // Rate limit registration attempts
    if (!await _rateLimiter.AllowAsync(userId))
        return StatusCode(429, "Too many requests");
    
    await RegisterTokenAsync(userId, request);
    return Ok();
}
```

### 3. Sensitive Data
- Never include passwords or API keys in notifications
- Use notifications as triggers to fetch secure data
- Implement end-to-end encryption for sensitive notifications
- Validate all notification content server-side

### 4. Key Management
- Store VAPID private key in secure vault (Azure Key Vault, AWS Secrets Manager)
- Rotate keys periodically
- Use different keys for dev/staging/production
- Never commit keys to version control

```csharp
// Good: Load from environment or key vault
var vapidPrivateKey = Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY") 
    ?? await _keyVault.GetSecretAsync("VapidPrivateKey");

// Bad: Hardcoded in source
var vapidPrivateKey = "BKpP3xYyv..."; // Never do this!
```

## Monitoring and Analytics

### Key Metrics to Track

1. **Registration Metrics**
   - Registration success rate
   - Registration failures by platform
   - Time to register
   - Active subscriptions/tokens

2. **Delivery Metrics**
   - Notifications sent
   - Delivery success rate
   - Delivery failures by platform
   - Average delivery time

3. **Engagement Metrics**
   - Notification open rate
   - Notification dismiss rate
   - Actions taken
   - Unsubscribe rate

### Implementation Example

```csharp
public class NotificationMetrics
{
    private readonly ILogger _logger;
    private readonly IMetricsCollector _metrics;
    
    public async Task<bool> SendNotificationAsync(string token, Notification notification)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _fcmService.SendAsync(token, notification);
            
            _metrics.Increment("notifications.sent");
            _metrics.Timing("notifications.send_duration", stopwatch.ElapsedMilliseconds);
            
            _logger.LogInformation("Notification sent successfully to {Token}", token);
            return true;
        }
        catch (Exception ex)
        {
            _metrics.Increment("notifications.failed");
            _logger.LogError(ex, "Failed to send notification to {Token}", token);
            return false;
        }
    }
}
```

## Performance Optimization

### 1. Batch Sending
```csharp
// Good: Send in batches
var messages = tokens.Select(t => CreateMessage(t, title, body)).ToList();
var response = await FirebaseMessaging.DefaultInstance.SendAllAsync(messages);

// Bad: Send one by one
foreach (var token in tokens)
{
    await FirebaseMessaging.DefaultInstance.SendAsync(CreateMessage(token, title, body));
}
```

### 2. Caching
```csharp
private readonly IMemoryCache _cache;

public async Task<List<string>> GetUserTokensAsync(string userId)
{
    var cacheKey = $"tokens:{userId}";
    
    if (_cache.TryGetValue(cacheKey, out List<string> tokens))
        return tokens;
    
    tokens = await _database.GetTokensByUserIdAsync(userId);
    
    _cache.Set(cacheKey, tokens, TimeSpan.FromMinutes(5));
    return tokens;
}
```

### 3. Async Processing
```csharp
public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
{
    // Queue for background processing
    await _queue.EnqueueAsync(new NotificationJob
    {
        Title = request.Title,
        Body = request.Body,
        UserIds = request.UserIds
    });
    
    return Accepted(); // Return immediately
}
```

## Troubleshooting Guide

### Web Push Issues
1. Check VAPID keys are configured
2. Verify HTTPS is used (except localhost)
3. Check service worker is registered
4. Verify browser supports push notifications
5. Check browser console for errors

### Android Issues
1. Verify google-services.json is in correct location
2. Check package name matches across all configs
3. Ensure Google Play Services is installed
4. Check Firebase project is active
5. Verify internet permission is granted

### iOS Issues
1. Test on physical device (simulator has limitations)
2. Verify APNs certificate is uploaded to Firebase
3. Check bundle ID matches across all configs
4. Ensure Push Notifications capability is enabled
5. Verify provisioning profile includes Push Notifications

## Resources

### Documentation
- [Web Push Protocol](https://web.dev/push-notifications-overview/)
- [Firebase Cloud Messaging](https://firebase.google.com/docs/cloud-messaging)
- [Apple Push Notification Service](https://developer.apple.com/documentation/usernotifications)
- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)

### Tools
- [web-push (NPM)](https://www.npmjs.com/package/web-push) - Generate VAPID keys
- [Firebase Console](https://console.firebase.google.com/) - Manage Firebase projects
- [Apple Developer Portal](https://developer.apple.com/) - Manage certificates and keys

### Libraries
- [Lib.Net.Http.WebPush](https://github.com/tpeczek/Lib.Net.Http.WebPush) - VAPID protocol for .NET
- [Plugin.Firebase](https://github.com/TobiasBuchholz/Plugin.Firebase) - Firebase for .NET MAUI
- [FirebaseAdmin](https://www.nuget.org/packages/FirebaseAdmin/) - Firebase Admin SDK for .NET

## Conclusion

This template demonstrates a complete multi-platform push notification system:

✅ **Web**: VAPID-based push for browsers  
✅ **Android**: Firebase Cloud Messaging  
✅ **iOS**: FCM with APNs integration  
✅ **Unified Backend**: Single API for all platforms  
✅ **MAUI Integration**: Native tokens injected into WebView  

Each platform has its own characteristics and requirements, but the overall architecture allows for a cohesive notification strategy across all platforms.
