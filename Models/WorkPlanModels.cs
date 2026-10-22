using System;
using System.Collections.Generic;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Services;

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
    public string FormattedPlannedHours => TimeFormatHelper.FormatHoursWithUnit(PlannedHours, "—");
    public string FormattedActualDuration => $"{(int)ActualDuration.TotalHours}h {ActualDuration.Minutes:D2}m {ActualDuration.Seconds:D2}s";
    public string FormattedActualHours => TimeFormatHelper.FormatHoursWithUnit(ActualHours, "0 mins");
    
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
                return $"✨ +{TimeFormatHelper.FormatHours(ActualHours)} Unplanned";
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

    public string ShortDateFormatted => Date.ToString("dd MMM");
    public string DayStatusEmoji => IsGoalMet ? "✅" : (PlannedHours <= 0 && ActualHours <= 0 ? "💤" : (ActualHours > 0 ? "⏳" : "—"));

    public bool IsWeekend => Date.DayOfWeek == DayOfWeek.Saturday || Date.DayOfWeek == DayOfWeek.Sunday;
    public SolidColorBrush StatusBackgroundBrush => GoalColorHelper.GetStatusBrushesForDailyGoal(PlannedHours, ActualHours).Background;
    public SolidColorBrush StatusBorderBrush => GoalColorHelper.GetStatusBrushesForDailyGoal(PlannedHours, ActualHours).Border;
    public SolidColorBrush StatusForegroundBrush => GoalColorHelper.GetStatusBrushesForDailyGoal(PlannedHours, ActualHours).Foreground;
    public SolidColorBrush VarianceForegroundBrush => GoalColorHelper.GetVarianceBrush(VarianceHours, PlannedHours, ActualHours);
    public SolidColorBrush ProgressBarBrush => GoalColorHelper.GetProgressBarBrush(CompletionPercentage, IsGoalMet);
}

public class WeeklyGoalSummary
{
    public DateTime WeekStartDate { get; set; } // Monday
    public DateTime WeekEndDate { get; set; } // Sunday
    public string FormattedWeekRange => $"{WeekStartDate:ddd, dd MMM} - {WeekEndDate:ddd, dd MMM yyyy}";
    public double TotalPlannedHours { get; set; }
    public double TotalActualHours { get; set; }
    public TimeSpan TotalActualDuration => TimeSpan.FromHours(TotalActualHours);
    public string FormattedPlannedHours => TimeFormatHelper.FormatHoursWithUnit(TotalPlannedHours, "—");
    public string FormattedActualDuration => $"{(int)TotalActualDuration.TotalHours}h {TotalActualDuration.Minutes:D2}m {TotalActualDuration.Seconds:D2}s";
    public string FormattedActualHours => TimeFormatHelper.FormatHoursWithUnit(TotalActualHours, "0 mins");

    public double VarianceHours => TotalActualHours - TotalPlannedHours;
    public string FormattedVariance => VarianceHours >= 0 ? $"+{VarianceHours:F2} hrs" : $"{VarianceHours:F2} hrs";
    
    public double RemainingHours => Math.Max(0, TotalPlannedHours - TotalActualHours);
    public string FormattedRemainingHours => TimeFormatHelper.FormatRemainingHours(RemainingHours);

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
                return TotalActualHours > 0 ? $"✨ Worked {TimeFormatHelper.FormatHoursWithUnit(TotalActualHours)} (No target set)" : "No weekly goal scheduled yet";
            if (IsGoalMet)
                return $"🎉 Weekly Goal Achieved! ({TimeFormatHelper.FormatHours(TotalActualHours)} / {TimeFormatHelper.FormatHours(TotalPlannedHours)})";
            return $"🎯 {TimeFormatHelper.FormatHours(TotalActualHours)} / {TimeFormatHelper.FormatHours(TotalPlannedHours)} ({CompletionPercentage:F0}%) • {TimeFormatHelper.FormatRemainingHours(RemainingHours)}";
        }
    }

    public List<DailyGoalSummary> DailySummaries { get; set; } = new();
    public List<ProjectGoalComparison> ProjectComparisons { get; set; } = new();

    public SolidColorBrush ProgressBarBrush => GoalColorHelper.GetProgressBarBrush(CompletionPercentage, IsGoalMet);
    public SolidColorBrush VarianceForegroundBrush => GoalColorHelper.GetVarianceBrush(VarianceHours, TotalPlannedHours, TotalActualHours);
}

public class ProjectGoalComparison
{
    public string ProjectName { get; set; } = string.Empty;
    public double PlannedHours { get; set; }
    public double ActualHours { get; set; }
    public string RewardIcon { get; set; } = "⭐";
    public TimeSpan ActualDuration => TimeSpan.FromHours(ActualHours);
    public string FormattedPlanned => TimeFormatHelper.FormatHours(PlannedHours);
    public string FormattedActual => TimeFormatHelper.FormatHours(ActualHours);
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
    public string FormattedProjectWithReward => IsGoalMet ? $"{RewardIcon} {ProjectName}" : ProjectName;
    public string FormattedVariance => (PlannedHours <= 0 && ActualHours <= 0) ? "—" : (VarianceHours >= 0 ? $"+{VarianceHours:F2} hrs" : $"{VarianceHours:F2} hrs");
    public string StatusText => IsGoalMet ? $"{RewardIcon} Target Reached ({FormattedPercentage})" : (ActualHours > 0 ? $"⏳ In Progress ({FormattedPercentage})" : "⚪ Not Started");

