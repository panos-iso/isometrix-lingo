# Isometrix Lingo

Cross-platform desktop application for managing translation files (JSON and RESX) for the Isometrix Lumina platform.

Built with **Avalonia UI** and **.NET 10** for macOS and Windows.

## Features

- 📂 **Import JSON and RESX files** with automatic language detection
- 🔍 **Search** across keys and all language values
- 🎯 **Filter** by source file with dropdown selector
- ✏️ **Edit translations** - double-click rows or use edit icon
- ➕ **Add new keys** with the "Add Key" button
- 💾 **Export all translations** preserving original file structure
- 🌍 **Multi-language support** (English and Spanish)
- 🔄 **Save/Load progress** - resume your work anytime
- 📋 **Template preservation** - maintains original file structure and order
- 🎨 **Theme-aware UI** - adapts to system dark/light mode

## Download

### Pre-built Releases

Download the latest release for your platform:

👉 **[Releases Page](https://github.com/panos-iso/isometrix-lingo/releases)**

- **macOS (Apple Silicon)**: `TranslationTool-macOS-arm64.tar.gz`
- **Windows (x64)**: `TranslationTool-Windows-x64.zip`

### Installation

**macOS:**

1. Extract the downloaded tar.gz file:
   ```bash
   tar -xzf isometrix-lingo-v*-macos-arm64.tar
   ```

2. Remove quarantine attribute (required for unsigned apps):
   ```bash
   xattr -d com.apple.quarantine IsometrixLingo.app
   ```

3. Double-click `IsometrixLingo.app` to launch

> **Note**: The quarantine removal is needed because the app is not code-signed. This is a one-time step after downloading.

**Windows:**

1. Extract `isometrix-lingo-v*-windows-x64.zip`
2. Run `isometrix-lingo.exe`

No .NET runtime installation required - fully self-contained!

## Usage

1. **Import Files**: Click "Import Files" and select translation files (JSON or RESX)
2. **View Translations**: Browse all keys with values for each language
3. **Filter**: Use dropdown to filter by source file
4. **Search**: Type to search across keys and all language values
5. **Edit**: Double-click a row or click the edit icon to modify translations
6. **Add Key**: Click "Add Key" to create new translation entries
7. **Export**: Click "Export All" to save changes (default: `output/` directory)
8. **Save Progress**: Save your work in progress to continue later
9. **Start Over**: Clear all data and import fresh files

### Supported File Formats

#### JSON Files (Frontend)

Files must follow the pattern: `BaseName.language.json`

Examples:

- `Settings.en.json` → English translations for "Settings"
- `Settings.es.json` → Spanish translations for "Settings"
- `Forms.en.json` → English translations for "Forms"

#### RESX Files (Backend)

Files must follow the pattern:

- English: `BaseName.resx` (no language code)
- Spanish: `BaseName_es.resx` (underscore + language code)

Examples:

- `FormTranslations.resx` → English translations
- `FormTranslations_es.resx` → Spanish translations
- `CommonTranslations.resx` → English translations
- `CommonTranslations_es.resx` → Spanish translations

### Supported Languages

Currently supports: **English (en)** and **Spanish (es)**

### Template Preservation

The tool preserves the original structure of your files:

- **JSON**: Maintains key order and nested structure
- **RESX**: Preserves all `<resheader>` elements and data element order
- **Updates**: Existing keys are updated in place, new keys are appended

## Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)

### Build from Source

```bash
# Clone repository
git clone https://github.com/panos-iso/isometrix-lingo.git
cd isometrix-lingo

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project IsometrixLingo/IsometrixLingo.csproj

# Run tests
dotnet test
```

### Project Structure

```
IsometrixLingo/          # Main application
├── Models/                         # Data models
├── Services/                       # Business logic (readers, writers, store)
├── ViewModels/                     # MVVM view models
├── Views/                          # XAML UI views
└── Helpers/                        # Utility classes

IsometrixLingo.Tests/    # Unit tests (xUnit)
└── Services/                       # Service tests (90%+ coverage)
```

### Architecture

- **MVVM Pattern**: Clean separation of UI and logic
- **Service Layer**: Isolated, testable services
- **Observable Collections**: Automatic UI updates
- **In-Memory Storage**: Changes persist only in memory until export

## Creating Releases

### Build Platform-Specific Executables

**macOS (Apple Silicon):**

```bash
chmod +x publish-macos.sh
./publish-macos.sh
```

**Windows (x64):**

```bash
chmod +x publish-windows.sh
./publish-windows.sh
```

Executables will be in `publish/macos/` and `publish/windows/`.

### Create GitHub Release

1. **Build executables** using the publish scripts above
2. **Create archives**:

   ```bash
   # macOS
   cd publish/macos
   tar -czf TranslationTool-macOS-arm64.tar.gz isometrix-lingo

   # Windows
   cd publish/windows
   zip -r TranslationTool-Windows-x64.zip isometrix-lingo.exe
   ```

3. **Create GitHub Release**:
   - Go to repository → Releases → "Draft a new release"
   - Tag version: `v1.0.0` (on `main` branch)
   - Release title: `v1.0.0 - Initial Release`
   - Upload both archive files
   - Publish release

## Technology Stack

- **Framework**: .NET 10 (LTS until November 2028)
- **UI**: Avalonia UI 12.0.3 (cross-platform XAML)
- **MVVM**: CommunityToolkit.Mvvm 8.4.1
- **Testing**: xUnit with 90%+ coverage for core services
- **JSON**: System.Text.Json (built-in)

## License

[Your License Here]

## Contributing

[Your Contributing Guidelines Here]
