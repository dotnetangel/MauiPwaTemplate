# Documentation Index

Welcome to the MAUI PWA Template documentation. This template combines the power of Progressive Web Apps with native mobile capabilities through .NET MAUI.

## 📱 Native SDK Bindings

Complete guides for integrating platform-specific native SDKs with your MAUI PWA application:

### Getting Started
- **[🚀 Quick Start Guide](./native-bindings-quickstart.md)** - Get up and running in 15 minutes
  - Prerequisites and setup
  - Step-by-step integration
  - Complete example with code
  - Common patterns and troubleshooting

### Core Documentation
- **[📖 Overview & Architecture](./native-bindings-overview.md)** - Understand the system
  - Architecture diagrams
  - Binding approaches (Slim vs Traditional)
  - Design decisions and patterns
  - Best practices

### Platform-Specific Guides
- **[🤖 Android Bindings Guide](./native-bindings-android.md)** - Complete Android integration (13KB)
  - MAUI Slim Bindings method
  - Traditional Binding Library method
  - Java/Kotlin interop
  - ProGuard configuration
  - Troubleshooting and examples

- **[🍎 iOS Bindings Guide](./native-bindings-ios.md)** - Complete iOS integration (17KB)
  - MAUI Slim Bindings method
  - Objective-C and Swift bindings
  - Export attributes reference
  - Framework integration
  - XCFramework support

### Implementation Details
- **[🌉 Web-to-Native Bridge](./web-to-native-bridge.md)** - Bridge architecture and usage (19KB)
  - How the bridge works
  - Message format and routing
  - Adding new methods
  - Advanced patterns (events, progress, batching)
  - Security considerations
  - Performance optimization
  - Debugging techniques

### Testing and Deployment
- **[🧪 Testing & Building Guide](./testing-and-building.md)** - Complete testing guide (10KB)
  - Build instructions for all platforms
  - Testing procedures
  - WebView debugging (Chrome DevTools, Safari)
  - Performance testing
  - CI/CD integration
  - Common issues and solutions

## 🔔 Push Notifications

Comprehensive guides for implementing push notifications across all platforms:

- **[Overview](./push-notifications-overview.md)** - High-level architecture
- **[Web Push (VAPID)](./push-notifications-web.md)** - Browser push notifications
- **[Android Push (FCM)](./push-notifications-android.md)** - Firebase Cloud Messaging
- **[iOS Push (FCM + APNs)](./push-notifications-ios.md)** - Apple Push Notifications

## 📂 Quick Reference

### File Locations

**Native SDK Integration:**
```
MauiPwaShell/
├── NativeBindings/          # Native SDK files
│   ├── Android/            # Java/Kotlin SDKs
│   └── iOS/                # Objective-C/Swift SDKs
├── Services/               # C# interfaces and bridge
│   ├── INativeService.cs   # Cross-platform interface
│   └── NativeBridge.cs     # Message routing service
├── Platforms/              # Platform implementations
│   ├── Android/NativeService.cs
│   └── iOS/NativeService.cs
└── MainPage.xaml.cs        # Bridge integration

PwaWeb/wwwroot/
├── index.html              # Native SDK UI
├── main.js                 # JavaScript bridge client
└── styles.css              # Styling
```

### Common Tasks

**Add a New Native Method:**
1. Update `INativeService.cs` interface
2. Implement in `Platforms/Android/NativeService.cs`
3. Implement in `Platforms/iOS/NativeService.cs`
4. Add handler in `NativeBridge.cs`
5. Add JavaScript method in `main.js`
6. Test end-to-end

**Debug the Bridge:**
- Android: `adb logcat | grep -E "MAUI|ExampleSdk"`
- iOS: Xcode → Devices and Simulators → View Logs
- WebView: Chrome DevTools (`chrome://inspect`) or Safari Develop menu

**Build for Platform:**
```bash
# Android
dotnet build -f net8.0-android

# iOS (macOS only)
dotnet build -f net8.0-ios
```

## 🎯 Learning Path

### For Beginners
1. Read the [Quick Start Guide](./native-bindings-quickstart.md)
2. Try the example SDKs included in the template
3. Test in the MAUI app to see it working
4. Review the [Overview](./native-bindings-overview.md) to understand architecture

### For Integrators
1. Follow your platform's guide ([Android](./native-bindings-android.md) or [iOS](./native-bindings-ios.md))
2. Replace example SDKs with your actual native libraries
3. Update service implementations to match your SDK API
4. Add bridge methods as needed
5. Test thoroughly using the [Testing Guide](./testing-and-building.md)

### For Advanced Users
1. Study the [Bridge Implementation](./web-to-native-bridge.md) in depth
2. Implement advanced patterns (events, streaming, batching)
3. Optimize for performance
4. Add comprehensive error handling
5. Consider packaging as reusable component

## 📊 Documentation Stats

- **Total Documentation:** 80+ pages
- **Code Examples:** 100+ snippets
- **Diagrams:** Multiple architecture and flow diagrams
- **Troubleshooting:** Comprehensive issue resolution guides

## 🔗 External Resources

### MAUI
- [.NET MAUI Official Docs](https://learn.microsoft.com/dotnet/maui/)
- [MAUI GitHub Repository](https://github.com/dotnet/maui)
- [MAUI Community Toolkit](https://github.com/CommunityToolkit/Maui)

### Platform Bindings
- [Xamarin Android Binding](https://learn.microsoft.com/xamarin/android/platform/binding-java-library/)
- [Xamarin iOS Binding](https://learn.microsoft.com/xamarin/ios/platform/binding-objective-c/)
- [Binding Types Reference](https://learn.microsoft.com/xamarin/cross-platform/macios/binding/binding-types-reference)

### JavaScript Bridge
- [WebView Documentation](https://learn.microsoft.com/dotnet/maui/user-interface/controls/webview)
- [JavaScript Interop](https://learn.microsoft.com/dotnet/maui/platform-integration/invoke-platform-code)

## 💬 Support

- **Issues:** [GitHub Issues](https://github.com/dotnetangel/MauiPwaTemplate/issues)
- **Discussions:** [GitHub Discussions](https://github.com/dotnetangel/MauiPwaTemplate/discussions)
- **Documentation:** This directory

## ✨ What's Included

This documentation covers:
- ✅ Complete working examples
- ✅ Step-by-step tutorials
- ✅ Architecture explanations
- ✅ Best practices and patterns
- ✅ Security considerations
- ✅ Performance optimization
- ✅ Troubleshooting guides
- ✅ Real-world examples
- ✅ Testing strategies
- ✅ CI/CD integration

## 🎉 Next Steps

1. Choose your starting point from above
2. Follow the guides step by step
3. Experiment with the example code
4. Customize for your needs
5. Deploy to production!

Happy coding! 🚀
