package com.example.nativesdk;

import android.content.Context;
import android.util.Log;

/**
 * Example Native Android SDK
 * This is a sample SDK to demonstrate native binding integration with MAUI.
 * Replace this with your actual Android SDK.
 */
public class ExampleSdk {
    private static final String TAG = "ExampleSdk";
    private Context context;
    private boolean isInitialized = false;

    public ExampleSdk(Context context) {
        this.context = context;
    }

    /**
     * Initialize the SDK
     */
    public void initialize(String apiKey) {
        Log.d(TAG, "Initializing SDK with API key: " + apiKey);
        this.isInitialized = true;
    }

    /**
     * Perform a sample operation
     */
    public String performOperation(String input) {
        if (!isInitialized) {
            throw new IllegalStateException("SDK not initialized. Call initialize() first.");
        }
        Log.d(TAG, "Performing operation with input: " + input);
        return "Android SDK processed: " + input;
    }

    /**
     * Get device information
     */
    public String getDeviceInfo() {
        if (!isInitialized) {
            throw new IllegalStateException("SDK not initialized. Call initialize() first.");
        }
        return "Android Device: " + android.os.Build.MODEL + " (API " + android.os.Build.VERSION.SDK_INT + ")";
    }

    /**
     * Check if SDK is initialized
     */
    public boolean isInitialized() {
        return isInitialized;
    }

    /**
     * Cleanup resources
     */
    public void dispose() {
        Log.d(TAG, "Disposing SDK");
        this.isInitialized = false;
    }
}
