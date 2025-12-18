# Native SDK Integration - Quick Start Guide

## What You'll Build

This guide shows you how to integrate a native Android or iOS SDK into your MAUI PWA app, enabling web-based buttons to trigger native functionality.

**Result:** PWA button clicks → JavaScript → MAUI Bridge → Native SDK APIs

## Prerequisites (5 minutes)

1. **.NET 8.0 SDK**
   ```bash
   dotnet --version  # Should show 8.0+
   ```

2. **MAUI Workloads**
   ```bash
   dotnet workload install maui
   ```

3. **Your Native SDK**
   - Android: `.jar`, `.aar`, or Java source files
   - iOS: `.framework`, `.xcframework`, or Objective-C/Swift source

## Quick Start (15 minutes)

### Step 1: Add Your Native SDK Files

**Android:**
```bash
# Place your SDK files here:
MauiPwaShell/
  NativeBindings/
    Android/
      YourSdk.java      # Or .jar/.aar file
```

**iOS:**
```bash
# Place your SDK files here:
MauiPwaShell/
  NativeBindings/
    iOS/
      YourSdk.h
      YourSdk.m         # Or .framework
```

### Step 2: Update Project Configuration

**For Java Source Files (.java):**

Edit `MauiPwaShell.csproj`, already configured:
```xml
<ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-android'))">
  <AndroidJavaSource Include="NativeBindings\Android\*.java" />
</ItemGroup>
```

**For Objective-C Source Files (.h/.m):**

Already configured in `MauiPwaShell.csproj`:
```xml
<ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
  <Compile Include="NativeBindings\iOS\*.m">
    <Link>Platforms\iOS\NativeLib\%(Filename)%(Extension)</Link>
  </Compile>
</ItemGroup>
```

**For Pre-compiled Libraries (.jar, .aar, .framework):**

See detailed guides in `docs/native-bindings-android.md` or `docs/native-bindings-ios.md`

### Step 3: Create C# Wrapper

Update `Platforms/Android/NativeService.cs` or `Platforms/iOS/NativeService.cs` to call your SDK:

**Android Example:**
```csharp
public void Initialize(string apiKey)
{
    _sdk = new Com.Yourcompany.Yoursdk.YourSdk(_context);
    _sdk.Initialize(apiKey);
}

public string PerformOperation(string input)
{
    return _sdk.YourMethod(input);
}
```

**iOS Example:**
```csharp
public void Initialize(string apiKey)
{
    _sdk = new YourSdk();
    _sdk.InitializeWithApiKey(apiKey);
}

// Add binding definition
[BaseType(typeof(NSObject))]
interface YourSdk
{
    [Export("initializeWithApiKey:")]
    void InitializeWithApiKey(string apiKey);
    
    [Export("yourMethod:")]
    string YourMethod(string input);
}
```

### Step 4: Add JavaScript Methods

The bridge is already set up! Just call it from your PWA:

```javascript
// Already available in the PWA:
window.nativeBridge.initialize('your-api-key')
window.nativeBridge.performOperation('data')
window.nativeBridge.getDeviceInfo()
```

To add new methods, update `NativeBridge.cs`:

```csharp
return request.Action switch
{
    // Existing actions...
    "yourNewMethod" => HandleYourNewMethod(request),
    _ => CreateErrorResponse($"Unknown action: {request.Action}")
};
```

### Step 5: Test It!

```bash
# Terminal 1: Start PWA server
cd PwaWeb
dotnet run

# Terminal 2: Run MAUI app
cd MauiPwaShell
dotnet build -f net8.0-android -t:Run  # Android
# or
dotnet build -f net8.0-ios -t:Run      # iOS
```

In the app, click the Native SDK buttons to test!

## Example: Complete Integration

Here's a complete example with a hypothetical payment SDK:

### 1. Native SDK (Android - PaymentSdk.java)

```java
package com.payment.sdk;

import android.content.Context;

public class PaymentSdk {
    private Context context;
    
    public PaymentSdk(Context context) {
        this.context = context;
    }
    
    public void initialize(String apiKey) {
        // Initialize payment SDK
    }
    
    public String processPayment(String amount, String currency) {
        // Process payment
        return "Payment successful: " + amount + " " + currency;
    }
}
```

### 2. C# Interface (INativeService.cs)

```csharp
public interface INativeService
{
    void Initialize(string apiKey);
    string ProcessPayment(string amount, string currency);
}
```

### 3. Android Implementation (Platforms/Android/NativeService.cs)

```csharp
public partial class NativeService : INativeService
{
    private Com.Payment.Sdk.PaymentSdk? _sdk;
    
    public NativeService()
    {
        _sdk = new Com.Payment.Sdk.PaymentSdk(Android.App.Application.Context);
    }
    
    public void Initialize(string apiKey)
    {
        _sdk?.Initialize(apiKey);
    }
    
    public string ProcessPayment(string amount, string currency)
    {
        return _sdk?.ProcessPayment(amount, currency) ?? "Error";
    }
}
```

### 4. Bridge Handler (Services/NativeBridge.cs)

```csharp
private string HandleProcessPayment(NativeBridgeRequest request)
{
    try
    {
        var amount = request.Data?.GetProperty("amount").GetString();
        var currency = request.Data?.GetProperty("currency").GetString();
        
        var result = _nativeService.ProcessPayment(amount, currency);
        return CreateSuccessResponse(result);
    }
    catch (Exception ex)
    {
        return CreateErrorResponse($"Payment failed: {ex.Message}");
    }
}
```

