# Native SDK Bindings Integration Guide

## Overview

This guide demonstrates how to integrate native platform-specific SDKs into your MAUI PWA application using Java (Android) and Objective-C (iOS) bindings, following the .NET MAUI Community Toolkit Native Library Interop approach. The implementation showcases a complete end-to-end integration where web-based button clicks in the PWA can trigger native SDK APIs through a JavaScript-to-Native bridge.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Native Binding Approaches](#native-binding-approaches)
3. [Quick Start](#quick-start)
4. [Implementation Guides](#implementation-guides)
5. [Web-to-Native Bridge](#web-to-native-bridge)
6. [Testing and Debugging](#testing-and-debugging)

## Getting Started

- **New to native bindings?** → [Quick Start Guide](./native-bindings-quickstart.md)
- **Want to avoid common issues?** → [⚠️ Gotchas & Best Practices](./native-bindings-gotchas.md)
- **Want detailed steps?** → See Implementation Guides below
- **Ready to test?** → [Testing and Building Guide](./testing-and-building.md)

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                         PWA (Web Layer)                      │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  HTML/CSS/JavaScript (index.html, main.js)            │ │
│  │  - Button click handlers                               │ │
│  │  - window.nativeBridge interface                       │ │
│  └────────────────────────────────────────────────────────┘ │
└────────────────────┬────────────────────────────────────────┘
                     │ JavaScript Bridge (URL scheme)
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              MAUI Shell (WebView Container)                  │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  MainPage.xaml.cs - WebView Event Handlers            │ │
│  │  - Intercepts nativebridge:// calls                    │ │
│  │  - Routes to NativeBridge service                      │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  NativeBridge Service (Services/NativeBridge.cs)       │ │
│  │  - Message routing and serialization                   │ │
│  │  - Action handlers (initialize, performOperation, etc) │ │
│  └────────────────────────────────────────────────────────┘ │
└────────────────────┬────────────────────────────────────────┘
                     │ Interface (INativeService)
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Platform-Specific Services                      │
│  ┌──────────────────────┐    ┌─────────────────────────┐   │
│  │  Android              │    │  iOS                    │   │
│  │  NativeService.cs     │    │  NativeService.cs       │   │
│  │  (Platforms/Android)  │    │  (Platforms/iOS)        │   │
│  │  - LibraryImport      │    │  - LibraryImport        │   │
│  │  - P/Invoke calls     │    │  - P/Invoke calls       │   │
│  └──────────────────────┘    └─────────────────────────┘   │
└────────────────────┬────────────────────┬───────────────────┘
                     │                     │
                     ▼                     ▼
┌──────────────────────────────┐  ┌─────────────────────────┐
│  Native C/C++ Library        │  │  Native C/C++ Library   │
│  (.so libraries)             │  │  (compiled into app)    │
│  - libexamplesdk.so          │  │  - examplesdk.c         │
│  - Direct C function calls   │  │  - Direct C calls       │
└──────────────────────────────┘  └─────────────────────────┘
```

## Native Binding Approaches

### 1. LibraryImport (Recommended - Used in This Template)

**What it is:** Modern P/Invoke using source-generated interop introduced in .NET 7+. This is the recommended approach by the .NET MAUI Community Toolkit for calling native C/C++ libraries.

**Pros:**
- Cross-platform C/C++ codebase (write once, use everywhere)
- Modern source-generated P/Invoke (faster, AOT-friendly)
- No need for platform-specific wrapper layers (Java/Objective-C)
- Better performance - direct native calls
- Type-safe with compile-time checking
- Simpler maintenance - single codebase

**Cons:**
- Requires building native C/C++ libraries
- Need NDK (Android) and Xcode (iOS) for compilation
- Less suitable if SDK is only available as Java/.NET/Objective-C

**Best for:** 
- Native C/C++ libraries
- Performance-critical operations
- Cross-platform native code
- Direct hardware/OS API access

**Example:**
```csharp
[LibraryImport("examplesdk", StringMarshalling = StringMarshalling.Utf8)]
[return: MarshalAs(UnmanagedType.I4)]
private static partial int ExampleSdk_Initialize(string apiKey);
```

### 2. DllImport (Legacy P/Invoke)

**What it is:** Traditional P/Invoke for calling native libraries.

**Pros:**
- Works with older .NET versions
- Well-documented and widely used
- Compatible with existing code

**Cons:**
- Runtime marshaling (slower than LibraryImport)
- Not AOT-friendly
- More marshaling overhead

**Best for:**
- Compatibility with older codebases
- When LibraryImport is not available

### 3. Java/Objective-C Bindings (Alternative Approach)

**What it is:** Creating C# wrappers for Java (Android) and Objective-C/Swift (iOS) SDKs.

**Pros:**
- Works with platform-specific SDKs
- Can bind existing Java/Swift libraries
- Good for platform-specific features

**Cons:**
- Requires separate implementations for each platform
- More complex with wrapper layers
- Additional marshaling overhead
- Two codebases to maintain

**Best for:**
- When SDK is only available as Java/Objective-C
- Platform-specific features without C API
- Existing SDKs without C interface


## Implementation Guides

Detailed step-by-step guides for each platform:

- **[🚀 Quick Start Guide](./native-bindings-quickstart.md)** - Get started in 15 minutes
- **[Android Native Bindings](./native-bindings-android.md)** - Complete guide for Android LibraryImport integration
- **[iOS Native Bindings](./native-bindings-ios.md)** - Complete guide for iOS LibraryImport integration
- **[Web-to-Native Bridge](./web-to-native-bridge.md)** - JavaScript bridge implementation details
- **[Testing and Building](./testing-and-building.md)** - Comprehensive testing and build guide

## Quick Start

**New to native bindings?** Start here: **[Quick Start Guide](./native-bindings-quickstart.md)**

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 17.8+ or Visual Studio Code with C# Dev Kit
- MAUI workloads installed: `dotnet workload install maui`
- For Android: Android SDK (API 21+) and Android NDK
- For iOS: Xcode 15+ (macOS only)
- C/C++ development tools

### Basic Setup Steps

1. **Create Your Native C/C++ Library**
   - Write your native code in C/C++ with exported functions
   - Use proper export macros (`__declspec(dllexport)` on Windows, `__attribute__((visibility("default")))` on Unix)
   - Review the example library in `NativeBindings/examplesdk.h/c`

2. **Build Native Libraries**
   - Android: Use NDK to build `.so` files for each architecture
   - iOS: Compile directly into app or build `.dylib`/`.a` files
   - Use provided build scripts: `build-android.sh` and `build-ios.sh`

3. **Create LibraryImport Declarations**
   - Add P/Invoke declarations in platform-specific C# files
   - Use `[LibraryImport]` attribute for modern source-generated interop
   - Handle string marshalling and return types properly

4. **Configure Project**
   - Android: Add `<AndroidNativeLibrary>` entries for each architecture
   - iOS: Add `<Compile>` entry for C source or `<NativeReference>` for libraries
   - Update `.csproj` with proper build configuration

2. **Choose Your Binding Approach**
   - Use Slim Bindings for simple SDKs (demonstrated in this template)
   - Use Traditional Binding Libraries for complex SDKs

3. **Implement Platform-Specific Services**
   - Create an interface in shared code (`INativeService`)
   - Implement for each platform in `Platforms/Android` and `Platforms/iOS`

4. **Create the Bridge Layer**
   - Implement `NativeBridge` service for message routing
   - Register WebView handlers in `MainPage.xaml.cs`

5. **Add JavaScript Interface**
   - Inject JavaScript bridge code into WebView
   - Create convenience methods for PWA to call

6. **Test End-to-End**
   - Build and deploy to physical devices or emulators
   - Test button clicks trigger native SDK operations
   - Verify responses return correctly to JavaScript

## Web-to-Native Bridge

The bridge uses a custom URL scheme (`nativebridge://`) to communicate between JavaScript and native code:

**Flow:**
1. JavaScript calls `window.nativeBridge.someMethod()`
2. Bridge creates a callback and navigation request
3. WebView navigating event intercepts the custom URL
4. Native code processes the request through `NativeBridge`
5. Platform-specific service executes the SDK operation
6. Response is serialized and sent back via JavaScript callback

## Testing and Debugging

### Android Testing
```bash
# Build for Android
dotnet build -f net8.0-android

# Deploy to emulator
dotnet build -f net8.0-android -t:Run

# View logs
adb logcat | grep "MAUI\|ExampleSdk"
```

### iOS Testing
```bash
# Build for iOS (macOS only)
dotnet build -f net8.0-ios

# View device logs using Xcode Devices & Simulators
```

### WebView Debugging

**Android:**
- Enable WebView debugging in `MainActivity.cs`
- Use Chrome DevTools: `chrome://inspect`

**iOS:**
- Enable Web Inspector in Safari preferences
- Connect device and use Safari's Develop menu

## Example Files

This template includes example native SDK files:
- `MauiPwaShell/NativeBindings/Android/ExampleSdk.java` - Sample Android SDK
- `MauiPwaShell/NativeBindings/iOS/ExampleSdk.h/m` - Sample iOS SDK

These are demonstration files. Replace them with your actual SDK files.

## Common Issues and Solutions

### Issue: "Type not found" errors during build

**Android:**
- Ensure `ExampleSdk.java` is compiled to a .jar file
- Add the .jar to your project with `AndroidLibrary` build action
- Or use `@(AndroidJavaSource)` to include source directly

**iOS:**
- Ensure native code is compiled to a .framework or .a library
- Add the library with appropriate build action
- Include necessary linker flags in .csproj

### Issue: Bridge not responding

- Check WebView events are properly registered
- Verify URL scheme interception is working
- Check browser console for JavaScript errors
- Ensure native service is properly initialized

### Issue: Platform-specific code not compiling

- Use conditional compilation: `#if ANDROID` / `#if IOS`
- Ensure files are in correct platform folders
- Check TargetFramework matches platform

## Next Steps

1. Read the [Android Bindings Guide](./native-bindings-android.md)
2. Read the [iOS Bindings Guide](./native-bindings-ios.md)
3. Review the [Bridge Implementation Guide](./web-to-native-bridge.md)
4. Customize for your specific native SDK
5. Implement additional native features as needed

## Resources

- [.NET MAUI Documentation](https://learn.microsoft.com/dotnet/maui/)
- [Android Binding Projects](https://learn.microsoft.com/xamarin/android/platform/binding-java-library/)
- [iOS Binding Projects](https://learn.microsoft.com/xamarin/ios/platform/binding-objective-c/)
- [MAUI WebView Documentation](https://learn.microsoft.com/dotnet/maui/user-interface/controls/webview)
