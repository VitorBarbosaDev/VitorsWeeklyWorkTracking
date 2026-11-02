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
        try
        {
            var primaryPath = SettingsFilePath;
            if (File.Exists(primaryPath))
            {
                var json = File.ReadAllText(primaryPath);
                settings = DeserializeSettings(json);
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "FocusCompanionStorage.Load");
        }

        settings ??= new WorkRestSettings();

        // If key settings (such as UserName) are empty in primary file, scan candidate legacy files to restore user settings
        try
        {
            if (string.IsNullOrWhiteSpace(settings.UserName) || settings.CompletionRewardIcon == "Star" && !settings.AutoAdvancePhases)
            {
                MergeFromCandidateLegacySettings(settings);
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

    private static void MergeFromCandidateLegacySettings(WorkRestSettings targetSettings)
    {
        var candidateNames = StoragePathHelper.GetCandidateFileNames("focus-settings.json", "focus_companion_settings.json");
        var legacyDirs = StoragePathHelper.GetCandidateLegacyDirectories().ToList();
        var currentFolder = StoragePathHelper.GetDataFolder();
        legacyDirs.Insert(0, currentFolder);

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
                        var json = File.ReadAllText(candidateFile);
                        var legacySettings = DeserializeSettings(json);

                        if (string.IsNullOrWhiteSpace(targetSettings.UserName) && !string.IsNullOrWhiteSpace(legacySettings.UserName))
                        {
                            targetSettings.UserName = legacySettings.UserName;
                        }

                        if (targetSettings.CompletionRewardIcon == "Star" && legacySettings.CompletionRewardIcon != "Star" && !string.IsNullOrWhiteSpace(legacySettings.CompletionRewardIcon))
                        {
                            targetSettings.CompletionRewardIcon = legacySettings.CompletionRewardIcon;
                        }

                        if (!targetSettings.AutoAdvancePhases && legacySettings.AutoAdvancePhases)
                        {
                            targetSettings.AutoAdvancePhases = true;
                        }

                        if (targetSettings.CompletionRewardColor == "ThemeDefault" && legacySettings.CompletionRewardColor != "ThemeDefault" && !string.IsNullOrWhiteSpace(legacySettings.CompletionRewardColor))
                        {
                            targetSettings.CompletionRewardColor = legacySettings.CompletionRewardColor;
                        }

                        if (targetSettings.FocusXp == 0 && legacySettings.FocusXp > 0)
                        {
                            targetSettings.FocusXp = legacySettings.FocusXp;
                        }
                    }
                    catch { }
                }
            }
        }
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
