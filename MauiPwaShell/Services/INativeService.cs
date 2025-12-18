namespace MauiPwaShell.Services;

/// <summary>
/// Interface for native SDK operations
/// This provides a cross-platform abstraction over native SDKs
/// </summary>
public interface INativeService
{
    /// <summary>
    /// Initialize the native SDK
    /// </summary>
    void Initialize(string apiKey);

    /// <summary>
    /// Perform a sample operation using the native SDK
    /// </summary>
    string PerformOperation(string input);

    /// <summary>
    /// Get device information from the native SDK
    /// </summary>
    string GetDeviceInfo();

    /// <summary>
    /// Check if the SDK is initialized
    /// </summary>
    bool IsInitialized();

    /// <summary>
    /// Cleanup native SDK resources
    /// </summary>
    void Dispose();
}
