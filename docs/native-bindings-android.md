# Android Native SDK Bindings - Step-by-Step Guide

## Overview

This guide walks through creating Android native SDK bindings for your MAUI application using both the MAUI Slim Bindings approach and the traditional Binding Library approach.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Understanding Android Binding Approaches](#understanding-android-binding-approaches)
3. [Method 1: MAUI Slim Bindings (Recommended)](#method-1-maui-slim-bindings-recommended)
4. [Method 2: Traditional Android Binding Library](#method-2-traditional-android-binding-library)
5. [Testing Your Bindings](#testing-your-bindings)
6. [Troubleshooting](#troubleshooting)

## Prerequisites

- .NET 8.0 SDK or later
- Android SDK (API level 21 or higher)
- Visual Studio 2022 17.8+ or VS Code with C# Dev Kit
- MAUI workloads: `dotnet workload install maui`
- Your native Android SDK (as .jar, .aar, or source .java files)

## Understanding Android Binding Approaches

### What is a Binding?

Android native libraries are written in Java or Kotlin. To use them from C#, we need to create "bindings" - C# wrappers that call the native code via Java interop.

### MAUI Slim Bindings vs Traditional Bindings

| Feature | Slim Bindings | Traditional Binding Library |
|---------|---------------|----------------------------|
| Project Type | Integrated in MAUI app | Separate binding project |
| Complexity | Simple | More complex |
| Reusability | App-specific | Can be packaged as NuGet |
| Best for | Small SDKs, prototypes | Large SDKs, team distribution |
| Build Time | Faster | Slower |

## Method 1: MAUI Slim Bindings (Recommended)

This approach embeds the native library directly in your MAUI project.

### Step 1: Prepare Your Native Android SDK

You need one of the following:
- **Option A:** Pre-compiled `.jar` or `.aar` file
- **Option B:** Java source files (`.java`)

For this example, we'll use both approaches.

### Step 2: Add Native Library to Your Project

#### Option A: Using a Pre-compiled .jar/.aar File

1. **Create a directory for native libraries:**
   ```bash
   mkdir -p MauiPwaShell/Platforms/Android/Libs
   ```

2. **Compile your Java SDK to a .jar file:**
   ```bash
   # Navigate to where your Java source is
   cd MauiPwaShell/NativeBindings/Android
   
   # Compile Java to .class files
   javac -source 1.8 -target 1.8 -d ./bin -cp "$ANDROID_HOME/platforms/android-33/android.jar" ExampleSdk.java
   
   # Create .jar file
   cd bin
   jar cvf ExampleSdk.jar com/example/nativesdk/*.class
   
   # Move to Libs folder
   mv ExampleSdk.jar ../../Platforms/Android/Libs/
   ```

3. **Add the .jar to your .csproj:**

   Edit `MauiPwaShell/MauiPwaShell.csproj` and add:

   ```xml
   <!-- Android Native Libraries -->
   <ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-android'))">
     <AndroidLibrary Include="Platforms\Android\Libs\ExampleSdk.jar" />
   </ItemGroup>
   ```

#### Option B: Using Java Source Files Directly

1. **Add Java source files to your project:**

   Edit `MauiPwaShell/MauiPwaShell.csproj` and add:

   ```xml
   <!-- Android Java Sources -->
   <ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-android'))">
     <AndroidJavaSource Include="NativeBindings\Android\*.java" />
   </ItemGroup>
   ```

2. **Ensure proper package structure:**
   
   Your Java file must have the correct package declaration:
   ```java
   package com.example.nativesdk;
   
   public class ExampleSdk {
       // Your SDK code
   }
   ```

### Step 3: Create C# Interface

Create a shared interface that both Android and iOS will implement:

**File:** `MauiPwaShell/Services/INativeService.cs`

```csharp
namespace MauiPwaShell.Services;

public interface INativeService
{
    void Initialize(string apiKey);
    string PerformOperation(string input);
    string GetDeviceInfo();
    bool IsInitialized();
    void Dispose();
}
```

### Step 4: Implement Android-Specific Service

**File:** `MauiPwaShell/Platforms/Android/NativeService.cs`

```csharp
using Android.Content;

namespace MauiPwaShell.Services;

public partial class NativeService : INativeService
{
    private Com.Example.Nativesdk.ExampleSdk? _sdk;
    private readonly Context _context;

    public NativeService()
    {
        _context = Android.App.Application.Context;
        _sdk = new Com.Example.Nativesdk.ExampleSdk(_context);
    }

    public void Initialize(string apiKey)
    {
        if (_sdk == null)
        {
            _sdk = new Com.Example.Nativesdk.ExampleSdk(_context);
        }
        _sdk.Initialize(apiKey);
    }

    public string PerformOperation(string input)
    {
        if (_sdk == null)
        {
            throw new InvalidOperationException("SDK not initialized");
        }
        return _sdk.PerformOperation(input) ?? string.Empty;
    }

    public string GetDeviceInfo()
    {
        if (_sdk == null)
        {
            throw new InvalidOperationException("SDK not initialized");
        }
        return _sdk.GetDeviceInfo() ?? string.Empty;
    }

    public bool IsInitialized()
    {
        return _sdk?.IsInitialized() ?? false;
    }

    public void Dispose()
    {
        _sdk?.Dispose();
        _sdk = null;
    }
}
```

**Important Notes:**
- The namespace mapping: Java `com.example.nativesdk` becomes C# `Com.Example.Nativesdk`
- Java class `ExampleSdk` is accessed as `ExampleSdk` in C#
- All first letters of package segments are capitalized in C#

### Step 5: Register the Service

Update `MauiPwaShell/MauiProgram.cs`:

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder.UseMauiApp<App>();

#if ANDROID || IOS
    // Register native service
    builder.Services.AddSingleton<INativeService, NativeService>();
#endif

    // ... rest of configuration
}
```

### Step 6: Build and Test

```bash
# Build for Android
dotnet build -f net8.0-android

# If successful, deploy to device/emulator
dotnet build -f net8.0-android -t:Run
```

## Method 2: Traditional Android Binding Library

For larger, more complex SDKs, you may want a separate binding library project.

### Step 1: Create Android Binding Library Project

```bash
# Create new binding library
dotnet new android-bindinglib -n MyAndroidBindings

# Add to solution
dotnet sln add MyAndroidBindings/MyAndroidBindings.csproj
```

### Step 2: Add Native Library to Binding Project

1. **Copy your .jar or .aar file:**
   ```bash
   cp path/to/sdk.jar MyAndroidBindings/Jars/
   ```

2. **Set build action in .csproj:**
   ```xml
   <ItemGroup>
     <EmbeddedJar Include="Jars\sdk.jar" />
     <!-- OR for .aar files: -->
     <LibraryProjectZip Include="Jars\sdk.aar" />
   </ItemGroup>
   ```

### Step 3: Handle Binding Metadata

The binding generator may create errors. Fix them with metadata:

**File:** `MyAndroidBindings/Transforms/Metadata.xml`

```xml
<metadata>
  <!-- Remove problematic types -->
  <remove-node path="/api/package[@name='com.example.nativesdk']/class[@name='InternalClass']" />
  
  <!-- Fix naming conflicts -->
  <attr path="/api/package[@name='com.example.nativesdk']/class[@name='Builder']" name="managedName">SdkBuilder</attr>
  
  <!-- Make internal classes internal in C# -->
  <attr path="/api/package[@name='com.example.nativesdk.internal']" name="visibility">internal</attr>
</metadata>
```

### Step 4: Build Binding Library

```bash
cd MyAndroidBindings
dotnet build
```

This produces `MyAndroidBindings.dll`.

### Step 5: Reference in MAUI Project

Add to `MauiPwaShell.csproj`:

```xml
<ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-android'))">
  <ProjectReference Include="..\MyAndroidBindings\MyAndroidBindings.csproj" />
</ItemGroup>
```

### Step 6: Use in Your Code

The usage is the same as Method 1 - create your `NativeService.cs` implementation.

## Advanced Binding Configurations

### Handling ProGuard/R8 Rules

If your SDK uses ProGuard, create a rules file:

**File:** `MauiPwaShell/Platforms/Android/proguard.cfg`

```proguard
-keep class com.example.nativesdk.** { *; }
-keepclassmembers class com.example.nativesdk.** { *; }
```

Add to .csproj:

```xml
<ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-android'))">
  <ProguardConfiguration Include="Platforms\Android\proguard.cfg" />
</ItemGroup>
```

### Adding Android Permissions

If your SDK requires permissions, add to `AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

### Customizing Java Namespace Mapping

By default, `com.example.sdk` → `Com.Example.Sdk`. To customize:

```xml
<ItemGroup>
  <AndroidManagedLibraryReference Include="Jars\sdk.jar">
    <Bind>
      <Package>MyCompany.CustomNamespace</Package>
    </Bind>
  </AndroidManagedLibraryReference>
</ItemGroup>
```

## Testing Your Bindings

### Unit Testing

Create a test method in your MAUI app:

```csharp
public void TestNativeBinding()
{
    var service = new NativeService();
    service.Initialize("test-key");
    
    var result = service.PerformOperation("test input");
    Console.WriteLine($"Result: {result}");
    
    Assert.IsNotNull(result);
}
```

### Integration Testing

Test through the WebView bridge:

1. Run the MAUI app on Android emulator/device
2. Open Chrome DevTools: `chrome://inspect`
3. Select your WebView
4. Test in console:
   ```javascript
   await window.nativeBridge.initialize('test-key');
   await window.nativeBridge.performOperation('Hello!');
   ```

### Debug Logging

Enable verbose logging:

**File:** `MauiPwaShell/Platforms/Android/MainActivity.cs`

```csharp
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);
    
#if DEBUG
    // Enable WebView debugging
    Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
}
```

View logs:
```bash
adb logcat | grep -E "MAUI|ExampleSdk|chromium"
```

## Troubleshooting

### Issue: "Type or namespace 'Com' could not be found"

**Solution:**
- Verify Java source is compiling correctly
- Check package name matches between Java and C#
- Clean and rebuild: `dotnet clean && dotnet build`
- Check `@(AndroidJavaSource)` or `@(AndroidLibrary)` is properly configured

### Issue: "java.lang.NoClassDefFoundError" at runtime

**Solution:**
- Ensure .jar is marked as `AndroidLibrary`
- Check ProGuard isn't stripping your classes
- Verify all dependencies are included

### Issue: Methods not found or wrong signature

**Solution:**
- Ensure Java method signatures are compatible
- Use `@(BindingJavaSourceCodeImporter)` for better binding generation
- Check for overloaded methods - may need metadata transforms

### Issue: Build fails with binding errors

**Solution:**
- Check `obj/Debug/net8.0-android/api.xml` for what the binding generator sees
- Add metadata transforms to remove or rename problematic members
- Use `$(AndroidBoundInterfacesContainConstants)` and `$(AndroidBoundInterfacesContainTypes)` properties

### Issue: Native code crashes

**Solution:**
- Check Java exception stack traces in logcat
- Ensure Context is being passed correctly
- Verify SDK initialization sequence
- Check thread safety - use `MainThread.BeginInvokeOnMainThread` if needed

## Example: Real-World SDK Integration

Here's how to integrate a real SDK (e.g., a payment SDK):

1. **Obtain the SDK:**
   ```bash
   # Download from vendor or use Gradle dependency
   # For example: implementation 'com.payment:sdk:1.0.0'
   ```

2. **Extract .aar from Gradle:**
   ```bash
   # In an Android project with the dependency
   ./gradlew assembleDebug
   # Find in: ~/.gradle/caches/modules-2/files-2.1/com.payment/sdk/1.0.0/
   ```

3. **Add to MAUI project:**
   ```xml
   <ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-android'))">
     <AndroidLibrary Include="Platforms\Android\Libs\payment-sdk.aar" />
   </ItemGroup>
   ```

4. **Create service interface and implementation** (as shown above)

5. **Handle any transitive dependencies:**
   ```xml
   <ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-android'))">
     <PackageReference Include="Xamarin.AndroidX.AppCompat" Version="1.6.1.5" />
     <!-- Add other required AndroidX packages -->
   </ItemGroup>
   ```

## Best Practices

1. **Keep bindings thin:** Only expose what you need in C#
2. **Use interfaces:** Abstract platform-specific code behind interfaces
3. **Handle errors gracefully:** Wrap native calls in try-catch blocks
4. **Version control:** Track SDK versions and update regularly
5. **Document dependencies:** List all required libraries and permissions
6. **Test on real devices:** Emulators may not catch all issues
7. **Consider threading:** Some native SDKs require main thread execution

## Additional Resources

- [Xamarin Android Binding Documentation](https://learn.microsoft.com/xamarin/android/platform/binding-java-library/)
- [Android Java Interop](https://learn.microsoft.com/xamarin/android/platform/java-integration/)
- [Metadata Transforms Reference](https://learn.microsoft.com/xamarin/android/platform/binding-java-library/customizing-bindings/java-bindings-metadata)
- [MAUI Android Platform Integration](https://learn.microsoft.com/dotnet/maui/android/)

## Next Steps

1. Implement your actual Android SDK using this guide
2. Review the [iOS Bindings Guide](./native-bindings-ios.md)
3. Set up the [Web-to-Native Bridge](./web-to-native-bridge.md)
4. Test end-to-end integration
