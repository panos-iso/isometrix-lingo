# Isometrix Lingo

Cross-platform desktop application for managing translation files (JSON and RESX) for the Isometrix Lumina platform.

Built with **Avalonia UI** and **.NET 10** for macOS and Windows.

## Features

### Core Translation Management
- 📂 **Import JSON and RESX files** with automatic language detection
- 🔍 **Search** across keys and all language values
- 🎯 **Filter** by source file with dropdown selector
- 🎯 **Filter unconfirmed translations** - show only keys needing review
- ✏️ **Edit translations** - double-click rows or use edit icon
- ➕ **Add new keys** with the "Add Key" button
- 💾 **Export all translations** preserving original file structure
- 🌍 **Multi-language support** (English and Spanish)
- 🔄 **Save/Load progress** - resume your work anytime
- 📋 **Template preservation** - maintains original file structure and order

### AI-Powered Features
- 🤖 **Suggest Mode** - Get AI-powered translation suggestions for missing values
- ✅ **Confirmation Tracking** - Auto-track confirmed translations with username and timestamp during export
- 👤 **Profile Management** - Set your username for audit trails

### User Interface
- 🎨 **Theme-aware UI** - adapts to system dark/light mode
- 📊 **Last Confirmed By** column - see who confirmed translations and when
- 🎯 **Edit/Suggest Mode Toggle** - switch between editing and getting AI suggestions
- 💬 **Personalized greeting** - friendly user welcome in title bar
- 📌 **Version display** - always know which version you're running

## Download

### Pre-built Releases

Download the latest release for your platform:

👉 **[Releases Page](https://github.com/panos-iso/isometrix-lingo/releases)**

- **macOS (Apple Silicon)**: `isometrix-lingo-v{version}-macos-arm64.tar.gz`
- **Windows (x64)**: `isometrix-lingo-v{version}-windows-x64.zip`

### Installation

**macOS (Apple Silicon):**

1. Extract the downloaded tar file
2. Remove macOS quarantine (choose **ONE** method):

   **Option A - For Non-Technical Users (GUI Method):**
   - Locate `IsometrixLingo.app` in Finder
   - **Right-click** (or Control+click) on the app
   - Select **"Open"** from the menu
   - Click **"Open"** in the security dialog that appears
   - The app will now launch (this only needs to be done once)

   **Option B - For Technical Users (Command Line):**
   ```bash
   xattr -d com.apple.quarantine IsometrixLingo.app
   ```

3. Double-click `IsometrixLingo.app` to launch

> **Why is this needed?** The app is not code-signed with an Apple Developer certificate. This is a one-time step after downloading.

**Windows (x64):**

1. Extract the ZIP file
2. Run `isometrix-lingo.exe`
3. If Windows SmartScreen appears, click "More info" → "Run anyway"

**No .NET runtime installation required** - fully self-contained!

## Usage

### Getting Started

1. **Set Your Profile**: Click the profile icon (👤) in the top-right to set your username
2. **Import Files**: Click "Import Files" and select translation files (JSON or RESX)
3. **Choose Mode**: Select Edit or Suggest mode based on your workflow

### Working with Translations

**Edit Mode:**
1. **View Translations**: Browse all keys with values for each language
2. **Filter by File**: Use dropdown to filter by source file
3. **Filter Unconfirmed**: Toggle "Only Unconfirmed" to see translations needing review
4. **Search**: Type to search across keys and all language values
5. **Edit**: Double-click a row or click the edit icon to modify translations
6. **Add Key**: Click "Add Key" to create new translation entries
7. **Export**: Click "Export All" to save changes (default: `output/` directory)
   - Translations with both English and Spanish values are automatically marked as confirmed with your username and timestamp

**Suggest Mode:**
1. **View Missing Translations**: See which keys are missing values
2. **Get AI Suggestions**: Click "Suggest Translations" to get AI-powered suggestions for missing values
3. **Review Suggestions**: Check the suggested translations in the grid
4. **Accept**: Click "Accept All Suggestions" to apply them
5. **Export**: Save the accepted suggestions to your files

### Additional Features

- **Save Progress**: Save your work in progress to continue later
- **Start Over**: Clear all data and import fresh files
- **Confirmation Tracking**: View who confirmed each translation and when in the "Last Confirmed By" column

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

## Release History

### v3.3.1 (May 2026)
- **Bug Fix**: Fixed profile button not clickable in custom title bar

### v3.3.0 (May 2026)
- **New**: Custom title bar with app version display
- **New**: Personalized user greeting with wave emoji

### v3.2.1 (May 2026)
- **Bug Fix**: Fixed confirmation tracking incorrectly activating in Suggest mode

### v3.2.0 (May 2026)
- **New**: Confirmation tracking system - auto-track who confirmed translations and when
- **New**: "Only Unconfirmed" filter to show translations needing review
- **New**: "Last Confirmed By" column showing confirmation audit trail

### v3.1.0 (May 2026)
- **New**: Enhanced error handling with detailed error dialogs
- **New**: Theme-aware UI improvements for dark/light mode compatibility

### v3.0.0 (May 2026)
- **Major**: Suggest Mode - AI-powered translation suggestions for missing values
- **New**: Edit/Suggest mode toggle workflow
- **New**: Accept/reject suggestions functionality

### v2.x (May 2026)
- Core translation management features
- JSON and RESX file support
- Import/Export functionality
- Search and filter capabilities

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
