using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

[Collection("StorageTests")]
public class StreakAndMiniWidgetVisibilityTests
{
    private static (string tempDir, string dataDir) SetupSandbox()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "VisibilityTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;
        StreakStorage.ResetInstance();
        StreakService.ResetInstance();
        CalendarPlanStorage.ResetInstance();
        BackupService.ResetInstance();
        var dataDir = StoragePathHelper.GetDataFolder();
        return (tempDir, dataDir);
    }

    private static void CleanupSandbox(string tempDir)
    {
        StoragePathHelper.OverrideAppDataFolder = null;
        StreakStorage.ResetInstance();
        StreakService.ResetInstance();
        CalendarPlanStorage.ResetInstance();
        BackupService.ResetInstance();
        if (Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
            }
        }
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            throw new Exception("STA test execution failed", exception);
        }
    }

    [Fact]
    public void StreakColorHelper_ApplyStreakButtonStyle_WhenDisabled_HidesButton()
    {
        RunInSta(() =>
        {
            var button = new Button();

            // 1. When streak is disabled -> Button is Collapsed
            var disabledSummary = new StreakSummary
            {
                IsEnabled = false,
                CurrentStreak = 0
            };
            StreakColorHelper.ApplyStreakButtonStyle(button, disabledSummary);
            Assert.Equal(Visibility.Collapsed, button.Visibility);

            // 2. When streak is enabled -> Button is Visible
            var enabledSummary = new StreakSummary
            {
                IsEnabled = true,
                CurrentStreak = 5,
                TodayTier = StreakTier.Bronze,
                TodayStatus = StreakDayStatus.Active
            };
            StreakColorHelper.ApplyStreakButtonStyle(button, enabledSummary);
            Assert.Equal(Visibility.Visible, button.Visibility);
            Assert.Equal("🔥 5 🥉", button.Content);

            // 3. Null summary defaults to disabled -> Collapsed
            StreakColorHelper.ApplyStreakButtonStyle(button, null);
            Assert.Equal(Visibility.Collapsed, button.Visibility);
        });
    }

    [Fact]
    public void MainWindow_WhenStreakIsSwitchedOff_HidesStreakButton()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                StreakService.Instance.IsEnabled = false;

                var mainWindow = new MainWindow();
                try
                {
                    // Streak system is disabled -> StreakQuickButton is Collapsed
                    Assert.Equal(Visibility.Collapsed, mainWindow.StreakQuickButton.Visibility);

                    // Switch streak system ON -> StreakQuickButton is Visible
                    StreakService.Instance.IsEnabled = true;
                    Assert.Equal(Visibility.Visible, mainWindow.StreakQuickButton.Visibility);

                    // Switch streak system OFF -> StreakQuickButton is Collapsed
                    StreakService.Instance.IsEnabled = false;
                    Assert.Equal(Visibility.Collapsed, mainWindow.StreakQuickButton.Visibility);
                }
                finally
                {
                    mainWindow.Close();
                    StreakService.Instance.IsEnabled = true;
                }
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void MiniTimerWidget_WhenCompanionIsDisabled_HidesCompanionElements()
    {
        RunInSta(() =>
        {
            var widget = new MiniTimerWidget();
            try
            {
                // Default: Companion is visible
                Assert.True(widget.IsCompanionVisible);
                Assert.Equal(Visibility.Visible, widget.MiniVisualBorder.Visibility);
                Assert.Equal(Visibility.Visible, widget.MiniArtModeButton.Visibility);

                // Set Companion Visible = False
                widget.IsCompanionVisible = false;
                Assert.False(widget.IsCompanionVisible);
                Assert.Equal(Visibility.Collapsed, widget.MiniVisualBorder.Visibility);
                Assert.Equal(Visibility.Collapsed, widget.MiniAsciiBorder.Visibility);
                Assert.Equal(Visibility.Collapsed, widget.MiniCompanionLine.Visibility);
                Assert.Equal(Visibility.Collapsed, widget.MiniArtModeButton.Visibility);

                // Re-enable Companion Visible = True
                widget.IsCompanionVisible = true;
                Assert.True(widget.IsCompanionVisible);
                Assert.Equal(Visibility.Visible, widget.MiniVisualBorder.Visibility);
                Assert.Equal(Visibility.Visible, widget.MiniArtModeButton.Visibility);
            }
            finally
            {
                widget.Close();
            }
        });
    }

    [Fact]
    public void MiniTimerWidget_UpdateDisplay_WithCompanionDisabled_HidesCompanionAndSuppressesCheer()
    {
        RunInSta(() =>
        {
            var widget = new MiniTimerWidget();
            try
            {
                // Update display with companion disabled
                widget.UpdateDisplay(
                    isTracking: true,
                    timerText: "00:15:00",
                    statusText: "RECORDING",
                    statusColor: Brushes.Green,
                    projectName: "Project A",
                    activity: "Coding",
                    isCountDownMode: false,
                    hasGoal: true,
                    goalProgressPercentage: 50.0,
                    goalStatsText: "15m / 30m",
                    companionMiniLine: "🚴 Boy is pedaling steadily...",
                    isCompanionVisible: false);

                Assert.False(widget.IsCompanionVisible);
                Assert.Equal(Visibility.Collapsed, widget.MiniVisualBorder.Visibility);
                Assert.Equal(Visibility.Collapsed, widget.MiniAsciiBorder.Visibility);
                Assert.Equal(Visibility.Collapsed, widget.MiniCompanionLine.Visibility);
                Assert.Equal(Visibility.Collapsed, widget.MiniArtModeButton.Visibility);

                // Triggering cheer when companion is hidden should not show companion line
                widget.TriggerMiniCheer();
                Assert.Equal(Visibility.Collapsed, widget.MiniCompanionLine.Visibility);

                // Re-enable with UpdateDisplay
                widget.UpdateDisplay(
                    isTracking: true,
                    timerText: "00:16:00",
                    statusText: "RECORDING",
                    statusColor: Brushes.Green,
                    projectName: "Project A",
                    activity: "Coding",
                    isCountDownMode: false,
                    hasGoal: true,
                    goalProgressPercentage: 53.0,
                    goalStatsText: "16m / 30m",
                    companionMiniLine: "🚴 Boy is pedaling steadily...",
                    isCompanionVisible: true);

                Assert.True(widget.IsCompanionVisible);
                Assert.Equal(Visibility.Visible, widget.MiniVisualBorder.Visibility);
                Assert.Equal(Visibility.Visible, widget.MiniArtModeButton.Visibility);
                Assert.Equal(Visibility.Visible, widget.MiniCompanionLine.Visibility);
                Assert.Equal("🚴 Boy is pedaling steadily...", widget.MiniCompanionLine.Text);
            }
            finally
            {
                widget.Close();
            }
        });
    }

    [Fact]
    public void MiniTimerWidget_AsciiMode_WhenCompanionIsDisabled_HidesAsciiBorderAndArtButton()
    {
        RunInSta(() =>
        {
            var widget = new MiniTimerWidget();
            try
            {
                // 1. ASCII mode with companion enabled
                widget.UpdateDisplay(
                    isTracking: true,
                    timerText: "00:10:00",
                    statusText: "RECORDING",
                    statusColor: Brushes.Green,
                    projectName: "Work",
                    activity: "Dev",
                    isCountDownMode: false,
                    hasGoal: false,
                    goalProgressPercentage: 0,
                    goalStatsText: "",
                    artMode: "ascii",
                    asciiArtText: "ASCII_ART_TEST",
                    isCompanionVisible: true);

                Assert.Equal(Visibility.Collapsed, widget.MiniVisualBorder.Visibility);
                Assert.Equal(Visibility.Visible, widget.MiniAsciiBorder.Visibility);
                Assert.Equal(Visibility.Visible, widget.MiniArtModeButton.Visibility);
                Assert.Equal("📟", widget.MiniArtModeButton.Content);

                // 2. ASCII mode with companion disabled
                widget.UpdateDisplay(
                    isTracking: true,
                    timerText: "00:10:00",
                    statusText: "RECORDING",
                    statusColor: Brushes.Green,
                    projectName: "Work",
                    activity: "Dev",
                    isCountDownMode: false,
                    hasGoal: false,
                    goalProgressPercentage: 0,
                    goalStatsText: "",
                    artMode: "ascii",
                    asciiArtText: "ASCII_ART_TEST",
                    isCompanionVisible: false);

                Assert.Equal(Visibility.Collapsed, widget.MiniVisualBorder.Visibility);
                Assert.Equal(Visibility.Collapsed, widget.MiniAsciiBorder.Visibility);
                Assert.Equal(Visibility.Collapsed, widget.MiniArtModeButton.Visibility);
            }
            finally
            {
                widget.Close();
            }
        });
    }

    [Fact]
    public void MainWindow_CompanionQuickToggleButton_TogglesMiniWidgetCompanionVisibility()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                var initialSettings = new WorkRestSettings
                {
                    CompanionVisible = true,
                    MiniWidgetEnabled = true
                };
                FocusCompanionStorage.Save(initialSettings);

                var mainWindow = new MainWindow();
                try
                {
                    // Mini overlay button initializes mini widget
                    mainWindow.MiniOverlayQuickButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                    // Initial state: Companion is ON
                    Assert.Equal(Visibility.Visible, mainWindow.CompanionControl.Visibility);
                    Assert.Equal("🎨 Companion: ON", mainWindow.CompanionQuickToggleButton.Content);

                    // Toggle companion OFF via MainWindow quick button
                    mainWindow.CompanionQuickToggleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                    Assert.Equal(Visibility.Collapsed, mainWindow.CompanionControl.Visibility);
                    Assert.Equal("🎨 Companion: OFF", mainWindow.CompanionQuickToggleButton.Content);

                    // Toggle companion ON again
                    mainWindow.CompanionQuickToggleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                    Assert.Equal(Visibility.Visible, mainWindow.CompanionControl.Visibility);
                    Assert.Equal("🎨 Companion: ON", mainWindow.CompanionQuickToggleButton.Content);
                }
                finally
                {
                    mainWindow.Close();
                }
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void MiniTimerWidget_WidgetSize_AppliesCorrectScaleAndFiresEvent()
    {
        RunInSta(() =>
        {
            var widget = new MiniTimerWidget();
            try
            {
                string? changedSize = null;
                widget.WidgetSizeChanged += s => changedSize = s;

                // 1. Default is Medium (100% / scale 1.0)
                Assert.Equal("Medium", widget.WidgetSize);
                Assert.Equal(1.0, widget.WidgetScale);
                Assert.Equal(1.0, widget.WidgetScaleTransform.ScaleX);
                Assert.Equal(1.0, widget.WidgetScaleTransform.ScaleY);

                // 2. Set to Small (85% / scale 0.85)
                widget.SetWidgetSize("Small");
                Assert.Equal("Small", widget.WidgetSize);
                Assert.Equal(0.85, widget.WidgetScale);
                Assert.Equal(0.85, widget.WidgetScaleTransform.ScaleX);
                Assert.Equal(0.85, widget.WidgetScaleTransform.ScaleY);
                Assert.True(widget.MenuSizeCompact.IsChecked);
                Assert.False(widget.MenuSizeStandard.IsChecked);

                // 3. Set to Large (115% / scale 1.15)
                widget.SetWidgetSize("Large");
                Assert.Equal("Large", widget.WidgetSize);
                Assert.Equal(1.15, widget.WidgetScale);
                Assert.Equal(1.15, widget.WidgetScaleTransform.ScaleX);
                Assert.Equal(1.15, widget.WidgetScaleTransform.ScaleY);
                Assert.True(widget.MenuSizeLarge.IsChecked);
                Assert.False(widget.MenuSizeCompact.IsChecked);

                // 4. Set to ExtraLarge (130% / scale 1.30)
                widget.SetWidgetSize("ExtraLarge");
                Assert.Equal("ExtraLarge", widget.WidgetSize);
                Assert.Equal(1.30, widget.WidgetScale);
                Assert.Equal(1.30, widget.WidgetScaleTransform.ScaleX);
                Assert.Equal(1.30, widget.WidgetScaleTransform.ScaleY);
                Assert.True(widget.MenuSizeExtraLarge.IsChecked);
            }
            finally
            {
                widget.Close();
            }
        });
    }

    [Fact]
    public void MiniTimerWidget_SizeButton_CyclesThroughSizes()
    {
        RunInSta(() =>
        {
            var widget = new MiniTimerWidget();
            try
            {
                string? latestSize = null;
                widget.WidgetSizeChanged += s => latestSize = s;

                // Initial Medium -> Click -> Large
                widget.MiniSizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal("Large", widget.WidgetSize);
                Assert.Equal("Large", latestSize);
                Assert.Equal(1.15, widget.WidgetScale);

                // Large -> Click -> ExtraLarge
                widget.MiniSizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal("ExtraLarge", widget.WidgetSize);
                Assert.Equal("ExtraLarge", latestSize);
                Assert.Equal(1.30, widget.WidgetScale);

                // ExtraLarge -> Click -> Small
                widget.MiniSizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal("Small", widget.WidgetSize);
                Assert.Equal("Small", latestSize);
                Assert.Equal(0.85, widget.WidgetScale);

                // Small -> Click -> Medium
                widget.MiniSizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal("Medium", widget.WidgetSize);
                Assert.Equal("Medium", latestSize);
                Assert.Equal(1.0, widget.WidgetScale);
            }
            finally
            {
                widget.Close();
            }
        });
    }

    [Fact]
    public void WorkRestSettingsDialog_LoadsAndSaves_MiniWidgetSize()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                var initialSettings = new WorkRestSettings
                {
                    MiniWidgetSize = "Large"
                };

                var dialog = new WorkRestSettingsDialog(initialSettings);
                try
                {
                    // Check loaded ComboBox selection (Large is index 2)
                    Assert.Equal(2, dialog.MiniSizeComboBox.SelectedIndex);

                    // Change selection to Compact (index 0)
                    dialog.MiniSizeComboBox.SelectedIndex = 0;

                    // Trigger save
                    dialog.SaveButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                    Assert.Equal("Small", dialog.Settings.MiniWidgetSize);
                }
                finally
                {
                    dialog.Close();
                }
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void WorkRestSettingsDialog_MiniWidgetSize_DetectsUnsavedChanges()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                var initialSettings = new WorkRestSettings
                {
                    MiniWidgetSize = "Medium"
                };

                var dialog = new WorkRestSettingsDialog(initialSettings);
                try
                {
                    Assert.False(dialog.HasUnsavedChanges());

                    // Changing size marks unsaved changes
                    dialog.MiniSizeComboBox.SelectedIndex = 2; // Large
                    Assert.True(dialog.HasUnsavedChanges());

                    // Reverting size clears unsaved changes
                    dialog.MiniSizeComboBox.SelectedIndex = 1; // Medium
                    Assert.False(dialog.HasUnsavedChanges());
                }
                finally
                {
                    dialog.Close();
                }
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void FocusCompanionStorage_SavesAndLoads_MiniWidgetSizeAndAliases()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            // 1. Direct save and load
            var settings = new WorkRestSettings
            {
                MiniWidgetSize = "ExtraLarge"
            };
            FocusCompanionStorage.Save(settings);

            var loaded = FocusCompanionStorage.Load();
            Assert.Equal("ExtraLarge", loaded.MiniWidgetSize);

            // 2. Load from JSON alias "widget_size"
            var jsonPath = Path.Combine(dataDir, "focus-settings.json");
            File.WriteAllText(jsonPath, "{\n  \"widget_size\": \"Small\"\n}");

            var loadedAlias = FocusCompanionStorage.Load();
            Assert.Equal("Small", loadedAlias.MiniWidgetSize);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void SettingsPersistence_AddingExtraSetting_DoesNotLoseUserExistingSettings()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            // Simulate an existing pre-update settings JSON file with custom user settings and NO "miniWidgetSize" property
            var jsonPath = Path.Combine(dataDir, "focus-settings.json");
            var preUpdateJson = """
            {
              "intervalModeEnabled": true,
              "focusMinutes": 50,
              "shortBreakMinutes": 10,
              "longBreakMinutes": 20,
              "cyclesBeforeLongBreak": 3,
              "soundAlertEnabled": false,
              "soundVolume": 42,
              "goalMilestoneSoundVolume": 65,
              "userName": "Vitor Barbosa",
              "welcomePopupEnabled": false,
              "completionRewardIcon": "Dinosaur",
              "completionRewardColor": "Emerald",
              "selectedSceneId": "coffee_jazz",
              "artMode": "ascii",
              "companionVisible": false,
              "companionExpanded": false,
              "miniWidgetCorner": "TopLeft",
              "miniWidgetEnabled": false,
              "goalNotificationsEnabled": false,
              "dailyGoalNotificationEnabled": false,
              "weeklyGoalNotificationEnabled": false,
              "projectGoalNotificationEnabled": false,
              "milestoneRemindersEnabled": false,
              "milestoneDisplayMode": "AllUpcoming"
            }
            """;
            File.WriteAllText(jsonPath, preUpdateJson);

            // Load settings via FocusCompanionStorage
            var loaded = FocusCompanionStorage.Load();

            // Assert that all user settings are intact and not lost or reset to defaults
            Assert.True(loaded.IntervalModeEnabled);
            Assert.Equal(50, loaded.FocusMinutes);
            Assert.Equal(10, loaded.ShortBreakMinutes);
            Assert.Equal(20, loaded.LongBreakMinutes);
            Assert.Equal(3, loaded.CyclesBeforeLongBreak);
            Assert.False(loaded.SoundAlertEnabled);
            Assert.Equal(42, loaded.SoundVolume);
            Assert.Equal(65, loaded.GoalMilestoneSoundVolume);
            Assert.Equal("Vitor Barbosa", loaded.UserName);
            Assert.False(loaded.WelcomePopupEnabled);
            Assert.Equal("Dinosaur", loaded.CompletionRewardIcon);
            Assert.Equal("Emerald", loaded.CompletionRewardColor);
            Assert.Equal("coffee_jazz", loaded.SelectedSceneId);
            Assert.Equal("ascii", loaded.ArtMode);
            Assert.False(loaded.CompanionVisible);
            Assert.False(loaded.CompanionExpanded);
            Assert.Equal("TopLeft", loaded.MiniWidgetCorner);
            Assert.False(loaded.MiniWidgetEnabled);
            Assert.False(loaded.GoalNotificationsEnabled);
            Assert.False(loaded.DailyGoalNotificationEnabled);
            Assert.False(loaded.WeeklyGoalNotificationEnabled);
            Assert.False(loaded.ProjectGoalNotificationEnabled);
            Assert.False(loaded.MilestoneRemindersEnabled);
            Assert.Equal("AllUpcoming", loaded.MilestoneDisplayMode);

            // New setting has default value
            Assert.Equal("Medium", loaded.MiniWidgetSize);

            // Now open in WorkRestSettingsDialog and save to confirm roundtrip does not lose settings
            RunInSta(() =>
            {
                var dialog = new WorkRestSettingsDialog(loaded);
                try
                {
                    dialog.SaveSettingsToModel();
                    FocusCompanionStorage.Save(dialog.Settings);
                }
                finally
                {
                    dialog.Close();
                }
            });

            var reloaded = FocusCompanionStorage.Load();
            Assert.True(reloaded.IntervalModeEnabled);
            Assert.Equal(50, reloaded.FocusMinutes);
            Assert.Equal(10, reloaded.ShortBreakMinutes);
            Assert.Equal(20, reloaded.LongBreakMinutes);
            Assert.Equal(3, reloaded.CyclesBeforeLongBreak);
            Assert.False(reloaded.SoundAlertEnabled);
            Assert.Equal(42, reloaded.SoundVolume);
            Assert.Equal(65, reloaded.GoalMilestoneSoundVolume);
            Assert.Equal("Vitor Barbosa", reloaded.UserName);
            Assert.False(reloaded.WelcomePopupEnabled);
            Assert.Equal("Dinosaur", reloaded.CompletionRewardIcon);
            Assert.Equal("Emerald", reloaded.CompletionRewardColor);
            Assert.Equal("coffee_jazz", reloaded.SelectedSceneId);
            Assert.Equal("ascii", reloaded.ArtMode);
            Assert.False(reloaded.CompanionVisible);
            Assert.False(reloaded.CompanionExpanded);
            Assert.Equal("TopLeft", reloaded.MiniWidgetCorner);
            Assert.False(reloaded.MiniWidgetEnabled);
            Assert.False(reloaded.GoalNotificationsEnabled);
            Assert.False(reloaded.DailyGoalNotificationEnabled);
            Assert.False(reloaded.WeeklyGoalNotificationEnabled);
            Assert.False(reloaded.ProjectGoalNotificationEnabled);
            Assert.False(reloaded.MilestoneRemindersEnabled);
            Assert.Equal("AllUpcoming", reloaded.MilestoneDisplayMode);
            Assert.Equal("Medium", reloaded.MiniWidgetSize);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void WorkRestSettings_Clone_DeepCopiesAllProperties()
    {
        var original = new WorkRestSettings
        {
            IntervalModeEnabled = true,
            FocusMinutes = 45,
            ShortBreakMinutes = 8,
            LongBreakMinutes = 25,
            CyclesBeforeLongBreak = 5,
            SoundAlertEnabled = false,
            SoundVolume = 30,
            GoalMilestoneSoundVolume = 40,
            SoundOnStartEnabled = false,
            SoundOnEndEnabled = false,
            SoundOnGoalAchievedEnabled = false,
            SoundOnDailyGoalAchievedEnabled = false,
            SoundOnWeeklyGoalAchievedEnabled = false,
            SoundOnBreakStartEnabled = false,
            SoundOnBreakEndEnabled = false,
            SoundOnCheerEnabled = false,
            AutoAdvancePhases = true,
            SelectedSceneId = "lumberjack",
            ArtMode = "ascii",
            CompanionVisible = false,
            CompanionExpanded = false,
            MiniWidgetCorner = "BottomLeft",
            MiniWidgetTargetMonitor = "Display_2",
            MiniWidgetMonitorIndex = 1,
            MiniWidgetEnabled = false,
            MiniWidgetSize = "ExtraLarge",
            UserName = "CloneTestUser",
            WelcomePopupEnabled = false,
            LastWelcomePopupDate = new DateTime(2026, 11, 5),
            ForceWelcomePopupOnNextLaunch = true,
            FocusXp = 250,
            FocusStreak = 7,
            TotalSessionsCompleted = 14,
            CompletionRewardIcon = "Rocket",
            CompletionRewardColor = "Coral",
            GoalNotificationsEnabled = false,
            DailyGoalNotificationEnabled = false,
            WeeklyGoalNotificationEnabled = false,
            ProjectGoalNotificationEnabled = false,
            MilestoneRemindersEnabled = false,
            MilestoneDisplayMode = "AllUpcoming"
        };

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.NotSame(original.EnabledSoundVariations, clone.EnabledSoundVariations);
        Assert.Equal(original.IntervalModeEnabled, clone.IntervalModeEnabled);
        Assert.Equal(original.FocusMinutes, clone.FocusMinutes);
        Assert.Equal(original.ShortBreakMinutes, clone.ShortBreakMinutes);
        Assert.Equal(original.LongBreakMinutes, clone.LongBreakMinutes);
        Assert.Equal(original.CyclesBeforeLongBreak, clone.CyclesBeforeLongBreak);
        Assert.Equal(original.SoundAlertEnabled, clone.SoundAlertEnabled);
        Assert.Equal(original.SoundVolume, clone.SoundVolume);
        Assert.Equal(original.GoalMilestoneSoundVolume, clone.GoalMilestoneSoundVolume);
        Assert.Equal(original.UserName, clone.UserName);
        Assert.Equal(original.MiniWidgetSize, clone.MiniWidgetSize);
        Assert.Equal(original.SelectedSceneId, clone.SelectedSceneId);
        Assert.Equal(original.CompletionRewardIcon, clone.CompletionRewardIcon);
        Assert.Equal(original.CompletionRewardColor, clone.CompletionRewardColor);
        Assert.Equal(original.LastWelcomePopupDate, clone.LastWelcomePopupDate);
        Assert.Equal(original.FocusXp, clone.FocusXp);
    }

    [Fact]
    public void SettingsPersistence_LegacyDirectoriesDoNotOverwriteActivePrimarySettings()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            var legacyDir = Path.Combine(tempDir, "legacy_install");
            Directory.CreateDirectory(legacyDir);
            StoragePathHelper.CustomLegacyDirectoriesForTesting = new List<string> { legacyDir };
            StoragePathHelper.EnableLegacyMigrationInTest = true;

            // 1. Create primary focus-settings.json with user's active choice
            var primarySettings = new WorkRestSettings
            {
                FocusMinutes = 25, // User chose standard 25 mins
                SoundAlertEnabled = true, // User chose sound ON
                SelectedSceneId = "cycling",
                UserName = "ActiveUser"
            };
            FocusCompanionStorage.Save(primarySettings);

            // 2. Create old legacy file with different old settings
            var legacySettings = new WorkRestSettings
            {
                FocusMinutes = 45, // Old 45 mins
                SoundAlertEnabled = false, // Old sound OFF
                SelectedSceneId = "lumberjack",
                UserName = "OldUser"
            };
            File.WriteAllText(Path.Combine(legacyDir, "focus-settings.json"), System.Text.Json.JsonSerializer.Serialize(legacySettings));

            // 3. Load settings via FocusCompanionStorage
            var loaded = FocusCompanionStorage.Load();

            // 4. Primary settings MUST NOT be overwritten by legacy settings
            Assert.Equal(25, loaded.FocusMinutes);
            Assert.True(loaded.SoundAlertEnabled);
            Assert.Equal("cycling", loaded.SelectedSceneId);
            Assert.Equal("ActiveUser", loaded.UserName);
        }
        finally
        {
            StoragePathHelper.CustomLegacyDirectoriesForTesting = null;
            StoragePathHelper.EnableLegacyMigrationInTest = false;
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void StreakStorage_LegacyDirectoriesDoNotOverwriteActivePrimaryStreaks()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            var legacyDir = Path.Combine(tempDir, "legacy_streaks");
            Directory.CreateDirectory(legacyDir);
            StoragePathHelper.CustomLegacyDirectoriesForTesting = new List<string> { legacyDir };
            StoragePathHelper.EnableLegacyMigrationInTest = true;

            // 1. Primary streaks file has Enabled = false
            var primaryProfile = new StreakProfile
            {
                Enabled = false,
                ExcludedDays = new List<DayOfWeek> { DayOfWeek.Sunday }
            };
            StreakStorage.Instance.Save(primaryProfile);

            // 2. Legacy file has Enabled = true
            var legacyProfile = new StreakProfile
            {
                Enabled = true,
                ExcludedDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }
            };
            File.WriteAllText(Path.Combine(legacyDir, "streaks.json"), System.Text.Json.JsonSerializer.Serialize(legacyProfile));

            // 3. Load via StreakStorage
            var loaded = StreakStorage.Instance.Load();

            // 4. Primary profile must be preserved
            Assert.False(loaded.Enabled);
            Assert.Single(loaded.ExcludedDays);
            Assert.Contains(DayOfWeek.Sunday, loaded.ExcludedDays);
        }
        finally
        {
            StoragePathHelper.CustomLegacyDirectoriesForTesting = null;
            StoragePathHelper.EnableLegacyMigrationInTest = false;
            CleanupSandbox(tempDir);
        }
    }
}
