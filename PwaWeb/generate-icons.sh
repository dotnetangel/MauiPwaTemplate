#!/bin/bash
# Script to generate placeholder PWA icons

echo "Generating PWA icon placeholders..."

# Check if ImageMagick is installed
if ! command -v convert &> /dev/null; then
    echo "ImageMagick not found. Please install it or create icons manually."
    echo ""
    echo "Alternative methods:"
    echo "1. Use online tool: https://favicon.io/"
    echo "2. Create manually using any image editor"
    echo "3. Use the create-icons.html file in wwwroot"
    echo ""
    echo "Required files:"
    echo "  - icon-192.png (192x192)"
    echo "  - icon-512.png (512x512)"
    echo "  - badge-72.png (72x72)"
    exit 1
fi

# Generate 192x192 icon
convert -size 192x192 xc:'#4CAF50' \
    -gravity center \
    -font Arial -pointsize 96 \
    -fill white \
    -annotate +0+0 "🔔" \
    wwwroot/icon-192.png

# Generate 512x512 icon
convert -size 512x512 xc:'#4CAF50' \
    -gravity center \
    -font Arial -pointsize 256 \
    -fill white \
    -annotate +0+0 "🔔" \
    wwwroot/icon-512.png

# Generate 72x72 badge
convert -size 72x72 xc:'#4CAF50' \
    -gravity center \
    -font Arial -pointsize 36 \
    -fill white \
    -annotate +0+0 "🔔" \
    wwwroot/badge-72.png

echo "Icons generated successfully!"
echo "  - wwwroot/icon-192.png"
echo "  - wwwroot/icon-512.png"
echo "  - wwwroot/badge-72.png"
