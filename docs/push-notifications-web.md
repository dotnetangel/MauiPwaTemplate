# Web Push Notifications Registration Flow (VAPID)

This document describes the complete push notification registration flow for the **web browser** platform using the VAPID (Voluntary Application Server Identification) protocol.

## Overview

Web push notifications use the W3C Push API with VAPID for authentication. The flow involves:
1. Service worker registration
2. Notification permission request
3. Push subscription creation
4. Subscription storage on the server

## Architecture Components

### Client-Side Components
- **Browser**: Chrome, Firefox, Edge, Safari (limited support)
- **Service Worker**: `service-worker.js` - Handles push events in the background
- **Main JavaScript**: `main.js` - Manages registration flow
- **Push API**: Browser's native Push Manager API

### Server-Side Components
- **ASP.NET Core 8.0**: Backend web server
- **RegisterPushController**: Handles subscription registration
- **WebPushService**: Manages subscriptions and sends notifications
- **Lib.Net.Http.WebPush**: VAPID protocol implementation library

## Detailed Registration Flow

```
┌─────────────┐                ┌──────────────────┐                ┌────────────────┐
│   Browser   │                │  ASP.NET Server  │                │ Push Service   │
│  (Client)   │                │    (Backend)     │                │   (Browser)    │
└──────┬──────┘                └────────┬─────────┘                └────────┬───────┘
       │                                 │                                   │
       │  1. User clicks "Register"     │                                   │
       │─────────────────────────────────┐                                  │
       │                                 │                                   │
       │  2. Check browser support       │                                   │
       │─────────────────────────────────┐                                  │
       │                                 │                                   │
       │  3. Request permission          │                                   │
       │────────────────────────────────>│                                   │
       │                                 │                                   │
       │  4. User grants permission      │                                   │
       │<────────────────────────────────│                                   │
       │                                 │                                   │
       │  5. Register service worker     │                                   │
       │─────────────────────────────────┐                                  │
       │                                 │                                   │
       │  6. GET /api/registerpush/      │                                   │
       │        vapidPublicKey           │                                   │
       │─────────────────────────────────>                                   │
       │                                 │                                   │
       │  7. Return VAPID public key     │                                   │
       │<─────────────────────────────────                                   │
       │                                 │                                   │
       │  8. Subscribe to push           │                                   │
       │     (with VAPID key)            │                                   │
       │─────────────────────────────────────────────────────────────────>  │
       │                                 │                                   │
       │  9. Create subscription         │                                   │
       │    (endpoint + keys)            │                                   │
       │<─────────────────────────────────────────────────────────────────  │
       │                                 │                                   │
       │  10. POST /api/registerpush     │                                   │
       │      (subscription object)      │                                   │
       │─────────────────────────────────>                                   │
       │                                 │                                   │
       │                                 │ 11. Store subscription            │
       │                                 │─────────────────────────┐         │
       │                                 │                         │         │
       │                                 │<────────────────────────┘         │
       │                                 │                                   │
       │  12. Return success             │                                   │
       │<─────────────────────────────────                                   │
       │                                 │                                   │
       │  13. Update UI status           │                                   │
       │─────────────────────────────────┐                                  │
       │                                 │                                   │
```

## Step-by-Step Process

### Step 1: User Initiates Registration
**Location**: `wwwroot/main.js` - Line 75

```javascript
document.getElementById('registerBtn').addEventListener('click', async () => {
    // User clicks "Register for Push" button
});
```

The user clicks the "Register for Push" button in the UI, triggering the registration flow.

### Step 2: Check Browser Support
**Location**: `wwwroot/main.js` - Lines 83-85

```javascript
if (!('Notification' in window)) {
    throw new Error('Notifications are not supported in this browser');
}
```

The client checks if the browser supports:
- Notification API
- Service Workers
- Push Manager

### Step 3-4: Request Notification Permission
**Location**: `wwwroot/main.js` - Lines 88-91

```javascript
const permission = await Notification.requestPermission();
if (permission !== 'granted') {
    throw new Error('Notification permission denied');
}
```

The browser displays a native permission dialog. The user must grant permission for notifications to proceed.

**Permission States**:
- `granted`: User allowed notifications
- `denied`: User blocked notifications
- `default`: User hasn't decided yet

### Step 5: Register Service Worker
**Location**: `wwwroot/main.js` - Lines 22-27

