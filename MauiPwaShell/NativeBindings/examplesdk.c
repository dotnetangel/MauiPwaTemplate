#include "examplesdk.h"
#include <string.h>
#include <stdio.h>

#if defined(__ANDROID__)
    #include <android/log.h>
    #define LOG_TAG "ExampleSdk"
    #define LOGI(...) __android_log_print(ANDROID_LOG_INFO, LOG_TAG, __VA_ARGS__)
#elif defined(__APPLE__)
    #include <TargetConditionals.h>
    #if TARGET_OS_IPHONE
        #include <os/log.h>
        #define LOGI(...) os_log(OS_LOG_DEFAULT, __VA_ARGS__)
    #else
        #define LOGI(...) printf(__VA_ARGS__)
    #endif
#else
    #define LOGI(...) printf(__VA_ARGS__)
#endif

static int g_isInitialized = 0;
static char g_apiKey[256] = {0};

EXPORT_API int ExampleSdk_Initialize(const char* apiKey) {
    if (apiKey == NULL || strlen(apiKey) == 0) {
        LOGI("ExampleSdk_Initialize: Invalid API key\n");
        return 0;
    }
    
    strncpy(g_apiKey, apiKey, sizeof(g_apiKey) - 1);
    g_apiKey[sizeof(g_apiKey) - 1] = '\0';
    g_isInitialized = 1;
    
    LOGI("ExampleSdk_Initialize: SDK initialized with API key: %s\n", apiKey);
    return 1;
}

EXPORT_API int ExampleSdk_PerformOperation(const char* input, char* output, int outputSize) {
    if (!g_isInitialized) {
        LOGI("ExampleSdk_PerformOperation: SDK not initialized\n");
        return -1;
    }
    
    if (input == NULL || output == NULL || outputSize <= 0) {
        return -1;
    }
    
    LOGI("ExampleSdk_PerformOperation: Processing input: %s\n", input);
    
    const char* platform;
#if defined(__ANDROID__)
    platform = "Android";
#elif defined(__APPLE__)
    #if TARGET_OS_IPHONE
        platform = "iOS";
    #else
        platform = "macOS";
    #endif
#else
    platform = "Unknown";
#endif
    
    int written = snprintf(output, outputSize, "%s Native SDK processed: %s", platform, input);
    return (written < outputSize) ? written : -1;
}

EXPORT_API int ExampleSdk_GetDeviceInfo(char* output, int outputSize) {
    if (!g_isInitialized) {
        LOGI("ExampleSdk_GetDeviceInfo: SDK not initialized\n");
        return -1;
    }
    
    if (output == NULL || outputSize <= 0) {
        return -1;
    }
    
    const char* platform;
    const char* details;
    
#if defined(__ANDROID__)
    platform = "Android";
    details = "Native C Library";
#elif defined(__APPLE__)
    #if TARGET_OS_IPHONE
        platform = "iOS";
        details = "Native C Library";
    #else
        platform = "macOS";
        details = "Native C Library";
    #endif
#else
    platform = "Unknown";
    details = "Native C Library";
#endif
    
    int written = snprintf(output, outputSize, "%s Device (%s)", platform, details);
    LOGI("ExampleSdk_GetDeviceInfo: %s\n", output);
    
    return (written < outputSize) ? written : -1;
}

EXPORT_API int ExampleSdk_IsInitialized(void) {
    return g_isInitialized;
}

EXPORT_API void ExampleSdk_Dispose(void) {
    LOGI("ExampleSdk_Dispose: Disposing SDK\n");
    g_isInitialized = 0;
    memset(g_apiKey, 0, sizeof(g_apiKey));
}
