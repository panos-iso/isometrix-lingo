namespace IsometrixLingo.Models;

public class UserSettings
{
    public string Username { get; set; } = string.Empty;
    public string SettingsFilePath { get; set; } = string.Empty;
    public string GitHubToken { get; set; } = string.Empty;
    public string GitHubUsername { get; set; } = string.Empty;
}
