using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public static class ProjectColorHelper
{
    private static readonly ConcurrentDictionary<string, string> _customColors = new(StringComparer.OrdinalIgnoreCase);

    public const string DefaultProjectColorHex = "#6B7280"; // Clean neutral default grey

    public static readonly string[] ProjectPalette =
    {
        "#6B7280", // Default Grey
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

    public static readonly IReadOnlyList<(string Hex, string Name)> CuratedPalette = new List<(string, string)>
    {
        ("#6B7280", "Default Grey"),
        ("#2563EB", "Blue"),
        ("#3B82F6", "Sky Blue"),
        ("#06B6D4", "Cyan"),
        ("#14B8A6", "Teal"),
        ("#10B981", "Emerald"),
        ("#84CC16", "Lime"),
        ("#EAB308", "Yellow"),
        ("#F59E0B", "Amber"),
        ("#F97316", "Orange"),
        ("#EF4444", "Red"),
        ("#E11D48", "Rose"),
        ("#EC4899", "Pink"),
        ("#A855F7", "Purple"),
        ("#8B5CF6", "Violet"),
        ("#6366F1", "Indigo")
    };

    public static void RegisterProjects(IEnumerable<Project>? projects)
    {
        if (projects == null) return;
        foreach (var project in projects)
        {
            if (string.IsNullOrWhiteSpace(project.Name)) continue;
            if (!string.IsNullOrWhiteSpace(project.ColorHex) && IsValidHex(project.ColorHex))
            {
                _customColors[project.Name.Trim()] = NormalizeHex(project.ColorHex);
            }
            else
            {
                _customColors.TryRemove(project.Name.Trim(), out _);
            }
        }
    }

    public static void SetProjectColor(string projectName, string? colorHex)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return;

        var key = projectName.Trim();
        if (!string.IsNullOrWhiteSpace(colorHex) && IsValidHex(colorHex))
        {
            _customColors[key] = NormalizeHex(colorHex);
        }
        else
        {
            _customColors.TryRemove(key, out _);
        }
    }

    public static void RemoveProjectColor(string? projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return;
        _customColors.TryRemove(projectName.Trim(), out _);
    }

    public static string? GetCustomProjectColorHex(string? projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return null;
        return _customColors.TryGetValue(projectName.Trim(), out var hex) ? hex : null;
    }

    public static bool HasCustomColor(string? projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return false;
        return _customColors.ContainsKey(projectName.Trim());
    }

    public static void ClearCustomColors()
    {
        _customColors.Clear();
    }

    public static string GetProjectColorHex(string? projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return DefaultProjectColorHex;

        var trimmed = projectName.Trim();
        if (_customColors.TryGetValue(trimmed, out var customHex) && IsValidHex(customHex))
        {
            return customHex;
        }

        return DefaultProjectColorHex;
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

    public static bool IsValidHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return false;
        var clean = hex.Trim();
        if (!clean.StartsWith('#'))
        {
            clean = "#" + clean;
        }

        if (clean.Length != 7 && clean.Length != 9 && clean.Length != 4)
            return false;

        return uint.TryParse(clean.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    }

    public static string NormalizeHex(string hex)
    {
        var clean = hex.Trim();
        if (!clean.StartsWith('#'))
            clean = "#" + clean;

        if (clean.Length == 4) // #RGB -> #RRGGBB
        {
            clean = $"#{clean[1]}{clean[1]}{clean[2]}{clean[2]}{clean[3]}{clean[3]}";
        }

        return clean.ToUpperInvariant();
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
