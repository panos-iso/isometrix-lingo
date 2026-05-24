#!/bin/bash

# Publish script for Windows (x64)
# Generates a self-contained executable for Windows

echo "Publishing Translation Management Tool for Windows (x64)..."

# Clean previous builds
rm -rf publish/windows

# Publish self-contained app
dotnet publish IsometrixLingo/IsometrixLingo.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
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
