using System;
using System.Collections.Generic;
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
    public int SoundVolume { get; set; } = 75;
    public bool SoundOnStartEnabled { get; set; } = true;
    public bool SoundOnEndEnabled { get; set; } = true;
    public bool SoundOnGoalAchievedEnabled { get; set; } = true;
    public bool SoundOnDailyGoalAchievedEnabled { get; set; } = true;
    public bool SoundOnWeeklyGoalAchievedEnabled { get; set; } = true;
    public bool SoundOnBreakStartEnabled { get; set; } = true;
    public bool SoundOnBreakEndEnabled { get; set; } = true;
    public bool SoundOnCheerEnabled { get; set; } = true;
    public List<string> EnabledSoundVariations { get; set; } = new()
    {
        "start_ascending", "start_pop", "start_marimba", "start_digital",
        "end_completion", "end_accomplished", "end_sparkle", "end_calm",
        "goal_victory", "goal_levelup", "goal_bell", "goal_cosmic",
        "daily_triumph", "daily_sunburst", "daily_chime", "daily_quest",
        "weekly_grand", "weekly_champions", "weekly_fiesta", "weekly_celestial",
        "breakstart_zen", "breakstart_lofi", "breakstart_bowl", "breakstart_waterdrop",
        "breakend_wakeup", "breakend_ready", "breakend_morning", "breakend_focusbell",
        "cheer_cycling_bell", "cheer_cycling_bark", "cheer_cycling_whistle",
        "cheer_cafe_cup", "cheer_cafe_raindrop", "cheer_cafe_steam",
        "cheer_jazz_sax", "cheer_jazz_piano", "cheer_jazz_sparkle",
        "cheer_icecream_jingle", "cheer_icecream_pop", "cheer_icecream_bell",
        "cheer_metro_melody", "cheer_metro_lofi", "cheer_metro_doors",
        "cheer_cat_meow", "cheer_cat_purr", "cheer_cat_bounce",
        "cheer_rocket_laser", "cheer_rocket_beacon", "cheer_rocket_warp",
        "cheer_runner_whistle", "cheer_runner_horn", "cheer_runner_squeak",
        "cheer_pet_happy", "cheer_pet_levelup", "cheer_pet_heart"
    };
    public bool AutoAdvancePhases { get; set; } = false;
    public string SelectedSceneId { get; set; } = "cycling";
    public string ArtMode { get; set; } = "graphics"; // "graphics" or "ascii"
    public bool CompanionVisible { get; set; } = true;
    public bool CompanionExpanded { get; set; } = true;
    public string MiniWidgetCorner { get; set; } = "BottomRight"; // "BottomRight" or "BottomLeft"
    public string MiniWidgetTargetMonitor { get; set; } = "Primary"; // "Primary" or device name / monitor id
    public int MiniWidgetMonitorIndex { get; set; } = -1; // -1 for auto/primary, 0, 1, 2... for explicit monitor index
    public bool MiniWidgetEnabled { get; set; } = true;
    public int FocusXp { get; set; } = 0;
    public int FocusStreak { get; set; } = 0;
    public int TotalSessionsCompleted { get; set; } = 0;

    [JsonIgnore]
    public int FocusLevel => Math.Max(1, (FocusXp / 100) + 1);

    [JsonIgnore]
    public int XpInCurrentLevel => FocusXp % 100;

    public bool IsVariationEnabled(string variationId)
    {
        if (EnabledSoundVariations == null || EnabledSoundVariations.Count == 0)
            return true;
        return EnabledSoundVariations.Contains(variationId);
    }

    public void SetVariationEnabled(string variationId, bool enabled)
    {
        EnabledSoundVariations ??= new List<string>();
        if (enabled && !EnabledSoundVariations.Contains(variationId))
        {
            EnabledSoundVariations.Add(variationId);
        }
        else if (!enabled && EnabledSoundVariations.Contains(variationId))
        {
            EnabledSoundVariations.Remove(variationId);
        }
    }
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
