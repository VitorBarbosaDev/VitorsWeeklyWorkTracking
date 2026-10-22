using System;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking.Models;

public class PlannedWorkItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Date { get; set; } = DateTime.Today;
    public string ProjectName { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public double PlannedHours { get; set; } = 1.0;
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string FormattedHours => TimeFormatHelper.FormatHours(PlannedHours);

    public string FormattedDate => Date.ToString("yyyy-MM-dd");
    public string DayOfWeekName => Date.ToString("dddd");
    public string ShortDayDate => Date.ToString("ddd, dd MMM");

    public bool IsCompleted { get; set; } = false;
    public double ActualHours { get; set; } = 0.0;
    public string FormattedActualHours => TimeFormatHelper.FormatHours(ActualHours);
    public string RewardIcon { get; set; } = "⭐";
    public string FormattedProjectWithReward => ProjectName;

    public double CompletionPercentage => PlannedHours > 0 ? (ActualHours / PlannedHours) * 100.0 : (ActualHours > 0 ? 100.0 : 0.0);
    public string FormattedCompletionPercentage => $"{CompletionPercentage:F0}%";

    public string CompletionStatusText => IsCompleted ? $"{RewardIcon} Goal Met" : (ActualHours > 0 ? $"⏳ In Progress ({FormattedCompletionPercentage})" : "In Progress");

    public SolidColorBrush StatusBackgroundBrush => GoalColorHelper.GetStatusBrushes(IsCompleted ? GoalStatusCategory.GoalMet : (ActualHours > 0 ? GoalStatusCategory.InProgress : GoalStatusCategory.NotStarted)).Background;
    public SolidColorBrush StatusBorderBrush => GoalColorHelper.GetStatusBrushes(IsCompleted ? GoalStatusCategory.GoalMet : (ActualHours > 0 ? GoalStatusCategory.InProgress : GoalStatusCategory.NotStarted)).Border;
    public SolidColorBrush StatusForegroundBrush => GoalColorHelper.GetStatusBrushes(IsCompleted ? GoalStatusCategory.GoalMet : (ActualHours > 0 ? GoalStatusCategory.InProgress : GoalStatusCategory.NotStarted)).Foreground;
    public SolidColorBrush ProjectBrush => ProjectColorHelper.GetProjectBrush(ProjectName);
    public SolidColorBrush ProgressBarBrush => GoalColorHelper.GetProgressBarBrush(CompletionPercentage, IsCompleted);
}
