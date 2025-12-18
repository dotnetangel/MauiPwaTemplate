# Native SDK Bindings Integration Guide

## Overview

This guide demonstrates how to integrate native platform-specific SDKs into your MAUI PWA application using MAUI's Slim Binding methodology. The implementation showcases a complete end-to-end integration where web-based button clicks in the PWA can trigger native SDK APIs through a JavaScript-to-Native bridge.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Native Binding Approaches](#native-binding-approaches)
3. [Implementation Guides](#implementation-guides)
4. [Web-to-Native Bridge](#web-to-native-bridge)
5. [Testing and Debugging](#testing-and-debugging)

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
│  └──────────────────────┘    └─────────────────────────┘   │
└────────────────────┬────────────────────┬───────────────────┘
                     │                     │
                     ▼                     ▼
┌──────────────────────────────┐  ┌─────────────────────────┐
│  Native Android SDK          │  │  Native iOS SDK         │
│  (.jar/.aar libraries)       │  │  (.framework/.xcframework)│
│  - Java/Kotlin classes       │  │  - Swift/Objective-C    │
│  - Bound via C# wrappers     │  │  - Bound via C# wrappers│
└──────────────────────────────┘  └─────────────────────────┘
```

## Native Binding Approaches

### 1. MAUI Slim Bindings (Recommended for New Projects)

**What it is:** A lightweight approach introduced in .NET 8+ that allows you to create bindings directly in your MAUI project without needing separate binding library projects.

**Pros:**
- Simpler project structure
- Less ceremony and boilerplate
- Faster iteration during development
- Better integration with modern .NET tooling

**Cons:**
- Less suitable for complex SDKs with many types
- Cannot be easily shared across projects as a NuGet package

**Best for:** 
- Small to medium SDKs
- Rapid prototyping
- App-specific bindings

### 2. Traditional Binding Libraries

**What it is:** Creating dedicated Android Binding Library or iOS Binding Library projects that produce reusable DLLs.

**Pros:**
- Better for large, complex SDKs
- Can be packaged and distributed as NuGet packages
- Separation of concerns

**Cons:**
- More complex setup
- Additional project maintenance
- Longer build times

**Best for:**
- Large SDKs with many types
- SDKs that will be reused across multiple projects
- Team/organization-wide distribution

### 3. MAUI Community Toolkit Approach

The MAUI Community Toolkit doesn't provide specific binding tools, but it offers useful helpers:
- Platform-specific dependency injection
- Feature detection utilities
- Cross-platform abstractions

## Implementation Guides

Detailed step-by-step guides for each platform:

- **[Android Native Bindings](./native-bindings-android.md)** - Complete guide for Android SDK integration
- **[iOS Native Bindings](./native-bindings-ios.md)** - Complete guide for iOS SDK integration
- **[Web-to-Native Bridge](./web-to-native-bridge.md)** - JavaScript bridge implementation details

## Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 17.8+ or Visual Studio Code with C# Dev Kit
- MAUI workloads installed: `dotnet workload install maui`
- For Android: Android SDK (API 21+)
- For iOS: Xcode 15+ (macOS only)

### Basic Setup Steps

1. **Create or Identify Your Native SDK**
   - Obtain the native library files (.jar/.aar for Android, .framework/.xcframework for iOS)
   - Review the SDK's public API documentation

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
