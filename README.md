# Translation Management Tool

Cross-platform desktop application for managing translation files (JSON and RESX).

Built with **Avalonia UI** and **.NET 10** for macOS and Windows.

## Features

- 📂 **Import JSON translation files** with automatic language detection
- 🔍 **Search** across keys and all language values
- 🎯 **Filter** by source file with toggle buttons
- ✏️ **Edit translations** for all languages in one dialog
- 💾 **Export modified translations** to JSON files
- 🌍 **Multi-language support** with dynamic column generation
- 🔄 **Key consolidation** - automatically merges keys from multiple files

## Download

### Pre-built Releases

Download the latest release for your platform:

👉 **[Releases Page](https://github.com/panos-iso/translations-management-tool/releases)**

- **macOS (Apple Silicon)**: `TranslationTool-macOS-arm64.tar.gz`
- **Windows (x64)**: `TranslationTool-Windows-x64.zip`

### Installation

**macOS:**

```bash
tar -xzf TranslationTool-macOS-arm64.tar.gz
./TranslationManagementTool
```

**Windows:**

1. Extract `TranslationTool-Windows-x64.zip`
2. Run `TranslationManagementTool.exe`

No .NET runtime installation required - fully self-contained!

## Usage

1. **First Run**: Enter your name when prompted
2. **Import Files**: Click "Import Files" and select JSON translation files (e.g., `forms_en.json`, `forms_es.json`)
3. **View Translations**: Browse all keys with values for each language
4. **Filter**: Toggle file filters to show specific translation files
5. **Search**: Type to search across keys and all language values
6. **Edit**: Click "Edit" to modify translation values for all languages
7. **Export**: Click "Export Modified" to save changes (default: `output/` directory)

### File Naming Convention

Files must follow the pattern: `basename_language.json`

Examples:

- `forms_en.json` → English translations for "forms"
- `forms_es.json` → Spanish translations for "forms"
- `errors_fr.json` → French translations for "errors"

The tool automatically detects the language code and groups files by their base name.

## Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)

### Build from Source

```bash
# Clone repository
git clone https://github.com/panos-iso/translations-management-tool.git
cd translations-management-tool

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project TranslationManagementTool/TranslationManagementTool.csproj

# Run tests
dotnet test
```

### Project Structure

```
TranslationManagementTool/          # Main application
├── Models/                         # Data models
├── Services/                       # Business logic (readers, writers, store)
├── ViewModels/                     # MVVM view models
├── Views/                          # XAML UI views
└── Helpers/                        # Utility classes

TranslationManagementTool.Tests/    # Unit tests (xUnit)
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
   tar -czf TranslationTool-macOS-arm64.tar.gz TranslationManagementTool

   # Windows
   cd publish/windows
   zip -r TranslationTool-Windows-x64.zip TranslationManagementTool.exe
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
