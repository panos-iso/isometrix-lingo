# IsometrixLingo v3.4.0 Release Notes

**Release Date:** May 29, 2026

## 🎉 Major Feature: Deployment Mode

Version 3.4.0 introduces **Deployment Mode** (LP-13172), a developer-focused workflow that automates the deployment of translation files back to their original repository locations with intelligent path validation and safety features.

---

## ✨ New Features

### Deployment Mode Workflow
- **Developer-Only Feature**: Enable via "Developer Mode" checkbox in user profile
- **Fast Deployment Path**: Skip editing step and go straight from import → export → deploy
- **Third Mode Selection**: New 🚀 Deployment Mode option alongside Edit and Suggest modes
- **6-Step Workflow**: Import → File Mapping → Mode Selection → Export → Deploy

### Smart Deployment Detection
- **Automatic Root Suggestion**: Searches parent/sibling directories for matching repository location
- **ZIP Prefix Matching**: Extracts directory name from ZIP filename (e.g., "iso_exported_timestamp.zip" → suggests "iso" directory)
- **One-Click Accept**: Use suggested deployment root with a single button click
- **Persistent Settings**: Remembers last deployment root for quick reuse

### Two-Level Path Validation
- **Soft Validation** (Warning):
  - Checks if deployment root name matches ZIP prefix
  - Shows warning dialog but allows user to continue
  - Use case: Deploying to different directory structure
- **Hard Validation** (Abort):
  - Validates ALL file paths map to valid deployment locations
  - Prevents deployment outside root directory (security)
  - Blocks deployment if ANY path is invalid
  - Shows detailed errors in error viewer

### Deployment Execution
- **All-or-Nothing Deployment**: If any file fails, all changes are automatically rolled back
- **Deployment Preview**: View source → target file mappings before deploying
- **Progress Tracking**: Real-time status updates during deployment
- **Error Recovery**: Automatic rollback ensures repository is never left in partial state

### Pre-Deployment Validation
- **Missing Translation Warning**: Alerts user before export if translations are incomplete
- **Cancel or Continue**: Option to return to editing or proceed with deployment
- **Mode-Aware Detection**: Uses deployment mode validation logic

### Iterative Development Workflow
- **Deploy Again**: Quickly re-deploy to same location after making changes
- **View Exported Files**: Open export folder in file explorer with one click
- **Start Over**: Clear all state and begin fresh import

---

## 🔧 Technical Implementation

### New Services
- **DeploymentService**:
  - `SuggestDeploymentRoot()`: Smart directory detection algorithm
  - `ValidateRootNameMatch()`: Soft validation with warning messages
  - `ValidateAllPaths()`: Hard validation for security and correctness
  - `GetDeploymentPreview()`: Generate source → target mappings
  - `ExecuteDeployment()`: All-or-nothing file copying with rollback

### Enhanced Models
- **DeploymentPreviewItem**: Source/target path mapping model
- **ImportErrorType Extensions**: Added FileNotFound, InvalidPath, ReadError, WriteError
- **UserSettings Extensions**: LastDeploymentRoot persistence
- **WorkflowStep Extension**: Deploy = 6 step added

### UI Components
- **Deployment Step Panel**: Dedicated UI for deployment configuration
- **Smart Suggestion Box**: Highlighted display of suggested deployment root
- **Deployment Preview List**: ScrollViewer with source → target file mappings
- **Validation Results Display**: Color-coded validation messages (green/orange/red)
- **Action Buttons**: Validate & Deploy, Deploy Again, View Exported Files, Start Over

### State Management
- **Step 6 Status Tracking**: Deploy step with NotStarted/InProgress/Completed states
- **Error Integration**: Reuses existing ImportErrors collection and error viewer
- **Auto-Save Progress**: Deployment state persists across sessions
- **Conditional Workflow**: Edit step skipped when Deployment mode selected

---

## 🛡️ Safety Features

### Path Security
- **Root Boundary Validation**: Prevents files from escaping deployment directory
- **Path Normalization**: Full path resolution to detect directory traversal attempts
- **Write Permission Checks**: Validates target directories exist or can be created

