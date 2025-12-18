#!/bin/bash
# Build script for Android native library

# This script builds the native C library for Android
# Requires Android NDK to be installed

NDK_PATH="${ANDROID_NDK_HOME:-$ANDROID_HOME/ndk-bundle}"

if [ ! -d "$NDK_PATH" ]; then
    echo "Error: Android NDK not found. Please set ANDROID_NDK_HOME or install NDK."
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/../Platforms/Android/libs"

# Create output directories for different architectures
mkdir -p "$OUTPUT_DIR/arm64-v8a"
mkdir -p "$OUTPUT_DIR/armeabi-v7a"
mkdir -p "$OUTPUT_DIR/x86"
mkdir -p "$OUTPUT_DIR/x86_64"

echo "Building native library for Android..."

# Build for each architecture
for ARCH in arm64-v8a armeabi-v7a x86 x86_64; do
    echo "Building for $ARCH..."
    
    case $ARCH in
        arm64-v8a)
            TOOLCHAIN_PREFIX="aarch64-linux-android"
            ;;
        armeabi-v7a)
            TOOLCHAIN_PREFIX="armv7a-linux-androideabi"
            ;;
        x86)
            TOOLCHAIN_PREFIX="i686-linux-android"
            ;;
        x86_64)
            TOOLCHAIN_PREFIX="x86_64-linux-android"
            ;;
    esac
    
    API_LEVEL=21
    TOOLCHAIN="$NDK_PATH/toolchains/llvm/prebuilt/linux-x86_64"
    CC="$TOOLCHAIN/bin/${TOOLCHAIN_PREFIX}${API_LEVEL}-clang"
    
    if [ ! -f "$CC" ]; then
        echo "Warning: Compiler not found: $CC"
        continue
    fi
    
    $CC -shared -fPIC \
        -D__ANDROID__ \
        -o "$OUTPUT_DIR/$ARCH/libexamplesdk.so" \
        "$SCRIPT_DIR/examplesdk.c" \
        -llog
    
    if [ $? -eq 0 ]; then
        echo "✓ Built for $ARCH"
    else
        echo "✗ Failed to build for $ARCH"
    fi
done

echo "Build complete. Libraries in: $OUTPUT_DIR"
