#!/bin/bash
# Build script for iOS native library

# This script builds the native C library for iOS
# Requires Xcode to be installed (macOS only)

if [ "$(uname)" != "Darwin" ]; then
    echo "Error: iOS build requires macOS with Xcode installed."
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/../Platforms/iOS/libs"

mkdir -p "$OUTPUT_DIR"

echo "Building native library for iOS..."

# iOS SDK paths
IOS_SDK=$(xcrun --sdk iphoneos --show-sdk-path)
IOS_SIM_SDK=$(xcrun --sdk iphonesimulator --show-sdk-path)

# Build for iOS devices (arm64)
echo "Building for iOS device (arm64)..."
xcrun clang -arch arm64 \
    -isysroot $IOS_SDK \
    -mios-version-min=11.0 \
    -D__APPLE__ \
    -dynamiclib \
    -o "$OUTPUT_DIR/libexamplesdk-arm64.dylib" \
    "$SCRIPT_DIR/examplesdk.c" \
    -framework Foundation

if [ $? -eq 0 ]; then
    echo "✓ Built for iOS device (arm64)"
else
    echo "✗ Failed to build for iOS device"
fi

# Build for iOS simulator (x86_64 and arm64)
echo "Building for iOS simulator (x86_64)..."
xcrun clang -arch x86_64 \
    -isysroot $IOS_SIM_SDK \
    -mios-simulator-version-min=11.0 \
    -D__APPLE__ \
    -dynamiclib \
    -o "$OUTPUT_DIR/libexamplesdk-sim-x86_64.dylib" \
    "$SCRIPT_DIR/examplesdk.c" \
    -framework Foundation

if [ $? -eq 0 ]; then
    echo "✓ Built for iOS simulator (x86_64)"
fi

echo "Building for iOS simulator (arm64)..."
xcrun clang -arch arm64 \
    -isysroot $IOS_SIM_SDK \
    -mios-simulator-version-min=11.0 \
    -D__APPLE__ \
    -dynamiclib \
    -o "$OUTPUT_DIR/libexamplesdk-sim-arm64.dylib" \
    "$SCRIPT_DIR/examplesdk.c" \
    -framework Foundation

if [ $? -eq 0 ]; then
    echo "✓ Built for iOS simulator (arm64)"
fi

# Create universal library for simulator
if [ -f "$OUTPUT_DIR/libexamplesdk-sim-x86_64.dylib" ] && [ -f "$OUTPUT_DIR/libexamplesdk-sim-arm64.dylib" ]; then
    echo "Creating universal simulator library..."
    lipo -create \
        "$OUTPUT_DIR/libexamplesdk-sim-x86_64.dylib" \
        "$OUTPUT_DIR/libexamplesdk-sim-arm64.dylib" \
        -output "$OUTPUT_DIR/libexamplesdk-sim.dylib"
    
    if [ $? -eq 0 ]; then
        echo "✓ Created universal simulator library"
        rm "$OUTPUT_DIR/libexamplesdk-sim-x86_64.dylib" "$OUTPUT_DIR/libexamplesdk-sim-arm64.dylib"
    fi
fi

echo "Build complete. Libraries in: $OUTPUT_DIR"
