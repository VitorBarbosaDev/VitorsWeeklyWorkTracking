using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

/// <summary>
/// Helper for streak UI visuals, fiery badge coloring, and dynamic button styling.
/// </summary>
public static class StreakColorHelper
{
    public static bool IsDarkTheme()
    {
        return ThemeManager.CurrentTheme != AppTheme.ModernLight && ThemeManager.CurrentTheme != AppTheme.CandyPop;
    }

    /// <summary>
    /// Returns true if work has been completed and saved today, setting the streak flame on fire.
    /// Awarded when at least Bronze tier is achieved for the day.
    /// </summary>
    public static bool IsStreakOnFire(StreakSummary? summary)
    {
        if (summary == null || !summary.IsEnabled) return false;
        return summary.TodayTier != StreakTier.None || summary.TodayStatus == StreakDayStatus.Active;
    }

    /// <summary>
    /// Computes the fiery or neutral brushes for the streak button based on active tier and theme.
    /// </summary>
    public static (Brush Background, Brush Border, Brush Foreground, double BorderThickness) GetStreakButtonBrushes(
        StreakSummary? summary,
        bool? isDark = null,
        FrameworkElement? resourceContext = null)
    {
        bool dark = isDark ?? IsDarkTheme();

        if (summary == null || !summary.IsEnabled)
        {
            var disabledBg = resourceContext?.TryFindResource("Theme.Secondary") as Brush ?? (dark ? CreateBrush("#1E293B") : CreateBrush("#F1F5F9"));
            var disabledBorder = resourceContext?.TryFindResource("Theme.SecondaryBorder") as Brush ?? (dark ? CreateBrush("#334155") : CreateBrush("#CBD5E1"));
            var disabledFg = resourceContext?.TryFindResource("Theme.ForegroundMuted") as Brush ?? (dark ? CreateBrush("#64748B") : CreateBrush("#94A3B8"));
            return (disabledBg, disabledBorder, disabledFg, 1.0);
        }

        if (IsStreakOnFire(summary))
        {
            return summary.TodayTier switch
            {
                StreakTier.Gold => (
                    CreateGradientBrush(Color.FromRgb(245, 158, 11), Color.FromRgb(234, 88, 12), Color.FromRgb(220, 38, 38)),
                    CreateBrush("#FDE047"),
                    Brushes.White,
                    2.0
                ),
                StreakTier.Silver => (
                    CreateGradientBrush(Color.FromRgb(2, 132, 199), Color.FromRgb(234, 88, 12), Color.FromRgb(154, 52, 18)),
                    CreateBrush("#38BDF8"),
                    Brushes.White,
                    1.5
                ),
                StreakTier.Bronze or _ => (
                    CreateGradientBrush(Color.FromRgb(234, 88, 12), Color.FromRgb(217, 119, 6), Color.FromRgb(154, 52, 18)),
                    CreateBrush("#F59E0B"),
                    Brushes.White,
                    1.5
                )
            };
        }

        if (summary.TodayStatus == StreakDayStatus.Holiday || summary.TodayStatus == StreakDayStatus.SickDay || summary.TodayStatus == StreakDayStatus.Excluded)
        {
            var protectedBg = resourceContext?.TryFindResource("Theme.CardBackgroundAlt") as Brush ?? (dark ? CreateBrush("#1E293B") : CreateBrush("#F8FAFC"));
            var protectedBorder = resourceContext?.TryFindResource("Theme.Primary") as Brush ?? CreateBrush("#3B82F6");
            var protectedFg = resourceContext?.TryFindResource("Theme.Foreground") as Brush ?? (dark ? CreateBrush("#F8FAFC") : CreateBrush("#0F172A"));
            return (protectedBg, protectedBorder, protectedFg, 1.2);
        }

        // Pending / Unlit day (White/neutral fire icon)
        var pendingBg = resourceContext?.TryFindResource("Theme.Secondary") as Brush ?? (dark ? CreateBrush("#1E293B") : CreateBrush("#F1F5F9"));
        var pendingBorder = resourceContext?.TryFindResource("Theme.SecondaryBorder") as Brush ?? (dark ? CreateBrush("#334155") : CreateBrush("#CBD5E1"));
        var pendingFg = resourceContext?.TryFindResource("Theme.Foreground") as Brush ?? (dark ? CreateBrush("#F8FAFC") : CreateBrush("#0F172A"));
        return (pendingBg, pendingBorder, pendingFg, 1.0);
    }

