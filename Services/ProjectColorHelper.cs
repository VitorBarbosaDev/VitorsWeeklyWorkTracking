using System;
using System.Windows.Media;

namespace VitorsWeeklyWorkTracking.Services;

public static class ProjectColorHelper
{
    private static readonly string[] ProjectPalette =
    {
        "#2563EB", // Blue
        "#10B981", // Emerald
        "#8B5CF6", // Purple
        "#F59E0B", // Amber
        "#EC4899", // Pink
        "#06B6D4", // Cyan
        "#F97316", // Orange
        "#14B8A6", // Teal
        "#84CC16", // Lime
        "#E11D48", // Rose
        "#6366F1", // Indigo
        "#A855F7"  // Violet
    };

    public static string GetProjectColorHex(string? projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return "#94A3B8";

        uint hash = 5381;
        foreach (char c in projectName.Trim().ToLowerInvariant())
        {
            hash = ((hash << 5) + hash) + c;
        }

        int index = (int)(hash % (uint)ProjectPalette.Length);
        return ProjectPalette[index];
    }

    public static SolidColorBrush GetProjectBrush(string? projectName)
    {
        var hex = GetProjectColorHex(projectName);
        return CreateBrush(hex);
    }

    public static (SolidColorBrush Background, SolidColorBrush Border, SolidColorBrush Foreground) GetProjectBadgeBrushes(string? projectName, bool? isDark = null)
    {
        var hex = GetProjectColorHex(projectName);
        bool dark = isDark ?? (ThemeManager.CurrentTheme != AppTheme.ModernLight && ThemeManager.CurrentTheme != AppTheme.CandyPop);

        try
        {
            var baseColor = (Color)ColorConverter.ConvertFromString(hex);
            if (dark)
            {
                var bg = Color.FromArgb(50, baseColor.R, baseColor.G, baseColor.B);
                var border = Color.FromArgb(140, baseColor.R, baseColor.G, baseColor.B);
                var fg = Color.FromArgb(255, (byte)Math.Min(255, baseColor.R + 70), (byte)Math.Min(255, baseColor.G + 70), (byte)Math.Min(255, baseColor.B + 70));
                return (CreateBrush(bg), CreateBrush(border), CreateBrush(fg));
            }
            else
            {
                var bg = Color.FromArgb(32, baseColor.R, baseColor.G, baseColor.B);
                var border = Color.FromArgb(100, baseColor.R, baseColor.G, baseColor.B);
                return (CreateBrush(bg), CreateBrush(border), CreateBrush(hex));
            }
        }
        catch
        {
            return (CreateBrush("#EFF6FF"), CreateBrush("#BFDBFE"), CreateBrush("#1D4ED8"));
        }
    }

    private static SolidColorBrush CreateBrush(string hex)
    {
        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
        catch
        {
            var fallback = new SolidColorBrush(Colors.DodgerBlue);
            fallback.Freeze();
            return fallback;
        }
    }

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
