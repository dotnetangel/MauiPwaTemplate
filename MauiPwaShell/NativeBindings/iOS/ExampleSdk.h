//
//  ExampleSdk.h
//  Example Native iOS SDK
//
//  This is a sample SDK to demonstrate native binding integration with MAUI.
//  Replace this with your actual iOS SDK.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface ExampleSdk : NSObject

/**
 * Initialize the SDK
 */
- (void)initializeWithApiKey:(NSString *)apiKey;

/**
 * Perform a sample operation
 */
- (NSString *)performOperation:(NSString *)input;

/**
 * Get device information
 */
- (NSString *)getDeviceInfo;

/**
 * Check if SDK is initialized
 */
- (BOOL)isInitialized;

/**
 * Cleanup resources
 */
- (void)dispose;

@end

NS_ASSUME_NONNULL_END
