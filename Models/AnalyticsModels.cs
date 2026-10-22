using System;
using System.Collections.Generic;
using System.Linq;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking.Models;

public class ProjectAnalyticsItem
{
    public string ProjectName { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public double TotalHours => TotalDuration.TotalHours;
    public string FormattedDuration => TimeFormatHelper.FormatHours(TotalHours);
    public double Percentage { get; set; }
    public string FormattedPercentage => $"{Percentage:F1}%";
    public int EntryCount { get; set; }
}

public class ActivityAnalyticsItem
{
    public string Activity { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public double TotalHours => TotalDuration.TotalHours;
    public string FormattedDuration => TimeFormatHelper.FormatHours(TotalHours);
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
    public string FormattedDuration => TimeFormatHelper.FormatHours(TotalHours);
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
    public string FormattedDuration => TimeFormatHelper.FormatHours(TotalHours);
    public int EntryCount { get; set; }
    public string ProjectsSummary { get; set; } = string.Empty;
    public string ActivitiesSummary { get; set; } = string.Empty;
    public string TasksSummary { get; set; } = string.Empty;

    // Calendar Planned Work & Goal tracking properties
    public double PlannedHours { get; set; }
    public string FormattedPlannedHours => TimeFormatHelper.FormatHoursWithUnit(PlannedHours, "—");
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
            if (PlannedHours <= 0 && TotalHours > 0) return $"✨ +{TimeFormatHelper.FormatHours(TotalHours)} Unplanned";
            if (TotalHours >= PlannedHours) return $"🎯 Goal Met ({CompletionPercentage:F0}%)";
            if (TotalHours > 0) return $"⏳ In Progress ({CompletionPercentage:F0}%)";
            return "⚠️ Not Started";
        }
    }
}

public enum ShareCategory
{
    Project,
    Activity,
    Task
}

public enum ShareAnalyticsNavigationAction
{
    GoToProjectBreakdown,
    GoToActivityBreakdown,
    GoToTaskBreakdown,
    FilterInWorkLog
}

public class ShareSubBreakdownItem
{
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public double TotalHours => Duration.TotalHours;
    public string FormattedDuration => TimeFormatHelper.FormatHours(TotalHours);
    public double Percentage { get; set; }
    public string FormattedPercentage => $"{Percentage:F1}%";
    public int EntryCount { get; set; }
}

