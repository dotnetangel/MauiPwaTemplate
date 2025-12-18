# Testing and Building the Native SDK Integration

## Overview

This guide provides instructions for building and testing the native SDK bindings integration in the MAUI PWA template.

## Prerequisites

Before building and testing, ensure you have:

- **.NET 8.0 SDK or later**
  ```bash
  dotnet --version  # Should be 8.0 or higher
  ```

- **MAUI Workloads**
  ```bash
  dotnet workload install maui
  ```

- **For Android:**
  - Android SDK (API 21+)
  - Android emulator or physical device
  - Java JDK 11 or higher

- **For iOS (macOS only):**
  - Xcode 15+
  - iOS Simulator or physical device
  - Apple Developer account (for device deployment)

## Building the Project

### Build All Targets

```bash
# Navigate to the solution directory
cd /path/to/MauiPwaTemplate

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

### Build for Specific Platform

**Android:**
```bash
cd MauiPwaShell
dotnet build -f net8.0-android
```

**iOS (macOS only):**
```bash
cd MauiPwaShell
dotnet build -f net8.0-ios
```

### Build and Deploy

**Android:**
```bash
# Deploy to connected device/emulator
dotnet build -f net8.0-android -t:Run

# Or with specific device
adb devices  # List devices
dotnet build -f net8.0-android -t:Run -p:AndroidDevice=<device-id>
```

**iOS:**
```bash
# Deploy to simulator
dotnet build -f net8.0-ios -t:Run
```

## Running the PWA Backend

The MAUI app requires the PWA web server to be running:

```bash
# In a separate terminal
cd PwaWeb
dotnet restore
dotnet run --urls "http://localhost:5000"
```

The PWA will be available at `http://localhost:5000`.

### Network Configuration

- **Android Emulator:** Uses `http://10.0.2.2:5000` to reach host's `localhost:5000`
- **iOS Simulator:** Can use `http://localhost:5000` directly
- **Physical Devices:** Update `PwaUrl` in `MainPage.xaml.cs` to use your machine's IP address

## Testing the Native SDK Integration

### Step 1: Start the PWA Server

```bash
cd PwaWeb
dotnet run
```

You should see output like:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### Step 2: Launch the MAUI App

**Android:**
```bash
cd MauiPwaShell
dotnet build -f net8.0-android -t:Run
```

**iOS:**
```bash
cd MauiPwaShell
dotnet build -f net8.0-ios -t:Run
```

### Step 3: Test Native Bridge Functionality

Once the app launches, you'll see the PWA interface. Scroll to the "Native SDK Integration" section:

1. **Initialize SDK**
   - Click the "🔌 Initialize SDK" button
   - Should see status change to "SDK Initialized ✅"
   - Success message appears

2. **Perform Operation**
   - Click the "⚙️ Perform Operation" button
   - Enter text in the prompt (e.g., "Hello from PWA!")
   - Should see result like: "Android SDK processed: Hello from PWA!"

3. **Get Device Info**
   - Click the "📱 Get Device Info" button
   - Should see device information like: "Android Device: sdk_gphone64_arm64 (API 34)"

### Step 4: Verify Bridge Communication

Check the console logs to verify bridge communication:

**Android:**
```bash
# View logs
adb logcat | grep -E "MAUI|ExampleSdk"
```

You should see logs like:
```
[MAUI] Native bridge setup complete
[MAUI] Handled native bridge call: {"action":"initialize","data":{"apiKey":"demo-api-key-12345"}}
[ExampleSdk] Initializing SDK with API key: demo-api-key-12345
[MAUI] Handled native bridge call: {"action":"performOperation","data":{"input":"Hello from PWA!"}}
[ExampleSdk] Performing operation with input: Hello from PWA!
```

**iOS:**
```bash
# View device/simulator logs using Xcode
# Xcode → Window → Devices and Simulators → Open Console
```

## Testing in Browser (PWA Only)

To test the PWA without MAUI:

```bash
cd PwaWeb
dotnet run
```

Open `http://localhost:5000` in Chrome or Edge.

**Note:** Native SDK features will not work in the browser. You'll see error messages like:
```
Native bridge not available. This feature only works in the MAUI app.
```

This is expected - the native bridge only functions when running in the MAUI wrapper.

## Debugging

### WebView Debugging

**Android:**

1. Enable WebView debugging in `MainActivity.cs`:
   ```csharp
   #if DEBUG
   Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
   #endif
   ```

2. Open Chrome on your desktop: `chrome://inspect`

3. You should see your device listed with the WebView

4. Click "inspect" to open DevTools

**iOS:**

1. In Safari on macOS, enable Develop menu:
   - Safari → Preferences → Advanced
   - Check "Show Develop menu in menu bar"

2. Run the app on simulator/device

3. Safari → Develop → [Your Device] → [Your App]

4. This opens Web Inspector for the WebView

### Common Issues

#### Issue: "Native bridge not responding"

