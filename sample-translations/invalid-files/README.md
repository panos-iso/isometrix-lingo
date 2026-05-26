# Invalid Translation Files for Testing

This directory contains intentionally invalid translation files to test error handling in Isometrix Lingo v3.1.0.

## Test Files

### 1. Invalid Naming Convention

**InvalidName.json**
- **Error Type:** Invalid naming convention (JSON)
- **Issue:** Missing language code in filename
- **Expected:** Should fail with error message suggesting pattern: `{BaseName}.{language}.json`

**InvalidName.txt.resx**
- **Error Type:** Invalid naming convention (RESX)
- **Issue:** Extra `.txt` extension in filename
- **Expected:** Should fail with error message suggesting patterns: `{BaseName}.resx` or `{BaseName}_{language}.resx`

### 2. Malformed File Structure

**Malformed.en.json**
- **Error Type:** JSON parse error
- **Issue:** Missing commas between JSON properties (lines 3, 5)
- **Expected:** Should fail with JSON parse error and guidance to check syntax

**Malformed_es.resx**
- **Error Type:** RESX/XML parse error
- **Issue:** Missing closing `</value>` tag on line 21
- **Expected:** Should fail with XML parse error and guidance about valid RESX structure

## Testing Instructions

1. Open Isometrix Lingo v3.1.0
2. Click "Import Files"
3. Select all 4 files from this directory
4. The import should fail with errors
5. Click "View Error Details" button in the bottom banner
6. You should see all 4 errors listed with:
   - Filename in bold red
   - Clear error message
   - Helpful guidance on how to fix

## Expected Error Messages

1. **InvalidName.json**: "File name doesn't match expected pattern" → "Expected: {BaseName}.{language}.json (e.g., Forms.en.json, Forms.es.json)"
2. **InvalidName.txt.resx**: "File name doesn't match expected pattern" → "Expected: {BaseName}.resx or {BaseName}_{language}.resx (e.g., FormTranslations.resx, FormTranslations_es.resx)"
3. **Malformed.en.json**: "Failed to parse JSON: ..." → "Ensure the file contains valid JSON syntax. Check for missing commas, brackets, or quotes."
4. **Malformed_es.resx**: "Failed to parse RESX XML: ..." → "Ensure the file is a valid RESX file with proper XML structure."
