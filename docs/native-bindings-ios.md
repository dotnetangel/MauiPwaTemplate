# iOS Native SDK Bindings - Step-by-Step Guide

## Overview

This guide walks through creating iOS native SDK bindings for your MAUI application using both the MAUI Slim Bindings approach and the traditional Binding Library approach.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Understanding iOS Binding Approaches](#understanding-ios-binding-approaches)
3. [Method 1: MAUI Slim Bindings (Recommended)](#method-1-maui-slim-bindings-recommended)
4. [Method 2: Traditional iOS Binding Library](#method-2-traditional-ios-binding-library)
5. [Testing Your Bindings](#testing-your-bindings)
6. [Troubleshooting](#troubleshooting)

## Prerequisites

- .NET 8.0 SDK or later
- macOS with Xcode 15+ (required for iOS development)
- Visual Studio 2022 for Mac or VS Code with C# Dev Kit
- MAUI workloads: `dotnet workload install maui`
- Your native iOS SDK (as .framework, .xcframework, or source .h/.m files)

## Understanding iOS Binding Approaches

### What is a Binding?

iOS native libraries are written in Objective-C or Swift. To use them from C#, we create "bindings" - C# definitions that map to native APIs using attributes and runtime interop.

### MAUI Slim Bindings vs Traditional Bindings

| Feature | Slim Bindings | Traditional Binding Library |
|---------|---------------|----------------------------|
| Project Type | Integrated in MAUI app | Separate binding project |
| API Definitions | In-place with attributes | Separate .cs files |
| Complexity | Simple | More complex |
| Reusability | App-specific | Can be packaged as NuGet |
| Best for | Small SDKs, prototypes | Large SDKs, team distribution |

## Method 1: MAUI Slim Bindings (Recommended)

This approach embeds the native library and binding definitions directly in your MAUI project.

### Step 1: Prepare Your Native iOS SDK

You need one of the following:
- **Option A:** Pre-compiled `.framework` or `.xcframework`
- **Option B:** Static library `.a` with header files `.h`
- **Option C:** Objective-C source files (`.h` and `.m`)

For this example, we'll use Objective-C source files.

### Step 2: Add Native Source to Your Project

#### Option A: Using Pre-compiled Framework

1. **Copy the framework to your project:**
   ```bash
   cp -r /path/to/MySDK.framework MauiPwaShell/Platforms/iOS/Frameworks/
   ```

2. **Add to .csproj:**
   ```xml
   <ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
     <NativeReference Include="Platforms\iOS\Frameworks\MySDK.framework">
       <Kind>Framework</Kind>
       <SmartLink>True</SmartLink>
     </NativeReference>
   </ItemGroup>
   ```

#### Option B: Using Static Library

1. **Add library and headers:**
   ```bash
   mkdir -p MauiPwaShell/Platforms/iOS/NativeLib
   cp /path/to/libMySDK.a MauiPwaShell/Platforms/iOS/NativeLib/
   cp /path/to/*.h MauiPwaShell/Platforms/iOS/NativeLib/
   ```

2. **Add to .csproj:**
   ```xml
   <ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
     <NativeReference Include="Platforms\iOS\NativeLib\libMySDK.a">
       <Kind>Static</Kind>
       <SmartLink>True</SmartLink>
       <ForceLoad>True</ForceLoad>
     </NativeReference>
   </ItemGroup>
   ```

#### Option C: Using Source Files (Demonstrated in Template)

1. **Organize your Objective-C files:**
   ```
   MauiPwaShell/
     NativeBindings/
       iOS/
         ExampleSdk.h
         ExampleSdk.m
   ```

2. **Add to .csproj:**
   ```xml
   <ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
     <Compile Include="NativeBindings\iOS\*.m">
       <Link>Platforms\iOS\NativeLib\%(Filename)%(Extension)</Link>
     </Compile>
     <ObjcBindingApiDefinition Include="NativeBindings\iOS\*.h" />
   </ItemGroup>
   ```

### Step 3: Create API Definitions with Attributes

This is where MAUI Slim Bindings shine. Define the C# interface to your Objective-C code using attributes.

**File:** `MauiPwaShell/Platforms/iOS/NativeService.cs`

```csharp
using Foundation;
using ObjCRuntime;

namespace MauiPwaShell.Services;

public partial class NativeService : INativeService
{
    private ExampleSdk? _sdk;

    public NativeService()
    {
        _sdk = new ExampleSdk();
    }

    public void Initialize(string apiKey)
    {
        if (_sdk == null)
        {
            _sdk = new ExampleSdk();
        }
        _sdk.InitializeWithApiKey(apiKey);
    }

    public string PerformOperation(string input)
    {
        if (_sdk == null)
        {
            throw new InvalidOperationException("SDK not initialized");
        }
        return _sdk.PerformOperation(input);
    }

    public string GetDeviceInfo()
    {
        if (_sdk == null)
        {
            throw new InvalidOperationException("SDK not initialized");
        }
        return _sdk.GetDeviceInfo();
    }

    public bool IsInitialized()
    {
        return _sdk?.IsInitialized ?? false;
    }

    public void Dispose()
    {
        _sdk?.Dispose();
        _sdk = null;
    }
}

// ============ Binding Definitions ============
// These attributes tell the runtime how to map C# to Objective-C

[BaseType(typeof(NSObject))]
interface ExampleSdk
{
    // Maps to: - (void)initializeWithApiKey:(NSString *)apiKey;
    [Export("initializeWithApiKey:")]
    void InitializeWithApiKey(string apiKey);

    // Maps to: - (NSString *)performOperation:(NSString *)input;
    [Export("performOperation:")]
    string PerformOperation(string input);

    // Maps to: - (NSString *)getDeviceInfo;
    [Export("getDeviceInfo")]
    string GetDeviceInfo();

    // Maps to: - (BOOL)isInitialized;
    [Export("isInitialized")]
    bool IsInitialized { get; }

    // Maps to: - (void)dispose;
    [Export("dispose")]
    void Dispose();
}
```

### Step 4: Understanding Binding Attributes

#### Common Attributes

**`[BaseType]`** - Specifies the base class
```csharp
[BaseType(typeof(NSObject))]  // Most common base
[BaseType(typeof(UIView))]     // For UI components
```

**`[Export]`** - Maps to Objective-C selector
```csharp
[Export("methodName")]           // No parameters
[Export("methodName:")]          // One parameter
[Export("method:withParam:")]    // Multiple parameters
```

**`[Static]`** - Class methods
```csharp
[Static]
[Export("sharedInstance")]
ExampleSdk SharedInstance { get; }
```

**`[Internal]`** - Hide from public API
```csharp
[Internal]
[Export("internalMethod")]
void InternalMethod();
```

#### Property Bindings

```csharp
// Read-only property
[Export("propertyName")]
string PropertyName { get; }

// Read-write property
[Export("propertyName")]
string PropertyName { get; set; }

// Property with custom getter
[Export("isEnabled")]
bool Enabled { [Bind("isEnabled")] get; }
```

#### Method Bindings

```csharp
// Simple method
[Export("simpleMethod")]
void SimpleMethod();

// Method with parameters
[Export("methodWithParam:")]
void MethodWithParam(string param);

// Method with multiple parameters
[Export("method:withSecondParam:")]
void Method(string first, int second);

// Method with return value
[Export("getString")]
string GetString();

// Async method (completion handler)
[Export("fetchDataWithCompletion:")]
void FetchData(Action<NSData, NSError> completion);
```

#### Advanced Bindings

**Protocols (Delegates):**
```csharp
[Protocol, Model]
[BaseType(typeof(NSObject))]
interface IExampleDelegate
{
    [Export("didComplete:")]
    void DidComplete(string result);
}

[BaseType(typeof(NSObject))]
interface ExampleSdk
{
    [Export("delegate", ArgumentSemantic.Weak)]
    IExampleDelegate? Delegate { get; set; }
}
```

**Categories/Extensions:**
```csharp
[Category]
[BaseType(typeof(NSString))]
interface NSString_Extensions
{
    [Export("customMethod")]
    string CustomMethod();
}
```

### Step 5: Handle Complex Types

#### Collections
```csharp
// NSArray<ObjectType>
[Export("getItems")]
ObjectType[] GetItems();

[Export("setItems:")]
void SetItems(ObjectType[] items);
```

#### Blocks (Closures)
```csharp
// Objective-C: void (^)(NSString *result, NSError *error)
[Export("executeWithCompletion:")]
void Execute(Action<NSString, NSError> completion);
```

#### Enums
```csharp
// Define C# enum matching Objective-C
public enum ExampleStatus : long
{
    None = 0,
    Active = 1,
    Completed = 2
}

[Export("status")]
ExampleStatus Status { get; }
```

### Step 6: Link Frameworks and Libraries

If your SDK depends on iOS frameworks:

```xml
<ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
  <!-- Link iOS system frameworks -->
  <FrameworkReference Include="UIKit" />
  <FrameworkReference Include="Foundation" />
  <FrameworkReference Include="CoreLocation" />
  
  <!-- Custom linker flags -->
  <MtouchExtraArgs>-gcc_flags "-ObjC -lstdc++"</MtouchExtraArgs>
</ItemGroup>
```

### Step 7: Build and Test

```bash
# Build for iOS (requires macOS)
dotnet build -f net8.0-ios

# Deploy to simulator
dotnet build -f net8.0-ios -t:Run
```

## Method 2: Traditional iOS Binding Library

For complex SDKs, create a dedicated binding library.

### Step 1: Create iOS Binding Library Project

```bash
# Create new binding library
dotnet new iosbinding -n MyiOSBindings

# Add to solution
dotnet sln add MyiOSBindings/MyiOSBindings.csproj
```

### Step 2: Add Native Library

Copy your framework/library to the binding project:

```bash
cp -r /path/to/MySDK.framework MyiOSBindings/
```

Update `.csproj`:

```xml
<ItemGroup>
  <NativeReference Include="MySDK.framework">
    <Kind>Framework</Kind>
    <SmartLink>True</SmartLink>
  </NativeReference>
</ItemGroup>
```

### Step 3: Create API Definitions

**File:** `MyiOSBindings/ApiDefinitions.cs`

```csharp
using System;
using Foundation;
using ObjCRuntime;

namespace MyiOSBindings
{
    [BaseType(typeof(NSObject))]
    interface MySDK
    {
        [Static]
        [Export("sharedInstance")]
        MySDK SharedInstance { get; }

        [Export("initializeWithApiKey:")]
        void Initialize(string apiKey);

        [Export("performAction:completion:")]
        void PerformAction(string action, Action<string, NSError> completion);
    }
}
```

### Step 4: Define Structs and Enums (if needed)

**File:** `MyiOSBindings/StructsAndEnums.cs`

```csharp
using System;
using ObjCRuntime;

namespace MyiOSBindings
{
    [Native]
    public enum MySDKStatus : long
    {
        None = 0,
        Active = 1,
        Completed = 2
    }
}
```

### Step 5: Build Binding Library

```bash
cd MyiOSBindings
dotnet build
```

### Step 6: Reference in MAUI Project

```xml
<ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
  <ProjectReference Include="..\MyiOSBindings\MyiOSBindings.csproj" />
</ItemGroup>
```

## Advanced Binding Scenarios

### Binding Swift Libraries

Swift libraries require additional setup:

1. **Generate Objective-C header:**
   In Xcode, ensure your Swift classes are marked with `@objc`:
   ```swift
   @objc public class MySwiftSDK: NSObject {
       @objc public func initialize(apiKey: String) {
           // ...
       }
   }
   ```

2. **Import the generated interface header:**
   Xcode generates `MySDK-Swift.h` automatically.

3. **Bind as normal:**
   ```csharp
   [BaseType(typeof(NSObject))]
   interface MySwiftSDK
   {
       [Export("initializeWithApiKey:")]
       void Initialize(string apiKey);
   }
   ```

### Working with XCFrameworks

XCFrameworks support multiple architectures:

```xml
<ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
  <NativeReference Include="MySDK.xcframework">
    <Kind>Framework</Kind>
    <SmartLink>True</SmartLink>
  </NativeReference>
</ItemGroup>
```

### Handling Dependencies

If your SDK depends on CocoaPods:

1. **Extract dependencies:**
   ```bash
   pod install
   # Find frameworks in Pods/ directory
   ```

2. **Add each framework:**
   ```xml
   <ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
     <NativeReference Include="Platforms\iOS\Frameworks\Alamofire.framework">
       <Kind>Framework</Kind>
     </NativeReference>
   </ItemGroup>
   ```

### Custom Linker Flags

Some SDKs require specific linker flags:

```xml
<PropertyGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
  <MtouchExtraArgs>$(MtouchExtraArgs) -gcc_flags "-ObjC -lstdc++ -lz"</MtouchExtraArgs>
  <MtouchFloat32>true</MtouchFloat32>
</PropertyGroup>
```

Common flags:
- `-ObjC` - Load all Objective-C classes (required for categories)
- `-lstdc++` - Link C++ standard library
- `-lz` - Link compression library
- `-lsqlite3` - Link SQLite

## Testing Your Bindings

### Create Test Methods

```csharp
#if IOS
public void TestIOSBinding()
{
    var sdk = new ExampleSdk();
    sdk.InitializeWithApiKey("test-key");
    
    var result = sdk.PerformOperation("test");
    Console.WriteLine($"iOS Result: {result}");
}
#endif
```

### Debug in Xcode

1. Build your MAUI app
2. Find the .app bundle in `bin/Debug/net8.0-ios/`
3. Open in Xcode
4. Set breakpoints in native code
5. Run and debug

### View Native Logs

```bash
# Using device console
xcrun simctl spawn booted log stream --predicate 'process == "YourAppName"'

# Or in Xcode: Window > Devices and Simulators > Open Console
```

## Troubleshooting

### Issue: "MT5210: Native linking failed"

**Solution:**
- Check that all required frameworks are linked
- Add missing linker flags: `-ObjC`, `-lstdc++`
- Verify framework paths are correct
- Check for architecture mismatches (arm64 vs x86_64)

### Issue: "Undefined symbols for architecture"

**Solution:**
- Framework doesn't contain the required architecture
- Add `<ForceLoad>True</ForceLoad>` to NativeReference
- Check that symbol exists in the library:
  ```bash
  nm -g MySDK.framework/MySDK | grep symbolName
  ```

### Issue: "Type or namespace not found"

**Solution:**
- Ensure `[BaseType]` and `[Export]` attributes are correct
- Check that native library is properly linked
- Clean and rebuild
- Verify conditional compilation: `#if IOS`

### Issue: Runtime crash with "Selector not found"

**Solution:**
- Check `[Export]` selector matches Objective-C method exactly
- Verify method signatures match (parameter types)
- Use Objective-C runtime tools:
  ```bash
  otool -L MySDK.framework/MySDK
  nm MySDK.framework/MySDK | grep "methodName"
  ```

### Issue: Memory leaks or crashes

**Solution:**
- Review object lifetime management
- Use weak references for delegates: `ArgumentSemantic.Weak`
- Ensure proper disposal of native objects
- Check for retain cycles

### Issue: Swift interop not working

**Solution:**
- Ensure Swift class/methods are marked with `@objc`
- Check that Swift symbols are exported:
  ```bash
  nm -g MySDK.framework/MySDK | grep "_TtC"
  ```
- Add compatibility header if needed
- Consider using `@objcMembers` on the class

## Real-World Example: Firebase-Like SDK

Here's how to bind a complex SDK structure:

```csharp
// Main SDK class
[BaseType(typeof(NSObject))]
interface FirebaseAnalytics
{
    [Static]
    [Export("sharedInstance")]
    FirebaseAnalytics SharedInstance { get; }

    [Export("logEventWithName:parameters:")]
    void LogEvent(string name, NSDictionary<NSString, NSObject> parameters);

    [Export("setUserProperty:forName:")]
    void SetUserProperty([NullAllowed] string value, string name);
}

// Configuration class
[BaseType(typeof(NSObject))]
interface FirebaseConfiguration
{
    [Static]
    [Export("defaultConfiguration")]
    FirebaseConfiguration DefaultConfiguration { get; }

    [Export("analyticsEnabled")]
    bool AnalyticsEnabled { get; set; }
}

// Delegate protocol
[Protocol, Model]
[BaseType(typeof(NSObject))]
interface FirebaseAnalyticsDelegate
{
    [Export("analyticsDidLogEvent:")]
    void DidLogEvent(string eventName);
}
```

## Best Practices

1. **Start small:** Bind only what you need initially
2. **Use attributes correctly:** Match Objective-C signatures exactly
3. **Handle memory:** Be mindful of object ownership semantics
4. **Test incrementally:** Test each binding as you add it
5. **Document mappings:** Keep notes on C# → Objective-C mappings
6. **Version carefully:** Track native SDK versions
7. **Consider async:** Use async/await for completion handlers
8. **Leverage smart linking:** Use `<SmartLink>True</SmartLink>` to reduce app size

## Converting Objective-C to C# Bindings Quick Reference

| Objective-C | C# Binding |
|-------------|------------|
| `- (void)method;` | `[Export("method")] void Method();` |
| `- (NSString *)getName;` | `[Export("getName")] string GetName();` |
| `- (void)setName:(NSString *)name;` | `[Export("setName:")] void SetName(string name);` |
| `+ (instancetype)sharedInstance;` | `[Static] [Export("sharedInstance")] MyClass SharedInstance { get; }` |
| `@property (nonatomic, strong) NSString *name;` | `[Export("name")] string Name { get; set; }` |
| `@property (nonatomic, weak) id<Delegate> delegate;` | `[Export("delegate", ArgumentSemantic.Weak)] IDelegate Delegate { get; set; }` |

## Additional Resources

- [Xamarin iOS Binding Documentation](https://learn.microsoft.com/xamarin/ios/platform/binding-objective-c/)
- [Objective-C Selectors](https://learn.microsoft.com/xamarin/ios/internals/objective-c-selectors)
- [iOS Platform Features](https://learn.microsoft.com/dotnet/maui/ios/)
- [Binding Types Reference](https://learn.microsoft.com/xamarin/cross-platform/macios/binding/binding-types-reference)

## Next Steps

1. Implement your actual iOS SDK using this guide
2. Review the [Android Bindings Guide](./native-bindings-android.md)
3. Set up the [Web-to-Native Bridge](./web-to-native-bridge.md)
4. Test end-to-end integration with the PWA
