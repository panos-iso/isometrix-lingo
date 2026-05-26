#!/bin/bash

# Publish script for Windows (x64)
# Generates a self-contained executable for Windows
# Usage: ./publish-windows.sh <version>
# Example: ./publish-windows.sh 3.1.0

if [ -z "$1" ]; then
    echo "❌ Error: Version parameter is required"
    echo "Usage: ./publish-windows.sh <version>"
    echo "Example: ./publish-windows.sh 3.1.0"
    exit 1
fi

VERSION="$1"

echo "Publishing Translation Management Tool for Windows (x64) - Version $VERSION..."

# Clean previous builds
rm -rf publish/windows

# Publish self-contained app (without trimming for Windows compatibility)
# IncludeNativeLibrariesForSelfExtract extracts SkiaSharp native libs on first run
dotnet publish IsometrixLingo/IsometrixLingo.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish/windows

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    echo "📦 Output: publish/windows/"
    echo ""
    echo "To create distributable archive:"
    echo "  cd publish/windows"
    echo "  zip -r TranslationTool-Windows-x64.zip isometrix-lingo.exe"
else
    echo "❌ Build failed!"
    exit 1
fi
