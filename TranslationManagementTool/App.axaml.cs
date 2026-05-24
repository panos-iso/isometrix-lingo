using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using TranslationManagementTool.ViewModels;
using TranslationManagementTool.Views;
using TranslationManagementTool.Services;

namespace TranslationManagementTool;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsService = new UserSettingsService();

            if (settingsService.IsFirstRun())
            {
                var welcomeWindow = new WelcomeWindow();
                welcomeWindow.Show();
                welcomeWindow.Closed += (s, e) =>
                {
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(),
                    };
                    desktop.MainWindow.Show();
                };
            }
            else
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}