    /// <summary>
    /// Returns a rich tooltip describing the streak state, active tier, and remaining sick days.
    /// </summary>
    public static string GetStreakButtonTooltip(StreakSummary? summary)
    {
        if (summary == null || !summary.IsEnabled)
        {
            return "Duo Streak System is currently disabled in Settings.\nClick to view or enable streak tracking.";
        }

        int currentStreak = summary.CurrentStreak;
        int longestStreak = summary.LongestStreak;
        var todayTier = summary.TodayTier;
        var todayStatus = summary.TodayStatus;
        int restDaysLeft = summary.AvailableRestDays;
        int goldStars = summary.GoldStarsBalance;

        if (IsStreakOnFire(summary))
        {
            string tierDescription = todayTier switch
            {
                StreakTier.Gold => "Gold Tier Legend 🥇 (Completed all scheduled goals)",
                StreakTier.Silver => "Silver Tier Achieved 🥈 (Completed planned project goal)",
                StreakTier.Bronze => "Bronze Tier Achieved 🥉 (Work logged & progress saved)",
                _ => "Active & Preserved 🔥"
            };

            return $"🔥 DAILY STREAK IS ON FIRE!\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"Streak: {currentStreak} Day{(currentStreak == 1 ? "" : "s")} ({tierDescription})\n" +
                   $"Best Streak: {longestStreak} Day{(longestStreak == 1 ? "" : "s")}\n" +
                   $"Today's Status: Active & Preserved 🔥\n" +
                   $"🛡️ Rest Days Available: {restDaysLeft}\n" +
                   $"⭐ Gold Stars Balance: {goldStars} ⭐ (30 ⭐ per rest day)\n\n" +
                   $"Click to open Streak Dashboard, buy rest days & manage holidays.";
        }

        if (todayStatus == StreakDayStatus.Holiday || todayStatus == StreakDayStatus.SickDay || todayStatus == StreakDayStatus.Excluded)
        {
            string protectionType = todayStatus switch
            {
                StreakDayStatus.Holiday => "🏖️ Scheduled Holiday",
                StreakDayStatus.SickDay => "💊 Protected Sick Day",
                StreakDayStatus.Excluded => "🏖️ Scheduled Rest Day",
                _ => "🛡️ Protected Day"
            };

            return $"Duo Streak: {currentStreak} Day{(currentStreak == 1 ? "" : "s")} (Safe & Protected Today)\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"Status: {protectionType} (Streak preserved)\n" +
                   $"Best Streak: {longestStreak} Day{(longestStreak == 1 ? "" : "s")}\n" +
                   $"🛡️ Rest Days Available: {restDaysLeft}\n" +
                   $"⭐ Gold Stars Balance: {goldStars} ⭐ (30 ⭐ per rest day)\n\n" +
                   $"Click to view streak tiers, buy rest days & manage holidays.";
        }

        if (currentStreak > 0)
        {
            return $"Duo Streak: {currentStreak} Day{(currentStreak == 1 ? "" : "s")} (Pending Today)\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"Best Streak: {longestStreak} Day{(longestStreak == 1 ? "" : "s")}\n" +
                   $"Log and save work today to set your streak on fire! 🔥\n" +
                   $"🛡️ Rest Days Available: {restDaysLeft}\n" +
                   $"⭐ Gold Stars Balance: {goldStars} ⭐ (30 ⭐ per rest day)\n\n" +
                   $"Click to view streak tiers, buy rest days & manage holidays.";
        }

        return $"Duo Streak: 0 Days\n" +
               $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
               $"Log and save work today to start your streak and set it on fire! 🔥\n" +
               $"🛡️ Rest Days Available: {restDaysLeft}\n" +
               $"⭐ Gold Stars Balance: {goldStars} ⭐ (30 ⭐ per rest day)\n\n" +
               $"Click to view streak dashboard, buy rest days & settings.";
    }

