namespace VitorsWeeklyWorkTracking.Models;

public class Project
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public override string ToString() => Name;
}
