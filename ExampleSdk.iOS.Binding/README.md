# ExampleSdk.iOS.Binding

This project provides .NET bindings for the ExampleSdk iOS library (Objective-C).

## Overview

This is a .NET iOS binding library project that generates C# bindings for the native Objective-C SDK located in `../MauiPwaShell/NativeBindings/iOS/`.

## Project Structure

```
ExampleSdk.iOS.Binding/
├── ApiDefinition.cs        # C# binding definitions with [BaseType] and [Export]
├── StructsAndEnums.cs      # Enumerations and structures from the native SDK
└── ExampleSdk.iOS.Binding.csproj
```

## How It Works

1. **Objective-C Source Compilation**: The Objective-C source files from `NativeBindings/iOS/*.m` are compiled directly into the binding library.

2. **Binding Definitions**: `ApiDefinition.cs` defines the C# interface to the Objective-C classes using attributes:
   - `[BaseType]` - Declares the Objective-C base class
   - `[Export]` - Maps C# methods/properties to Objective-C selectors

3. **Usage in MAUI App**: The MauiPwaShell project references this binding project, making the SDK available in C# code:
   ```csharp
   using ExampleSdkiOS;
   
   var sdk = new ExampleSdk();
   sdk.InitializeWithApiKey("api-key");
   ```

## Binding Definitions

### ApiDefinition.cs

This file defines the C# interface to your Objective-C classes:

```csharp
[BaseType(typeof(NSObject))]
interface ExampleSdk
{
    // Objective-C: - (void)initializeWithApiKey:(NSString *)apiKey;
    [Export("initializeWithApiKey:")]
    void InitializeWithApiKey(string apiKey);
    
    // Objective-C: - (NSString *)performOperation:(NSString *)input;
    [Export("performOperation:")]
    string PerformOperation(string input);
    
    // Objective-C: - (BOOL)isInitialized;
    [Export("isInitialized")]
    bool IsInitialized { get; }
    
    // Class method: + (instancetype)sharedInstance;
    [Static]
    [Export("sharedInstance")]
    ExampleSdk SharedInstance { get; }
}
```

### StructsAndEnums.cs

Define enumerations and structures from your Objective-C SDK:

```csharp
[Native]
public enum ExampleStatus : long
{
    Unknown = 0,
    Success = 1,
    Error = 2
}

[StructLayout(LayoutKind.Sequential)]
public struct ExampleConfig
{
    public IntPtr ApiKey;
    public int Timeout;
}
```

## Objective-C to C# Mapping

### Selectors

Objective-C method names (selectors) must match exactly in the `[Export]` attribute:

| Objective-C | Export Attribute |
|-------------|------------------|
| `initWithValue:` | `[Export("initWithValue:")]` |
| `setValue:forKey:` | `[Export("setValue:forKey:")]` |
| `calculate` | `[Export("calculate")]` |
| `sharedInstance` (class method) | `[Static] [Export("sharedInstance")]` |

### Properties

```csharp
// Read-only property
[Export("status")]
Status Status { get; }

// Read-write property
[Export("timeout")]
int Timeout { get; set; }

// Or as methods:
[Export("getTimeout")]
int GetTimeout();

[Export("setTimeout:")]
void SetTimeout(int value);
```

### Types

| Objective-C | C# |
|-------------|-----|
| `NSString *` | `string` |
| `NSNumber *` | `NSNumber` |
| `NSArray *` | `NSArray` or `string[]`, `int[]` etc |
| `NSDictionary *` | `NSDictionary` or `Dictionary<,>` |
| `BOOL` | `bool` |
| `NSInteger` | `nint` |
| `NSUInteger` | `nuint` |
| `CGFloat` | `nfloat` |
| `void` | `void` |

## Replacing with Your Own SDK

To use your own iOS SDK:

1. **Replace Objective-C files**: Put your SDK's `.h` and `.m` files in `../MauiPwaShell/NativeBindings/iOS/`

2. **Update ApiDefinition.cs**: Define bindings for your SDK's classes:
   ```csharp
   [BaseType(typeof(NSObject))]
   interface YourSDK
   {
       [Export("yourMethod:")]
       void YourMethod(string parameter);
   }
   ```

3. **Find correct selectors**: Use Xcode or `nm` command:
   ```bash
   nm -g YourSDK.framework/YourSDK | grep "your"
   ```

4. **Handle frameworks**: If using a pre-built framework instead of source:
   ```xml
   <ItemGroup>
     <NativeReference Include="Path/To/YourSDK.framework">
       <Kind>Framework</Kind>
       <SmartLink>True</SmartLink>
     </NativeReference>
   </ItemGroup>
   ```

5. **Add linker flags if needed**:
   ```xml
   <MtouchExtraArgs>$(MtouchExtraArgs) -gcc_flags "-ObjC -lstdc++"</MtouchExtraArgs>
   ```

## Swift Interop

If your SDK is written in Swift:

1. The Swift class must be marked `@objc`:
   ```swift
   @objc public class MySDK: NSObject {
       @objc public func initialize(apiKey: String) {
           // ...
       }
   }
   ```

2. Swift generates an Objective-C interface header automatically

3. Create bindings for the Objective-C interface:
   ```csharp
   [BaseType(typeof(NSObject))]
   interface MySDK
   {
       [Export("initializeWithApiKey:")]
       void Initialize(string apiKey);
   }
   ```

**Note**: Not all Swift features can be exposed to Objective-C (generics, tuples, etc.).

## Building

```bash
# Build the binding project (iOS)
dotnet build ExampleSdk.iOS.Binding.csproj -f net8.0-ios

# Build for Mac Catalyst
dotnet build ExampleSdk.iOS.Binding.csproj -f net8.0-maccatalyst

# Or build the entire solution
dotnet build ../MauiPwaWrapper.sln
```

## Troubleshooting

### "Undefined symbols for architecture"

- Ensure `.m` files are being compiled (check project file)
- Verify binding definitions match Objective-C headers
- May need additional linker flags

### "Selector not recognized"

- Check the selector spelling and colons in `[Export]`
- Use `nm` or Xcode to verify the correct selector name
- Ensure the method exists in the Objective-C implementation

### "Type 'ExampleSdk' not found"

- Clean and rebuild: `dotnet clean && dotnet build`
- Verify the namespace: `using ExampleSdkiOS;`
- Check that Objective-C source files are in the correct location

### Architecture issues

Verify your framework/library contains the required architectures:

```bash
lipo -info YourSDK.framework/YourSDK
# Should show: arm64 x86_64 (or arm64 for simulator on M1+ Macs)
```

## References

- [Binding iOS Libraries (Microsoft Docs)](https://learn.microsoft.com/xamarin/ios/platform/binding-objective-c/)
- [Binding Types Reference](https://learn.microsoft.com/xamarin/ios/platform/binding-objective-c/binding-types-reference)
- [Community Toolkit Native Library Interop](https://github.com/CommunityToolkit/Maui.NativeLibraryInterop)
