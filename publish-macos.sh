#!/bin/bash

# Publish script for macOS (Apple Silicon)
# Generates a self-contained executable for macOS arm64

echo "Publishing Translation Management Tool for macOS (Apple Silicon)..."

# Clean previous builds
rm -rf publish/macos

# Publish self-contained app
dotnet publish IsometrixLingo/IsometrixLingo.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -o publish/macos

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    echo "📦 Output: publish/macos/"
    echo ""
    echo "To create distributable archive:"
    echo "  cd publish/macos"
    echo "  tar -czf TranslationTool-macOS-arm64.tar.gz isometrix-lingo"
else
    echo "❌ Build failed!"
    exit 1
fi
