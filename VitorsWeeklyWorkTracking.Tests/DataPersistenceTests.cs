using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

[Collection("StorageTests")]
public class DataPersistenceTests
{
    [Fact]
    public void Load_DoesNotRemoveUserProjectsEvenIfTheyMatchDummyNames()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var projectsFile = Path.Combine(tempDir, "projects.json");
            var entriesFile = Path.Combine(tempDir, "time-entries.json");
            
            // Create a projects file with a "dummy" name but custom color
            var initialProjects = new List<Project>
            {
                new Project { Name = "Alpha", IsActive = true, ColorHex = "#FF0000" },
                new Project { Name = "Real Project", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(projectsFile, JsonSerializer.Serialize(initialProjects));

            // Create an empty entries file so discovery doesn't find "Alpha"
            File.WriteAllText(entriesFile, "[]");

            // We need to trick StoragePathHelper to use our temp files, but ProjectStorage 
            // allows passing a path directly. However, it might still call StoragePathHelper
            // for discovery. 
            // Actually ProjectStorage calls StoragePathHelper.GetFilePath("time-entries.json")
            // inside DiscoverProjects. This is hard to mock without changing StoragePathHelper.
            
            // But wait, the Load() method in ProjectStorage uses the _filePath passed in constructor
            // for loading projects, but it uses StoragePathHelper for discovery.
            
            var storage = new ProjectStorage(projectsFile);
            var loaded = storage.Load();

            // REPRODUCTION OF BUG: "Alpha" should be there, but it will be removed by current code.
            var alpha = loaded.FirstOrDefault(p => p.Name == "Alpha");
            Assert.NotNull(alpha);
            Assert.Equal("#FF0000", alpha.ColorHex);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_DoesNotRemoveUserActivitiesEvenIfTheyMatchDummyNames()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_act_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var activitiesFile = Path.Combine(tempDir, "activities.json");
            
            var initialActivities = new List<ActivityItem>
            {
                new ActivityItem { Name = "Marketing", IsActive = true, ColorHex = "#FF0000" },
                new ActivityItem { Name = "Real Activity", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(activitiesFile, JsonSerializer.Serialize(initialActivities));

            var storage = new ActivityStorage(activitiesFile);
            var loaded = storage.Load();

            var marketing = loaded.FirstOrDefault(a => a.Name == "Marketing");
            Assert.NotNull(marketing);
            Assert.Equal("#FF0000", marketing.ColorHex);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_RepairsListByRemovingUnusedDummyItemsWithoutColors()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_repair_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var projectsFile = Path.Combine(tempDir, "projects.json");
            
            // Create a projects file with a dummy name, NO color
            var initialProjects = new List<Project>
            {
                new Project { Name = "Alpha", IsActive = true, ColorHex = null },
                new Project { Name = "Real Project", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(projectsFile, JsonSerializer.Serialize(initialProjects));

            var storage = new ProjectStorage(projectsFile);
            var loaded = storage.Load();

            // "Alpha" should be removed because it's a dummy, has no color, and is not discovered from data
            Assert.DoesNotContain(loaded, p => p.Name == "Alpha");
            Assert.Contains(loaded, p => p.Name == "Real Project");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_RemovesUnusedDefaultActivitiesWithoutColors()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_act_repair_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;

        try
        {
            var activitiesFile = Path.Combine(tempDir, "activities.json");
            
            // Create an activities file with a default name, NO color
            var initialActivities = new List<ActivityItem>
            {
                new ActivityItem { Name = "Game Dev", IsActive = true, ColorHex = null }, // Default activity
                new ActivityItem { Name = "Real Activity", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(activitiesFile, JsonSerializer.Serialize(initialActivities));

            var storage = new ActivityStorage(activitiesFile);
            var loaded = storage.Load();

            // "Game Dev" should be removed because it's now in IsDummyActivityPlaceholder
            Assert.DoesNotContain(loaded, a => a.Name == "Game Dev");
            Assert.Contains(loaded, a => a.Name == "Real Activity");
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_RemovesUnusedGeneralFreelanceProjectWithoutColors()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_proj_repair_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;

        try
        {
            var projectsFile = Path.Combine(tempDir, "projects.json");
            
            // Create a projects file with "General Freelance", NO color
            var initialProjects = new List<Project>
            {
                new Project { Name = "General Freelance", IsActive = true, ColorHex = null },
                new Project { Name = "Real Project", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(projectsFile, JsonSerializer.Serialize(initialProjects));

            var storage = new ProjectStorage(projectsFile);
            var loaded = storage.Load();

            // "General Freelance" should be removed if we update IsDummyPlaceholder to include it
            Assert.DoesNotContain(loaded, p => p.Name == "General Freelance");
            Assert.Contains(loaded, p => p.Name == "Real Project");
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
    [Fact]
    public void Load_RemovesUnusedDefaultActivitiesEvenWithDefaultColor()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_act_color_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;

        try
        {
            var activitiesFile = Path.Combine(tempDir, "activities.json");
            
            // Create an activities file with a default name, and the DEFAULT color hex
            var initialActivities = new List<ActivityItem>
            {
                new ActivityItem { Name = "Coding", IsActive = true, ColorHex = "#6B7280" }, // Default activity with default color
                new ActivityItem { Name = "Real Activity", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(activitiesFile, JsonSerializer.Serialize(initialActivities));

            var storage = new ActivityStorage(activitiesFile);
            var loaded = storage.Load();

            // "Coding" should be removed because although it has ColorHex, it's the default color
            Assert.DoesNotContain(loaded, a => a.Name == "Coding");
            Assert.Contains(loaded, a => a.Name == "Real Activity");
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_InitializesWithGeneralFreelanceIfProjectListIsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_proj_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            StoragePathHelper.OverrideAppDataFolder = tempDir;
            var projectsFile = Path.Combine(tempDir, "projects.json");
            
            // Create an empty projects file
            File.WriteAllText(projectsFile, "[]");

            var storage = new ProjectStorage(projectsFile);
            var loaded = storage.Load();

            // Should be initialized with "General Freelance"
            Assert.Single(loaded);
            Assert.Equal("General Freelance", loaded[0].Name);
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Theory]
    [InlineData("{\"Theme\":\"DarkSlate\"}", AppTheme.DarkSlate)]
    [InlineData("{\"theme\":\"NordicFrost\"}", AppTheme.NordicFrost)]
    [InlineData("{\"currentTheme\":\"CyberpunkSynthwave\"}", AppTheme.CyberpunkSynthwave)]
    [InlineData("{\"selectedTheme\":\"MidnightNeon\"}", AppTheme.MidnightNeon)]
    [InlineData("{\"Theme\":1}", AppTheme.PitchBlack)]
    [InlineData("\"EmeraldForest\"", AppTheme.EmeraldForest)]
    [InlineData("SunsetGlow", AppTheme.SunsetGlow)]
    public void ThemeManager_LoadsLegacyThemeFormats_Successfully(string jsonContent, AppTheme expectedTheme)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"theme_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;

        try
        {
            var themeFile = Path.Combine(tempDir, "theme.json");
            File.WriteAllText(themeFile, jsonContent);

            ThemeManager.Initialize();
            Assert.Equal(expectedTheme, ThemeManager.CurrentTheme);
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FocusCompanionStorage_LoadsLegacyCamelCaseAndAliasedSettings_Successfully()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"focus_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;

        try
        {
            // Write legacy camelCase settings file
            var settingsFile = Path.Combine(tempDir, "focus-settings.json");
            var legacyJson = """
            {
              "intervalModeEnabled": true,
              "focusMinutes": 45,
              "shortBreakMinutes": 10,
              "longBreakMinutes": 20,
              "soundVolume": 85,
              "userName": "Vitor Programmer",
              "selectedSceneId": "coffee_jazz",
              "miniWidgetEnabled": true,
              "miniWidgetCorner": "BottomLeft",
              "completionRewardIcon": "Trophy",
              "completionRewardColor": "Gold"
            }
            """;
            File.WriteAllText(settingsFile, legacyJson);

            var loaded = FocusCompanionStorage.Load();
            Assert.True(loaded.IntervalModeEnabled);
            Assert.Equal(45, loaded.FocusMinutes);
            Assert.Equal(10, loaded.ShortBreakMinutes);
            Assert.Equal(20, loaded.LongBreakMinutes);
            Assert.Equal(85, loaded.SoundVolume);
            Assert.Equal("Vitor Programmer", loaded.UserName);
            Assert.Equal("coffee_jazz", loaded.SelectedSceneId);
            Assert.Equal("BottomLeft", loaded.MiniWidgetCorner);
            Assert.Equal("Trophy", loaded.CompletionRewardIcon);
            Assert.Equal("Gold", loaded.CompletionRewardColor);
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void StoragePathHelper_MigratesFilesFromLegacyLocations_WhenTargetDoesNotExist()
    {
        var activeDir = Path.Combine(Path.GetTempPath(), $"active_data_{Guid.NewGuid():N}");
        Directory.CreateDirectory(activeDir);

        var legacyDir = Path.Combine(Path.GetTempPath(), $"legacy_data_{Guid.NewGuid():N}");
        Directory.CreateDirectory(legacyDir);

        var legacyFile = Path.Combine(legacyDir, "focus_companion_settings.json");
        var legacyJson = "{\"FocusMinutes\":50,\"UserName\":\"UpdatedUser\"}";

        try
        {
            File.WriteAllText(legacyFile, legacyJson);
            StoragePathHelper.OverrideAppDataFolder = activeDir;
            StoragePathHelper.CustomLegacyDirectoriesForTesting = new List<string> { legacyDir };

            var targetPath = StoragePathHelper.GetFilePath("focus-settings.json", "focus_companion_settings.json");

            Assert.True(File.Exists(targetPath));
            var content = File.ReadAllText(targetPath);
            Assert.Contains("UpdatedUser", content);
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            StoragePathHelper.CustomLegacyDirectoriesForTesting = null;
            if (Directory.Exists(legacyDir))
                Directory.Delete(legacyDir, true);
            if (Directory.Exists(activeDir))
                Directory.Delete(activeDir, true);
        }
    }

    [Fact]
    public void StoragePathHelper_MigrateAllLegacyData_CopiesAllMonitoredFilesAndBackups()
    {
        var activeDir = Path.Combine(Path.GetTempPath(), $"active_migrate_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(activeDir);

        var legacyDir = Path.Combine(Path.GetTempPath(), $"legacy_migrate_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(legacyDir);

        var legacyTheme = Path.Combine(legacyDir, "theme.json");
        var legacyTimeEntries = Path.Combine(legacyDir, "time_entries.json");

        var legacyBackupsDir = Path.Combine(legacyDir, "backups", "backup_legacy_001");
        Directory.CreateDirectory(legacyBackupsDir);
        var backupFile = Path.Combine(legacyBackupsDir, "theme.json");

        try
        {
            File.WriteAllText(legacyTheme, "{\"Theme\":\"MidnightNeon\"}");
            File.WriteAllText(legacyTimeEntries, "[{\"TaskName\":\"Legacy Task\"}]");
            File.WriteAllText(backupFile, "{\"Theme\":\"PitchBlack\"}");

            StoragePathHelper.OverrideAppDataFolder = activeDir;
            StoragePathHelper.CustomLegacyDirectoriesForTesting = new List<string> { legacyDir };

            int count = StoragePathHelper.MigrateAllLegacyData();
            Assert.True(count >= 2);

            var activeTheme = Path.Combine(activeDir, "theme.json");
            var activeEntries = Path.Combine(activeDir, "time-entries.json");
            var activeBackup = Path.Combine(activeDir, "backups", "backup_legacy_001", "theme.json");

            Assert.True(File.Exists(activeTheme));
            Assert.True(File.Exists(activeEntries));
            Assert.True(File.Exists(activeBackup));
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            StoragePathHelper.CustomLegacyDirectoriesForTesting = null;
            if (Directory.Exists(legacyDir))
                Directory.Delete(legacyDir, true);
            if (Directory.Exists(activeDir))
                Directory.Delete(activeDir, true);
        }
    }

    [Fact]
    public void FocusCompanionStorage_DeserializeSettings_SupportsLegacyPropertyAliases()
    {
        var legacyJson = """
        {
          "user_name": "Vitor Programmer",
          "show_welcome_back_popup": true,
          "reward_icon": "Fire",
          "auto_advance_sessions": true,
          "reward_color": "Amber"
        }
        """;

        var settings = FocusCompanionStorage.DeserializeSettings(legacyJson);

        Assert.Equal("Vitor Programmer", settings.UserName);
        Assert.True(settings.WelcomePopupEnabled);
        Assert.Equal("Fire", settings.CompletionRewardIcon);
        Assert.True(settings.AutoAdvancePhases);
        Assert.Equal("Amber", settings.CompletionRewardColor);
    }

    [Fact]
    public void FocusCompanionStorage_MergeFromCandidateLegacySettings_RestoresSettingsWhenPrimaryEmpty()
    {
        var activeDir = Path.Combine(Path.GetTempPath(), $"active_merge_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(activeDir);

        var legacyDir = Path.Combine(Path.GetTempPath(), $"legacy_merge_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(legacyDir);

        // Active file has empty user name and default icon
        var activeSettingsFile = Path.Combine(activeDir, "focus-settings.json");
        var activeJson = """
        {
          "UserName": "",
          "CompletionRewardIcon": "Star",
          "AutoAdvancePhases": false
        }
        """;
        File.WriteAllText(activeSettingsFile, activeJson);

        // Older alias file has the actual user settings
        var legacyFile = Path.Combine(legacyDir, "work-rest-settings.json");
        var legacyJson = """
        {
          "user_name": "Vitor Master",
          "completion_reward_icon": "Diamond",
          "auto_advance": true
        }
        """;
        File.WriteAllText(legacyFile, legacyJson);

        try
        {
            StoragePathHelper.OverrideAppDataFolder = activeDir;
            StoragePathHelper.CustomLegacyDirectoriesForTesting = new List<string> { legacyDir };

            var loaded = FocusCompanionStorage.Load();

            Assert.Equal("Vitor Master", loaded.UserName);
            Assert.Equal("Diamond", loaded.CompletionRewardIcon);
            Assert.True(loaded.AutoAdvancePhases);
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            StoragePathHelper.CustomLegacyDirectoriesForTesting = null;
            if (Directory.Exists(legacyDir))
                Directory.Delete(legacyDir, true);
            if (Directory.Exists(activeDir))
                Directory.Delete(activeDir, true);
        }
    }

    [Fact]
    public void FocusCompanionStorage_DeserializeSettings_SupportsFullSettingsPropertiesAndAliases()
    {
        var customJson = """
        {
          "user_name": "Vitor",
          "focus_minutes": 50,
          "short_break_minutes": 10,
          "long_break_minutes": 20,
          "cycles_before_long_break": 3,
          "interval_mode_enabled": true,
          "sound_alert_enabled": true,
          "sound_volume": 85,
          "goal_milestone_sound_volume": 90,
          "selected_scene_id": "lumberjack",
          "art_mode": "ascii",
          "mini_widget_corner": "TopLeft",
          "mini_widget_enabled": true,
          "focus_xp": 1250,
          "focus_streak": 7,
          "total_sessions_completed": 42
        }
        """;

        var settings = FocusCompanionStorage.DeserializeSettings(customJson);

        Assert.Equal("Vitor", settings.UserName);
        Assert.Equal(50, settings.FocusMinutes);
        Assert.Equal(10, settings.ShortBreakMinutes);
        Assert.Equal(20, settings.LongBreakMinutes);
        Assert.Equal(3, settings.CyclesBeforeLongBreak);
        Assert.True(settings.IntervalModeEnabled);
        Assert.True(settings.SoundAlertEnabled);
        Assert.Equal(85, settings.SoundVolume);
        Assert.Equal(90, settings.GoalMilestoneSoundVolume);
        Assert.Equal("lumberjack", settings.SelectedSceneId);
        Assert.Equal("ascii", settings.ArtMode);
        Assert.Equal("TopLeft", settings.MiniWidgetCorner);
        Assert.True(settings.MiniWidgetEnabled);
        Assert.Equal(1250, settings.FocusXp);
        Assert.Equal(7, settings.FocusStreak);
        Assert.Equal(42, settings.TotalSessionsCompleted);
    }

    [Fact]
    public void FocusCompanionStorage_MergeFromCandidateLegacySettings_PreservesCustomDurationsAndAllSettingsAcrossUpdates()
    {
        var activeDir = Path.Combine(Path.GetTempPath(), $"active_settings_update_{Guid.NewGuid():N}");
        Directory.CreateDirectory(activeDir);

        var legacyDir = Path.Combine(Path.GetTempPath(), $"legacy_settings_update_{Guid.NewGuid():N}");
        Directory.CreateDirectory(legacyDir);

        // Active file is default (created on update)
        var activeSettingsFile = Path.Combine(activeDir, "focus-settings.json");
        var activeJson = JsonSerializer.Serialize(new WorkRestSettings());
        File.WriteAllText(activeSettingsFile, activeJson);

        // Legacy file has user customized settings
        var legacyFile = Path.Combine(legacyDir, "focus_companion_settings.json");
        var legacyCustom = new WorkRestSettings
        {
            UserName = "Vitor Pro",
            FocusMinutes = 45,
            ShortBreakMinutes = 15,
            LongBreakMinutes = 30,
            CyclesBeforeLongBreak = 2,
            IntervalModeEnabled = true,
            SoundVolume = 60,
            SelectedSceneId = "coffee_jazz",
            ArtMode = "ascii",
            MiniWidgetCorner = "BottomLeft",
            FocusXp = 500
        };
        File.WriteAllText(legacyFile, JsonSerializer.Serialize(legacyCustom));

        try
        {
            StoragePathHelper.OverrideAppDataFolder = activeDir;
            StoragePathHelper.CustomLegacyDirectoriesForTesting = new List<string> { legacyDir };

            var loaded = FocusCompanionStorage.Load();

            Assert.Equal("Vitor Pro", loaded.UserName);
            Assert.Equal(45, loaded.FocusMinutes);
            Assert.Equal(15, loaded.ShortBreakMinutes);
            Assert.Equal(30, loaded.LongBreakMinutes);
            Assert.Equal(2, loaded.CyclesBeforeLongBreak);
            Assert.True(loaded.IntervalModeEnabled);
            Assert.Equal(60, loaded.SoundVolume);
            Assert.Equal("coffee_jazz", loaded.SelectedSceneId);
            Assert.Equal("ascii", loaded.ArtMode);
            Assert.Equal("BottomLeft", loaded.MiniWidgetCorner);
            Assert.Equal(500, loaded.FocusXp);
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            StoragePathHelper.CustomLegacyDirectoriesForTesting = null;
            if (Directory.Exists(legacyDir))
                Directory.Delete(legacyDir, true);
            if (Directory.Exists(activeDir))
                Directory.Delete(activeDir, true);
        }
    }
}