**Check:**
- PWA server is running on correct port
- Network configuration (10.0.2.2 for Android emulator)
- WebView has loaded the page (check Navigated event fired)
- JavaScript console for errors

**Solution:**
```bash
# Check if PWA server is accessible
# Android emulator:
adb shell curl http://10.0.2.2:5000

# iOS simulator:
curl http://localhost:5000
```

#### Issue: "Type 'Com.Example.Nativesdk.ExampleSdk' not found"

**Check:**
- Java source is included in .csproj: `<AndroidJavaSource Include="..."/>`
- Package name in Java matches C# usage
- Clean and rebuild

**Solution:**
```bash
dotnet clean
dotnet build -f net8.0-android
```

#### Issue: iOS binding errors

**Check:**
- Objective-C files are compiled: `<Compile Include="..."/>`
- Binding attributes match Objective-C selectors
- All required frameworks are linked

**Solution:**
Check build output for specific errors and adjust binding definitions.

#### Issue: Bridge callbacks timeout

**Check:**
- Callback naming is unique (timestamp + random)
- No page reloads during bridge call
- JavaScript errors in console

**Solution:**
Enable verbose logging and check both native and JS logs.

## Performance Testing

### Measure Bridge Latency

Add this test function to `main.js`:

```javascript
async function testBridgePerformance() {
    console.time('Bridge Initialize');
    await window.nativeBridge.initialize('test-key');
    console.timeEnd('Bridge Initialize');
    
    console.time('Bridge Operation');
    await window.nativeBridge.performOperation('test');
    console.timeEnd('Bridge Operation');
}
```

Expected latency: < 100ms per call for simple operations.

### Memory Testing

Monitor memory usage during bridge operations:

**Android:**
```bash
# Monitor memory
adb shell dumpsys meminfo com.example.mauipwashell
```

**iOS:**
Use Xcode Instruments → Allocations tool

## Automated Testing

### Unit Testing Bridge Logic

While MAUI doesn't have built-in test infrastructure in this template, you can test the bridge logic:

```csharp
// Example unit test (requires xUnit or similar)
[Fact]
public async Task TestNativeBridgeInitialize()
{
    var mockService = new Mock<INativeService>();
    var bridge = new NativeBridge(mockService.Object);
    
    var request = @"{""action"":""initialize"",""data"":{""apiKey"":""test-key""}}";
    var response = await bridge.HandleMessageAsync(request);
    
    var result = JsonSerializer.Deserialize<NativeBridgeResponse>(response);
    Assert.True(result.Success);
    mockService.Verify(s => s.Initialize("test-key"), Times.Once);
}
```

### Integration Testing

Create a test HTML page in `PwaWeb/wwwroot/test.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>Bridge Tests</title>
</head>
<body>
    <h1>Native Bridge Tests</h1>
    <div id="results"></div>
    <script>
        async function runTests() {
            const results = [];
            
            try {
                await window.nativeBridge.initialize('test-key');
                results.push('✓ Initialize test passed');
            } catch (e) {
                results.push('✗ Initialize test failed: ' + e.message);
            }
            
            try {
                const result = await window.nativeBridge.performOperation('test');
                if (result.includes('processed')) {
                    results.push('✓ Perform operation test passed');
                } else {
                    results.push('✗ Unexpected result: ' + result);
                }
            } catch (e) {
                results.push('✗ Perform operation test failed: ' + e.message);
            }
            
            document.getElementById('results').innerHTML = results.join('<br>');
        }
        
        window.addEventListener('nativeBridgeReady', runTests);
    </script>
</body>
</html>
```

## CI/CD Considerations

For automated builds in CI/CD pipelines:

### GitHub Actions Example

```yaml
name: Build MAUI App

on: [push, pull_request]

jobs:
  build-android:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Install MAUI workload
        run: dotnet workload install maui
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build Android
        run: dotnet build -f net8.0-android --no-restore
  
  build-ios:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Install MAUI workload
        run: dotnet workload install maui
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build iOS
        run: dotnet build -f net8.0-ios --no-restore
```

## Troubleshooting Build Issues

### Issue: MAUI workload not found

```bash
# Install MAUI workload
dotnet workload install maui

# Or restore from manifest
dotnet workload restore
```

### Issue: Android SDK not found

```bash
# Set ANDROID_HOME environment variable
export ANDROID_HOME=/path/to/android-sdk

# Or on Windows
set ANDROID_HOME=C:\path\to\android-sdk
```

### Issue: Xcode command line tools not found (macOS)

```bash
# Install Xcode command line tools
xcode-select --install

# Set Xcode path
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
```

## Next Steps

1. Test with your own native SDKs
2. Add more bridge methods as needed
3. Implement error handling for production
4. Add analytics/logging for bridge usage
5. Consider implementing offline fallbacks
6. Add unit and integration tests

## Support

For issues specific to:
- **MAUI:** https://github.com/dotnet/maui/issues
- **This Template:** https://github.com/dotnetangel/MauiPwaTemplate/issues
- **Native Bindings:** See the comprehensive documentation in `docs/`
