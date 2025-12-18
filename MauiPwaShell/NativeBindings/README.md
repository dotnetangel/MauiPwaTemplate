# Native SDK Bindings

This directory contains example native SDK files for Android and iOS that demonstrate how to integrate platform-specific SDKs with your MAUI application.

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

## Integration with MAUI

These native SDK files are integrated into the MAUI project using the **Slim Bindings** approach:

### Android Integration

The Java source files are automatically compiled and bound during the build process:

```xml
<ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-android'))">
  <AndroidJavaSource Include="NativeBindings\Android\*.java" />
</ItemGroup>
```

The C# wrapper is located at: `MauiPwaShell/Platforms/Android/NativeService.cs`

### iOS Integration

The Objective-C source files are compiled and the binding definitions are included:

```xml
<ItemGroup Condition="$(TargetFramework.StartsWith('net8.0-ios'))">
  <Compile Include="NativeBindings\iOS\*.m">
    <Link>Platforms\iOS\NativeLib\%(Filename)%(Extension)</Link>
  </Compile>
</ItemGroup>
```

The C# wrapper with binding attributes is located at: `MauiPwaShell/Platforms/iOS/NativeService.cs`

## Replacing with Your Own SDK

These are **example files** meant to demonstrate the integration pattern. To use your own native SDKs:

### For Android:

1. Replace `ExampleSdk.java` with your actual SDK files (.jar, .aar, or .java sources)
2. Update `MauiPwaShell/Platforms/Android/NativeService.cs` to match your SDK's API
3. Update the package name references in the C# code
4. Modify the `.csproj` file if using .jar/.aar files instead of source

### For iOS:

1. Replace `ExampleSdk.h/m` with your actual SDK files (.framework, .xcframework, or sources)
2. Update `MauiPwaShell/Platforms/iOS/NativeService.cs` binding definitions
3. Update the `[Export]` attributes to match your SDK's selectors
4. Modify the `.csproj` file if using frameworks instead of source

## Documentation

For detailed step-by-step guides on creating native bindings:

- **[Overview](../docs/native-bindings-overview.md)** - Architecture and approaches
- **[Android Bindings](../docs/native-bindings-android.md)** - Complete Android guide
- **[iOS Bindings](../docs/native-bindings-ios.md)** - Complete iOS guide
- **[Web-to-Native Bridge](../docs/web-to-native-bridge.md)** - Bridge implementation

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

## License

These example files are provided as part of the MAUI PWA Template for demonstration purposes.