    /// <summary>
    /// Applies the complete visual styling (background, border, text, tooltip, opacity) to the streak button.
    /// </summary>
    public static void ApplyStreakButtonStyle(Button button, StreakSummary? summary, FrameworkElement? resourceContext = null)
    {
        if (button == null) return;

        summary ??= new StreakSummary { IsEnabled = false };
        var (bg, border, fg, thickness) = GetStreakButtonBrushes(summary, null, resourceContext ?? button);

        button.Content = summary.DisplayBadge;
        button.ToolTip = GetStreakButtonTooltip(summary);
        button.Background = bg;
        button.BorderBrush = border;
        button.BorderThickness = new Thickness(thickness);
        button.Foreground = fg;

        bool onFire = IsStreakOnFire(summary);
        button.FontWeight = onFire ? FontWeights.Bold : FontWeights.SemiBold;
        button.Opacity = summary.IsEnabled ? 1.0 : 0.75;
    }

    // Cached solid brushes for dark theme
    private static readonly SolidColorBrush GoldDarkBg = CreateBrush("#2A1B02");
    private static readonly SolidColorBrush GoldBorder = CreateBrush("#F59E0B");
    private static readonly SolidColorBrush GoldBadgeBg = CreateBrush("#D97706");
    private static readonly SolidColorBrush GoldFg = CreateBrush("#FDE047");
    private static readonly SolidColorBrush GoldMutedFg = CreateBrush("#FCD34D");

    private static readonly SolidColorBrush SilverDarkBg = CreateBrush("#0F172A");
    private static readonly SolidColorBrush SilverBorder = CreateBrush("#38BDF8");
    private static readonly SolidColorBrush SilverBadgeBg = CreateBrush("#0284C7");
    private static readonly SolidColorBrush SilverFg = CreateBrush("#E2E8F0");
    private static readonly SolidColorBrush SilverMutedFg = CreateBrush("#94A3B8");

    private static readonly SolidColorBrush BronzeDarkBg = CreateBrush("#241206");
    private static readonly SolidColorBrush BronzeBorder = CreateBrush("#EA580C");
    private static readonly SolidColorBrush BronzeBadgeBg = CreateBrush("#C2410C");
    private static readonly SolidColorBrush BronzeFg = CreateBrush("#FB923C");
    private static readonly SolidColorBrush BronzeMutedFg = CreateBrush("#FDBA74");

    private static readonly SolidColorBrush SickDarkBg = CreateBrush("#291018");
    private static readonly SolidColorBrush SickBorder = CreateBrush("#F43F5E");
    private static readonly SolidColorBrush SickBadgeBg = CreateBrush("#E11D48");
    private static readonly SolidColorBrush SickFg = CreateBrush("#FDA4AF");
    private static readonly SolidColorBrush SickMutedFg = CreateBrush("#FECDD3");

    private static readonly SolidColorBrush HolidayDarkBg = CreateBrush("#0B2019");
    private static readonly SolidColorBrush HolidayBorder = CreateBrush("#10B981");
    private static readonly SolidColorBrush HolidayBadgeBg = CreateBrush("#059669");
    private static readonly SolidColorBrush HolidayFg = CreateBrush("#6EE7B7");
    private static readonly SolidColorBrush HolidayMutedFg = CreateBrush("#A7F3D0");

    private static readonly SolidColorBrush ExcludedDarkBg = CreateBrush("#111827");
    private static readonly SolidColorBrush ExcludedBorder = CreateBrush("#3B82F6");
    private static readonly SolidColorBrush ExcludedBadgeBg = CreateBrush("#2563EB");
    private static readonly SolidColorBrush ExcludedFg = CreateBrush("#93C5FD");
    private static readonly SolidColorBrush ExcludedMutedFg = CreateBrush("#BFDBFE");

