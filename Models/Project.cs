using System.Text.Json.Serialization;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking.Models;

public class Project
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? ColorHex { get; set; }

    [JsonIgnore]
    public string DisplayColorHex => ProjectColorHelper.GetProjectColorHex(Name);

    [JsonIgnore]
    public SolidColorBrush Brush => ProjectColorHelper.GetProjectBrush(Name);

    [JsonIgnore]
    public bool HasCustomColor
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ColorHex) || !ProjectColorHelper.IsValidHex(ColorHex))
                return false;
            
            // Default color hex is not considered "custom" for repair purposes
            return !ColorHex.Equals(ProjectColorHelper.DefaultProjectColorHex, StringComparison.OrdinalIgnoreCase);
        }
    }

    public override string ToString() => Name;
}
