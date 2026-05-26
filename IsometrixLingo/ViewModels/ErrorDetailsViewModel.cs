using System.Collections.ObjectModel;
using IsometrixLingo.Models;

namespace IsometrixLingo.ViewModels;

/// <summary>
/// ViewModel for the Error Details dialog.
/// </summary>
public class ErrorDetailsViewModel : ViewModelBase
{
    public ObservableCollection<ImportError> ImportErrors { get; }

    public ErrorDetailsViewModel(ObservableCollection<ImportError> errors)
    {
        ImportErrors = errors;
    }
}
