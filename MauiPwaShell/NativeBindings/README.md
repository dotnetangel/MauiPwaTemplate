# Native SDK Bindings

This directory contains native C/C++ library files that demonstrate how to integrate platform-specific native libraries with your MAUI application using the .NET MAUI Community Toolkit's Native Library Interop approach with `LibraryImport`/`DllImport`.

## Directory Structure

```
NativeBindings/
├── examplesdk.h            # C header file with API definitions
├── examplesdk.c            # C implementation
├── build-android.sh        # Android build script (requires NDK)
├── build-ios.sh            # iOS build script (requires Xcode)
└── README.md               # This file
```

## What's Included

### Native C Library (examplesdk.h/c)

A sample native C library that demonstrates:
- C API with exported functions
- SDK initialization with API key
- Performing operations with input/output
- Getting device information
- Proper state management and cleanup
- Cross-platform compilation (Android, iOS, macOS)

**API Functions:**
- `ExampleSdk_Initialize(const char* apiKey)` - Initialize the SDK
- `ExampleSdk_PerformOperation(const char* input, char* output, int outputSize)` - Process input
- `ExampleSdk_GetDeviceInfo(char* output, int outputSize)` - Get device info
- `ExampleSdk_IsInitialized()` - Check initialization status
- `ExampleSdk_Dispose()` - Clean up resources

## Integration with MAUI

These native library files are integrated into the MAUI project using the **LibraryImport** approach (modern P/Invoke):

### C# Wrapper (Platform-Specific)

Both Android and iOS use the same C library through P/Invoke:

```csharp
// Android: Platforms/Android/NativeService.cs
[LibraryImport("examplesdk", StringMarshalling = StringMarshalling.Utf8)]
[return: MarshalAs(UnmanagedType.I4)]
private static partial int ExampleSdk_Initialize(string apiKey);

// iOS: Platforms/iOS/NativeService.cs  
[LibraryImport("__Internal", StringMarshalling = StringMarshalling.Utf8)]
[return: MarshalAs(UnmanagedType.I4)]
private static partial int ExampleSdk_Initialize(string apiKey);
```

### Android Integration

The C library must be compiled for Android using the NDK:

```bash
cd NativeBindings
./build-android.sh
```

This creates native libraries for all Android architectures:
- `arm64-v8a/libexamplesdk.so`
- `armeabi-v7a/libexamplesdk.so`
- `x86/libexamplesdk.so`
- `x86_64/libexamplesdk.so`

The `.csproj` file then packages these into the APK:

```xml
<AndroidNativeLibrary Include="Platforms\Android\libs\arm64-v8a\libexamplesdk.so">
  <Abi>arm64-v8a</Abi>
</AndroidNativeLibrary>
```

### iOS Integration

For iOS, the C source is compiled directly into the app:

```xml
<Compile Include="NativeBindings\examplesdk.c">
  <CompileAs>C</CompileAs>
</Compile>
```

Alternatively, you can pre-build a static library:

```bash
cd NativeBindings
./build-ios.sh
```

## Advantages of LibraryImport Approach

1. **Modern .NET** - Uses source-generated P/Invoke (faster, AOT-friendly)
2. **Cross-Platform** - Same C library works on Android, iOS, macOS, Windows
3. **No Bindings** - No need for Java/Objective-C wrapper layers
4. **Performance** - Direct native calls without marshaling overhead
5. **Simpler** - One C codebase instead of Java + Objective-C
6. **Type-Safe** - Compile-time checking of P/Invoke signatures

## Replacing with Your Own Library

These are **example files** demonstrating the integration pattern. To use your own native library:

### For C/C++ Libraries:

1. **Replace** `examplesdk.h/c` with your actual C/C++ library files
2. **Update** `NativeService.cs` files to match your API:
   ```csharp
   [LibraryImport("yourlibrary")]
   private static partial int YourLibrary_YourFunction();
   ```
