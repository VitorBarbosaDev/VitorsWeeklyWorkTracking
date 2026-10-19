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
    public string RewardIcon { get; set; } = "⭐";
    public string FormattedProjectWithReward => ProjectName;
    public string CompletionStatusText => IsCompleted ? $"{RewardIcon} Goal Met" : "In Progress";
}
