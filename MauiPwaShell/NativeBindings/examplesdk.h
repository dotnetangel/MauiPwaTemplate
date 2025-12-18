#ifndef EXAMPLESDK_H
#define EXAMPLESDK_H

#ifdef __cplusplus
extern "C" {
#endif

#if defined(_WIN32) || defined(_WIN64)
    #define EXPORT_API __declspec(dllexport)
#else
    #define EXPORT_API __attribute__((visibility("default")))
#endif

/**
 * Example Native SDK - C API
 * This is a sample SDK demonstrating native library interop with MAUI using LibraryImport/DllImport.
 * Replace this with your actual native SDK.
 */

/**
 * Initialize the SDK with an API key
 * @param apiKey The API key string
 * @return 1 on success, 0 on failure
 */
EXPORT_API int ExampleSdk_Initialize(const char* apiKey);

/**
 * Perform a sample operation
 * @param input Input string
 * @param output Buffer to receive output string (caller must provide)
 * @param outputSize Size of the output buffer
 * @return Number of characters written (excluding null terminator), or -1 on error
 */
EXPORT_API int ExampleSdk_PerformOperation(const char* input, char* output, int outputSize);

/**
 * Get device information
 * @param output Buffer to receive device info string (caller must provide)
 * @param outputSize Size of the output buffer
 * @return Number of characters written (excluding null terminator), or -1 on error
 */
EXPORT_API int ExampleSdk_GetDeviceInfo(char* output, int outputSize);

/**
 * Check if SDK is initialized
 * @return 1 if initialized, 0 if not
 */
EXPORT_API int ExampleSdk_IsInitialized(void);

/**
 * Cleanup and dispose SDK resources
 */
EXPORT_API void ExampleSdk_Dispose(void);

#ifdef __cplusplus
}
#endif

#endif // EXAMPLESDK_H
