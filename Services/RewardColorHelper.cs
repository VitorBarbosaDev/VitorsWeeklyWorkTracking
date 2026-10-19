using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class RewardColorOption
{
    public string Key { get; set; } = "ThemeDefault";
    public string DisplayName { get; set; } = "🌿 Match Theme (Default)";
    public string PreviewHex { get; set; } = "#10B981";

    public override string ToString() => DisplayName;
}

public static class RewardColorHelper
{
    public static readonly IReadOnlyList<RewardColorOption> AvailableColors = new List<RewardColorOption>
    {
        new() { Key = "ThemeDefault", DisplayName = "🌿 Match Theme (Default)", PreviewHex = "#10B981" },
        new() { Key = "Gold", DisplayName = "⭐ Gold / Amber", PreviewHex = "#F59E0B" },
        new() { Key = "Emerald", DisplayName = "🍃 Emerald Green", PreviewHex = "#10B981" },
        new() { Key = "Coral", DisplayName = "🔥 Warm Coral", PreviewHex = "#F97316" },
        new() { Key = "Violet", DisplayName = "🦕 Dino Violet", PreviewHex = "#8B5CF6" },
        new() { Key = "Ocean", DisplayName = "🌊 Ocean Blue", PreviewHex = "#0284C7" },
        new() { Key = "Rose", DisplayName = "🌸 Berry Rose", PreviewHex = "#F43F5E" }
    };

    public static (Brush Background, Brush Border, Brush Text) GetRewardBrushes(string? colorKey, FrameworkElement? context = null)
    {
        var key = (colorKey ?? "ThemeDefault").Trim();
        if (string.IsNullOrEmpty(key) || key.Equals("ThemeDefault", StringComparison.OrdinalIgnoreCase))
        {
            var bg = context?.TryFindResource("Theme.StatCard2Bg") as Brush ?? CreateBrush("#E6F4EA");
            var border = context?.TryFindResource("Theme.StatCard2Border") as Brush ?? CreateBrush("#10B981");
            var text = context?.TryFindResource("Theme.StatCard2Text") as Brush ?? CreateBrush("#047857");
            return (bg, border, text);
        }

        bool isDark = ThemeManager.CurrentTheme != AppTheme.ModernLight && ThemeManager.CurrentTheme != AppTheme.CandyPop;

        return key.ToLowerInvariant() switch
        {
            "gold" or "amber" or "yellow" => isDark
                ? (CreateBrush("#451A03"), CreateBrush("#F59E0B"), CreateBrush("#FCD34D"))
                : (CreateBrush("#FEF3C7"), CreateBrush("#F59E0B"), CreateBrush("#B45309")),

            "emerald" or "green" => isDark
                ? (CreateBrush("#064E3B"), CreateBrush("#10B981"), CreateBrush("#34D399"))
                : (CreateBrush("#DCFCE7"), CreateBrush("#10B981"), CreateBrush("#047857")),

            "coral" or "orange" or "sunset" => isDark
                ? (CreateBrush("#431407"), CreateBrush("#F97316"), CreateBrush("#FB923C"))
                : (CreateBrush("#FFEDD5"), CreateBrush("#F97316"), CreateBrush("#C2410C")),

            "violet" or "purple" or "dino" => isDark
                ? (CreateBrush("#2E1065"), CreateBrush("#8B5CF6"), CreateBrush("#C4B5FD"))
                : (CreateBrush("#EDE9FE"), CreateBrush("#8B5CF6"), CreateBrush("#6D28D9")),

            "ocean" or "blue" or "cyan" => isDark
                ? (CreateBrush("#082F49"), CreateBrush("#0284C7"), CreateBrush("#38BDF8"))
                : (CreateBrush("#E0F2FE"), CreateBrush("#0284C7"), CreateBrush("#0369A1")),

            "rose" or "red" or "berry" or "pink" => isDark
                ? (CreateBrush("#4C0519"), CreateBrush("#F43F5E"), CreateBrush("#FDA4AF"))
                : (CreateBrush("#FFE4E6"), CreateBrush("#F43F5E"), CreateBrush("#BE123C")),

            _ => (context?.TryFindResource("Theme.StatCard2Bg") as Brush ?? CreateBrush("#E6F4EA"),
                  context?.TryFindResource("Theme.StatCard2Border") as Brush ?? CreateBrush("#10B981"),
                  context?.TryFindResource("Theme.StatCard2Text") as Brush ?? CreateBrush("#047857"))
        };
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
            var fallback = new SolidColorBrush(Colors.ForestGreen);
            fallback.Freeze();
            return fallback;
        }
    }
}
