using System.Text.Json;

namespace MauiPwaShell.Services;

/// <summary>
/// Bridge service that enables communication between the PWA (JavaScript) and native code
/// </summary>
public class NativeBridge
{
    private readonly INativeService _nativeService;

    public NativeBridge(INativeService nativeService)
    {
        _nativeService = nativeService;
    }

    /// <summary>
    /// Handle incoming messages from JavaScript
    /// </summary>
    public async Task<string> HandleMessageAsync(string message)
    {
        try
        {
            var request = JsonSerializer.Deserialize<NativeBridgeRequest>(message);
            if (request == null)
            {
                return CreateErrorResponse("Invalid request format");
            }

            return request.Action switch
            {
                "initialize" => HandleInitialize(request),
                "performOperation" => HandlePerformOperation(request),
                "getDeviceInfo" => HandleGetDeviceInfo(request),
                "isInitialized" => HandleIsInitialized(request),
                _ => CreateErrorResponse($"Unknown action: {request.Action}")
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Error processing message: {ex.Message}");
        }
    }

    private string HandleInitialize(NativeBridgeRequest request)
    {
        try
        {
            var apiKey = request.Data?.GetProperty("apiKey").GetString();
            if (string.IsNullOrEmpty(apiKey))
            {
                return CreateErrorResponse("API key is required");
            }

            _nativeService.Initialize(apiKey);
            return CreateSuccessResponse("SDK initialized successfully");
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Initialization failed: {ex.Message}");
        }
    }

    private string HandlePerformOperation(NativeBridgeRequest request)
    {
        try
        {
            var input = request.Data?.GetProperty("input").GetString();
            if (string.IsNullOrEmpty(input))
            {
                return CreateErrorResponse("Input is required");
            }

            var result = _nativeService.PerformOperation(input);
            return CreateSuccessResponse(result);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Operation failed: {ex.Message}");
        }
    }

    private string HandleGetDeviceInfo(NativeBridgeRequest request)
    {
        try
        {
            var info = _nativeService.GetDeviceInfo();
            return CreateSuccessResponse(info);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Failed to get device info: {ex.Message}");
        }
    }

    private string HandleIsInitialized(NativeBridgeRequest request)
    {
        try
        {
            var isInitialized = _nativeService.IsInitialized();
            return CreateSuccessResponse(isInitialized.ToString());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Failed to check initialization status: {ex.Message}");
        }
    }

    private string CreateSuccessResponse(string data)
    {
        var response = new NativeBridgeResponse
        {
            Success = true,
            Data = data
        };
        return JsonSerializer.Serialize(response);
    }

    private string CreateErrorResponse(string error)
    {
        var response = new NativeBridgeResponse
        {
            Success = false,
            Error = error
        };
        return JsonSerializer.Serialize(response);
    }
}

public class NativeBridgeRequest
{
    public string Action { get; set; } = string.Empty;
    public JsonElement? Data { get; set; }
}

public class NativeBridgeResponse
{
    public bool Success { get; set; }
    public string? Data { get; set; }
    public string? Error { get; set; }
}
