#!/bin/bash

# Create macOS application bundle from the published binary

APP_NAME="Isometrix Lingo"
BUNDLE_NAME="IsometrixLingo.app"
EXECUTABLE_NAME="isometrix-lingo"
BUNDLE_IDENTIFIER="com.isometrix.lingo"
VERSION="1.0.0"

echo "Creating macOS application bundle..."

# Remove old bundle if it exists
rm -rf "publish/macos/$BUNDLE_NAME"

# Create app bundle structure
mkdir -p "publish/macos/$BUNDLE_NAME/Contents/MacOS"
mkdir -p "publish/macos/$BUNDLE_NAME/Contents/Resources"

# Copy executable and libraries
cp "publish/macos/$EXECUTABLE_NAME" "publish/macos/$BUNDLE_NAME/Contents/MacOS/"
cp publish/macos/*.dylib "publish/macos/$BUNDLE_NAME/Contents/MacOS/" 2>/dev/null || true

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
