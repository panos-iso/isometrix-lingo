# Release Notes Template

Use this template when creating GitHub releases.

---

## Installation Instructions

### macOS (Apple Silicon)

1. Download `isometrix-lingo-v*-macos-arm64.tar`
2. Extract the archive
3. **Important**: Remove the quarantine attribute to allow the app to run:
   ```bash
   xattr -d com.apple.quarantine IsometrixLingo.app
   ```
4. Double-click `IsometrixLingo.app` to launch

> The quarantine removal is required because the app is not code-signed. This is a one-time step after downloading.

### Windows (x64)

1. Download `isometrix-lingo-v*-windows-x64.zip`
2. Extract the archive
3. Run `isometrix-lingo.exe`

---

## What's New in vX.X.X

[Add release-specific changes here]

---

## Known Issues

[Add any known issues here]
