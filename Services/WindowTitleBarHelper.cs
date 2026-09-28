using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace VitorsWeeklyWorkTracking.Services;

/// <summary>
/// Helper to configure the Windows Desktop Window Manager (DWM) title bar attributes
/// so the top title bar (caption area with minimize/maximize/close buttons and title text)
/// dynamically matches the active theme colors.
/// </summary>
public static class WindowTitleBarHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    static WindowTitleBarHelper()
    {
        ThemeManager.ThemeChanged += _ => UpdateAllWindows();
    }

    /// <summary>
    /// Explicit initialization method to ensure static constructor and event hooks are registered.
    /// </summary>
    public static void Initialize()
    {
        // Static constructor runs automatically
    }

    /// <summary>
    /// Updates title bar styling for all currently open application windows.
    /// </summary>
    public static void UpdateAllWindows()
    {
        if (Application.Current == null) return;

        if (Application.Current.Dispatcher.CheckAccess())
        {
            ApplyToAllWindows();
        }
        else
        {
            Application.Current.Dispatcher.InvokeAsync(ApplyToAllWindows);
        }
    }

    private static void ApplyToAllWindows()
    {
        if (Application.Current == null) return;

        foreach (var window in Application.Current.Windows.OfType<Window>().ToArray())
        {
            ApplyThemeTitleBar(window);
        }
    }

    /// <summary>
    /// Applies theme-matched styling to the native Windows title bar of the specified window.
    /// </summary>
    public static void ApplyThemeTitleBar(Window? window)
    {
        if (window == null) return;

        // Skip windows without standard window style (e.g., custom transparent popup widgets)
        if (window.WindowStyle == WindowStyle.None) return;

        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle != IntPtr.Zero)
            {
                ApplyThemeTitleBarToHwnd(handle);
            }
        }
        catch
        {
            // Handle might not be created yet; hooked events below will catch it.
        }

        window.SourceInitialized -= OnWindowSourceInitialized;
        window.SourceInitialized += OnWindowSourceInitialized;

        window.Loaded -= OnWindowLoaded;
        window.Loaded += OnWindowLoaded;
    }

    /// <summary>
    /// Alias for backwards compatibility.
    /// </summary>
    public static void ApplyDarkTitleBar(Window? window) => ApplyThemeTitleBar(window);

    private static void OnWindowSourceInitialized(object? sender, EventArgs e)
    {
        if (sender is Window window && window.WindowStyle != WindowStyle.None)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd != IntPtr.Zero)
            {
                ApplyThemeTitleBarToHwnd(hwnd);
            }
        }
    }

    private static void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Window window && window.WindowStyle != WindowStyle.None)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd != IntPtr.Zero)
            {
                ApplyThemeTitleBarToHwnd(hwnd);
            }
        }
    }

    /// <summary>
    /// Applies DWM attributes to set the title bar caption, text, and border colors according to the current theme.
    /// </summary>
    public static void ApplyThemeTitleBarToHwnd(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;

        try
        {
            var (bgColor, fgColor, borderColor) = GetThemeColors();

            // Calculate luminance to decide dark vs light caption controls (minimize, maximize, close buttons)
            bool isDark = IsColorDark(bgColor);
            int darkMode = isDark ? 1 : 0;

            int hr = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            if (hr != 0)
            {
                // Fallback for Windows 10 versions 1809 - 1909
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
            }

            // Windows 11 (build 22000+): Set caption background color
            int captionColorRef = ToColorRef(bgColor);
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColorRef, sizeof(int));

            // Windows 11 (build 22000+): Set caption text color
            int textColorRef = ToColorRef(fgColor);
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColorRef, sizeof(int));

            // Windows 11 (build 22000+): Set window border color
            int borderColorRef = ToColorRef(borderColor);
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColorRef, sizeof(int));
        }
        catch
        {
            // Silently ignore if DWM or platform does not support these attributes
        }
    }

    private static (Color bg, Color fg, Color border) GetThemeColors()
    {
        Color bgColor = Color.FromRgb(15, 23, 42); // Fallback Dark Slate
        Color fgColor = Color.FromRgb(248, 250, 252);
        Color borderColor = Color.FromRgb(51, 65, 85);

        try
        {
            if (Application.Current?.Resources != null)
            {
                if (Application.Current.Resources["Theme.WindowBackground"] is SolidColorBrush bgBrush)
                {
                    bgColor = bgBrush.Color;
                }
                if (Application.Current.Resources["Theme.Foreground"] is SolidColorBrush fgBrush)
                {
                    fgColor = fgBrush.Color;
                }
                if (Application.Current.Resources["Theme.CardBorder"] is SolidColorBrush borderBrush)
                {
                    borderColor = borderBrush.Color;
                }
            }
        }
        catch
        {
            // Fall back to default colors
        }

        return (bgColor, fgColor, borderColor);
    }

    private static bool IsColorDark(Color color)
    {
        // Standard ITU-R BT.601 perceived luminance formula
        double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
        return luminance < 128.0;
    }

    private static int ToColorRef(Color color)
    {
        // Win32 COLORREF format: 0x00BBGGRR
        return (color.R) | (color.G << 8) | (color.B << 16);
    }
}
