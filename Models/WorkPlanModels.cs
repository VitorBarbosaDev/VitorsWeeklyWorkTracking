using System;
using System.Collections.Generic;

namespace VitorsWeeklyWorkTracking.Models;

public class DailyGoalSummary
{
    public DateTime Date { get; set; }
    public string DayOfWeekName => Date.ToString("dddd");
    public string ShortDayName => Date.ToString("ddd");
    public string FormattedDate => Date.ToString("ddd, dd MMM yyyy");
    public double PlannedHours { get; set; }
    public double ActualHours { get; set; }
    public TimeSpan ActualDuration => TimeSpan.FromHours(ActualHours);
    public string FormattedPlannedHours => PlannedHours > 0 ? $"{PlannedHours:F1} hrs" : "—";
    public string FormattedActualDuration => $"{(int)ActualDuration.TotalHours}h {ActualDuration.Minutes:D2}m {ActualDuration.Seconds:D2}s";
    public string FormattedActualHours => $"{ActualHours:F2} hrs";
    
    public double VarianceHours => ActualHours - PlannedHours;
    public string FormattedVariance
    {
        get
        {
            if (PlannedHours <= 0 && ActualHours <= 0) return "—";
            if (VarianceHours >= 0)
                return $"+{VarianceHours:F2} hrs";
            return $"{VarianceHours:F2} hrs";
        }
    }

    public double CompletionPercentage
    {
        get
        {
            if (PlannedHours <= 0)
                return ActualHours > 0 ? 100.0 : 0.0;
            return (ActualHours / PlannedHours) * 100.0;
        }
    }

    public string FormattedCompletionPercentage => PlannedHours > 0 ? $"{CompletionPercentage:F0}%" : (ActualHours > 0 ? "100%" : "—");

    public bool IsGoalMet => PlannedHours > 0 && ActualHours >= PlannedHours;

    public string StatusText
    {
        get
        {
            if (PlannedHours <= 0 && ActualHours <= 0)
                return "💤 Rest Day";
            if (PlannedHours <= 0 && ActualHours > 0)
                return $"✨ +{ActualHours:F1}h Unplanned";
            if (ActualHours >= PlannedHours)
                return $"🎯 Goal Met ({CompletionPercentage:F0}%)";
            if (ActualHours > 0)
                return $"⏳ In Progress ({CompletionPercentage:F0}%)";
            return "⚠️ Not Started";
        }
    }

    public int EntryCount { get; set; }
    public string ProjectsSummary { get; set; } = string.Empty;
    public string ActivitiesSummary { get; set; } = string.Empty;
    public string TasksSummary { get; set; } = string.Empty;
    public List<PlannedWorkItem> PlannedItems { get; set; } = new();
}

public class WeeklyGoalSummary
{
    public DateTime WeekStartDate { get; set; } // Monday
    public DateTime WeekEndDate { get; set; } // Sunday
    public string FormattedWeekRange => $"{WeekStartDate:ddd, dd MMM} - {WeekEndDate:ddd, dd MMM yyyy}";
    public double TotalPlannedHours { get; set; }
    public double TotalActualHours { get; set; }
    public TimeSpan TotalActualDuration => TimeSpan.FromHours(TotalActualHours);
    public string FormattedPlannedHours => $"{TotalPlannedHours:F1} hrs";
    public string FormattedActualDuration => $"{(int)TotalActualDuration.TotalHours}h {TotalActualDuration.Minutes:D2}m {TotalActualDuration.Seconds:D2}s";
    public string FormattedActualHours => $"{TotalActualHours:F2} hrs";

    public double VarianceHours => TotalActualHours - TotalPlannedHours;
    public string FormattedVariance => VarianceHours >= 0 ? $"+{VarianceHours:F2} hrs" : $"{VarianceHours:F2} hrs";
    
    public double RemainingHours => Math.Max(0, TotalPlannedHours - TotalActualHours);
    public string FormattedRemainingHours => $"{RemainingHours:F1} hrs left";

    public double CompletionPercentage
    {
        get
        {
            if (TotalPlannedHours <= 0)
                return TotalActualHours > 0 ? 100.0 : 0.0;
            return (TotalActualHours / TotalPlannedHours) * 100.0;
        }
    }

    public string FormattedCompletionPercentage => $"{CompletionPercentage:F1}%";
    public bool IsGoalMet => TotalPlannedHours > 0 && TotalActualHours >= TotalPlannedHours;

    public string SummaryStatus
    {
        get
        {
            if (TotalPlannedHours <= 0)
                return TotalActualHours > 0 ? $"✨ Worked {TotalActualHours:F1} hrs (No target set)" : "No weekly goal scheduled yet";
            if (IsGoalMet)
                return $"🎉 Weekly Goal Achieved! ({TotalActualHours:F1}h / {TotalPlannedHours:F1}h)";
            return $"🎯 {TotalActualHours:F1}h / {TotalPlannedHours:F1}h ({CompletionPercentage:F0}%) • {RemainingHours:F1}h remaining";
        }
    }

    public List<DailyGoalSummary> DailySummaries { get; set; } = new();
    public List<ProjectGoalComparison> ProjectComparisons { get; set; } = new();
}

public class ProjectGoalComparison
{
    public string ProjectName { get; set; } = string.Empty;
    public double PlannedHours { get; set; }
    public double ActualHours { get; set; }
    public TimeSpan ActualDuration => TimeSpan.FromHours(ActualHours);
    public string FormattedPlanned => PlannedHours > 0 ? $"{PlannedHours:F1}h" : "0h";
    public string FormattedActual => $"{(int)ActualDuration.TotalHours}h {ActualDuration.Minutes:D2}m";
    public double CompletionPercentage
    {
        get
        {
            if (PlannedHours <= 0)
                return ActualHours > 0 ? 100.0 : 0.0;
            return (ActualHours / PlannedHours) * 100.0;
        }
    }
    public string FormattedPercentage => $"{CompletionPercentage:F1}%";
    public double VarianceHours => ActualHours - PlannedHours;
    public bool IsGoalMet => PlannedHours > 0 && ActualHours >= PlannedHours;
}
