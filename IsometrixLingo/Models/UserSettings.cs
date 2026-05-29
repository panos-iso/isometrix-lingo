namespace IsometrixLingo.Models;

public class UserSettings
{
    public string Username { get; set; } = string.Empty;
    public string SettingsFilePath { get; set; } = string.Empty;
    public string LastImportDirectory { get; set; } = string.Empty;
    public string LastExportDirectory { get; set; } = string.Empty;
    public bool IsDeveloper { get; set; } = false;
    public string LastDeploymentRoot { get; set; } = string.Empty;
}
