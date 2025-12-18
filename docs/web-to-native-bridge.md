# Web-to-Native Bridge Implementation Guide

## Overview

This guide explains how the web-to-native bridge works in the MAUI PWA template, enabling JavaScript code in the PWA to call native platform SDK APIs seamlessly.

## Architecture

The bridge consists of three layers:

1. **JavaScript Layer** (PWA) - Provides a clean API for web developers
2. **Bridge Layer** (MAUI C#) - Routes messages between web and native code
3. **Native Layer** (Platform Services) - Executes platform-specific SDK operations

```
┌──────────────────────────────────────────────────────────┐
│                    JavaScript (PWA)                       │
│                                                           │
│  window.nativeBridge.initialize('key')                   │
│         ↓                                                 │
│  Creates callback + navigation request                    │
│         ↓                                                 │
│  nativebridge://call?callback=xyz&message={...}          │
└────────────────────────┬──────────────────────────────────┘
                         │
                         ↓ WebView.Navigating event
                         │
┌────────────────────────┴──────────────────────────────────┐
│              MAUI C# (MainPage.xaml.cs)                   │
│                                                           │
│  OnWebViewNavigating() intercepts URL                     │
│         ↓                                                 │
│  Extracts callback name and message                       │
│         ↓                                                 │
│  NativeBridge.HandleMessageAsync(message)                │
│         ↓                                                 │
│  Routes to platform service via INativeService            │
│         ↓                                                 │
│  Serializes response                                      │
│         ↓                                                 │
│  Calls JavaScript callback with result                    │
└────────────────────────┬──────────────────────────────────┘
                         │
                         ↓ INativeService interface
                         │
┌────────────────────────┴──────────────────────────────────┐
│           Platform-Specific Services                      │
│                                                           │
│  Android: NativeService.cs (Platforms/Android)           │
│  iOS:     NativeService.cs (Platforms/iOS)               │
│         ↓                                                 │
│  Calls native SDK APIs                                    │
│         ↓                                                 │
│  Returns result to bridge                                 │
└───────────────────────────────────────────────────────────┘
```

## Implementation Details

### 1. JavaScript Bridge Interface

**Location:** `MauiPwaShell/MainPage.xaml.cs` → `SetupNativeBridgeAsync()`

The bridge is injected into the WebView after page load:

```javascript
window.nativeBridge = {
    sendMessage: async function(action, data) {
        return new Promise((resolve, reject) => {
            const request = JSON.stringify({ action: action, data: data });
            const callbackName = 'nativeCallback_' + Date.now() + '_' + Math.random();
            
            // Create temporary callback
            window[callbackName] = function(response) {
                delete window[callbackName];
                const result = JSON.parse(response);
                if (result.success) {
                    resolve(result.data);
                } else {
                    reject(new Error(result.error));
                }
            };
            
            // Navigate to custom URL scheme
            window.location = 'nativebridge://call?callback=' + callbackName + '&message=' + encodeURIComponent(request);
        });
    },
    
    // Convenience methods
    initialize: function(apiKey) {
        return this.sendMessage('initialize', { apiKey: apiKey });
    },
    performOperation: function(input) {
        return this.sendMessage('performOperation', { input: input });
    }
};
```

#### How It Works

1. **Promise-based API:** Each method returns a Promise for async handling
2. **Dynamic callbacks:** Creates unique callback function names to avoid conflicts
3. **URL navigation trick:** Uses custom URL scheme to communicate with native code
4. **Cleanup:** Removes callback after execution to prevent memory leaks

### 2. WebView Navigation Interception

**Location:** `MauiPwaShell/MainPage.xaml.cs` → `OnWebViewNavigating()`

```csharp
private async void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
{
    // Intercept native bridge calls
    if (e.Url.StartsWith("nativebridge://call"))
    {
        e.Cancel = true;  // Prevent actual navigation
        await HandleNativeBridgeCallAsync(e.Url);
    }
}
```

**Key Points:**
- Intercepts ALL navigation events
- Checks for custom URL scheme prefix
- Cancels navigation to prevent page reload
- Passes URL to handler for processing

### 3. Message Handler

**Location:** `MauiPwaShell/MainPage.xaml.cs` → `HandleNativeBridgeCallAsync()`

```csharp
private async Task HandleNativeBridgeCallAsync(string url)
{
    var uri = new Uri(url);
    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
    var callbackName = query["callback"];
    var message = query["message"];

    // Process through bridge
    var response = await _nativeBridge.HandleMessageAsync(message);

    // Call back to JavaScript
    var escapedResponse = response.Replace("\\", "\\\\")
                                 .Replace("'", "\\'")
                                 .Replace("\n", "\\n");
    var callbackJs = $"if (typeof window['{callbackName}'] === 'function') {{ window['{callbackName}']('{escapedResponse}'); }}";

    await MainThread.InvokeOnMainThreadAsync(async () =>
    {
        await PwaView.EvaluateJavaScriptAsync(callbackJs);
    });
}
```

**Flow:**
1. Parses URL to extract callback and message
2. Deserializes message and routes to NativeBridge
3. Receives response from platform service
4. Escapes response for JavaScript
5. Invokes callback on main thread

### 4. Native Bridge Service

**Location:** `MauiPwaShell/Services/NativeBridge.cs`

This service routes messages to platform-specific implementations:

```csharp
public class NativeBridge
{
    private readonly INativeService _nativeService;

    public async Task<string> HandleMessageAsync(string message)
    {
        var request = JsonSerializer.Deserialize<NativeBridgeRequest>(message);
        
        return request.Action switch
        {
            "initialize" => HandleInitialize(request),
            "performOperation" => HandlePerformOperation(request),
            "getDeviceInfo" => HandleGetDeviceInfo(request),
            _ => CreateErrorResponse($"Unknown action: {request.Action}")
        };
    }

    private string HandleInitialize(NativeBridgeRequest request)
    {
        var apiKey = request.Data?.GetProperty("apiKey").GetString();
        _nativeService.Initialize(apiKey);
        return CreateSuccessResponse("SDK initialized successfully");
    }
}
```

**Responsibilities:**
- Deserialize incoming JSON messages
- Route to appropriate handler based on action
- Extract parameters from request data
- Call platform service methods
- Serialize responses
- Handle errors gracefully

### 5. Message Format

#### Request Format
```json
{
    "action": "performOperation",
    "data": {
        "input": "Hello from PWA"
    }
}
```

#### Response Format
```json
{
    "success": true,
    "data": "Android SDK processed: Hello from PWA"
}
```

Or on error:
```json
{
    "success": false,
    "error": "SDK not initialized. Call initialize() first."
}
```

## Adding New Bridge Methods

### Step 1: Add Method to INativeService Interface

```csharp
public interface INativeService
{
    // Existing methods...
    
    // New method
    string GetUserLocation();
}
```

### Step 2: Implement in Platform Services

**Android:**
```csharp
public string GetUserLocation()
{
    return _sdk.GetLocation() ?? "Unknown";
}
```

**iOS:**
```csharp
public string GetUserLocation()
{
    return _sdk.GetLocation();
}
```

### Step 3: Add Handler to NativeBridge

```csharp
public async Task<string> HandleMessageAsync(string message)
{
    // ...
    return request.Action switch
    {
        // Existing actions...
        "getUserLocation" => HandleGetUserLocation(request),
        _ => CreateErrorResponse($"Unknown action: {request.Action}")
    };
}

private string HandleGetUserLocation(NativeBridgeRequest request)
{
    try
    {
        var location = _nativeService.GetUserLocation();
        return CreateSuccessResponse(location);
    }
    catch (Exception ex)
    {
        return CreateErrorResponse($"Failed to get location: {ex.Message}");
    }
}
```

### Step 4: Add JavaScript Convenience Method

```javascript
window.nativeBridge.getUserLocation = function() {
    return this.sendMessage('getUserLocation', {});
};
```

### Step 5: Use in PWA

```javascript
const locationBtn = document.getElementById('getLocationBtn');
locationBtn.addEventListener('click', async () => {
    try {
        const location = await window.nativeBridge.getUserLocation();
        console.log('User location:', location);
    } catch (error) {
        console.error('Error:', error);
    }
});
```

## Advanced Patterns

### Handling Complex Data Types

For complex objects, use JSON serialization:

**JavaScript:**
```javascript
window.nativeBridge.updateUserProfile = function(profile) {
    return this.sendMessage('updateUserProfile', { 
        profile: profile  // Will be JSON serialized
    });
};

// Usage
await window.nativeBridge.updateUserProfile({
    name: 'John Doe',
    email: 'john@example.com',
    preferences: {
        notifications: true,
        theme: 'dark'
    }
});
```

**C# Handler:**
```csharp
private string HandleUpdateUserProfile(NativeBridgeRequest request)
{
    var profileJson = request.Data?.GetProperty("profile").GetRawText();
    var profile = JsonSerializer.Deserialize<UserProfile>(profileJson);
    
    _nativeService.UpdateProfile(profile);
    return CreateSuccessResponse("Profile updated");
}
```

### Event Broadcasting from Native to Web

For native events that need to notify the web:

**C# (Native):**
```csharp
public async Task BroadcastEventToWeb(string eventName, object data)
{
    var json = JsonSerializer.Serialize(new { eventName, data });
    var js = $@"
        if (window.nativeBridge && window.nativeBridge.onEvent) {{
            window.nativeBridge.onEvent('{eventName}', {json});
        }}
    ";
    
    await MainThread.InvokeOnMainThreadAsync(async () =>
    {
        await PwaView.EvaluateJavaScriptAsync(js);
    });
}
```

**JavaScript:**
```javascript
window.nativeBridge.onEvent = function(eventName, data) {
    console.log('Native event:', eventName, data);
    window.dispatchEvent(new CustomEvent(eventName, { detail: data }));
};

// Listen for events
window.addEventListener('locationChanged', (e) => {
    console.log('Location changed:', e.detail);
});
```

### Progress Updates for Long Operations

**C# (Native):**
```csharp
private async Task HandleLongOperation(NativeBridgeRequest request)
{
    var callbackName = request.Data?.GetProperty("progressCallback").GetString();
    
    await Task.Run(async () =>
    {
        for (int i = 0; i <= 100; i += 10)
        {
            await BroadcastProgress(callbackName, i);
            await Task.Delay(500);
        }
    });
}

private async Task BroadcastProgress(string callbackName, int progress)
{
    var js = $"if (window['{callbackName}']) window['{callbackName}']({progress});";
    await MainThread.InvokeOnMainThreadAsync(async () =>
    {
        await PwaView.EvaluateJavaScriptAsync(js);
    });
}
```

**JavaScript:**
```javascript
window.nativeBridge.startLongOperation = function(onProgress) {
    const progressCallback = 'progress_' + Date.now();
    window[progressCallback] = (progress) => {
        onProgress(progress);
        if (progress >= 100) {
            delete window[progressCallback];
        }
    };
    
    return this.sendMessage('longOperation', { 
        progressCallback: progressCallback 
    });
};

// Usage
await window.nativeBridge.startLongOperation((progress) => {
    console.log('Progress:', progress + '%');
});
```

## Security Considerations

### 1. Input Validation

Always validate inputs in the bridge handler:

```csharp
private string HandleInitialize(NativeBridgeRequest request)
{
    var apiKey = request.Data?.GetProperty("apiKey").GetString();
    
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return CreateErrorResponse("API key is required and cannot be empty");
    }
    
    if (apiKey.Length < 10 || apiKey.Length > 100)
    {
        return CreateErrorResponse("API key length must be between 10 and 100 characters");
    }
    
    _nativeService.Initialize(apiKey);
    return CreateSuccessResponse("SDK initialized successfully");
}
```

### 2. Sanitize Outputs

Prevent XSS by escaping JavaScript strings:

```csharp
private string EscapeForJavaScript(string input)
{
    return input.Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("<", "\\x3C")
                .Replace(">", "\\x3E");
}
```

### 3. Rate Limiting

Prevent abuse by rate limiting bridge calls:

```csharp
private readonly Dictionary<string, DateTime> _lastCallTimes = new();
private const int MIN_CALL_INTERVAL_MS = 100;

private bool IsRateLimited(string action)
{
    if (_lastCallTimes.TryGetValue(action, out var lastCall))
    {
        if ((DateTime.Now - lastCall).TotalMilliseconds < MIN_CALL_INTERVAL_MS)
        {
            return true;
        }
    }
    _lastCallTimes[action] = DateTime.Now;
    return false;
}
```

### 4. Origin Checking

Verify the request comes from your PWA:

```csharp
private async void PwaView_Navigated(object? sender, WebNavigatedEventArgs e)
{
    // Only setup bridge for trusted origins
    if (e.Url.StartsWith("https://yourdomain.com") || 
        e.Url.StartsWith("http://localhost"))
    {
        await SetupNativeBridgeAsync();
    }
}
```

## Performance Optimization

### 1. Batch Operations

Group multiple calls into one:

```javascript
window.nativeBridge.batchOperations = function(operations) {
    return this.sendMessage('batch', { operations: operations });
};

// Usage
await window.nativeBridge.batchOperations([
    { action: 'operation1', data: {...} },
    { action: 'operation2', data: {...} },
    { action: 'operation3', data: {...} }
]);
```

### 2. Caching

Cache responses for frequently called methods:

```csharp
private readonly Dictionary<string, (string result, DateTime expiry)> _cache = new();

private string HandleGetDeviceInfo(NativeBridgeRequest request)
{
    const string cacheKey = "deviceInfo";
    
    if (_cache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.Now)
    {
        return CreateSuccessResponse(cached.result);
    }
    
    var info = _nativeService.GetDeviceInfo();
    _cache[cacheKey] = (info, DateTime.Now.AddMinutes(5));
    
    return CreateSuccessResponse(info);
}
```

### 3. Minimize Data Transfer

Only send necessary data:

```javascript
// Bad: Sending entire object
await window.nativeBridge.updateUser({ ...largeUserObject });

// Good: Send only what's needed
await window.nativeBridge.updateUser({ 
    id: user.id, 
    name: user.name 
});
```

## Debugging Tips

### 1. Enable Verbose Logging

```csharp
private async Task HandleNativeBridgeCallAsync(string url)
{
    #if DEBUG
    Console.WriteLine($"[Bridge] Received: {url}");
    #endif
    
    // ... process ...
    
    #if DEBUG
    Console.WriteLine($"[Bridge] Response: {response}");
    #endif
}
```

### 2. JavaScript Console Logging

```javascript
window.nativeBridge.sendMessage = async function(action, data) {
    console.log('[Bridge] Sending:', action, data);
    
    try {
        const result = await this._sendMessage(action, data);
        console.log('[Bridge] Received:', result);
        return result;
    } catch (error) {
        console.error('[Bridge] Error:', error);
        throw error;
    }
};
```

### 3. WebView Debugging

**Android:**
```csharp
// In MainActivity.cs
#if DEBUG
Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
```

Then use Chrome DevTools: `chrome://inspect`

**iOS:**
Enable Web Inspector in Safari → Develop menu

## Testing

### Unit Testing Bridge Logic

```csharp
[Test]
public async Task TestBridgeInitialize()
{
    var mockService = new Mock<INativeService>();
    var bridge = new NativeBridge(mockService.Object);
    
    var request = @"{""action"":""initialize"",""data"":{""apiKey"":""test-key""}}";
    var response = await bridge.HandleMessageAsync(request);
    
    var result = JsonSerializer.Deserialize<NativeBridgeResponse>(response);
    Assert.IsTrue(result.Success);
    mockService.Verify(s => s.Initialize("test-key"), Times.Once);
}
```

### Integration Testing

```javascript
// Test file: test-bridge.js
async function testBridge() {
    console.log('Testing native bridge...');
    
    try {
        await window.nativeBridge.initialize('test-key');
        console.log('✓ Initialize successful');
        
        const result = await window.nativeBridge.performOperation('test');
        console.log('✓ Perform operation:', result);
        
        const info = await window.nativeBridge.getDeviceInfo();
        console.log('✓ Get device info:', info);
        
        console.log('All tests passed!');
    } catch (error) {
        console.error('✗ Test failed:', error);
    }
}

// Run tests when bridge is ready
window.addEventListener('nativeBridgeReady', testBridge);
```

## Common Issues and Solutions

### Issue: Bridge not responding

**Symptoms:** JavaScript calls hang or timeout

**Solutions:**
- Check WebView events are registered: `PwaView.Navigating += OnWebViewNavigating`
- Verify URL scheme is correct: `nativebridge://call`
- Check for JavaScript errors in console
- Ensure page has fully loaded before calling bridge

### Issue: Callback not found errors

**Symptoms:** `window[callbackName] is not a function`

**Solutions:**
- Don't reload page during bridge call
- Check callback naming doesn't conflict
- Ensure callback cleanup isn't too aggressive

### Issue: Data serialization errors

**Symptoms:** JSON parse errors or type mismatches

**Solutions:**
- Validate JSON before deserializing
- Use try-catch around serialization
- Check data types match between JS and C#
- Use `JsonElement` for flexible handling

## Next Steps

1. Implement your specific native SDK methods
2. Add appropriate error handling and validation
3. Create comprehensive tests for all bridge methods
4. Document your custom bridge API for web developers
5. Consider creating TypeScript definitions for type safety

## Additional Resources

- [WebView Documentation](https://learn.microsoft.com/dotnet/maui/user-interface/controls/webview)
- [JavaScript Interop](https://learn.microsoft.com/dotnet/maui/platform-integration/invoke-platform-code)
- [Async Programming in MAUI](https://learn.microsoft.com/dotnet/maui/platform-integration/appmodel/main-thread)
