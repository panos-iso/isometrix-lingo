#!/bin/bash

# Publish script for macOS (Apple Silicon)
# Generates a self-contained executable for macOS arm64

APP_NAME="Isometrix Lingo"
BUNDLE_NAME="IsometrixLingo.app"
EXECUTABLE_NAME="isometrix-lingo"
BUNDLE_IDENTIFIER="com.isometrix.lingo"
VERSION="2.0.0"

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
    
    # Create macOS app bundle
    echo "📦 Creating macOS application bundle..."
    
    # Create app bundle structure
    mkdir -p "publish/macos/$BUNDLE_NAME/Contents/MacOS"
    mkdir -p "publish/macos/$BUNDLE_NAME/Contents/Resources"
    
    # Copy executable and libraries
    cp "publish/macos/$EXECUTABLE_NAME" "publish/macos/$BUNDLE_NAME/Contents/MacOS/"
    cp publish/macos/*.dylib "publish/macos/$BUNDLE_NAME/Contents/MacOS/" 2>/dev/null || true
    
    # Copy app icon
    cp "IsometrixLingo/Assets/AppIcon.icns" "publish/macos/$BUNDLE_NAME/Contents/Resources/"
    
    # Create Info.plist
    cat > "publish/macos/$BUNDLE_NAME/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleExecutable</key>
    <string>$EXECUTABLE_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$BUNDLE_IDENTIFIER</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSHumanReadableCopyright</key>
    <string>Copyright © 2026 Isometrix. All rights reserved.</string>
</dict>
</plist>
EOF
    
    # Make executable
    chmod +x "publish/macos/$BUNDLE_NAME/Contents/MacOS/$EXECUTABLE_NAME"
    
    echo "✅ App bundle created: publish/macos/$BUNDLE_NAME"
    echo ""
    echo "To create distributable archive:"
    echo "  cd publish/macos"
    echo "  tar -czf isometrix-lingo-v$VERSION-macos-arm64.tar.gz \"$BUNDLE_NAME\""
else
    echo "❌ Build failed!"
    exit 1
fi
