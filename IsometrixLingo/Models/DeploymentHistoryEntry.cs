using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IsometrixLingo.Models;

public partial class DeploymentHistoryEntry : ObservableObject
{
    [ObservableProperty]
    private DateTime _timestamp;

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private bool _success;

    [ObservableProperty]
    private string _deploymentRoot = string.Empty;

    public string FormattedTimestamp => Timestamp.ToString("MMM dd, yyyy HH:mm:ss");
    public string StatusIcon => Success ? "✓" : "✗";
    public string StatusText => Success ? "Success" : "Failed";
    public string StatusColor => Success ? "#4CAF50" : "#EF5350";
}

/// <summary>
/// Serializable version of DeploymentHistoryEntry
/// </summary>
public class SerializableDeploymentHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public int FileCount { get; set; }
    public bool Success { get; set; }
    public string DeploymentRoot { get; set; } = string.Empty;
}
