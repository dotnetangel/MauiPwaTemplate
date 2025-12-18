# Native Library Interop - Gotchas and Best Practices

## Overview

This guide documents common issues, gotchas, and best practices when working with native library interop in .NET MAUI, based on real-world experience, the Microsoft blog post on [Native Library Interop in .NET MAUI](https://devblogs.microsoft.com/dotnet/native-library-interop-dotnet-maui/), and community feedback.

## Table of Contents

1. [Critical Gotchas](#critical-gotchas)
2. [Android-Specific Issues](#android-specific-issues)
3. [iOS-Specific Issues](#ios-specific-issues)
4. [Build and Deployment](#build-and-deployment)
5. [Performance Considerations](#performance-considerations)
6. [Debugging Tips](#debugging-tips)
7. [Best Practices](#best-practices)

## Critical Gotchas

### 1. Package Name Capitalization (Android)

**Issue:** Java package names are lowercase, but C# namespaces use PascalCase.

**Example:**
```java
// Java package
package com.example.mysdk;

public class MyClass { }
```

```csharp
// C# binding - note the capitalization!
var instance = new Com.Example.Mysdk.MyClass();
```

**Gotcha:** The binding generator capitalizes the first letter of each package segment:
- `com.example.mysdk` → `Com.Example.Mysdk` (not `Com.Example.MySdk`)
- This can cause confusion if you expect `MySdk` with capital 'S'

**Solution:** Always check the generated bindings in `obj/Debug/net8.0-android/generated/` to see the exact namespace.

### 2. Objective-C Selector Naming (iOS)

**Issue:** Objective-C method names don't directly map to C# method names.

**Example:**
```objc
// Objective-C
- (NSString *)performOperationWithInput:(NSString *)input 
                              andValue:(int)value;
```

```csharp
// C# binding - selector must match exactly
[Export("performOperationWithInput:andValue:")]
string PerformOperation(string input, int value);
```

**Gotcha:** The selector is `performOperationWithInput:andValue:` - note the colons! Missing or incorrect colons will cause runtime crashes.

**Solution:** Use Xcode or `nm` command to verify selector names:
```bash
nm -g YourLibrary.framework/YourLibrary | grep "perform"
```

### 3. Gradle vs Direct JAR/AAR (Android)

**Issue:** Modern Android libraries often use Gradle, which can be complex to integrate.

**Options:**
1. **AndroidGradleProject** (recommended) - References Gradle project directly
2. **AndroidLibrary** - Uses pre-built .aar/.jar files

**Gotcha:** If using Gradle:
- Must have `build.gradle.kts` or `build.gradle` file
- Module name must match between Gradle and .csproj
- Gradle build happens during .NET build (can be slow)

**Example (.csproj):**
```xml
<!-- Option 1: Gradle project -->
<AndroidGradleProject Include="../native/android/build.gradle.kts">
  <ModuleName>mylibrary</ModuleName>
</AndroidGradleProject>

<!-- Option 2: Pre-built AAR -->
<AndroidLibrary Include="libs/mylibrary.aar" />
```

**Solution:** For simple SDKs, extract the .aar from Gradle cache and use `AndroidLibrary`. For complex SDKs with dependencies, use `AndroidGradleProject`.

### 4. Xcode Project Integration (iOS)

**Issue:** iOS binding requires either source files or frameworks.

**Gotcha:** Using `XcodeProject` requires:
- Valid Xcode project file (.xcodeproj)
- Scheme name must match
- Xcode must be installed (macOS only)
- Build happens during .NET build

**Example:**
```xml
<XcodeProject Include="../native/ios/MySDK.xcodeproj">
  <SchemeName>MySDK</SchemeName>
</XcodeProject>
```

**Alternative for simple cases:**
```xml
<!-- Include Objective-C source directly -->
<Compile Include="native/*.m">
  <Link>Platforms/iOS/Native/%(Filename)%(Extension)</Link>
</Compile>
```

**Solution:** For simple SDKs, include source files directly. For frameworks, use `NativeReference` with pre-built framework.

### 5. ProGuard/R8 Issues (Android)

**Issue:** Code shrinking can remove classes the binding needs at runtime.

**Symptoms:**
- Build succeeds but crashes at runtime with `ClassNotFoundException`
- Works in Debug but fails in Release

**Solution:** Add ProGuard rules:
```proguard
# In proguard.cfg
-keep class com.example.mysdk.** { *; }
-keepclassmembers class com.example.mysdk.** { *; }
```

Then reference in .csproj:
```xml
<ProguardConfiguration Include="Platforms/Android/proguard.cfg" />
```

### 6. Metadata Transforms (Android)

**Issue:** Binding generator may create incorrect or conflicting bindings.

**Common problems:**
- Duplicate type names
- Incorrect generic type mappings
- Methods that shouldn't be public

**Solution:** Use `Metadata.xml` to fix bindings:
```xml
<!-- In Transforms/Metadata.xml -->
<metadata>
  <!-- Remove a problematic class -->
  <remove-node path="/api/package[@name='com.example']/class[@name='InternalClass']" />
  
  <!-- Rename a conflicting type -->
  <attr path="/api/package[@name='com.example']/class[@name='Builder']" 
        name="managedName">SdkBuilder</attr>
  
  <!-- Make internal classes internal in C# -->
  <attr path="/api/package[@name='com.example.internal']" 
        name="visibility">internal</attr>
</metadata>
```

## Android-Specific Issues

### Java Version Compatibility

**Issue:** Java code must be compiled for compatible JVM version.

**Gotcha:** .NET Android typically uses Java 8 bytecode.

**Solution:**
```gradle
android {
    compileOptions {
        sourceCompatibility JavaVersion.VERSION_1_8
        targetCompatibility JavaVersion.VERSION_1_8
    }
}
```

### AndroidX Dependencies

**Issue:** Modern Android libraries require AndroidX packages.

**Symptom:** Build errors like "NoClassDefFoundError" for androidx classes.

**Solution:** Add NuGet packages:
```xml
<PackageReference Include="Xamarin.AndroidX.AppCompat" Version="1.6.1" />
<PackageReference Include="Xamarin.AndroidX.Core" Version="1.12.0" />
```

### AAR File Structure

**Gotcha:** AAR files are ZIP archives containing:
- `classes.jar` - compiled Java classes
- `AndroidManifest.xml` - manifest
- `res/` - resources
- `libs/` - native .so libraries

**Issue:** If AAR contains native libraries, they must be extracted and included separately.

**Solution:**
```bash
# Extract AAR
unzip mylibrary.aar -d mylibrary/

# Check for native libs
ls mylibrary/jni/
```

If native libs exist, include them:
```xml
<AndroidNativeLibrary Include="jni/arm64-v8a/libnative.so">
  <Abi>arm64-v8a</Abi>
</AndroidNativeLibrary>
```

### Context Requirements

**Gotcha:** Many Android APIs require a `Context` parameter.

**Common mistake:**
```csharp
// Wrong - might be null
var sdk = new MySdk(Android.App.Application.Context);
```

**Solution:**
```csharp
// In a Service or Activity
public NativeService()
{
    _context = Android.App.Application.Context 
        ?? throw new InvalidOperationException("Context not available");
    _sdk = new MySdk(_context);
}
```

## iOS-Specific Issues

### Framework vs Dynamic Library vs Static Library

**Types:**
- **Framework** (.framework) - Bundle with headers, resources, and library
- **Dynamic Library** (.dylib) - Shared library
- **Static Library** (.a) - Linked into app binary

**Gotcha:** Each type requires different configuration.

**Framework:**
```xml
<NativeReference Include="MySDK.framework">
  <Kind>Framework</Kind>
  <SmartLink>True</SmartLink>
</NativeReference>
```

**Static Library:**
```xml
<NativeReference Include="libMySDK.a">
  <Kind>Static</Kind>
  <ForceLoad>True</ForceLoad>
</NativeReference>
```

**Dynamic Library (for app):**
```xml
<NativeReference Include="libMySDK.dylib">
  <Kind>Dynamic</Kind>
</NativeReference>
```

### Linker Flags

**Issue:** Some frameworks require additional linker flags.

**Common flags needed:**
- `-ObjC` - Required for categories (Objective-C extensions)
- `-lstdc++` - C++ standard library
- `-lz` - Compression library
- `-framework CoreLocation` - System frameworks

**Solution:**
```xml
<MtouchExtraArgs>$(MtouchExtraArgs) -gcc_flags "-ObjC -lstdc++"</MtouchExtraArgs>
```

### Architecture Issues

**Gotcha:** iOS requires different binaries for:
- Device (arm64)
- Simulator (x86_64, arm64 for M1+ Macs)

**Issue:** Framework might not contain all architectures.

**Check architectures:**
```bash
lipo -info MySDK.framework/MySDK
# Output: Architectures in the fat file: MySDK.framework/MySDK are: arm64 x86_64
```

**Solution for missing architectures:**
- Ask SDK vendor for universal/xcframework
- Or use separate frameworks for device/simulator

### Swift Interop

**Issue:** Swift libraries require special handling.

**Requirements:**
1. Swift class must be marked `@objc`:
```swift
@objc public class MySDK: NSObject {
    @objc public func initialize(apiKey: String) {
        // ...
    }
}
```

2. Swift generates an Objective-C interface header (automatically)

3. Binding definitions reference the Objective-C interface:
```csharp
[BaseType(typeof(NSObject))]
interface MySDK
{
    [Export("initializeWithApiKey:")]
    void Initialize(string apiKey);
}
```

**Gotcha:** Not all Swift features are Objective-C compatible (generics, tuples, etc.).

### Property vs Method Bindings

**Issue:** Objective-C properties can be bound as either properties or methods.

**Property binding:**
```csharp
[Export("isEnabled")]
bool IsEnabled { get; }
```

**Method binding:**
```csharp
[Export("isEnabled")]
bool IsEnabled();
```

**Gotcha:** Properties generate `get_IsEnabled()` method calls, while method binding generates direct calls. Choose based on how the property is implemented.

## Build and Deployment

### Build Order Dependencies

**Issue:** Native projects must build before binding projects.

**Gotcha:** Sometimes builds fail because native build hasn't completed.

**Solution:** Clean and rebuild:
```bash
dotnet clean
dotnet build
```

Or add explicit dependencies in .csproj if using separate projects.

### Cache Issues

**Symptom:** Changes to native code don't appear in C# bindings.

**Locations to clear:**
- `obj/` - Build intermediate files
- `bin/` - Output binaries
- For Android: `~/.gradle/caches/` (if using Gradle)

**Solution:**
```bash
# Clean everything
dotnet clean
rm -rf obj/ bin/
dotnet build
```

### Deployment to Physical Devices

**Android Gotcha:** Emulator uses x86/x86_64, physical devices use ARM.

**Solution:** Build for all architectures:
```xml
<ItemGroup>
  <AndroidNativeLibrary Include="libs/arm64-v8a/libsdk.so">
    <Abi>arm64-v8a</Abi>
  </AndroidNativeLibrary>
  <AndroidNativeLibrary Include="libs/armeabi-v7a/libsdk.so">
    <Abi>armeabi-v7a</Abi>
  </AndroidNativeLibrary>
</ItemGroup>
```

**iOS Gotcha:** Simulator and device require different frameworks.

**Solution:** Use xcframework (supports multiple architectures) or conditional compilation.

## Performance Considerations

### Marshalling Overhead

**Issue:** Crossing the native boundary has overhead.

**Avoid:**
```csharp
// Bad - multiple native calls in loop
for (int i = 0; i < 1000; i++)
{
    nativeSDK.ProcessItem(i);
}
```

**Prefer:**
```csharp
// Good - single native call with batch
nativeSDK.ProcessItems(items);
```

### String Marshalling

**Gotcha:** String marshalling allocates memory on both sides.

**For Android (Java strings):**
- Use `string` in C# - automatically marshalled
- Large strings are expensive to marshal

**For iOS (NSString):**
- Use `string` in C# - automatically converted
- Consider `NSString` for frequent operations

### Memory Management

**Issue:** Native objects may not be garbage collected promptly.

**Solution:** Implement IDisposable:
```csharp
public class NativeService : INativeService, IDisposable
{
    private MySdk? _sdk;
    
    public void Dispose()
    {
        _sdk?.Dispose();
        _sdk = null;
        GC.SuppressFinalize(this);
    }
}
```

## Debugging Tips

### Enable Verbose Logging

**Android:**
```bash
adb logcat | grep -E "MAUI|YourSDK|mono"
```

**iOS:**
```bash
# Use Xcode Devices & Simulators -> Open Console
# Or with device connected:
idevicesyslog | grep YourSDK
```

### Check Generated Bindings (Android)

**Location:** `obj/Debug/net8.0-android/generated/`

**Files to check:**
- `src/` - Generated C# binding code
- `__AndroidLibraryProjects__` - Extracted AAR contents

### Verify Native Library Loading

**Test code:**
```csharp
try
{
    var sdk = new Com.Example.Mysdk.MyClass();
    Console.WriteLine("Native library loaded successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to load: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
```

### Use Binding Generator Verbosity

**In .csproj:**
```xml
<PropertyGroup>
  <AndroidBoundInterfacesContainConstants>true</AndroidBoundInterfacesContainConstants>
  <AndroidBoundInterfacesContainTypes>true</AndroidBoundInterfacesContainTypes>
  <AndroidEnableMultiDex>true</AndroidEnableMultiDex>
  <!-- Verbose binding generation -->
  <AndroidVerboseBindings>true</AndroidVerboseBindings>
</PropertyGroup>
```

## Best Practices

### 1. Start Simple

Begin with a minimal SDK implementation:
- One class with one method
- No dependencies
- Verify binding works before adding complexity

### 2. Version Control Native Code

**Structure:**
```
YourProject/
├── native/
│   ├── android/
│   │   ├── build.gradle.kts
│   │   └── src/
│   └── ios/
│       └── MySDK.xcodeproj
├── MauiApp/
│   ├── NativeBindings/
│   └── ...
```

### 3. Use Separate Binding Projects for Large SDKs

**When to separate:**
- SDK has >10 classes
- Will be reused across multiple apps
- Want to distribute as NuGet

**Structure:**
```
Solution/
├── MySDK.Android.Binding/
├── MySDK.iOS.Binding/
└── MyApp/
```

### 4. Document Native Dependencies

Create a document listing:
- Native SDK version
- Minimum OS versions required
- Required system frameworks (iOS)
- Required permissions (Android)
- Known issues with SDK versions

### 5. Test on Physical Devices Early

Emulators/simulators may hide issues:
- ARM architecture bugs
- Performance problems
- Hardware-specific features

### 6. Handle Platform Differences

Use conditional compilation:
```csharp
#if ANDROID
    var result = _androidSdk.DoSomething();
#elif IOS
    var result = _iosSdk.DoSomething();
#else
    var result = "Unsupported platform";
#endif
```

Or use platform abstractions:
```csharp
public interface INativeService
{
    string DoSomething();
}

// Android implementation
public partial class NativeService : INativeService { }

// iOS implementation  
public partial class NativeService : INativeService { }
```

### 7. Keep Bindings Updated

When updating native SDK:
1. Update native library files
2. Clean and rebuild
3. Check for breaking changes in generated bindings
4. Test all functionality
5. Update version number in your app

### 8. Use Strong Names Carefully

**Android:** Package names are case-sensitive in Java but become PascalCase in C#.

**iOS:** Objective-C class names should be unique (use prefixes like `MY` or `MyCompany`).

**Both:** Avoid naming conflicts with system types or popular libraries.

### 9. Handle Initialization Timing

Some SDKs require initialization at specific times:

```csharp
// Android - in MainActivity.OnCreate
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);
    
    // Initialize SDK early
    MySdk.Initialize(this);
}

// iOS - in AppDelegate.FinishedLaunching
public override bool FinishedLaunching(UIApplication app, NSDictionary options)
{
    // Initialize SDK early
    MySdk.Initialize();
    
    return base.FinishedLaunching(app, options);
}
```

### 10. Plan for SDK Updates

Native SDKs update frequently. Make it easy to update:
- Keep native files in separate directory
- Document update procedure
- Consider automated update checks
- Version binding libraries separately from app

## Common Error Messages and Solutions

### "Could not load assembly"

**Cause:** Native library not found or wrong architecture.

**Solution:** Verify library is included in app package and matches device architecture.

### "Method not found" or "NoSuchMethodError"

**Cause:** Binding signature doesn't match native method.

**Solution:** Check method signatures, parameters, and return types. Verify selector (iOS) or method name (Android).

### "JNI ERROR (app bug): accessed deleted global reference"

**Cause:** Android - Using disposed Java object.

**Solution:** Don't cache Java objects across native boundaries. Recreate as needed.

### "Selector not recognized"

**Cause:** iOS - Export selector doesn't match Objective-C method.

**Solution:** Verify selector spelling and colons. Use `nm` or Xcode to find correct selector.

## Additional Resources

- **Microsoft Blog:** [Native Library Interop in .NET MAUI](https://devblogs.microsoft.com/dotnet/native-library-interop-dotnet-maui/)
- **Community Toolkit:** [Maui.NativeLibraryInterop Repository](https://github.com/CommunityToolkit/Maui.NativeLibraryInterop)
- **Documentation:** [Binding Java Libraries](https://learn.microsoft.com/xamarin/android/platform/binding-java-library/)
- **Documentation:** [Binding iOS Libraries](https://learn.microsoft.com/xamarin/ios/platform/binding-objective-c/)

## Contributing

Found a gotcha not listed here? Please contribute by:
1. Opening an issue
2. Submitting a PR with the gotcha and solution
3. Sharing on community forums

Remember: The best way to learn is by doing. Start simple, test frequently, and don't hesitate to ask for help in the community!