### 5. JavaScript (main.js)

```javascript
// Add convenience method
window.nativeBridge.processPayment = function(amount, currency) {
    return this.sendMessage('processPayment', { 
        amount: amount, 
        currency: currency 
    });
};

// Use it
async function handlePayment() {
    try {
        const result = await window.nativeBridge.processPayment('10.00', 'USD');
        alert('Payment result: ' + result);
    } catch (error) {
        alert('Payment failed: ' + error.message);
    }
}
```

## File Checklist

When integrating your SDK, you'll typically modify/create these files:

- [ ] `NativeBindings/Android/YourSdk.java` (or .jar)
- [ ] `NativeBindings/iOS/YourSdk.h/.m` (or .framework)
- [ ] `Platforms/Android/NativeService.cs` - Android implementation
- [ ] `Platforms/iOS/NativeService.cs` - iOS implementation with bindings
- [ ] `Services/INativeService.cs` - Add your methods to interface
- [ ] `Services/NativeBridge.cs` - Add action handlers
- [ ] `wwwroot/main.js` - Add JavaScript convenience methods
- [ ] `wwwroot/index.html` - Add UI buttons (optional)

## Common Patterns

### Pattern 1: Simple Method Call

**Native SDK:** `String getValue()`

**C# Interface:**
```csharp
string GetValue();
```

**Bridge Handler:**
```csharp
private string HandleGetValue(NativeBridgeRequest request)
{
    var value = _nativeService.GetValue();
    return CreateSuccessResponse(value);
}
```

**JavaScript:**
```javascript
const value = await window.nativeBridge.getValue();
```

### Pattern 2: Method with Parameters

**Native SDK:** `void updateSettings(String key, String value)`

**C# Interface:**
```csharp
void UpdateSettings(string key, string value);
```

**Bridge Handler:**
```csharp
private string HandleUpdateSettings(NativeBridgeRequest request)
{
    var key = request.Data?.GetProperty("key").GetString();
    var value = request.Data?.GetProperty("value").GetString();
    _nativeService.UpdateSettings(key, value);
    return CreateSuccessResponse("Settings updated");
}
```

**JavaScript:**
```javascript
await window.nativeBridge.updateSettings('theme', 'dark');
```

### Pattern 3: Method Returning Complex Data

**Native SDK:** `UserProfile getUserProfile()`

**C# Interface:**
```csharp
string GetUserProfile(); // Return JSON string
```

**Bridge Handler:**
```csharp
private string HandleGetUserProfile(NativeBridgeRequest request)
{
    var profile = _nativeService.GetUserProfile();
    return CreateSuccessResponse(profile); // Already JSON
}
```

**JavaScript:**
```javascript
const profileJson = await window.nativeBridge.getUserProfile();
const profile = JSON.parse(profileJson);
console.log(profile.name, profile.email);
```

## Debugging Tips

### Check if bridge is available:

```javascript
if (window.nativeBridge) {
    console.log('Native bridge is available');
} else {
    console.log('Running in browser - native features unavailable');
}
```

### View native logs:

**Android:**
```bash
adb logcat | grep -E "MAUI|YourSdk"
```

**iOS:**
Use Xcode → Devices and Simulators → View Device Logs

### Enable WebView debugging:

**Android:** Already enabled in DEBUG mode - use `chrome://inspect`
**iOS:** Safari → Develop → [Your Device] → [Your App]

## Troubleshooting

### "Native bridge not responding"
- **Check:** PWA server running on correct port
- **Fix:** Verify `PwaUrl` in `MainPage.xaml.cs`

### "Type not found" compilation error
- **Check:** Native SDK is properly configured in .csproj
- **Fix:** Run `dotnet clean && dotnet build`

### "SDK not initialized" at runtime
- **Check:** Initialize is called before other methods
- **Fix:** Always call `initialize()` first in JavaScript

## Next Steps

1. **Read Full Documentation:**
   - [Overview](./native-bindings-overview.md)
   - [Android Guide](./native-bindings-android.md)
   - [iOS Guide](./native-bindings-ios.md)
   - [Bridge Guide](./web-to-native-bridge.md)

2. **Customize for Your SDK:**
   - Add your SDK's specific methods
   - Implement error handling
   - Add validation and security checks

3. **Test Thoroughly:**
   - Test on physical devices
   - Test edge cases and errors
   - Test with poor network conditions

4. **Deploy:**
   - Build release versions
   - Test on production devices
   - Monitor for issues

## Need Help?

- Check the comprehensive documentation in `docs/`
- Review the example SDK implementations in `NativeBindings/`
- Open an issue on GitHub: https://github.com/dotnetangel/MauiPwaTemplate/issues

## Success Checklist

You've successfully integrated a native SDK when:

- [ ] App builds without errors for target platform(s)
- [ ] PWA loads in the MAUI WebView
- [ ] JavaScript can call `window.nativeBridge` methods
- [ ] Native SDK methods execute successfully
- [ ] Results return correctly to JavaScript
- [ ] Errors are handled gracefully
- [ ] Console logs show successful bridge communication

Congratulations! You now have a web app that can call native platform SDKs! 🎉
