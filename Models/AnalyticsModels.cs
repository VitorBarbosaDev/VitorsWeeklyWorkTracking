namespace VitorsWeeklyWorkTracking.Models;

public class ProjectAnalyticsItem
{
    public string ProjectName { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public double TotalHours => TotalDuration.TotalHours;
    public string FormattedDuration => $"{(int)TotalDuration.TotalHours}h {TotalDuration.Minutes:D2}m {TotalDuration.Seconds:D2}s";
    public double Percentage { get; set; }
    public string FormattedPercentage => $"{Percentage:F1}%";
    public int EntryCount { get; set; }
}

public class ActivityAnalyticsItem
{
    public string Activity { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public double TotalHours => TotalDuration.TotalHours;
    public string FormattedDuration => $"{(int)TotalDuration.TotalHours}h {TotalDuration.Minutes:D2}m {TotalDuration.Seconds:D2}s";
    public double Percentage { get; set; }
    public string FormattedPercentage => $"{Percentage:F1}%";
    public int EntryCount { get; set; }
}

public class TaskAnalyticsItem
{
    public string Description { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public double TotalHours => TotalDuration.TotalHours;
    public string FormattedDuration => $"{(int)TotalDuration.TotalHours}h {TotalDuration.Minutes:D2}m {TotalDuration.Seconds:D2}s";
    public double Percentage { get; set; }
    public string FormattedPercentage => $"{Percentage:F1}%";
    public int EntryCount { get; set; }
}

public class DailyAnalyticsItem
{
    public DateTime Date { get; set; }
    public string DayOfWeekName => Date.ToString("dddd");
    public string FormattedDate => Date.ToString("ddd, dd MMM yyyy");
    public TimeSpan TotalDuration { get; set; }
    public double TotalHours => TotalDuration.TotalHours;
    public string FormattedDuration => $"{(int)TotalDuration.TotalHours}h {TotalDuration.Minutes:D2}m {TotalDuration.Seconds:D2}s";
    public int EntryCount { get; set; }
    public string ProjectsSummary { get; set; } = string.Empty;
    public string ActivitiesSummary { get; set; } = string.Empty;
    public string TasksSummary { get; set; } = string.Empty;

    // Calendar Planned Work & Goal tracking properties
    public double PlannedHours { get; set; }
    public string FormattedPlannedHours => PlannedHours > 0 ? $"{PlannedHours:F1} hrs" : "—";
    public double VarianceHours => TotalHours - PlannedHours;
    public string FormattedVariance
    {
        get
        {
            if (PlannedHours <= 0 && TotalHours <= 0) return "—";
            if (VarianceHours >= 0) return $"+{VarianceHours:F2} hrs";
            return $"{VarianceHours:F2} hrs";
        }
    }
    public double CompletionPercentage
    {
        get
        {
            if (PlannedHours <= 0) return TotalHours > 0 ? 100.0 : 0.0;
            return (TotalHours / PlannedHours) * 100.0;
        }
    }
    public string FormattedCompletionPercentage => PlannedHours > 0 ? $"{CompletionPercentage:F0}%" : (TotalHours > 0 ? "100%" : "—");
    public bool IsGoalMet => PlannedHours > 0 && TotalHours >= PlannedHours;
    public string GoalStatusText
    {
        get
        {
            if (PlannedHours <= 0 && TotalHours <= 0) return "💤 Rest Day";
            if (PlannedHours <= 0 && TotalHours > 0) return $"✨ +{TotalHours:F1}h Unplanned";
            if (TotalHours >= PlannedHours) return $"🎯 Goal Met ({CompletionPercentage:F0}%)";
            if (TotalHours > 0) return $"⏳ In Progress ({CompletionPercentage:F0}%)";
            return "⚠️ Not Started";
        }
    }
}
