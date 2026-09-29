using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        // Global unhandled exception handlers to prevent silent crashes
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                StoragePathHelper.LogError(ex, "AppDomain.UnhandledException");
                MessageBox.Show(
                    $"An unexpected fatal error occurred:\n\n{ex.Message}\n\nDetails have been logged to the user data folder.",
                    "Vitor's Weekly Work Tracking - Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            if (args.Exception != null)
            {
                StoragePathHelper.LogError(args.Exception, "TaskScheduler.UnobservedTaskException");
                args.SetObserved();
            }
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        try
        {
            ThemeManager.Initialize();
            WindowTitleBarHelper.Initialize();

            // Automatically apply theme-matching title bar styling to all application windows
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "App.OnStartup");
            MessageBox.Show(
                $"Error initializing application:\n\n{ex.Message}",
                "Vitor's Weekly Work Tracking - Startup Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        base.OnStartup(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        StoragePathHelper.LogError(e.Exception, "DispatcherUnhandledException");
        
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nDetails have been logged.",
            "Vitor's Weekly Work Tracking - Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
        {
            try
            {
                WindowTitleBarHelper.ApplyThemeTitleBar(window);
            }
            catch (Exception ex)
            {
                StoragePathHelper.LogError(ex, "OnWindowLoaded.ApplyThemeTitleBar");
            }
        }
    }
}