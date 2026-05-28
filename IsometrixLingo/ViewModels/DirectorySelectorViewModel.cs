using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IsometrixLingo.Models;

namespace IsometrixLingo.ViewModels;

public partial class DirectorySelectorViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<DirectoryScanResult> _directories = new();

    [ObservableProperty]
    private string _parentDirectoryPath = string.Empty;

    [ObservableProperty]
    private int _totalDirectories;

    [ObservableProperty]
    private int _selectedDirectories;

    [ObservableProperty]
    private int _totalFiles;

    public DirectorySelectorViewModel()
    {
        // Design-time constructor
    }

    public DirectorySelectorViewModel(string parentPath, ObservableCollection<DirectoryScanResult> directories)
    {
        ParentDirectoryPath = parentPath;
        Directories = directories;
        UpdateStatistics();

        // Subscribe to changes in selection
        foreach (var dir in Directories)
        {
            dir.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DirectoryScanResult.IsSelected))
                {
                    UpdateStatistics();
                }
            };
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var dir in Directories)
        {
            dir.IsSelected = true;
        }
        UpdateStatistics();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var dir in Directories)
        {
            dir.IsSelected = false;
        }
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        TotalDirectories = Directories.Count;
        SelectedDirectories = Directories.Count(d => d.IsSelected);
        TotalFiles = Directories.Where(d => d.IsSelected).Sum(d => d.FileCount);
    }

    public bool HasSelection => SelectedDirectories > 0;
}
