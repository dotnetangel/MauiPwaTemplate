using Android.Content;

namespace MauiPwaShell.Services;

/// <summary>
/// Android-specific implementation of the native service
/// This wraps the native Android SDK using .NET Android bindings
/// </summary>
public partial class NativeService : INativeService
{
    private Com.Example.Nativesdk.ExampleSdk? _sdk;
    private readonly Context _context;

    public NativeService()
    {
        _context = Android.App.Application.Context;
        _sdk = new Com.Example.Nativesdk.ExampleSdk(_context);
    }

    public void Initialize(string apiKey)
    {
        if (_sdk == null)
        {
            _sdk = new Com.Example.Nativesdk.ExampleSdk(_context);
        }
        _sdk.Initialize(apiKey);
    }

    public string PerformOperation(string input)
    {
        if (_sdk == null)
        {
            throw new InvalidOperationException("SDK not initialized");
        }
        return _sdk.PerformOperation(input) ?? string.Empty;
    }

    public string GetDeviceInfo()
    {
        if (_sdk == null)
        {
            throw new InvalidOperationException("SDK not initialized");
        }
        return _sdk.GetDeviceInfo() ?? string.Empty;
    }

    public bool IsInitialized()
    {
        return _sdk?.IsInitialized() ?? false;
    }

    public void Dispose()
    {
        _sdk?.Dispose();
        _sdk = null;
    }
}
