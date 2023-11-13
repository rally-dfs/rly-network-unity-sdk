#import <Foundation/Foundation.h>
#import <UnityFramework/UnityFramework-Swift.h>

@interface UnitySdkPlugin : NSObject

+ (NSString *)getBundleId;
+ (NSString *)getMnemonic;
+ (NSString *)generateMnemonic;
+ (BOOL)saveMnemonic:(NSString *)mnemonic saveToCloud:(BOOL)saveToCloud rejectOnCloudSaveFailure:(BOOL)rejectOnCloudSaveFailure;
+ (BOOL)deleteMnemonic;
+ (NSString *)getPrivateKeyFromMnemonic:(NSString *)mnemonic;

@end

@implementation UnitySdkPlugin

static RlyNetworkMobileSdk *instance = nil;

+ (void)initialize {
    if (self == [UnitySdkPlugin class])
        instance = [[RlyNetworkMobileSdk alloc] init];
}

+ (NSString *)getBundleId {
    return [instance getBundleId];
}

+ (NSString *)getMnemonic {
    return [instance getMnemonic];
}

+ (NSString *)generateMnemonic {
    return [instance generateMnemonic];
}

+ (BOOL)saveMnemonic:(NSString *)mnemonic saveToCloud:(BOOL)saveToCloud rejectOnCloudSaveFailure:(BOOL)rejectOnCloudSaveFailure {
    return [instance saveMnemonic:mnemonic saveToCloud:saveToCloud rejectOnCloudSaveFailure:rejectOnCloudSaveFailure];
}

+ (BOOL)deleteMnemonic {
    return [instance deleteMnemonic];
}

+ (NSString *)getPrivateKeyFromMnemonic:(NSString *)mnemonic {
    id privateKey = [instance getPrivateKeyFromMnemonic:mnemonic];

    if ([privateKey isKindOfClass:[NSArray class]]) {
        NSMutableString *hexString = [NSMutableString string];
        for (NSNumber *byte in privateKey) {
            [hexString appendFormat:@"%02x", byte.unsignedCharValue];
        }
        return hexString;
    }

    return privateKey;
}

@end

const char* NSStringToCharArray(NSString *string) {
    if (string == nil)
        return nil;

    return strdup(string.UTF8String);
}

NSString* CharArrayToNSString (const char* string)
{
	if (string)
		return [NSString stringWithUTF8String: string];
	else
		return [NSString stringWithUTF8String: ""];
}

extern "C" {
    const char *getBundleId() {
        return NSStringToCharArray([UnitySdkPlugin getBundleId]);
    }
    
    const char *getMnemonic() {
        return NSStringToCharArray([UnitySdkPlugin getMnemonic]);
    }
    
    const char *generateMnemonic() {
        return NSStringToCharArray([UnitySdkPlugin generateMnemonic]);
    }
    
    bool saveMnemonic(const char *mnemonic, bool saveToCloud, bool rejectOnCloudSaveFailure) {
        return [UnitySdkPlugin saveMnemonic:CharArrayToNSString(mnemonic) saveToCloud:saveToCloud rejectOnCloudSaveFailure:rejectOnCloudSaveFailure];
    }
    
    bool deleteMnemonic() {
        return [UnitySdkPlugin deleteMnemonic];
    }
    
    const char *getPrivateKeyFromMnemonic(const char *mnemonic) {
        return NSStringToCharArray([UnitySdkPlugin getPrivateKeyFromMnemonic:CharArrayToNSString(mnemonic)]);
    }
}