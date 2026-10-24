using System;
using System.Text.Json.Serialization;
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

    [JsonIgnore]
    public string FormattedHours => TimeFormatHelper.FormatHours(PlannedHours);

    [JsonIgnore]
    public string FormattedDate => Date.ToString("yyyy-MM-dd");
    [JsonIgnore]
    public string DayOfWeekName => Date.ToString("dddd");
    [JsonIgnore]
    public string ShortDayDate => Date.ToString("ddd, dd MMM");

    public bool IsCompleted { get; set; } = false;
    public double ActualHours { get; set; } = 0.0;
    [JsonIgnore]
    public string FormattedActualHours => TimeFormatHelper.FormatHours(ActualHours);
    public string RewardIcon { get; set; } = "⭐";
    [JsonIgnore]
    public string FormattedProjectWithReward => ProjectName;

    [JsonIgnore]
    public double CompletionPercentage => PlannedHours > 0 ? (ActualHours / PlannedHours) * 100.0 : (ActualHours > 0 ? 100.0 : 0.0);
    [JsonIgnore]
    public string FormattedCompletionPercentage => $"{CompletionPercentage:F0}%";

    [JsonIgnore]
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

    [JsonIgnore]
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

    [JsonIgnore]
    public SolidColorBrush StatusBackgroundBrush => GoalColorHelper.GetStatusBrushes(StatusCategory).Background;
    [JsonIgnore]
    public SolidColorBrush StatusBorderBrush => GoalColorHelper.GetStatusBrushes(StatusCategory).Border;
    [JsonIgnore]
    public SolidColorBrush StatusForegroundBrush => GoalColorHelper.GetStatusBrushes(StatusCategory).Foreground;
    [JsonIgnore]
    public SolidColorBrush ProjectBrush => ProjectColorHelper.GetProjectBrush(ProjectName);
    [JsonIgnore]
    public SolidColorBrush ActivityBrush => ActivityColorHelper.GetActivityBrush(Activity);
    [JsonIgnore]
    public SolidColorBrush ActivityBadgeBackground => ActivityColorHelper.GetActivityBadgeBrushes(Activity).Background;
    [JsonIgnore]
    public SolidColorBrush ActivityBadgeBorder => ActivityColorHelper.GetActivityBadgeBrushes(Activity).Border;
    [JsonIgnore]
    public SolidColorBrush ActivityBadgeForeground => ActivityColorHelper.GetActivityBadgeBrushes(Activity).Foreground;
    [JsonIgnore]
    public SolidColorBrush ProgressBarBrush => GoalColorHelper.GetProgressBarBrush(CompletionPercentage, IsCompleted || (PlannedHours > 0 && ActualHours >= PlannedHours));
}
