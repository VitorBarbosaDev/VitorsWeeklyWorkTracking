using System;
using System.Windows.Media;

namespace VitorsWeeklyWorkTracking.Services;

public enum GoalStatusCategory
{
    GoalMet,
    InProgress,
    Unplanned,
    NotStarted,
    RestDay,
    GoalNotMet
}

public static class GoalColorHelper
{
    public static bool IsDarkTheme()
    {
        return ThemeManager.CurrentTheme != AppTheme.ModernLight && ThemeManager.CurrentTheme != AppTheme.CandyPop;
    }

    public static (SolidColorBrush Background, SolidColorBrush Border, SolidColorBrush Foreground) GetStatusBrushes(GoalStatusCategory category, bool? isDark = null)
    {
        bool dark = isDark ?? IsDarkTheme();

        return category switch
        {
            GoalStatusCategory.GoalMet => dark
                ? (CreateBrush("#064E3B"), CreateBrush("#10B981"), CreateBrush("#6EE7B7"))
                : (CreateBrush("#DCFCE7"), CreateBrush("#86EFAC"), CreateBrush("#15803D")),

            GoalStatusCategory.InProgress => dark
                ? (CreateBrush("#1E3A8A"), CreateBrush("#3B82F6"), CreateBrush("#93C5FD"))
                : (CreateBrush("#DBEAFE"), CreateBrush("#93C5FD"), CreateBrush("#1D4ED8")),

            GoalStatusCategory.Unplanned => dark
                ? (CreateBrush("#2E1065"), CreateBrush("#8B5CF6"), CreateBrush("#C4B5FD"))
                : (CreateBrush("#EDE9FE"), CreateBrush("#C4B5FD"), CreateBrush("#6D28D9")),

            GoalStatusCategory.NotStarted => dark
                ? (CreateBrush("#451A03"), CreateBrush("#F59E0B"), CreateBrush("#FCD34D"))
                : (CreateBrush("#FEF3C7"), CreateBrush("#FDE68A"), CreateBrush("#B45309")),

            GoalStatusCategory.GoalNotMet => dark
                ? (CreateBrush("#451A1A"), CreateBrush("#EF4444"), CreateBrush("#FCA5A5"))
                : (CreateBrush("#FEE2E2"), CreateBrush("#FCA5A5"), CreateBrush("#DC2626")),

            GoalStatusCategory.RestDay or _ => dark
                ? (CreateBrush("#1E293B"), CreateBrush("#475569"), CreateBrush("#94A3B8"))
                : (CreateBrush("#F1F5F9"), CreateBrush("#CBD5E1"), CreateBrush("#64748B"))
        };
    }

    public static (SolidColorBrush Background, SolidColorBrush Border, SolidColorBrush Foreground) GetStatusBrushesForDailyGoal(double plannedHours, double actualHours, DateTime? date = null, bool? isDark = null)
    {
        if (plannedHours <= 0 && actualHours <= 0)
            return GetStatusBrushes(GoalStatusCategory.RestDay, isDark);

        if (plannedHours <= 0 && actualHours > 0)
            return GetStatusBrushes(GoalStatusCategory.Unplanned, isDark);

        if (actualHours >= plannedHours)
            return GetStatusBrushes(GoalStatusCategory.GoalMet, isDark);

        if (date.HasValue && date.Value.Date < DateTime.Today)
            return GetStatusBrushes(GoalStatusCategory.GoalNotMet, isDark);

        if (actualHours > 0)
            return GetStatusBrushes(GoalStatusCategory.InProgress, isDark);

        return GetStatusBrushes(GoalStatusCategory.NotStarted, isDark);
    }

    public static SolidColorBrush GetVarianceBrush(double varianceHours, double plannedHours, double actualHours, bool? isDark = null)
    {
        bool dark = isDark ?? IsDarkTheme();

        if (plannedHours <= 0 && actualHours <= 0)
            return dark ? CreateBrush("#64748B") : CreateBrush("#94A3B8");

        if (varianceHours >= 0)
            return dark ? CreateBrush("#34D399") : CreateBrush("#16A34A");

        return dark ? CreateBrush("#FBBF24") : CreateBrush("#D97706");
    }

    public static SolidColorBrush GetProgressBarBrush(double progressPercentage, bool isGoalMet, bool? isDark = null)
    {
        bool dark = isDark ?? IsDarkTheme();

        if (isGoalMet || progressPercentage >= 100.0)
            return dark ? CreateBrush("#10B981") : CreateBrush("#16A34A");

        if (progressPercentage > 0)
            return dark ? CreateBrush("#3B82F6") : CreateBrush("#2563EB");

        return dark ? CreateBrush("#334155") : CreateBrush("#CBD5E1");
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
            var fallback = new SolidColorBrush(Colors.SlateGray);
            fallback.Freeze();
            return fallback;
        }
    }
}
