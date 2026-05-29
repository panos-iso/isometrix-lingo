using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using IsometrixLingo.Models;

namespace IsometrixLingo.ViewModels;

public partial class DeploymentDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    [ObservableProperty]
    private bool _showPreviewItems;

    [ObservableProperty]
    private bool _showErrors;

    [ObservableProperty]
    private bool _showHistory;

    public ObservableCollection<DeploymentPreviewItem> PreviewItems { get; } = new();
    public ObservableCollection<ImportError> Errors { get; } = new();
    public ObservableCollection<DeploymentHistoryEntry> HistoryEntries { get; } = new();

    public DeploymentDetailsViewModel()
    {
    }

    public static DeploymentDetailsViewModel CreateForPreview(List<DeploymentPreviewItem> items)
    {
        var vm = new DeploymentDetailsViewModel
        {
            Title = "Deployment Preview",
            Subtitle = $"{items.Count} file(s) ready for deployment",
            ShowPreviewItems = true,
            ShowErrors = false,
            ShowHistory = false
        };

        foreach (var item in items)
        {
            vm.PreviewItems.Add(item);
        }

        return vm;
    }

    public static DeploymentDetailsViewModel CreateForErrors(List<ImportError> errors)
    {
        var vm = new DeploymentDetailsViewModel
        {
            Title = "Deployment Validation Errors",
            Subtitle = $"{errors.Count} error(s) detected - deployment cannot proceed",
            ShowPreviewItems = false,
            ShowErrors = true,
            ShowHistory = false
        };

        foreach (var error in errors)
        {
            vm.Errors.Add(error);
        }

        return vm;
    }

    public static DeploymentDetailsViewModel CreateForHistory(List<DeploymentHistoryEntry> historyEntries)
    {
        var vm = new DeploymentDetailsViewModel
        {
            Title = "Deployment History",
            Subtitle = $"Last {historyEntries.Count} deployment(s)",
            ShowPreviewItems = false,
            ShowErrors = false,
            ShowHistory = true
        };

        foreach (var entry in historyEntries)
        {
            vm.HistoryEntries.Add(entry);
        }

        return vm;
    }
}
