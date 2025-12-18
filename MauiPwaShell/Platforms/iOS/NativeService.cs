using System.Runtime.InteropServices;
using System.Text;

namespace MauiPwaShell.Services;

/// <summary>
/// iOS-specific implementation of the native service
/// This uses LibraryImport (P/Invoke) to call native C library functions
/// </summary>
public partial class NativeService : INativeService
{
    private const string LibraryName = "__Internal"; // Use __Internal for iOS static libraries
    private const int BufferSize = 1024;

    // P/Invoke declarations using LibraryImport (modern .NET approach)
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial int ExampleSdk_Initialize(string apiKey);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial int ExampleSdk_PerformOperation(string input, byte[] output, int outputSize);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial int ExampleSdk_GetDeviceInfo(byte[] output, int outputSize);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial int ExampleSdk_IsInitialized();

    [LibraryImport(LibraryName)]
    private static partial void ExampleSdk_Dispose();

    public void Initialize(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
        }

        int result = ExampleSdk_Initialize(apiKey);
        if (result == 0)
        {
            throw new InvalidOperationException("Failed to initialize SDK");
        }
    }

    public string PerformOperation(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }

        byte[] buffer = new byte[BufferSize];
        int result = ExampleSdk_PerformOperation(input, buffer, buffer.Length);
        
        if (result < 0)
        {
            throw new InvalidOperationException("Operation failed. Ensure SDK is initialized.");
        }

        return Encoding.UTF8.GetString(buffer, 0, result);
    }

    public string GetDeviceInfo()
    {
        byte[] buffer = new byte[BufferSize];
        int result = ExampleSdk_GetDeviceInfo(buffer, buffer.Length);
        
        if (result < 0)
        {
            throw new InvalidOperationException("Failed to get device info. Ensure SDK is initialized.");
        }

        return Encoding.UTF8.GetString(buffer, 0, result);
    }

    public bool IsInitialized()
    {
        return ExampleSdk_IsInitialized() != 0;
    }

    public void Dispose()
    {
        ExampleSdk_Dispose();
    }
}
