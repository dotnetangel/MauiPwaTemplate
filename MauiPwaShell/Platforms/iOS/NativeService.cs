using Foundation;
using ObjCRuntime;

namespace MauiPwaShell.Services;

/// <summary>
/// iOS-specific implementation of the native service
/// This wraps the native iOS SDK using .NET iOS bindings
/// </summary>
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

// iOS Binding definitions for ExampleSdk
// This would normally be in a separate binding project, but for simplicity
// we're including it here with the MAUI Slim Bindings approach

[BaseType(typeof(NSObject))]
interface ExampleSdk
{
    [Export("initializeWithApiKey:")]
    void InitializeWithApiKey(string apiKey);

    [Export("performOperation:")]
    string PerformOperation(string input);

    [Export("getDeviceInfo")]
    string GetDeviceInfo();

    [Export("isInitialized")]
    bool IsInitialized { get; }

    [Export("dispose")]
    void Dispose();
}