    public SolidColorBrush ProgressBarBrush => GoalColorHelper.GetProgressBarBrush(CompletionPercentage, IsGoalMet);
    public SolidColorBrush VarianceForegroundBrush => GoalColorHelper.GetVarianceBrush(VarianceHours, PlannedHours, ActualHours);
    public SolidColorBrush StatusBackgroundBrush => GoalColorHelper.GetStatusBrushes(IsGoalMet ? GoalStatusCategory.GoalMet : (ActualHours > 0 ? GoalStatusCategory.InProgress : GoalStatusCategory.NotStarted)).Background;
    public SolidColorBrush StatusBorderBrush => GoalColorHelper.GetStatusBrushes(IsGoalMet ? GoalStatusCategory.GoalMet : (ActualHours > 0 ? GoalStatusCategory.InProgress : GoalStatusCategory.NotStarted)).Border;
    public SolidColorBrush StatusForegroundBrush => GoalColorHelper.GetStatusBrushes(IsGoalMet ? GoalStatusCategory.GoalMet : (ActualHours > 0 ? GoalStatusCategory.InProgress : GoalStatusCategory.NotStarted)).Foreground;
    public SolidColorBrush ProjectBrush => ProjectColorHelper.GetProjectBrush(ProjectName);
}

public class PlannedTaskItemViewModel
{
    public PlannedWorkItem Plan { get; set; } = new();
    public string ProjectName => Plan.ProjectName;
    public string Activity => Plan.Activity;
    public string Note => Plan.Note;
    public double PlannedHours => Plan.PlannedHours;
    public double ActualHours { get; set; }
    public string RewardIcon { get; set; } = "⭐";
    public double CompletionPercentage => PlannedHours > 0 ? (ActualHours / PlannedHours) * 100.0 : (ActualHours > 0 ? 100.0 : 0.0);
    public string FormattedPercentage => $"{CompletionPercentage:F0}%";
    public bool IsCompleted => PlannedHours > 0 && ActualHours >= PlannedHours;
    public string FormattedActual => TimeFormatHelper.FormatHours(ActualHours);
    public string FormattedPlanned => Plan.FormattedHours;
    public string StatusText => IsCompleted 
        ? $"{RewardIcon} Completed ({FormattedPercentage})" 
        : (ActualHours > 0 ? $"⏳ {FormattedActual} / {FormattedPlanned} ({FormattedPercentage})" : $"⚪ {FormattedPlanned}");

    public SolidColorBrush StatusBackgroundBrush => GoalColorHelper.GetStatusBrushes(IsCompleted ? GoalStatusCategory.GoalMet : (ActualHours > 0 ? GoalStatusCategory.InProgress : GoalStatusCategory.NotStarted)).Background;
    public SolidColorBrush StatusBorderBrush => GoalColorHelper.GetStatusBrushes(IsCompleted ? GoalStatusCategory.GoalMet : (ActualHours > 0 ? GoalStatusCategory.InProgress : GoalStatusCategory.NotStarted)).Border;
    public SolidColorBrush StatusForegroundBrush => GoalColorHelper.GetStatusBrushes(IsCompleted ? GoalStatusCategory.GoalMet : (ActualHours > 0 ? GoalStatusCategory.InProgress : GoalStatusCategory.NotStarted)).Foreground;
    public SolidColorBrush ProjectBrush => ProjectColorHelper.GetProjectBrush(ProjectName);
    public SolidColorBrush ProgressBarBrush => GoalColorHelper.GetProgressBarBrush(CompletionPercentage, IsCompleted);
}

public class ProjectGoalSummary
{
    public string ProjectName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsWeekly { get; set; } = false;
    public double PlannedHours { get; set; }
    public double ActualHours { get; set; }
    public TimeSpan ActualDuration => TimeSpan.FromHours(ActualHours);
    public string FormattedPlannedHours => TimeFormatHelper.FormatHoursWithUnit(PlannedHours, "—");
    public string FormattedActualDuration => $"{(int)ActualDuration.TotalHours}h {ActualDuration.Minutes:D2}m {ActualDuration.Seconds:D2}s";
    public string FormattedActualHours => TimeFormatHelper.FormatHoursWithUnit(ActualHours, "0 mins");

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
    public List<PlannedWorkItem> PlannedItems { get; set; } = new();
    public string ActivitiesSummary { get; set; } = string.Empty;
    public string TasksSummary { get; set; } = string.Empty;
    public string FormattedDate => IsWeekly ? $"{Date:ddd, dd MMM} (Weekly Goal)" : Date.ToString("dddd, dd MMMM yyyy");

    public SolidColorBrush ProgressBarBrush => GoalColorHelper.GetProgressBarBrush(CompletionPercentage, IsGoalMet);
    public SolidColorBrush VarianceForegroundBrush => GoalColorHelper.GetVarianceBrush(VarianceHours, PlannedHours, ActualHours);
    public SolidColorBrush ProjectBrush => ProjectColorHelper.GetProjectBrush(ProjectName);
}
