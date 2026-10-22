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

    public GoalStatusCategory StatusCategory
    {
        get
        {
            if (IsCompleted || (PlannedHours > 0 && ActualHours >= PlannedHours))
                return GoalStatusCategory.GoalMet;

            if (Date.Date < DateTime.Today)
                return GoalStatusCategory.GoalNotMet;

            if (ActualHours > 0)
                return GoalStatusCategory.InProgress;

            return GoalStatusCategory.NotStarted;
        }
    }

    public string CompletionStatusText
    {
        get
        {
            if (IsCompleted || (PlannedHours > 0 && ActualHours >= PlannedHours))
                return $"{RewardIcon} Goal Met";

            if (Date.Date < DateTime.Today)
            {
                if (ActualHours > 0)
                    return $"❌ Goal Not Met ({FormattedCompletionPercentage})";
                return "❌ Goal Not Met";
            }

            if (ActualHours > 0)
                return $"⏳ In Progress ({FormattedCompletionPercentage})";

            return "⚪ Not Started Yet";
        }
    }

    public SolidColorBrush StatusBackgroundBrush => GoalColorHelper.GetStatusBrushes(StatusCategory).Background;
    public SolidColorBrush StatusBorderBrush => GoalColorHelper.GetStatusBrushes(StatusCategory).Border;
    public SolidColorBrush StatusForegroundBrush => GoalColorHelper.GetStatusBrushes(StatusCategory).Foreground;
    public SolidColorBrush ProjectBrush => ProjectColorHelper.GetProjectBrush(ProjectName);
    public SolidColorBrush ActivityBrush => ActivityColorHelper.GetActivityBrush(Activity);
    public SolidColorBrush ActivityBadgeBackground => ActivityColorHelper.GetActivityBadgeBrushes(Activity).Background;
    public SolidColorBrush ActivityBadgeBorder => ActivityColorHelper.GetActivityBadgeBrushes(Activity).Border;
    public SolidColorBrush ActivityBadgeForeground => ActivityColorHelper.GetActivityBadgeBrushes(Activity).Foreground;
    public SolidColorBrush ProgressBarBrush => GoalColorHelper.GetProgressBarBrush(CompletionPercentage, IsCompleted || (PlannedHours > 0 && ActualHours >= PlannedHours));
}
