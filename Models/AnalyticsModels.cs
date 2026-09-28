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
}