### Error Handling
- **Comprehensive Error Types**: FileNotFound, InvalidPath, ReadError, WriteError
- **Detailed Error Messages**: Clear guidance for troubleshooting deployment failures
- **Error Viewer Integration**: Click "View Error Details" to see all validation errors
- **Graceful Degradation**: Failed deployments leave repository unchanged

### User Confirmation
- **Soft Validation Warnings**: Orange-themed warning dialogs for name mismatches
- **Hard Validation Blocks**: Red error messages prevent invalid deployments
- **Pre-Export Warnings**: Missing translations alert before creating ZIP

---

## 📋 Deployment Workflow Example

```
1. Enable Developer Mode in profile
2. Import translation files (e.g., from /Users/dev/translations/iso/)
3. Select Deployment Mode (🚀)
4. Export creates: output/iso_exported_20260529_143022.zip
5. Smart suggestion detects: /Users/dev/repos/lumina/iso
6. Preview shows:
   iso/Actions_es.resx → /Users/dev/repos/lumina/iso/Actions_es.resx
   iso/Forms.en.json → /Users/dev/repos/lumina/iso/Forms.en.json
7. Validate & Deploy copies all files
8. Success! All files deployed to repository
```

---

## 🧪 Testing

### Validation Scenarios
- ✅ Soft validation: Mismatched root directory names (warning + continue)
- ✅ Hard validation: Invalid file paths (error + abort)
- ✅ Smart detection: Parent/sibling directory search
- ✅ All-or-nothing: Partial failure triggers full rollback
- ✅ Path security: Directory traversal prevention
- ✅ Missing translations: Pre-export warning in deployment mode

### Platform Testing
- ✅ macOS (Apple Silicon): File deployment and path validation
- ✅ Windows (x64): File deployment and path validation
- ✅ Cross-platform ZIP handling
- ✅ File system permission handling

---

## 📝 Documentation Updates

### README Additions
- **Deployment Mode Section**: Complete workflow documentation
- **Safety Features**: All-or-nothing deployment explanation
- **Developer Mode**: Profile setting documentation
- **Use Cases**: When to use Deployment vs Edit/Suggest modes

---

## 🔄 Workflow Changes

### Conditional Edit Step
- **Deployment Mode**: Import → File Mapping → Mode Selection → Export → Deploy
- **Edit/Suggest Modes**: Import → File Mapping → Mode Selection → Edit → Export (unchanged)

### Export Behavior
- **Deployment Mode**: Transitions to Deploy step after export (no "Start Over" prompt)
- **Edit/Suggest Modes**: Shows "Start Over" prompt after export (unchanged)

---

## 💡 Use Cases

### Primary Use Case
**Developer updating repository translations:**
1. Import from exported ZIP or original files
2. Skip editing (already done elsewhere)
3. Automatically deploy to repository
4. Validate paths before deployment
5. One-click deployment with safety checks

### Iterative Workflow
**Making quick changes and re-deploying:**
1. Deploy translations to repository
2. Notice issue, click "Start Over"
3. Import same files
4. Make corrections in Edit mode
5. Switch to Deployment mode
6. Click "Deploy Again" (uses same root)
7. Fast re-deployment without re-selection

---

## 🎯 Key Benefits

- ⚡ **Speed**: Skip editing when translations are already complete
- 🛡️ **Safety**: All-or-nothing deployment prevents partial updates
- 🎯 **Accuracy**: Path validation ensures correct deployment location
- 🔄 **Iteration**: Deploy Again enables rapid testing cycles
- 📍 **Intelligence**: Smart detection reduces manual configuration

---

## 🔗 Related Issues

- **LP-13172**: Automatically deploy translations back to original repository locations

---

## ⚙️ Configuration

### Enable Deployment Mode
1. Click profile icon (👤) in top-right
2. Check "Developer Mode (enables deployment features)"
3. Click "Save"
4. Deployment mode option now visible in Step 3

### Persistent Settings
- `LastDeploymentRoot`: Stored in user settings (AppData)
- `IsDeveloper`: Persists across sessions
- Export directory preference retained

---

## 🐛 Bug Fixes

- None (new feature release)

---

## 📦 Distribution

Deployment mode is included in all platform distributions:
- macOS (Apple Silicon)
- Windows (x64)

No additional setup required - feature enabled via user profile.