```javascript
async function registerServiceWorker() {
    if ('serviceWorker' in navigator) {
        return await navigator.serviceWorker.register('/service-worker.js');
    }
    throw new Error('Service workers are not supported in this browser');
}
```

The service worker is registered to enable background push handling. The service worker:
- Runs in a separate thread
- Can receive push events when the app is closed
- Handles notification display

### Step 6-7: Get VAPID Public Key
**Location**: `wwwroot/main.js` - Lines 4-9

```javascript
async function getVapidPublicKey() {
    const resp = await fetch('/api/registerpush/vapidPublicKey');
    if (!resp.ok) throw new Error('Failed to get VAPID key');
    const data = await resp.json();
    return data.key;
}
```

**Server Endpoint**: `Controllers/RegisterPushController.cs` - Lines 22-40

```csharp
[HttpGet("vapidPublicKey")]
public IActionResult GetVapidKey()
{
    var key = _pushService.PublicKey;
    return Ok(new { key });
}
```

The VAPID public key is used to identify the application server. It's generated once and stored in `appsettings.json`.

**VAPID Key Configuration**: `appsettings.json`

```json
{
  "VapidKeys": {
    "PublicKey": "BKpP3xYyv...",
    "PrivateKey": "...",
    "Subject": "mailto:your-email@example.com"
  }
}
```

### Step 8-9: Subscribe to Push Service
**Location**: `wwwroot/main.js` - Lines 29-34

```javascript
async function subscribeForPush(reg, vapidPublicKey) {
    return await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
    });
}
```

The browser contacts its push service (Google for Chrome, Mozilla for Firefox) to create a subscription. The subscription contains:
- **endpoint**: URL where push messages should be sent
- **keys.p256dh**: Client's public key for encryption
- **keys.auth**: Authentication secret

**Example Subscription Object**:
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

### Step 10-11: Store Subscription on Server
**Location**: `wwwroot/main.js` - Lines 36-44

```javascript
async function sendSubscriptionToServer(sub) {
    const resp = await fetch('/api/registerpush', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(sub)
    });
    return await resp.json();
}
```

**Server Endpoint**: `Controllers/RegisterPushController.cs` - Lines 43-62

```csharp
[HttpPost]
public IActionResult Register([FromBody] PushSubscription? sub)
{
    if (sub == null)
    {
        return BadRequest(new { error = "Invalid subscription data" });
    }

    _pushService.AddSubscription(sub);
    _logger.LogInformation("Successfully registered push subscription");
    return Ok(new { success = true, message = "Subscription registered successfully" });
}
```

**Service**: `Services/WebPushService.cs`

The subscription is stored in memory (for production, use a database). The service maintains a list of all active subscriptions.

### Step 12-13: Confirm Registration
**Location**: `wwwroot/main.js` - Lines 98-99

```javascript
updatePushStatus('Push notifications active', '🔔');
showSuccess('pushSuccess', 'Successfully registered for push notifications!');
```

The UI is updated to show successful registration. The status indicator changes from inactive to active.

## Sending Push Notifications

Once registered, the server can send push notifications to the subscription:

### Server-Side Push Sending
**Location**: `Controllers/WebPushController.cs`

```csharp
[HttpPost("send")]
public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
{
    var notification = new PushMessage(JsonSerializer.Serialize(new
    {
        title = request.Title,
        body = request.Body,
        icon = "/icon-192.png"
    }));

    foreach (var subscription in _pushService.GetAllSubscriptions())
    {
        await _pushService.SendNotificationAsync(subscription, notification);
    }

    return Ok(new { success = true, sent = count });
}
```

### Client-Side Push Handling
**Location**: `wwwroot/service-worker.js` - Lines 87-136

```javascript
self.addEventListener('push', event => {
    let notificationData = {
        title: 'PWA Notification',
        body: 'You have a new notification',
        icon: '/icon-192.png',
        badge: '/badge-72.png'
    };
    
    if (event.data) {
        const data = event.data.json();
        notificationData.title = data.title || notificationData.title;
        notificationData.body = data.body || notificationData.body;
    }
    
    event.waitUntil(
        self.registration.showNotification(notificationData.title, {
            body: notificationData.body,
            icon: notificationData.icon,
            badge: notificationData.badge
        })
    );
});
```

## Push Message Flow

