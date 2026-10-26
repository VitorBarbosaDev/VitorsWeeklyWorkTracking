using System;
using System.Text.Json.Serialization;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking.Models;

public class ProjectMilestone
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProjectName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime TargetDate { get; set; } = DateTime.Today;
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public int DaysRemaining => (TargetDate.Date - DateTime.Today).Days;

    [JsonIgnore]
    public string CountdownText
    {
        get
        {
            if (IsCompleted) return "Completed";
            int days = DaysRemaining;
            if (days < 0) return $"{Math.Abs(days)} day{(Math.Abs(days) == 1 ? "" : "s")} overdue";
            if (days == 0) return "Due today!";
            if (days == 1) return "1 day remaining (tomorrow!)";
            return $"{days} days remaining";
        }
    }

    [JsonIgnore]
    public string DailyReminderText
    {
        get
        {
            if (IsCompleted) return $"{Title} (Completed)";
            int days = DaysRemaining;
            if (days < 0) return $"{Math.Abs(days)} day{(Math.Abs(days) == 1 ? "" : "s")} overdue for {Title}";
            if (days == 0) return $"Due today till {Title}!";
            if (days == 1) return $"1 day remaining till {Title} (tomorrow)";
            return $"{days} days remaining till {Title}";
        }
    }

    [JsonIgnore]
    public string FormattedTargetDate => TargetDate.ToString("ddd, dd MMM yyyy");

    [JsonIgnore]
    public string ShortTargetDate => TargetDate.ToString("MMM d, yyyy");

    [JsonIgnore]
    public SolidColorBrush ProjectBrush => ProjectColorHelper.GetProjectBrush(ProjectName);

    [JsonIgnore]
    public string ProjectColorHex => ProjectColorHelper.GetProjectColorHex(ProjectName);

    [JsonIgnore]
    public string UrgencyLevel
    {
        get
        {
            if (IsCompleted) return "Completed";
            int days = DaysRemaining;
            if (days < 0) return "Overdue";
            if (days == 0) return "Today";
            if (days == 1) return "Tomorrow";
            if (days <= 7) return "Soon";
            return "Upcoming";
        }
    }

    [JsonIgnore]
    public SolidColorBrush UrgencyBadgeBrush
    {
        get
        {
            if (IsCompleted) return GetThemeBrush("Theme.Success", Color.FromRgb(16, 185, 129)); // Emerald / Green
            int days = DaysRemaining;
            if (days <= 1) return GetThemeBrush("Theme.Danger", Color.FromRgb(239, 68, 68)); // Red (Overdue, Today, and 1 day remaining)
            if (days <= 3) return GetThemeBrush("Theme.Warning", Color.FromRgb(249, 115, 22)); // Orange (2-3 days remaining)
            if (days <= 7) return new SolidColorBrush(Color.FromRgb(234, 179, 8)); // Yellow / Amber (4-7 days remaining)
            return GetThemeBrush("Theme.Primary", Color.FromRgb(59, 130, 246)); // Sky Blue / Primary (> 7 days)
        }
    }

    [JsonIgnore]
    public SolidColorBrush UrgencyBadgeBackground
    {
        get
        {
            var brush = UrgencyBadgeBrush;
            var c = brush.Color;
            byte alpha = (byte)(IsCompleted ? 35 : (DaysRemaining <= 1 ? 40 : (DaysRemaining <= 3 ? 35 : 30)));
            return new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
        }
    }

    private static SolidColorBrush GetThemeBrush(string key, Color fallbackColor)
    {
        try
        {
            if (System.Windows.Application.Current?.Resources != null)
            {
                if (System.Windows.Application.Current.Resources[key] is SolidColorBrush brush)
                {
                    return brush;
                }
            }
        }
        catch
        {
            // Fallback for unit tests or non-WPF execution contexts
        }
        return new SolidColorBrush(fallbackColor);
    }

    [JsonIgnore]
    public string UrgencyBadgeText
    {
        get
        {
            if (IsCompleted) return "✅ Completed";
            int days = DaysRemaining;
            if (days < 0) return $"🚨 {Math.Abs(days)}d Overdue";
            if (days == 0) return "🔥 Due Today";
            if (days == 1) return "⏰ Tomorrow";
            if (days <= 7) return $"⏳ {days}d left";
            return $"📅 {days}d left";
        }
    }

    public override string ToString() => $"{ProjectName} - {Title} ({FormattedTargetDate})";
}
