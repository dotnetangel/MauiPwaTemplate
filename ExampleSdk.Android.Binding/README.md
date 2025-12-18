# ExampleSdk.Android.Binding

This project provides .NET bindings for the ExampleSdk Android library (Java).

## Overview

This is a .NET Android binding library project that generates C# bindings for the native Java SDK located in `../MauiPwaShell/NativeBindings/Android/`.

## Project Structure

```
ExampleSdk.Android.Binding/
├── Additions/              # Custom C# code to add to generated bindings
│   └── CustomAdditions.cs  # Add custom methods, properties, or constructors
├── Transforms/             # Metadata transforms for customizing bindings
│   ├── Metadata.xml        # Main metadata transformations
│   ├── EnumFields.xml      # Java enum to C# enum mappings
│   └── EnumMethods.xml     # Method parameter enum mappings
└── ExampleSdk.Android.Binding.csproj
```

## How It Works

1. **Java Source Compilation**: The Java source files from `NativeBindings/Android/*.java` are compiled into a JAR file during the build.

2. **Binding Generation**: The .NET Android build tools analyze the JAR file and automatically generate C# bindings.

3. **Namespace Mapping**: Java packages are converted to C# namespaces:
   - `com.example.nativesdk` → `Com.Example.Nativesdk`

4. **Usage in MAUI App**: The MauiPwaShell project references this binding project, making the SDK available in C# code:
   ```csharp
   var sdk = new Com.Example.Nativesdk.ExampleSdk(context);
   sdk.Initialize("api-key");
   ```

## Customizing the Bindings

### Metadata Transforms (Transforms/Metadata.xml)

Use metadata transforms to customize the generated bindings:

```xml
<metadata>
  <!-- Remove internal classes -->
  <remove-node path="/api/package[@name='com.example.internal']/class[@name='InternalClass']" />
  
  <!-- Rename a type to avoid conflicts -->
  <attr path="/api/package[@name='com.example']/class[@name='Builder']" 
        name="managedName">SdkBuilder</attr>
  
  <!-- Make internal classes internal in C# -->
  <attr path="/api/package[@name='com.example.internal']" 
        name="visibility">internal</attr>
</metadata>
```

### Additions (Additions/)

Add custom code to the generated classes:

```csharp
namespace Com.Example.Nativesdk
{
    public partial class ExampleSdk
    {
        // Add convenience methods
        public void InitializeWithDefaults()
        {
            Initialize("default-api-key");
        }
    }
}
```

## Replacing with Your Own SDK

To use your own Android SDK:

1. **Replace Java files**: Put your SDK's Java source files in `../MauiPwaShell/NativeBindings/Android/`
   - Or use AAR/JAR files by changing the project configuration

2. **Update package references**: If your SDK has a different package name, update the using statements in the MAUI app

3. **Add metadata transforms**: If the binding generator creates incorrect bindings, add transforms in `Transforms/Metadata.xml`

4. **Handle dependencies**: If your SDK has dependencies, add them as NuGet packages:
   ```xml
   <PackageReference Include="Xamarin.AndroidX.AppCompat" Version="1.6.1" />
   ```

## Building

```bash
# Build the binding project
dotnet build ExampleSdk.Android.Binding.csproj

# Or build the entire solution
dotnet build ../MauiPwaWrapper.sln
```

## Troubleshooting

### "Type 'Com.Example.Nativesdk.ExampleSdk' not found"

- Clean and rebuild: `dotnet clean && dotnet build`
- Verify the Java package name matches the C# namespace (with capitalization)
- Check that Java source files are in the correct location

### Binding generation errors

- Check `obj/Debug/net8.0-android/generated/` for generated binding code
- Review build output for binding warnings
- Add metadata transforms to fix problematic bindings

### ProGuard issues (Release builds)

If you get `ClassNotFoundException` in Release builds:

1. Add ProGuard rules in `proguard.cfg`
2. Reference in the project file:
   ```xml
   <ProguardConfiguration Include="proguard.cfg" />
   ```

## References

- [Binding Java Libraries (Microsoft Docs)](https://learn.microsoft.com/xamarin/android/platform/binding-java-library/)
- [Customizing Bindings with Metadata](https://learn.microsoft.com/xamarin/android/platform/binding-java-library/customizing-bindings/)
- [Community Toolkit Native Library Interop](https://github.com/CommunityToolkit/Maui.NativeLibraryInterop)
