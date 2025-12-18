//
//  ExampleSdk.m
//  Example Native iOS SDK
//

#import "ExampleSdk.h"
#import <UIKit/UIKit.h>

@implementation ExampleSdk {
    BOOL _isInitialized;
}

- (instancetype)init {
    self = [super init];
    if (self) {
        _isInitialized = NO;
    }
    return self;
}

- (void)initializeWithApiKey:(NSString *)apiKey {
    NSLog(@"[ExampleSdk] Initializing SDK with API key: %@", apiKey);
    _isInitialized = YES;
}

- (NSString *)performOperation:(NSString *)input {
    if (!_isInitialized) {
        [NSException raise:@"IllegalStateException" 
                    format:@"SDK not initialized. Call initializeWithApiKey: first."];
    }
    NSLog(@"[ExampleSdk] Performing operation with input: %@", input);
    return [NSString stringWithFormat:@"iOS SDK processed: %@", input];
}

- (NSString *)getDeviceInfo {
    if (!_isInitialized) {
        [NSException raise:@"IllegalStateException" 
                    format:@"SDK not initialized. Call initializeWithApiKey: first."];
    }
    UIDevice *device = [UIDevice currentDevice];
    return [NSString stringWithFormat:@"iOS Device: %@ (%@)", device.model, device.systemVersion];
}

- (BOOL)isInitialized {
    return _isInitialized;
}

- (void)dispose {
    NSLog(@"[ExampleSdk] Disposing SDK");
    _isInitialized = NO;
}

@end
