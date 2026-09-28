using System.Configuration;
using System.Data;
using System.Windows;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ThemeManager.Initialize();
        WindowTitleBarHelper.Initialize();

        // Automatically apply theme-matching title bar styling to all application windows
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));

        base.OnStartup(e);
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
        {
            WindowTitleBarHelper.ApplyThemeTitleBar(window);
        }
    }
}