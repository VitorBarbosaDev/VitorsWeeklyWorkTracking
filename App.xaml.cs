using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    private const string SingleInstanceMutexName = @"Local\VitorsWeeklyWorkTracking_Singleton_Mutex_Guid_2026";
    public const string ShowInstanceMessage = "VitorsWeeklyWorkTracking_ShowInstance_Broadcast_Msg";
    public static readonly int WM_SHOWINSTANCE = RegisterWindowMessage(ShowInstanceMessage);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    private const int HWND_BROADCAST = 0xffff;
    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;

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
        try
        {
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew);
            if (!createdNew)
            {
                // Another instance of the application is already running in the background.
                // Bring the existing instance to the foreground and terminate this duplicate instance immediately.
                BringExistingInstanceToFront();
                Shutdown();
                return;
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "App.SingleInstanceMutex");
        }

        ShutdownMode = ShutdownMode.OnMainWindowClose;
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

    private static void BringExistingInstanceToFront()
    {
        try
        {
            if (WM_SHOWINSTANCE != 0)
            {
                PostMessage((IntPtr)HWND_BROADCAST, WM_SHOWINSTANCE, IntPtr.Zero, IntPtr.Zero);
            }

            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(currentProcess.ProcessName);
            foreach (var proc in processes)
            {
                if (proc.Id == currentProcess.Id) continue;
                var handle = proc.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    if (IsIconic(handle))
                    {
                        ShowWindow(handle, SW_RESTORE);
                    }
                    else
                    {
                        ShowWindow(handle, SW_SHOW);
                    }
                    SetForegroundWindow(handle);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "BringExistingInstanceToFront");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_singleInstanceMutex != null)
        {
            try
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            catch { }
            try
            {
                _singleInstanceMutex.Dispose();
            }
            catch { }
            _singleInstanceMutex = null;
        }

        base.OnExit(e);
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