```
┌────────────────┐         ┌──────────────┐         ┌──────────────┐         ┌──────────┐
│ ASP.NET Server │         │ Push Service │         │Service Worker│         │ Browser  │
└───────┬────────┘         └──────┬───────┘         └──────┬───────┘         └────┬─────┘
        │                         │                        │                      │
        │ 1. Send push message    │                        │                      │
        │    with VAPID signature │                        │                      │
        │─────────────────────────>                        │                      │
        │                         │                        │                      │
        │                         │ 2. Validate signature  │                      │
        │                         │────────────────┐       │                      │
        │                         │                │       │                      │
        │                         │<───────────────┘       │                      │
        │                         │                        │                      │
        │                         │ 3. Forward to client   │                      │
        │                         │────────────────────────>                      │
        │                         │                        │                      │
        │                         │                        │ 4. 'push' event      │
        │                         │                        │──────────────┐       │
        │                         │                        │              │       │
        │                         │                        │<─────────────┘       │
        │                         │                        │                      │
        │                         │                        │ 5. Show notification │
        │                         │                        │──────────────────────>
        │                         │                        │                      │
        │ 6. Return success       │                        │                      │
        │<─────────────────────────                        │                      │
        │                         │                        │                      │
```

## Security Considerations

### 1. HTTPS Required
- Web Push requires HTTPS (except localhost for development)
- Service Workers only work on HTTPS origins
- This prevents man-in-the-middle attacks

### 2. VAPID Authentication
- The private VAPID key must be kept secure
- Each push message is signed with the private key
- Push services validate the signature before delivery

### 3. User Consent
- Permission must be explicitly granted by the user
- Permission can be revoked at any time
- Applications should respect the user's choice

### 4. Encryption
- All push messages are encrypted end-to-end
- Uses the p256dh public key from the subscription
- Browser decrypts using its private key

## Browser Compatibility

| Browser | Version | Support Level |
|---------|---------|---------------|
| Chrome | 50+ | Full support |
| Firefox | 44+ | Full support |
| Edge | 79+ | Full support |
| Safari | 16+ | Limited support (iOS 16.4+) |
| Opera | 37+ | Full support |

## Troubleshooting

### Common Issues

1. **"Notification permission denied"**
   - User clicked "Block" on permission dialog
   - Solution: User must manually enable in browser settings

2. **"Service worker registration failed"**
   - Not using HTTPS (except localhost)
   - Service worker file not found
   - Solution: Serve over HTTPS and verify file path

3. **"Failed to get VAPID key"**
   - VAPID keys not configured in `appsettings.json`
   - Solution: Generate and configure VAPID keys

4. **Push messages not received**
   - Subscription expired
   - Browser closed and background sync disabled
   - Solution: Re-register subscription

### Debugging Tips

1. **Check Service Worker Status**
   - Open DevTools > Application > Service Workers
   - Verify service worker is running and activated

2. **Monitor Push Events**
   - Add `console.log` in service worker push event handler
   - Check DevTools console for errors

3. **Test Subscription**
   ```bash
   curl -X POST http://localhost:5000/api/webpush/send \
     -H "Content-Type: application/json" \
     -d '{"title":"Test","body":"Hello"}'
   ```

## Best Practices

1. **Ask for Permission at the Right Time**
   - Don't ask immediately on page load
   - Explain why notifications are useful first
   - Provide context before requesting permission

2. **Handle Permission Denial Gracefully**
   - Don't repeatedly ask if user denied
   - Provide alternative ways to stay informed

3. **Manage Subscription Lifecycle**
   - Check subscription validity periodically
   - Re-subscribe if subscription expires
   - Remove invalid subscriptions from server

4. **Optimize Notification Content**
   - Keep titles and bodies concise
   - Use meaningful icons and images
   - Include actionable buttons when appropriate

5. **Test Across Browsers**
   - Different browsers have different push services
   - Subscription formats may vary slightly
   - Test on all supported platforms

## References

- [W3C Push API Specification](https://www.w3.org/TR/push-api/)
- [VAPID Protocol](https://datatracker.ietf.org/doc/html/rfc8292)
- [Web Push Protocol](https://datatracker.ietf.org/doc/html/rfc8030)
- [MDN Web Push API](https://developer.mozilla.org/en-US/docs/Web/API/Push_API)
- [Service Worker Cookbook](https://serviceworke.rs/)
