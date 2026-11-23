using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;
using Runix.Logic.Helpers;
using RunixLauncher.Helpers;

namespace RunixLauncher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            e.SetObserved();
            _ = CatchExceptionAsync(e.Exception).ConfigureAwait(true);
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        //GameLauncher.KillAllExistingProcesses();
    }

    private async Task CatchExceptionAsync(Exception exception)
    {
        try
        {
            LoggingHelper.LogError(exception);
        }
        catch { }

        // don't want this to loop endlessly loop for some reason
        try
        {
            await DependencyManager.OpenExceptionDialog("Unhandled exception", exception);
        }
        catch { }
    }
}