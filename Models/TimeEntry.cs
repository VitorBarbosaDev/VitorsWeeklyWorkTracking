namespace VitorsWeeklyWorkTracking.Models;

public class TimeEntry
{
    public string ProjectName { get; set; } = "";
    public string Activity { get; set; } = "";
    public string Description { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public TimeSpan Duration => EndTime - StartTime;
}