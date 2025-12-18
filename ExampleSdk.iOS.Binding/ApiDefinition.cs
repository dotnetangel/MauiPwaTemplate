using Foundation;
using ObjCRuntime;

namespace ExampleSdkiOS;

/// <summary>
/// API definitions for the ExampleSdk iOS binding.
/// This file defines the C# interface for the Objective-C SDK.
/// </summary>

// @interface ExampleSdk : NSObject
[BaseType(typeof(NSObject))]
interface ExampleSdk
{
    // -(void)initializeWithApiKey:(NSString * _Nonnull)apiKey;
    [Export("initializeWithApiKey:")]
    void InitializeWithApiKey(string apiKey);

    // -(NSString * _Nonnull)performOperation:(NSString * _Nonnull)input;
    [Export("performOperation:")]
    string PerformOperation(string input);

    // -(NSString * _Nonnull)getDeviceInfo;
    [Export("getDeviceInfo")]
    string GetDeviceInfo();

    // -(BOOL)isInitialized;
    [Export("isInitialized")]
    bool IsInitialized { get; }

    // -(void)dispose;
    [Export("dispose")]
    void Dispose();
}
