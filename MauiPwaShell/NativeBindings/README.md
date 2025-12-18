# Native SDK Bindings

This directory contains example native SDK files for Android and iOS that demonstrate how to integrate platform-specific SDKs with your MAUI application using the approach recommended by the .NET MAUI Community Toolkit Native Library Interop.

## Directory Structure

```
NativeBindings/
├── Android/
│   └── ExampleSdk.java         # Sample Android SDK (Java)
└── iOS/
    ├── ExampleSdk.h            # Sample iOS SDK header (Objective-C)
    └── ExampleSdk.m            # Sample iOS SDK implementation (Objective-C)
```

## What's Included

### Android SDK (ExampleSdk.java)

A sample Android SDK written in Java that demonstrates:
- SDK initialization with API key
- Performing operations with input/output
- Getting device information
- Proper state management and cleanup

**Package:** `com.example.nativesdk`

### iOS SDK (ExampleSdk.h/m)

A sample iOS SDK written in Objective-C that demonstrates:
- SDK initialization with API key
- Performing operations with input/output
- Getting device information
- Proper state management and cleanup

## Integration Approach

This implementation follows the .NET MAUI Community Toolkit Native Library Interop pattern (see [Maui.NativeLibraryInterop](https://github.com/CommunityToolkit/Maui.NativeLibraryInterop)).

### Key Concepts

**Android:**
- Native Java/Kotlin code in separate files
- Compiled and bound using `AndroidJavaSource` in .csproj
- C# wrappers access Java classes via generated bindings

**iOS:**
- Native Objective-C/Swift code in separate files  
- Binding definitions using `[BaseType]` and `[Export]` attributes
- C# wrappers access Objective-C classes via attributes

### Inline Bindings (Used Here)

For simplicity, this template uses "inline bindings" where:
- Native source files are included directly in the MAUI project
- Binding definitions are in the same file as the service implementation
- No separate binding library projects needed

This is suitable for:
- Small to medium SDKs
- Rapid prototyping
- App-specific integrations

### Traditional Binding Projects (Alternative)

For larger SDKs, the Community Toolkit recommends separate binding projects:
- `YourSdk.Android.Binding` - Android binding library project
- `YourSdk.iOS.Binding` - iOS binding library project
- Reference these from your MAUI app

See the [template directory](https://github.com/CommunityToolkit/Maui.NativeLibraryInterop/tree/main/template) for the full structure.

## Integration with MAUI

### Android Integration

The Java source files are automatically compiled and bound during the build process:

```xml
<ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-android'))">
  <AndroidJavaSource Include="NativeBindings\Android\*.java" />
</ItemGroup>
```

The C# wrapper is located at: `MauiPwaShell/Platforms/Android/NativeService.cs`

**Generated Bindings:**
```csharp
// Java: com.example.nativesdk.ExampleSdk
// C#:   Com.Example.Nativesdk.ExampleSdk
var sdk = new Com.Example.Nativesdk.ExampleSdk(context);
sdk.Initialize(apiKey);
```

### iOS Integration

The Objective-C source files are compiled and binding definitions use attributes:

```xml
<ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
  <Compile Include="NativeBindings\iOS\*.m">
    <Link>Platforms\iOS\NativeLib\%(Filename)%(Extension)</Link>
  </Compile>
</ItemGroup>
```

The C# wrapper with binding attributes is located at: `MauiPwaShell/Platforms/iOS/NativeService.cs`

**Binding Definitions:**
```csharp
[BaseType(typeof(NSObject))]
interface ExampleSdk
{
    [Export("initializeWithApiKey:")]
    void InitializeWithApiKey(string apiKey);
    
    [Export("performOperation:")]
    string PerformOperation(string input);
}
```

## Replacing with Your Own SDK

These are **example files** meant to demonstrate the integration pattern. To use your own native SDKs:

### For Android:

1. **Replace** `ExampleSdk.java` with your actual SDK files
   - Can use .jar, .aar, or .java source files
   - Update package name in Java code
   
2. **Update** `MauiPwaShell/Platforms/Android/NativeService.cs`
   - Change namespace: `Com.Example.Nativesdk` → `Com.Yourcompany.Yoursdk`
   - Update method calls to match your SDK's API
   
3. **Modify** `.csproj` if using .jar/.aar files:
   ```xml
   <ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-android'))">
     <AndroidLibrary Include="Platforms\Android\Libs\yoursdk.aar" />
   </ItemGroup>
   ```

### For iOS:

1. **Replace** `ExampleSdk.h/m` with your actual SDK files
   - Can use .framework, .xcframework, or Objective-C/Swift sources
   - Update class names and method signatures
   
2. **Update** `MauiPwaShell/Platforms/iOS/NativeService.cs`
   - Update `[Export]` attributes to match Objective-C selectors
   - Change interface name to match your SDK class
   - Update method signatures
   
3. **Modify** `.csproj` if using frameworks:
   ```xml
   <ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
     <NativeReference Include="Platforms\iOS\Frameworks\YourSDK.framework">
       <Kind>Framework</Kind>
       <SmartLink>True</SmartLink>
     </NativeReference>
   </ItemGroup>
   ```

## Binding Attributes Reference

### iOS Attributes

- **`[BaseType(typeof(...))]`** - Declares the base type for the interface
- **`[Export("selector:")]`** - Maps C# method to Objective-C selector
- **`[Static]`** - Marks a class method (Objective-C `+` methods)
- **`[Internal]`** - Makes the binding internal (not public)

**Example:**
```csharp
[BaseType(typeof(NSObject))]
interface MySDK
{
    [Static]
    [Export("sharedInstance")]
    MySDK SharedInstance { get; }
    
    [Export("doSomethingWithValue:")]
    void DoSomething(int value);
}
```

### Android Binding

Android bindings are automatically generated from Java bytecode:
- Package names: `com.example` → `Com.Example`
- Class names remain the same
- Method names follow C# conventions (PascalCase)

## Documentation

For detailed step-by-step guides:

- **[Overview](../docs/native-bindings-overview.md)** - Architecture and approaches
- **[Android Bindings](../docs/native-bindings-android.md)** - Complete Android guide
- **[iOS Bindings](../docs/native-bindings-ios.md)** - Complete iOS guide
- **[Web-to-Native Bridge](../docs/web-to-native-bridge.md)** - Bridge implementation
- **[Community Toolkit](https://github.com/CommunityToolkit/Maui.NativeLibraryInterop)** - Official repository with templates

## Testing the Example SDKs

The example SDKs are fully functional and can be tested through the PWA interface:

1. Run the MAUI application on Android or iOS
2. Navigate to the "Native SDK Integration" section in the PWA
3. Click "Initialize SDK" to initialize with a test API key
4. Click "Perform Operation" to test SDK operations
5. Click "Get Device Info" to retrieve device information

The SDK operations will execute in the native layer and return results to the web interface.

## Build Requirements

### Android

- Android SDK API level 21 or higher
- Java 8 compatibility for source compilation

### iOS

- Xcode 15+ (macOS only)
- Objective-C runtime support

## Troubleshooting

### Android: "Type 'Com.Example.Nativesdk.ExampleSdk' not found"

- Clean and rebuild the project
- Verify the package name in Java matches the C# usage
- Check that `AndroidJavaSource` is properly configured

### iOS: "Undefined symbols for architecture"

- Ensure .m files are being compiled
- Check that binding definitions match the Objective-C headers
- Verify linker flags if needed

### General: Bridge not responding

- Check that the native service is properly initialized
- Verify platform-specific implementations exist
- Review console logs for initialization errors

## Reference

This implementation is based on the .NET MAUI Community Toolkit Native Library Interop approach:
- **Repository**: https://github.com/CommunityToolkit/Maui.NativeLibraryInterop
- **Documentation**: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/native-library-interop/get-started
- **Templates**: Available in the repository's template directory

## License

These example files are provided as part of the MAUI PWA Template for demonstration purposes.

