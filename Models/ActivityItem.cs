using System.Text.Json.Serialization;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking.Models;

public class ActivityItem
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? ColorHex { get; set; }

    [JsonIgnore]
    public string DisplayColorHex => ActivityColorHelper.GetActivityColorHex(Name);

    [JsonIgnore]
    public SolidColorBrush Brush => ActivityColorHelper.GetActivityBrush(Name);

    [JsonIgnore]
    public bool HasCustomColor
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ColorHex) || !ActivityColorHelper.IsValidHex(ColorHex))
                return false;
            
            // Default color hex is not considered "custom" for repair purposes
            return !ColorHex.Equals(ActivityColorHelper.DefaultActivityColorHex, StringComparison.OrdinalIgnoreCase);
        }
    }

    public override string ToString() => Name;
}