    private static readonly SolidColorBrush MissedDarkBg = CreateBrush("#231215");
    private static readonly SolidColorBrush MissedBorder = CreateBrush("#EF4444");
    private static readonly SolidColorBrush MissedBadgeBg = CreateBrush("#DC2626");
    private static readonly SolidColorBrush MissedFg = CreateBrush("#FCA5A5");
    private static readonly SolidColorBrush MissedMutedFg = CreateBrush("#FECACA");

    private static readonly SolidColorBrush PendingDarkBg = CreateBrush("#18181B");
    private static readonly SolidColorBrush PendingBorder = CreateBrush("#52525B");
    private static readonly SolidColorBrush PendingBadgeBg = CreateBrush("#3F3F46");
    private static readonly SolidColorBrush PendingFg = CreateBrush("#A1A1AA");
    private static readonly SolidColorBrush PendingMutedFg = CreateBrush("#71717A");

    // Cached solid brushes for light theme
    private static readonly SolidColorBrush GoldLightBg = CreateBrush("#FFFBEB");
    private static readonly SolidColorBrush SilverLightBg = CreateBrush("#F1F5F9");
    private static readonly SolidColorBrush BronzeLightBg = CreateBrush("#FFF7ED");
    private static readonly SolidColorBrush SickLightBg = CreateBrush("#FFF1F2");
    private static readonly SolidColorBrush HolidayLightBg = CreateBrush("#ECFDF5");
    private static readonly SolidColorBrush ExcludedLightBg = CreateBrush("#F0F9FF");
    private static readonly SolidColorBrush MissedLightBg = CreateBrush("#FEF2F2");
    private static readonly SolidColorBrush PendingLightBg = CreateBrush("#F4F4F5");

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
            var fallback = new SolidColorBrush(Colors.SlateGray);
            fallback.Freeze();
            return fallback;
        }
    }

    private static LinearGradientBrush CreateGradientBrush(Color startColor, Color midColor, Color endColor)
    {
        var gradient = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop(startColor, 0.0),
                new GradientStop(midColor, 0.5),
                new GradientStop(endColor, 1.0)
            }
        };
        gradient.Freeze();
        return gradient;
    }

    public static Brush GetEvaluationCardBackground(StreakDayEvaluation? eval)
    {
        bool dark = IsDarkTheme();
        if (eval == null) return dark ? PendingDarkBg : PendingLightBg;

        if (eval.Tier == StreakTier.Gold) return dark ? GoldDarkBg : GoldLightBg;
        if (eval.Tier == StreakTier.Silver) return dark ? SilverDarkBg : SilverLightBg;
        if (eval.Tier == StreakTier.Bronze) return dark ? BronzeDarkBg : BronzeLightBg;

        return eval.Status switch
        {
            StreakDayStatus.SickDay => dark ? SickDarkBg : SickLightBg,
            StreakDayStatus.Holiday => dark ? HolidayDarkBg : HolidayLightBg,
            StreakDayStatus.Excluded => dark ? ExcludedDarkBg : ExcludedLightBg,
            StreakDayStatus.Missed => dark ? MissedDarkBg : MissedLightBg,
            _ => dark ? PendingDarkBg : PendingLightBg
        };
    }

    public static Brush GetEvaluationBorderBrush(StreakDayEvaluation? eval)
    {
        if (eval == null) return PendingBorder;

        if (eval.Tier == StreakTier.Gold) return GoldBorder;
        if (eval.Tier == StreakTier.Silver) return SilverBorder;
        if (eval.Tier == StreakTier.Bronze) return BronzeBorder;

        return eval.Status switch
        {
            StreakDayStatus.SickDay => SickBorder,
            StreakDayStatus.Holiday => HolidayBorder,
            StreakDayStatus.Excluded => ExcludedBorder,
            StreakDayStatus.Missed => MissedBorder,
            _ => PendingBorder
        };
    }

    public static Thickness GetEvaluationBorderThickness(StreakDayEvaluation? eval)
    {
        if (eval == null) return new Thickness(1);
        if (eval.Tier != StreakTier.None) return new Thickness(1.5);
        if (eval.Status == StreakDayStatus.SickDay || eval.Status == StreakDayStatus.Holiday || eval.Status == StreakDayStatus.Excluded || eval.Status == StreakDayStatus.Missed)
            return new Thickness(1.2);
        return new Thickness(1);
    }

    public static Brush GetEvaluationBadgeBackground(StreakDayEvaluation? eval)
    {
        if (eval == null) return PendingBadgeBg;

        if (eval.Tier == StreakTier.Gold) return GoldBadgeBg;
        if (eval.Tier == StreakTier.Silver) return SilverBadgeBg;
        if (eval.Tier == StreakTier.Bronze) return BronzeBadgeBg;

        return eval.Status switch
        {
            StreakDayStatus.SickDay => SickBadgeBg,
            StreakDayStatus.Holiday => HolidayBadgeBg,
            StreakDayStatus.Excluded => ExcludedBadgeBg,
            StreakDayStatus.Missed => MissedBadgeBg,
            _ => PendingBadgeBg
        };
    }

    public static Brush GetEvaluationBadgeForeground(StreakDayEvaluation? eval)
    {
        if (eval == null || (eval.Tier == StreakTier.None && eval.Status == StreakDayStatus.Future))
            return PendingFg;
        return Brushes.White;
    }

    public static Brush GetEvaluationForeground(StreakDayEvaluation? eval)
    {
        if (eval == null) return PendingFg;

        if (eval.Tier == StreakTier.Gold) return GoldFg;
        if (eval.Tier == StreakTier.Silver) return SilverFg;
        if (eval.Tier == StreakTier.Bronze) return BronzeFg;

        return eval.Status switch
        {
            StreakDayStatus.SickDay => SickFg,
            StreakDayStatus.Holiday => HolidayFg,
            StreakDayStatus.Excluded => ExcludedFg,
            StreakDayStatus.Missed => MissedFg,
            _ => PendingFg
        };
    }

    public static Brush GetEvaluationMutedForeground(StreakDayEvaluation? eval)
    {
        if (eval == null) return PendingMutedFg;

        if (eval.Tier == StreakTier.Gold) return GoldMutedFg;
        if (eval.Tier == StreakTier.Silver) return SilverMutedFg;
        if (eval.Tier == StreakTier.Bronze) return BronzeMutedFg;

        return eval.Status switch
        {
            StreakDayStatus.SickDay => SickMutedFg,
            StreakDayStatus.Holiday => HolidayMutedFg,
            StreakDayStatus.Excluded => ExcludedMutedFg,
            StreakDayStatus.Missed => MissedMutedFg,
            _ => PendingMutedFg
        };
    }

    public static string GetEvaluationBorderHex(StreakTier tier, StreakDayStatus status)
    {
        if (tier == StreakTier.Gold) return "#F59E0B";
        if (tier == StreakTier.Silver) return "#38BDF8";
        if (tier == StreakTier.Bronze) return "#EA580C";

        return status switch
        {
            StreakDayStatus.SickDay => "#F43F5E",
            StreakDayStatus.Holiday => "#10B981",
            StreakDayStatus.Excluded => "#3B82F6",
            StreakDayStatus.Missed => "#EF4444",
            _ => "#52525B"
        };
    }

    public static string GetEvaluationBgHex(StreakTier tier, StreakDayStatus status)
    {
        if (tier == StreakTier.Gold) return "#2A1B02";
        if (tier == StreakTier.Silver) return "#0F172A";
        if (tier == StreakTier.Bronze) return "#241206";

        return status switch
        {
            StreakDayStatus.SickDay => "#291018",
            StreakDayStatus.Holiday => "#0B2019",
            StreakDayStatus.Excluded => "#111827",
            StreakDayStatus.Missed => "#231215",
            _ => "#18181B"
        };
    }
}
