# IsometrixLingo v3.0.0 Release Notes

**Release Date:** May 26, 2026

## 🎉 Major Feature: Suggestion Mode

Version 3.0.0 introduces a comprehensive **Suggestion Mode** workflow, enabling collaborative translation management with suggestion tracking, review, and acceptance workflows.

---

## ✨ New Features

### Dual-Mode Workflow
- **Edit Mode**: Direct translation editing (existing functionality)
- **Suggest Mode**: Propose translations without modifying actual values
- Mode selection in Step 3 with visual feedback and clear guidance
- Mode-specific UI adaptations throughout the application

### Suggestion Management
- **Suggestion Format**: `actual value SUGGESTION:suggested_value,by:[username],at:[yyyy-MM-ddTHH:mm:ssZ]`
- **Accept/Reject Actions**: ✓ and ✗ buttons in both main grid and edit dialog
- **Visual Indicators**: Purple cell borders for rows with pending suggestions
- **Filter Support**: "Only With Suggestions" filter to focus on pending items
- **Suggestion Display**: Inline suggestions shown in edit dialog with attribution

### Smart Validation
- **Mode-Aware Missing Translations Detection**:
  - Edit Mode: Only checks actual values (suggestions don't count as complete)
  - Suggest Mode: Accepts either values OR suggestions as valid
- **Distinct Warning Dialogs**:
  - "⚠️ Unresolved Suggestions Detected" (Edit mode only)
  - "⚠️ Missing Translations Detected" (mode-aware logic)
- **Confirmation on Save**: Warns when saving edit dialog with pending suggestions

### UX Enhancements
- Clickable mode selection boxes with 4px borders and visual feedback
- Suggestion text displayed with username and timestamp
- Edit dialog adapted for both Edit and Suggest modes
- Show Original toggle automatically hidden in Suggest mode
- Improved confirmation dialog layout (no text overlap)
- Color-coded cell borders: Orange for modifications, Purple for suggestions

---

## 🔧 Technical Improvements

### Core Models & Services
- `TranslationKey.AcceptSuggestionForLanguage()`: Apply and remove suggestion
- `TranslationKey.RejectSuggestionForLanguage()`: Discard suggestion
- `TranslationKey.SetSuggestionForLanguage()`: Add/update/remove suggestions
- `Suggestion` model with Value, Username, Timestamp properties
- Enhanced `ConsolidateKeys()` to preserve suggestions during import
- Progress persistence includes mode selection state

### UI Architecture
- 15+ new XAML converters for mode-specific UI behavior
- Explicit `SuggestedValues` bindings to ensure Avalonia UI refresh
- Three-row grid layout in confirmation dialogs for better spacing
- Dynamic column generation with multi-binding support

### Data Persistence
- Suggestion format preserved in both JSON and RESX formats
- Export validation respects current mode
- Session state includes mode selection and status
- `HasUnsavedChanges` tracking throughout workflow

---

## 🧪 Testing

### Test Coverage
- **83 tests passing** (up from 63 baseline)
- **20 new tests** for suggestion mode features:
  - 14 tests for `TranslationKey` suggestion operations
  - 6 tests for mode-aware validation logic
  - Edge cases: empty values, whitespace, mixed scenarios
  - Real-world workflows: accept then clear, partial edits

### Test Categories
- Suggestion acceptance and rejection
- Mode-aware missing translations detection
- Suggestion persistence through import/export
- UI state management and refresh
- Validation in both Edit and Suggest modes

---

## 📦 Breaking Changes

**None** - This is a backward-compatible feature addition. Existing translation files without suggestions work normally in Edit mode.

---

## 🐛 Bug Fixes

- Fixed grid not refreshing after accept/reject suggestions (explicit bindings)
- Fixed Step 2 (File Mapping) status not persisting in progress save
- Fixed suggestions being lost during file consolidation
- Fixed validation incorrectly treating suggestions as values in Edit mode
- Fixed text overlap in confirmation dialogs
- Fixed Show Original toggle appearing in Suggest mode

---

## 🎯 Use Cases

### Team Collaboration
1. Translator suggests translations in Suggest mode
2. Reviewer switches to Edit mode to review all pending suggestions
3. Accept ✓ applies suggestion, Reject ✗ discards it
4. Export final translations with all suggestions resolved

### Quality Review
- Use "Only With Suggestions" filter to focus on items needing review
- Visual purple borders highlight rows with pending suggestions
- Accept/Reject directly from main grid or detailed edit dialog
- Confirmation prevents accidental loss of pending suggestions

### Workflow Separation
- Junior translators use Suggest mode (can't modify actual values)
- Senior reviewers use Edit mode (full control over translations)
- Clear mode selection with guidance text for each role

---

## 📝 Migration Notes

No migration required. Existing translation files work unchanged. The new Suggestion mode is opt-in via Step 3 mode selection.

---

## 🙏 Acknowledgments

This release represents a significant enhancement to the IsometrixLingo translation management workflow, enabling better collaboration and quality control for translation teams.

For detailed implementation notes, see PR #20: "Suggestion Mode v3.0 - Complete Implementation"

---

## 📥 Downloads

- **macOS (Apple Silicon)**: IsometrixLingo-v3.0.0-macos-arm64.app.zip
- **Windows (x64)**: IsometrixLingo-v3.0.0-windows-x64.zip

---

**Full Changelog**: https://github.com/panos-iso/isometrix-lingo/compare/v2.2...v3.0.0