3. **Build** the native libraries:
   - Android: Run `build-android.sh` (or use your build system)
   - iOS: Either compile directly or use `build-ios.sh`
4. **Update** `.csproj` file to reference your library name

### For Pre-Compiled Libraries:

If you have existing `.so` (Android) or `.dylib`/`.a` (iOS) files:

1. **Place** libraries in appropriate directories:
   - Android: `Platforms/Android/libs/{abi}/yourlib.so`
   - iOS: `Platforms/iOS/libs/yourlib.dylib` or `.a`

2. **Update** `.csproj`:
   ```xml
   <!-- Android -->
   <AndroidNativeLibrary Include="Platforms\Android\libs\arm64-v8a\yourlib.so">
     <Abi>arm64-v8a</Abi>
   </AndroidNativeLibrary>
   
   <!-- iOS -->
   <NativeReference Include="Platforms\iOS\libs\yourlib.a">
     <Kind>Static</Kind>
     <ForceLoad>True</ForceLoad>
   </NativeReference>
   ```

3. **Update** `LibraryImport` declarations to use your library name

## Building the Native Libraries

### Prerequisites

**Android:**
- Android NDK installed (`ANDROID_NDK_HOME` environment variable set)
- Linux, macOS, or WSL on Windows

**iOS:**
- macOS with Xcode installed
- Xcode Command Line Tools

### Build Commands

```bash
# Android
cd NativeBindings
./build-android.sh

# iOS (macOS only)
cd NativeBindings
./build-ios.sh
```

### Build Output

**Android:** `Platforms/Android/libs/{abi}/libexamplesdk.so`
**iOS:** `Platforms/iOS/libs/libexamplesdk-*.dylib`

## Documentation

For detailed step-by-step guides:

- **[Overview](../docs/native-bindings-overview.md)** - Architecture and approaches
- **[Android Guide](../docs/native-bindings-android.md)** - Complete Android integration
- **[iOS Guide](../docs/native-bindings-ios.md)** - Complete iOS integration
- **[Quick Start](../docs/native-bindings-quickstart.md)** - 15-minute setup guide

## Testing the Example Library

The example library is fully functional and can be tested through the PWA interface:

1. Build the native libraries (or use direct compilation for iOS)
2. Run the MAUI application on Android or iOS
3. Navigate to the "Native SDK Integration" section in the PWA
4. Click "Initialize SDK" to initialize with a test API key
5. Click "Perform Operation" to test SDK operations
6. Click "Get Device Info" to retrieve device information

The SDK operations will execute in the native C library and return results to the web interface.

## Troubleshooting

### Android: "DllNotFoundException: libexamplesdk.so"

- **Build the library:** Run `./build-android.sh`
- **Check .csproj:** Ensure `AndroidNativeLibrary` entries are uncommented
- **Verify files:** Check that `.so` files exist in `Platforms/Android/libs/*/`
- **Clean build:** `dotnet clean && dotnet build`

### iOS: "DllNotFoundException: __Internal"

- **Direct compilation:** Ensure `examplesdk.c` is included with `<CompileAs>C</CompileAs>`
- **Check linker:** May need additional linker flags in `.csproj`
- **Verify build:** Check build output for compilation errors

### Build Script Errors

- **NDK not found:** Set `ANDROID_NDK_HOME` environment variable
- **Xcode not found:** Install Xcode and Command Line Tools
- **Permission denied:** Run `chmod +x build-*.sh`

## Comparison with Previous Approach

**Previous (Java/Objective-C Bindings):**
- Required separate Java and Objective-C implementations
- Used `AndroidJavaSource` and `[Export]` attributes
- More complex with platform-specific code

**Current (LibraryImport):**
- Single C/C++ codebase for all platforms
- Uses modern `LibraryImport` with source generation
- More performant and simpler to maintain
- Follows .NET MAUI Community Toolkit recommendations

## License

These example files are provided as part of the MAUI PWA Template for demonstration purposes.

