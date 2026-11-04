using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class FocusCompanionStorage
{
    private static string SettingsFilePath => StoragePathHelper.GetFilePath("focus-settings.json", "focus_companion_settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static WorkRestSettings Load()
    {
        WorkRestSettings? settings = null;
        bool loadedFromPrimary = false;

        try
        {
            var primaryPath = SettingsFilePath;
            if (File.Exists(primaryPath))
            {
                var json = File.ReadAllText(primaryPath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    settings = DeserializeSettings(json);
                    loadedFromPrimary = true;
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "FocusCompanionStorage.Load");
        }

        settings ??= new WorkRestSettings();

        // Scan candidate legacy files to restore user settings across updates or migrations
        try
        {
            bool hasMerged = MergeFromCandidateLegacySettings(settings);
            if (!loadedFromPrimary || hasMerged)
            {
                // Ensure recovered settings are saved to primary location
                Save(settings);
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "FocusCompanionStorage.MergeLegacy");
        }

        EnsureSoundVariations(settings);
        return settings;
    }

    public static WorkRestSettings DeserializeSettings(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new WorkRestSettings();
        }

        WorkRestSettings settings;
        try
        {
            settings = JsonSerializer.Deserialize<WorkRestSettings>(json, JsonOptions) ?? new WorkRestSettings();
        }
        catch
        {
            settings = new WorkRestSettings();
        }

        // Parse JsonDocument for legacy property names and aliases
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                // 1. UserName aliases
                if (string.IsNullOrWhiteSpace(settings.UserName))
                {
                    string[] nameAliases = { "userName", "user_name", "username", "name", "user", "user_display_name", "display_name" };
                    foreach (var alias in nameAliases)
                    {
                        if (TryGetStringProperty(root, alias, out var val) && !string.IsNullOrWhiteSpace(val))
                        {
                            settings.UserName = val.Trim();
                            break;
                        }
                    }
                }

                // 2. WelcomePopupEnabled aliases
                string[] welcomeAliases =
                {
                    "welcomePopupEnabled", "welcome_popup_enabled", "showWelcomeBackPopup", "show_welcome_back_popup",
                    "showWelcomeBackDialog", "show_welcome_back_dialog", "showWelcomeBack", "show_welcome_back",
                    "showWelcomePopup", "show_welcome_popup", "welcomeBackPopup", "welcome_back_popup",
                    "enableWelcomeBackPopup", "enable_welcome_back_popup", "enableWelcomeBackDialog", "enable_welcome_back_dialog",
                    "welcome_popup", "welcomePopup"
                };
                foreach (var alias in welcomeAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.WelcomePopupEnabled = bVal;
                        break;
                    }
                }

                // 3. CompletionRewardIcon aliases
                string[] iconAliases =
                {
                    "completionRewardIcon", "completion_reward_icon", "rewardIcon", "reward_icon",
                    "rewardSymbol", "reward_symbol", "goalRewardIcon", "goal_reward_icon",
                    "completionIcon", "completion_icon", "reward_badge", "rewardBadge"
                };
                foreach (var alias in iconAliases)
                {
                    if (TryGetStringProperty(root, alias, out var val) && !string.IsNullOrWhiteSpace(val))
                    {
                        settings.CompletionRewardIcon = val.Trim();
                        break;
                    }
                }

                // 4. AutoAdvancePhases aliases
                string[] autoAdvanceAliases =
                {
                    "autoAdvancePhases", "auto_advance_phases", "autoAdvanceSessions", "auto_advance_sessions",
                    "autoAdvanceSession", "auto_advance_session", "autoAdvance", "auto_advance",
                    "autoStartNextSession", "auto_start_next_session", "autoNextPhase", "auto_next_phase"
                };
                foreach (var alias in autoAdvanceAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.AutoAdvancePhases = bVal;
                        break;
                    }
                }

                // 5. CompletionRewardColor aliases
                string[] colorAliases = { "completionRewardColor", "completion_reward_color", "rewardColor", "reward_color" };
                foreach (var alias in colorAliases)
                {
                    if (TryGetStringProperty(root, alias, out var val) && !string.IsNullOrWhiteSpace(val))
                    {
                        settings.CompletionRewardColor = val.Trim();
                        break;
                    }
                }

                // 6. FocusMinutes aliases
                string[] focusMinAliases = { "focusMinutes", "focus_minutes", "focusMin", "focus_min", "workMinutes", "work_minutes", "focusDuration", "workDuration" };
                foreach (var alias in focusMinAliases)
                {
                    if (TryGetIntProperty(root, alias, out var intVal) && intVal > 0)
                    {
                        settings.FocusMinutes = intVal;
                        break;
                    }
                }

                // 7. ShortBreakMinutes aliases
                string[] shortBreakAliases = { "shortBreakMinutes", "short_break_minutes", "shortBreak", "short_break", "breakMinutes", "break_minutes" };
                foreach (var alias in shortBreakAliases)
                {
                    if (TryGetIntProperty(root, alias, out var intVal) && intVal > 0)
                    {
                        settings.ShortBreakMinutes = intVal;
                        break;
                    }
                }

                // 8. LongBreakMinutes aliases
                string[] longBreakAliases = { "longBreakMinutes", "long_break_minutes", "longBreak", "long_break" };
                foreach (var alias in longBreakAliases)
                {
                    if (TryGetIntProperty(root, alias, out var intVal) && intVal > 0)
                    {
                        settings.LongBreakMinutes = intVal;
                        break;
                    }
                }

                // 9. CyclesBeforeLongBreak aliases
                string[] cyclesAliases = { "cyclesBeforeLongBreak", "cycles_before_long_break", "cyclesCount", "cycles_count", "sessionsBeforeLongBreak", "sessions_before_long_break" };
                foreach (var alias in cyclesAliases)
                {
                    if (TryGetIntProperty(root, alias, out var intVal) && intVal > 0)
                    {
                        settings.CyclesBeforeLongBreak = intVal;
                        break;
                    }
                }

                // 10. IntervalModeEnabled aliases
                string[] intervalModeAliases = { "intervalModeEnabled", "interval_mode_enabled", "intervalMode", "interval_mode", "pomodoroEnabled", "pomodoro_enabled", "enableIntervalMode", "enable_interval_mode" };
                foreach (var alias in intervalModeAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.IntervalModeEnabled = bVal;
                        break;
                    }
                }

                // 11. SoundAlertEnabled aliases
                string[] soundAlertAliases = { "soundAlertEnabled", "sound_alert_enabled", "soundAlerts", "sound_alerts", "soundEnabled", "sound_enabled", "audioEnabled", "audio_enabled" };
                foreach (var alias in soundAlertAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.SoundAlertEnabled = bVal;
                        break;
                    }
                }

                // 12. SoundVolume aliases
                string[] soundVolAliases = { "soundVolume", "sound_volume", "volume", "audioVolume", "audio_volume" };
                foreach (var alias in soundVolAliases)
                {
                    if (TryGetIntProperty(root, alias, out var intVal) && intVal >= 0)
                    {
                        settings.SoundVolume = intVal;
                        break;
                    }
                }

                // 13. GoalMilestoneSoundVolume aliases
                string[] milestoneVolAliases = { "goalMilestoneSoundVolume", "goal_milestone_sound_volume", "milestoneVolume", "milestone_volume", "fanfareVolume", "fanfare_volume" };
                foreach (var alias in milestoneVolAliases)
                {
                    if (TryGetIntProperty(root, alias, out var intVal) && intVal >= 0)
                    {
                        settings.GoalMilestoneSoundVolume = intVal;
                        break;
                    }
                }

                // 14. SelectedSceneId aliases
                string[] sceneAliases = { "selectedSceneId", "selected_scene_id", "sceneId", "scene_id", "scene", "selectedScene", "selected_scene" };
                foreach (var alias in sceneAliases)
                {
                    if (TryGetStringProperty(root, alias, out var val) && !string.IsNullOrWhiteSpace(val))
                    {
                        settings.SelectedSceneId = val.Trim();
                        break;
                    }
                }

                // 15. ArtMode aliases
                string[] artAliases = { "artMode", "art_mode", "asciiMode", "ascii_mode" };
                foreach (var alias in artAliases)
                {
                    if (TryGetStringProperty(root, alias, out var val) && !string.IsNullOrWhiteSpace(val))
                    {
                        settings.ArtMode = val.Trim();
                        break;
                    }
                }

                // 16. MiniWidget properties
                string[] widgetCornerAliases = { "miniWidgetCorner", "mini_widget_corner", "widgetCorner", "widget_corner" };
                foreach (var alias in widgetCornerAliases)
                {
                    if (TryGetStringProperty(root, alias, out var val) && !string.IsNullOrWhiteSpace(val))
                    {
                        settings.MiniWidgetCorner = val.Trim();
                        break;
                    }
                }

                string[] widgetEnabledAliases = { "miniWidgetEnabled", "mini_widget_enabled", "showMiniWidget", "show_mini_widget", "enableMiniWidget", "enable_mini_widget" };
                foreach (var alias in widgetEnabledAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.MiniWidgetEnabled = bVal;
                        break;
                    }
                }

                // 17. FocusXp / FocusStreak / TotalSessionsCompleted
                string[] xpAliases = { "focusXp", "focus_xp", "xp", "totalXp", "total_xp" };
                foreach (var alias in xpAliases)
                {
                    if (TryGetIntProperty(root, alias, out var intVal) && intVal > 0)
                    {
                        settings.FocusXp = intVal;
                        break;
                    }
                }

                string[] streakAliases = { "focusStreak", "focus_streak", "streak" };
                foreach (var alias in streakAliases)
                {
                    if (TryGetIntProperty(root, alias, out var intVal) && intVal > 0)
                    {
                        settings.FocusStreak = intVal;
                        break;
                    }
                }

                string[] sessionsAliases = { "totalSessionsCompleted", "total_sessions_completed", "completedSessions", "completed_sessions" };
                foreach (var alias in sessionsAliases)
                {
                    if (TryGetIntProperty(root, alias, out var intVal) && intVal > 0)
                    {
                        settings.TotalSessionsCompleted = intVal;
                        break;
                    }
                }

                // 18. Sound and Notification Toggles
                string[] soundStartAliases = { "soundOnStartEnabled", "sound_on_start_enabled", "soundOnStart", "sound_on_start" };
                foreach (var alias in soundStartAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.SoundOnStartEnabled = bVal;
                        break;
                    }
                }

                string[] soundEndAliases = { "soundOnEndEnabled", "sound_on_end_enabled", "soundOnEnd", "sound_on_end" };
                foreach (var alias in soundEndAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.SoundOnEndEnabled = bVal;
                        break;
                    }
                }

                string[] soundGoalAliases = { "soundOnGoalAchievedEnabled", "sound_on_goal_achieved_enabled", "soundOnGoal", "sound_on_goal" };
                foreach (var alias in soundGoalAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.SoundOnGoalAchievedEnabled = bVal;
                        break;
                    }
                }

                string[] soundDailyGoalAliases = { "soundOnDailyGoalAchievedEnabled", "sound_on_daily_goal_achieved_enabled" };
                foreach (var alias in soundDailyGoalAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.SoundOnDailyGoalAchievedEnabled = bVal;
                        break;
                    }
                }

                string[] soundWeeklyGoalAliases = { "soundOnWeeklyGoalAchievedEnabled", "sound_on_weekly_goal_achieved_enabled" };
                foreach (var alias in soundWeeklyGoalAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.SoundOnWeeklyGoalAchievedEnabled = bVal;
                        break;
                    }
                }

                string[] soundBreakStartAliases = { "soundOnBreakStartEnabled", "sound_on_break_start_enabled" };
                foreach (var alias in soundBreakStartAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.SoundOnBreakStartEnabled = bVal;
                        break;
                    }
                }

                string[] soundBreakEndAliases = { "soundOnBreakEndEnabled", "sound_on_break_end_enabled" };
                foreach (var alias in soundBreakEndAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.SoundOnBreakEndEnabled = bVal;
                        break;
                    }
                }

                string[] soundCheerAliases = { "soundOnCheerEnabled", "sound_on_cheer_enabled" };
                foreach (var alias in soundCheerAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.SoundOnCheerEnabled = bVal;
                        break;
                    }
                }

                string[] companionVisAliases = { "companionVisible", "companion_visible", "showCompanion", "companionShow" };
                foreach (var alias in companionVisAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.CompanionVisible = bVal;
                        break;
                    }
                }

                string[] companionExpAliases = { "companionExpanded", "companion_expanded", "expandCompanion" };
                foreach (var alias in companionExpAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.CompanionExpanded = bVal;
                        break;
                    }
                }

                string[] goalNotifAliases = { "goalNotificationsEnabled", "goal_notifications_enabled" };
                foreach (var alias in goalNotifAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.GoalNotificationsEnabled = bVal;
                        break;
                    }
                }

                string[] dailyGoalNotifAliases = { "dailyGoalNotificationEnabled", "daily_goal_notification_enabled" };
                foreach (var alias in dailyGoalNotifAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.DailyGoalNotificationEnabled = bVal;
                        break;
                    }
                }

                string[] weeklyGoalNotifAliases = { "weeklyGoalNotificationEnabled", "weekly_goal_notification_enabled" };
                foreach (var alias in weeklyGoalNotifAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.WeeklyGoalNotificationEnabled = bVal;
                        break;
                    }
                }

                string[] projectGoalNotifAliases = { "projectGoalNotificationEnabled", "project_goal_notification_enabled" };
                foreach (var alias in projectGoalNotifAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.ProjectGoalNotificationEnabled = bVal;
                        break;
                    }
                }

                string[] milestoneRemindAliases = { "milestoneRemindersEnabled", "milestone_reminders_enabled" };
                foreach (var alias in milestoneRemindAliases)
                {
                    if (TryGetBoolProperty(root, alias, out var bVal))
                    {
                        settings.MilestoneRemindersEnabled = bVal;
                        break;
                    }
                }

                string[] milestoneModeAliases = { "milestoneDisplayMode", "milestone_display_mode" };
                foreach (var alias in milestoneModeAliases)
                {
                    if (TryGetStringProperty(root, alias, out var val) && !string.IsNullOrWhiteSpace(val))
                    {
                        settings.MilestoneDisplayMode = val.Trim();
                        break;
                    }
                }
            }
        }
        catch { }

        EnsureSoundVariations(settings);
        return settings;
    }

    private static bool TryGetStringProperty(JsonElement element, string propName, out string? value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    value = prop.Value.GetString();
                    return true;
                }
            }
        }
        value = null;
        return false;
    }

    private static bool TryGetBoolProperty(JsonElement element, string propName, out bool value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind == JsonValueKind.True)
                {
                    value = true;
                    return true;
                }
                if (prop.Value.ValueKind == JsonValueKind.False)
                {
                    value = false;
                    return true;
                }
            }
        }
        value = false;
        return false;
    }

    private static bool TryGetIntProperty(JsonElement element, string propName, out int value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var intVal))
                {
                    value = intVal;
                    return true;
                }
            }
        }
        value = 0;
        return false;
    }

    public static bool MergeFromCandidateLegacySettings(WorkRestSettings targetSettings, IEnumerable<string>? customLegacyDirectories = null)
    {
        bool anyMerged = false;
        var candidateNames = StoragePathHelper.GetCandidateFileNames("focus-settings.json", "focus_companion_settings.json");
        List<string> legacyDirs;

        if (customLegacyDirectories != null)
        {
            legacyDirs = customLegacyDirectories.ToList();
        }
        else
        {
            legacyDirs = StoragePathHelper.GetCandidateLegacyDirectories().ToList();
            var currentFolder = StoragePathHelper.GetDataFolder();
            legacyDirs.Insert(0, currentFolder);
        }

        var discoveredFiles = new List<(string FilePath, DateTime LastWriteTime)>();

        foreach (var dir in legacyDirs)
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var name in candidateNames)
            {
                var candidateFile = Path.Combine(dir, name);
                if (File.Exists(candidateFile) && new FileInfo(candidateFile).Length > 0)
                {
                    try
                    {
                        var fullPath = Path.GetFullPath(candidateFile);
                        if (!discoveredFiles.Any(f => string.Equals(f.FilePath, fullPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            discoveredFiles.Add((fullPath, File.GetLastWriteTimeUtc(fullPath)));
                        }
                    }
                    catch { }
                }
            }
        }

        // Sort legacy files by most recent write time
        foreach (var (filePath, _) in discoveredFiles.OrderByDescending(f => f.LastWriteTime))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var legacySettings = DeserializeSettings(json);

                if (string.IsNullOrWhiteSpace(targetSettings.UserName) && !string.IsNullOrWhiteSpace(legacySettings.UserName))
                {
                    targetSettings.UserName = legacySettings.UserName;
                    anyMerged = true;
                }

                if (targetSettings.CompletionRewardIcon == "Star" && legacySettings.CompletionRewardIcon != "Star" && !string.IsNullOrWhiteSpace(legacySettings.CompletionRewardIcon))
                {
                    targetSettings.CompletionRewardIcon = legacySettings.CompletionRewardIcon;
                    anyMerged = true;
                }

                if (!targetSettings.AutoAdvancePhases && legacySettings.AutoAdvancePhases)
                {
                    targetSettings.AutoAdvancePhases = true;
                    anyMerged = true;
                }

                if (targetSettings.CompletionRewardColor == "ThemeDefault" && legacySettings.CompletionRewardColor != "ThemeDefault" && !string.IsNullOrWhiteSpace(legacySettings.CompletionRewardColor))
                {
                    targetSettings.CompletionRewardColor = legacySettings.CompletionRewardColor;
                    anyMerged = true;
                }

                if (targetSettings.FocusXp == 0 && legacySettings.FocusXp > 0)
                {
                    targetSettings.FocusXp = legacySettings.FocusXp;
                    anyMerged = true;
                }

                if (targetSettings.FocusMinutes == 25 && legacySettings.FocusMinutes != 25 && legacySettings.FocusMinutes > 0)
                {
                    targetSettings.FocusMinutes = legacySettings.FocusMinutes;
                    anyMerged = true;
                }

                if (targetSettings.ShortBreakMinutes == 5 && legacySettings.ShortBreakMinutes != 5 && legacySettings.ShortBreakMinutes > 0)
                {
                    targetSettings.ShortBreakMinutes = legacySettings.ShortBreakMinutes;
                    anyMerged = true;
                }

                if (targetSettings.LongBreakMinutes == 15 && legacySettings.LongBreakMinutes != 15 && legacySettings.LongBreakMinutes > 0)
                {
                    targetSettings.LongBreakMinutes = legacySettings.LongBreakMinutes;
                    anyMerged = true;
                }

                if (targetSettings.CyclesBeforeLongBreak == 4 && legacySettings.CyclesBeforeLongBreak != 4 && legacySettings.CyclesBeforeLongBreak > 0)
                {
                    targetSettings.CyclesBeforeLongBreak = legacySettings.CyclesBeforeLongBreak;
                    anyMerged = true;
                }

                if (!targetSettings.IntervalModeEnabled && legacySettings.IntervalModeEnabled)
                {
                    targetSettings.IntervalModeEnabled = true;
                    anyMerged = true;
                }

                if (targetSettings.SoundVolume == 75 && legacySettings.SoundVolume != 75 && legacySettings.SoundVolume >= 0)
                {
                    targetSettings.SoundVolume = legacySettings.SoundVolume;
                    anyMerged = true;
                }

                if (targetSettings.GoalMilestoneSoundVolume == 75 && legacySettings.GoalMilestoneSoundVolume != 75 && legacySettings.GoalMilestoneSoundVolume >= 0)
                {
                    targetSettings.GoalMilestoneSoundVolume = legacySettings.GoalMilestoneSoundVolume;
                    anyMerged = true;
                }

                if (targetSettings.SelectedSceneId == "cycling" && legacySettings.SelectedSceneId != "cycling" && !string.IsNullOrWhiteSpace(legacySettings.SelectedSceneId))
                {
                    targetSettings.SelectedSceneId = legacySettings.SelectedSceneId;
                    anyMerged = true;
                }

                if (targetSettings.ArtMode == "graphics" && legacySettings.ArtMode != "graphics" && !string.IsNullOrWhiteSpace(legacySettings.ArtMode))
                {
                    targetSettings.ArtMode = legacySettings.ArtMode;
                    anyMerged = true;
                }

                if (targetSettings.MiniWidgetCorner == "BottomRight" && legacySettings.MiniWidgetCorner != "BottomRight" && !string.IsNullOrWhiteSpace(legacySettings.MiniWidgetCorner))
                {
                    targetSettings.MiniWidgetCorner = legacySettings.MiniWidgetCorner;
                    anyMerged = true;
                }

                if (targetSettings.MiniWidgetTargetMonitor == "Primary" && legacySettings.MiniWidgetTargetMonitor != "Primary" && !string.IsNullOrWhiteSpace(legacySettings.MiniWidgetTargetMonitor))
                {
                    targetSettings.MiniWidgetTargetMonitor = legacySettings.MiniWidgetTargetMonitor;
                    anyMerged = true;
                }

                if (targetSettings.MiniWidgetMonitorIndex == -1 && legacySettings.MiniWidgetMonitorIndex != -1)
                {
                    targetSettings.MiniWidgetMonitorIndex = legacySettings.MiniWidgetMonitorIndex;
                    anyMerged = true;
                }

                if (targetSettings.FocusStreak == 0 && legacySettings.FocusStreak > 0)
                {
                    targetSettings.FocusStreak = legacySettings.FocusStreak;
                    anyMerged = true;
                }

                if (targetSettings.TotalSessionsCompleted == 0 && legacySettings.TotalSessionsCompleted > 0)
                {
                    targetSettings.TotalSessionsCompleted = legacySettings.TotalSessionsCompleted;
                    anyMerged = true;
                }

                if (targetSettings.MilestoneDisplayMode == "NextOnly" && legacySettings.MilestoneDisplayMode != "NextOnly" && !string.IsNullOrWhiteSpace(legacySettings.MilestoneDisplayMode))
                {
                    targetSettings.MilestoneDisplayMode = legacySettings.MilestoneDisplayMode;
                    anyMerged = true;
                }

                // Sound toggles (defaults are true, merge false if user disabled them)
                if (targetSettings.SoundAlertEnabled && !legacySettings.SoundAlertEnabled)
                {
                    targetSettings.SoundAlertEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.SoundOnStartEnabled && !legacySettings.SoundOnStartEnabled)
                {
                    targetSettings.SoundOnStartEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.SoundOnEndEnabled && !legacySettings.SoundOnEndEnabled)
                {
                    targetSettings.SoundOnEndEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.SoundOnGoalAchievedEnabled && !legacySettings.SoundOnGoalAchievedEnabled)
                {
                    targetSettings.SoundOnGoalAchievedEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.SoundOnDailyGoalAchievedEnabled && !legacySettings.SoundOnDailyGoalAchievedEnabled)
                {
                    targetSettings.SoundOnDailyGoalAchievedEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.SoundOnWeeklyGoalAchievedEnabled && !legacySettings.SoundOnWeeklyGoalAchievedEnabled)
                {
                    targetSettings.SoundOnWeeklyGoalAchievedEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.SoundOnBreakStartEnabled && !legacySettings.SoundOnBreakStartEnabled)
                {
                    targetSettings.SoundOnBreakStartEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.SoundOnBreakEndEnabled && !legacySettings.SoundOnBreakEndEnabled)
                {
                    targetSettings.SoundOnBreakEndEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.SoundOnCheerEnabled && !legacySettings.SoundOnCheerEnabled)
                {
                    targetSettings.SoundOnCheerEnabled = false;
                    anyMerged = true;
                }

                // Companion & Widget toggles
                if (targetSettings.CompanionVisible && !legacySettings.CompanionVisible)
                {
                    targetSettings.CompanionVisible = false;
                    anyMerged = true;
                }

                if (targetSettings.CompanionExpanded && !legacySettings.CompanionExpanded)
                {
                    targetSettings.CompanionExpanded = false;
                    anyMerged = true;
                }

                if (targetSettings.MiniWidgetEnabled && !legacySettings.MiniWidgetEnabled)
                {
                    targetSettings.MiniWidgetEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.WelcomePopupEnabled && !legacySettings.WelcomePopupEnabled)
                {
                    targetSettings.WelcomePopupEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.LastWelcomePopupDate == null && legacySettings.LastWelcomePopupDate != null)
                {
                    targetSettings.LastWelcomePopupDate = legacySettings.LastWelcomePopupDate;
                    anyMerged = true;
                }

                if (!targetSettings.ForceWelcomePopupOnNextLaunch && legacySettings.ForceWelcomePopupOnNextLaunch)
                {
                    targetSettings.ForceWelcomePopupOnNextLaunch = true;
                    anyMerged = true;
                }

                // Notification toggles
                if (targetSettings.GoalNotificationsEnabled && !legacySettings.GoalNotificationsEnabled)
                {
                    targetSettings.GoalNotificationsEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.DailyGoalNotificationEnabled && !legacySettings.DailyGoalNotificationEnabled)
                {
                    targetSettings.DailyGoalNotificationEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.WeeklyGoalNotificationEnabled && !legacySettings.WeeklyGoalNotificationEnabled)
                {
                    targetSettings.WeeklyGoalNotificationEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.ProjectGoalNotificationEnabled && !legacySettings.ProjectGoalNotificationEnabled)
                {
                    targetSettings.ProjectGoalNotificationEnabled = false;
                    anyMerged = true;
                }

                if (targetSettings.MilestoneRemindersEnabled && !legacySettings.MilestoneRemindersEnabled)
                {
                    targetSettings.MilestoneRemindersEnabled = false;
                    anyMerged = true;
                }

                // Sound variations
                if (legacySettings.EnabledSoundVariations != null && legacySettings.EnabledSoundVariations.Count > 0)
                {
                    targetSettings.EnabledSoundVariations ??= new();
                    foreach (var v in legacySettings.EnabledSoundVariations)
                    {
                        if (!targetSettings.EnabledSoundVariations.Contains(v))
                        {
                            targetSettings.EnabledSoundVariations.Add(v);
                            anyMerged = true;
                        }
                    }
                }
            }
            catch { }
        }

        return anyMerged;
    }

    private static void EnsureSoundVariations(WorkRestSettings settings)
    {
        if (settings.EnabledSoundVariations == null || settings.EnabledSoundVariations.Count == 0)
        {
            settings.EnabledSoundVariations = new WorkRestSettings().EnabledSoundVariations;
        }
        else
        {
            var defaultVariations = new WorkRestSettings().EnabledSoundVariations;
            foreach (var def in defaultVariations)
            {
                var prefix = def.Split('_')[0];
                if (!settings.EnabledSoundVariations.Any(v => v.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase)))
                {
                    settings.EnabledSoundVariations.Add(def);
                }
            }
        }
    }

    public static void Save(WorkRestSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                StoragePathHelper.EnsureDirectoryExists(dir);
            }

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "FocusCompanionStorage.Save");
        }
    }
}
