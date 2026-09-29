using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace VitorsWeeklyWorkTracking.Services;

public class MonitorDisplayInfo
{
    public int Index { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public Rect PhysicalBounds { get; set; }
    public Rect PhysicalWorkArea { get; set; }
    public Rect DipsBounds { get; set; }
    public Rect DipsWorkArea { get; set; }
    public double DpiScaleX { get; set; } = 1.0;
    public double DpiScaleY { get; set; } = 1.0;

    public override string ToString() => DisplayName;
}

public static class ScreenHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    private const int MONITORINFOF_PRIMARY = 0x00000001;
    private const int MDT_EFFECTIVE_DPI = 0;

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("shcore.dll", SetLastError = true)]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    /// <summary>
    /// Gets a list of all currently connected active display monitors.
    /// </summary>
    public static List<MonitorDisplayInfo> GetMonitors()
    {
        var monitors = new List<MonitorDisplayInfo>();
        int index = 0;

        try
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                var info = new MONITORINFOEX();
                info.cbSize = Marshal.SizeOf<MONITORINFOEX>();

                if (GetMonitorInfo(hMonitor, ref info))
                {
                    bool isPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;
                    double dpiScaleX = 1.0;
                    double dpiScaleY = 1.0;

                    try
                    {
                        if (GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY) == 0 && dpiX > 0 && dpiY > 0)
                        {
                            dpiScaleX = dpiX / 96.0;
                            dpiScaleY = dpiY / 96.0;
                        }
                    }
                    catch
                    {
                        dpiScaleX = 1.0;
                        dpiScaleY = 1.0;
                    }

                    var physicalBounds = new Rect(info.rcMonitor.Left, info.rcMonitor.Top, info.rcMonitor.Width, info.rcMonitor.Height);
                    var physicalWorkArea = new Rect(info.rcWork.Left, info.rcWork.Top, info.rcWork.Width, info.rcWork.Height);

                    var dipsBounds = new Rect(
                        physicalBounds.Left / dpiScaleX,
                        physicalBounds.Top / dpiScaleY,
                        physicalBounds.Width / dpiScaleX,
                        physicalBounds.Height / dpiScaleY);

                    var dipsWorkArea = new Rect(
                        physicalWorkArea.Left / dpiScaleX,
                        physicalWorkArea.Top / dpiScaleY,
                        physicalWorkArea.Width / dpiScaleX,
                        physicalWorkArea.Height / dpiScaleY);

                    int monitorNum = index + 1;
                    string resText = $"{info.rcMonitor.Width}×{info.rcMonitor.Height}";
                    string displayName = isPrimary
                        ? $"🖥️ Monitor {monitorNum} ({resText} - Primary)"
                        : $"🖥️ Monitor {monitorNum} ({resText})";

                    monitors.Add(new MonitorDisplayInfo
                    {
                        Index = index,
                        DeviceName = info.szDevice ?? $"DISPLAY{monitorNum}",
                        DisplayName = displayName,
                        ShortName = $"M{monitorNum}",
                        IsPrimary = isPrimary,
                        PhysicalBounds = physicalBounds,
                        PhysicalWorkArea = physicalWorkArea,
                        DipsBounds = dipsBounds,
                        DipsWorkArea = dipsWorkArea,
                        DpiScaleX = dpiScaleX,
                        DpiScaleY = dpiScaleY
                    });

                    index++;
                }

                return true;
            }, IntPtr.Zero);
        }
        catch
        {
            // Fallback in case EnumDisplayMonitors encounters an error
        }

        if (monitors.Count == 0)
        {
            var fallbackWork = SystemParameters.WorkArea;
            var fallbackBounds = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
            monitors.Add(new MonitorDisplayInfo
            {
                Index = 0,
                DeviceName = "\\\\.\\DISPLAY1",
                DisplayName = "🖥️ Primary Monitor",
                ShortName = "M1",
                IsPrimary = true,
                PhysicalBounds = fallbackBounds,
                PhysicalWorkArea = fallbackWork,
                DipsBounds = fallbackBounds,
                DipsWorkArea = fallbackWork,
                DpiScaleX = 1.0,
                DpiScaleY = 1.0
            });
        }

        return monitors;
    }

    /// <summary>
    /// Gets the primary monitor or first available monitor.
    /// </summary>
    public static MonitorDisplayInfo GetPrimaryMonitor()
    {
        var monitors = GetMonitors();
        return monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
    }

    /// <summary>
    /// Resolves a monitor by identifier, device name, index or primary keyword.
    /// </summary>
    public static MonitorDisplayInfo GetMonitor(string? targetMonitor, int monitorIndex = -1)
    {
        var monitors = GetMonitors();
        if (monitors.Count == 0)
        {
            return GetPrimaryMonitor();
        }

        // 1. If explicit valid index is specified
        if (monitorIndex >= 0 && monitorIndex < monitors.Count)
        {
            return monitors[monitorIndex];
        }

        // 2. If targetMonitor is "Primary" or null/empty
        if (string.IsNullOrWhiteSpace(targetMonitor) ||
            string.Equals(targetMonitor, "Primary", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(targetMonitor, "Default", StringComparison.OrdinalIgnoreCase))
        {
            return monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
        }

        // 3. Match by DeviceName (e.g. "\\.\DISPLAY1")
        var byDevice = monitors.FirstOrDefault(m => string.Equals(m.DeviceName, targetMonitor, StringComparison.OrdinalIgnoreCase));
        if (byDevice != null)
        {
            return byDevice;
        }

        // 4. Match by index number string
        if (int.TryParse(targetMonitor, out int parsedIdx) && parsedIdx >= 0 && parsedIdx < monitors.Count)
        {
            return monitors[parsedIdx];
        }

        // 5. Match by DisplayName or ShortName
        var byName = monitors.FirstOrDefault(m =>
            m.DisplayName.Contains(targetMonitor, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.ShortName, targetMonitor, StringComparison.OrdinalIgnoreCase));
        if (byName != null)
        {
            return byName;
        }

        // 6. Fallback to primary
        return monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
    }

    /// <summary>
    /// Returns the working area in WPF DIPs for the target monitor.
    /// </summary>
    public static Rect GetMonitorWorkArea(string? targetMonitor, int monitorIndex = -1)
    {
        var monitor = GetMonitor(targetMonitor, monitorIndex);
        return monitor.DipsWorkArea;
    }
}