public class ShareDailyDistributionItem
{
    public DateTime Date { get; set; }
    public string FormattedDate => Date.ToString("ddd, dd MMM yyyy");
    public string DayOfWeekName => Date.ToString("dddd");
    public TimeSpan Duration { get; set; }
    public double TotalHours => Duration.TotalHours;
    public string FormattedDuration => TimeFormatHelper.FormatHours(TotalHours);
    public int EntryCount { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class ShareSessionItem
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string FormattedDate => StartTime.ToString("yyyy-MM-dd");
    public string FormattedTimeRange => $"{StartTime:HH:mm} - {EndTime:HH:mm}";
    public string FormattedDuration => TimeFormatHelper.FormatHours(Duration.TotalHours);
    public string ProjectName { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class ShareAnalyticsSummary
{
    public ShareCategory Category { get; set; }
    public string ShareName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public TimeSpan TotalDuration { get; set; }
    public double TotalHours => TotalDuration.TotalHours;
    public string FormattedTotalDuration => TimeFormatHelper.FormatHours(TotalHours);
    public string FormattedTotalHours => $"{TotalHours:F2} hrs";

    public double SharePercentage { get; set; }
    public string FormattedSharePercentage => $"{SharePercentage:F1}%";

    public int SessionCount { get; set; }
    public int ActiveDaysCount { get; set; }
    public TimeSpan AverageSessionDuration { get; set; }
    public string FormattedAverageSession => TimeFormatHelper.FormatHours(AverageSessionDuration.TotalHours);

    public TimeSpan DailyAverage { get; set; }
    public string FormattedDailyAverage => TimeFormatHelper.FormatHours(DailyAverage.TotalHours);

    public List<ShareSubBreakdownItem> PrimaryBreakdown { get; set; } = new();
    public List<ShareSubBreakdownItem> SecondaryBreakdown { get; set; } = new();
    public List<ShareDailyDistributionItem> DailyDistribution { get; set; } = new();
    public List<ShareSessionItem> Sessions { get; set; } = new();

    public static ShareAnalyticsSummary Calculate(
        ShareCategory category,
        string shareName,
        IEnumerable<TimeEntry> periodEntries,
        DateTime startDate,
        DateTime endDate)
    {
        var entries = periodEntries?.ToList() ?? new List<TimeEntry>();
        long totalPeriodTicks = entries.Sum(e => e.Duration.Ticks);

        var matching = category switch
        {
            ShareCategory.Project => entries
                .Where(e => (string.IsNullOrWhiteSpace(e.ProjectName) ? "(Unassigned)" : e.ProjectName).Equals(shareName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.StartTime)
                .ToList(),

            ShareCategory.Activity => entries
                .Where(e => (string.IsNullOrWhiteSpace(e.Activity) ? "(Unassigned)" : e.Activity).Equals(shareName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.StartTime)
                .ToList(),

            ShareCategory.Task => entries
                .Where(e => (string.IsNullOrWhiteSpace(e.Description) ? "(No Description)" : e.Description.Trim()).Equals(shareName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.StartTime)
                .ToList(),

            _ => new List<TimeEntry>()
        };

        long matchingTicks = matching.Sum(e => e.Duration.Ticks);
        var matchingDuration = TimeSpan.FromTicks(matchingTicks);

        double sharePercentage = totalPeriodTicks > 0
            ? ((double)matchingTicks / totalPeriodTicks) * 100.0
            : (matchingTicks > 0 ? 100.0 : 0.0);

        var activeDays = matching
            .GroupBy(e => CalendarPlanStorage.Instance.GetEffectiveEntryDate(e))
            .OrderByDescending(g => g.Key)
            .ToList();

        var avgSession = matching.Count > 0 ? TimeSpan.FromTicks(matchingTicks / matching.Count) : TimeSpan.Zero;
        var dailyAvg = activeDays.Count > 0 ? TimeSpan.FromTicks(matchingTicks / activeDays.Count) : TimeSpan.Zero;

        var summary = new ShareAnalyticsSummary
        {
            Category = category,
            ShareName = shareName,
            StartDate = startDate,
            EndDate = endDate,
            TotalDuration = matchingDuration,
            SharePercentage = sharePercentage,
            SessionCount = matching.Count,
            ActiveDaysCount = activeDays.Count,
            AverageSessionDuration = avgSession,
            DailyAverage = dailyAvg
        };

        // Primary and Secondary Sub-Breakdowns
        if (category == ShareCategory.Project)
        {
            summary.PrimaryBreakdown = matching
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Activity) ? "(Unassigned)" : e.Activity)
                .Select(g =>
                {
                    long gTicks = g.Sum(x => x.Duration.Ticks);
                    double gPct = matchingTicks > 0 ? ((double)gTicks / matchingTicks) * 100.0 : 0.0;
                    return new ShareSubBreakdownItem
                    {
                        Name = g.Key,
                        Duration = TimeSpan.FromTicks(gTicks),
                        Percentage = gPct,
                        EntryCount = g.Count()
                    };
                })
                .OrderByDescending(b => b.Duration)
                .ToList();

            summary.SecondaryBreakdown = matching
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Description) ? "(No Description)" : e.Description.Trim())
                .Select(g =>
                {
                    long gTicks = g.Sum(x => x.Duration.Ticks);
                    double gPct = matchingTicks > 0 ? ((double)gTicks / matchingTicks) * 100.0 : 0.0;
                    var acts = string.Join(", ", g.Select(x => string.IsNullOrWhiteSpace(x.Activity) ? "(Unassigned)" : x.Activity).Distinct());
                    return new ShareSubBreakdownItem
                    {
                        Name = g.Key,
                        Subtitle = acts,
                        Duration = TimeSpan.FromTicks(gTicks),
                        Percentage = gPct,
                        EntryCount = g.Count()
                    };
                })
                .OrderByDescending(b => b.Duration)
                .ToList();
        }
        else if (category == ShareCategory.Activity)
        {
            summary.PrimaryBreakdown = matching
                .GroupBy(e => string.IsNullOrWhiteSpace(e.ProjectName) ? "(Unassigned)" : e.ProjectName)
                .Select(g =>
                {
                    long gTicks = g.Sum(x => x.Duration.Ticks);
                    double gPct = matchingTicks > 0 ? ((double)gTicks / matchingTicks) * 100.0 : 0.0;
                    return new ShareSubBreakdownItem
                    {
                        Name = g.Key,
                        Duration = TimeSpan.FromTicks(gTicks),
                        Percentage = gPct,
                        EntryCount = g.Count()
                    };
                })
                .OrderByDescending(b => b.Duration)
                .ToList();

            summary.SecondaryBreakdown = matching
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Description) ? "(No Description)" : e.Description.Trim())
                .Select(g =>
                {
                    long gTicks = g.Sum(x => x.Duration.Ticks);
                    double gPct = matchingTicks > 0 ? ((double)gTicks / matchingTicks) * 100.0 : 0.0;
                    var projs = string.Join(", ", g.Select(x => string.IsNullOrWhiteSpace(x.ProjectName) ? "(Unassigned)" : x.ProjectName).Distinct());
                    return new ShareSubBreakdownItem
                    {
                        Name = g.Key,
                        Subtitle = projs,
                        Duration = TimeSpan.FromTicks(gTicks),
                        Percentage = gPct,
                        EntryCount = g.Count()
                    };
                })
                .OrderByDescending(b => b.Duration)
                .ToList();
        }
        else if (category == ShareCategory.Task)
        {
            summary.PrimaryBreakdown = matching
                .GroupBy(e => (
                    Project: string.IsNullOrWhiteSpace(e.ProjectName) ? "(Unassigned)" : e.ProjectName,
                    Activity: string.IsNullOrWhiteSpace(e.Activity) ? "(Unassigned)" : e.Activity))
                .Select(g =>
                {
                    long gTicks = g.Sum(x => x.Duration.Ticks);
                    double gPct = matchingTicks > 0 ? ((double)gTicks / matchingTicks) * 100.0 : 0.0;
                    return new ShareSubBreakdownItem
                    {
                        Name = g.Key.Project,
                        Subtitle = g.Key.Activity,
                        Duration = TimeSpan.FromTicks(gTicks),
                        Percentage = gPct,
                        EntryCount = g.Count()
                    };
                })
                .OrderByDescending(b => b.Duration)
                .ToList();

            summary.SecondaryBreakdown = matching
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Note) ? "(No Notes)" : e.Note.Trim())
                .Select(g =>
                {
                    long gTicks = g.Sum(x => x.Duration.Ticks);
                    double gPct = matchingTicks > 0 ? ((double)gTicks / matchingTicks) * 100.0 : 0.0;
                    return new ShareSubBreakdownItem
                    {
                        Name = g.Key,
                        Duration = TimeSpan.FromTicks(gTicks),
                        Percentage = gPct,
                        EntryCount = g.Count()
                    };
                })
                .OrderByDescending(b => b.Duration)
                .ToList();
        }

        // Daily Distribution
        summary.DailyDistribution = activeDays.Select(dayGroup =>
        {
            long dayTicks = dayGroup.Sum(x => x.Duration.Ticks);
            var summaries = category switch
            {
                ShareCategory.Project => dayGroup
                    .GroupBy(x => string.IsNullOrWhiteSpace(x.Activity) ? "(Unassigned)" : x.Activity)
                    .Select(ag => $"{ag.Key} ({TimeFormatHelper.FormatHours(ag.Sum(p => p.Duration.TotalHours))})"),

                ShareCategory.Activity => dayGroup
                    .GroupBy(x => string.IsNullOrWhiteSpace(x.ProjectName) ? "(Unassigned)" : x.ProjectName)
                    .Select(pg => $"{pg.Key} ({TimeFormatHelper.FormatHours(pg.Sum(p => p.Duration.TotalHours))})"),

                _ => dayGroup
                    .Select(x => $"{x.ProjectName} • {x.Activity} ({TimeFormatHelper.FormatHours(x.Duration.TotalHours)})")
                    .Distinct()
            };

            return new ShareDailyDistributionItem
            {
                Date = dayGroup.Key,
                Duration = TimeSpan.FromTicks(dayTicks),
                EntryCount = dayGroup.Count(),
                Summary = string.Join(", ", summaries)
            };
        }).ToList();

        // Sessions List
        summary.Sessions = matching.Select(e => new ShareSessionItem
        {
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            Duration = e.Duration,
            ProjectName = string.IsNullOrWhiteSpace(e.ProjectName) ? "(Unassigned)" : e.ProjectName,
            Activity = string.IsNullOrWhiteSpace(e.Activity) ? "(Unassigned)" : e.Activity,
            Description = string.IsNullOrWhiteSpace(e.Description) ? "(No Description)" : e.Description,
            Notes = e.Note ?? string.Empty
        }).ToList();

        return summary;
    }
}
