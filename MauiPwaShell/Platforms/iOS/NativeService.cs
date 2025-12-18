using ExampleSdkiOS;

namespace MauiPwaShell.Services;

/// <summary>
/// iOS-specific implementation of the native service
/// This wraps the native iOS SDK using the ExampleSdk.iOS.Binding project
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
