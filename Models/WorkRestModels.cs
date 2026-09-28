using System;
using System.Text.Json.Serialization;

namespace VitorsWeeklyWorkTracking.Models;

public enum IntervalPhase
{
    None,
    Focus,
    ShortBreak,
    LongBreak
}

public class WorkRestSettings
{
    public bool IntervalModeEnabled { get; set; } = false;
    public int FocusMinutes { get; set; } = 25;
    public int ShortBreakMinutes { get; set; } = 5;
    public int LongBreakMinutes { get; set; } = 15;
    public int CyclesBeforeLongBreak { get; set; } = 4;
    public bool SoundAlertEnabled { get; set; } = true;
    public bool AutoAdvancePhases { get; set; } = false;
    public string SelectedSceneId { get; set; } = "cycling";
    public bool CompanionVisible { get; set; } = true;
    public bool CompanionExpanded { get; set; } = true;
    public int FocusXp { get; set; } = 0;
    public int FocusStreak { get; set; } = 0;
    public int TotalSessionsCompleted { get; set; } = 0;

    [JsonIgnore]
    public int FocusLevel => Math.Max(1, (FocusXp / 100) + 1);

    [JsonIgnore]
    public int XpInCurrentLevel => FocusXp % 100;
}

public class IntervalPreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int FocusMinutes { get; set; }
    public int ShortBreakMinutes { get; set; }
    public int LongBreakMinutes { get; set; }
    public int CyclesBeforeLongBreak { get; set; }

    public override string ToString() => Name;
}
