using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

[Collection("StorageTests")]
public class FeatureTutorialTests
{
    private static (string tempDir, string dataDir) SetupSandbox()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "TutorialTests_" + Guid.NewGuid().ToString("N"));
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
    public void WorkRestSettings_DefaultState_RequiresAllScreenTutorials()
    {
        var settings = new WorkRestSettings();

        Assert.False(settings.HasSeenTutorial);
        Assert.False(settings.HasSeenMainWindowTutorial);
        Assert.False(settings.HasSeenPlannerTutorial);
        Assert.False(settings.HasSeenAnalyticsTutorial);
        Assert.False(settings.HasSeenStreakTutorial);
        Assert.False(settings.ForceTutorialOnNextLaunch);

        Assert.True(settings.ShouldShowTutorial(TutorialScreen.MainWindow));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Planner));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Analytics));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Streak));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.General));
    }

    [Fact]
    public void WorkRestSettings_MarkTutorialShown_MarksSpecificScreenIndependently()
    {
        var settings = new WorkRestSettings();

        // Mark only Main Window
        settings.MarkTutorialShown(TutorialScreen.MainWindow);
        Assert.True(settings.HasSeenMainWindowTutorial);
        Assert.True(settings.HasSeenTutorial);
        Assert.False(settings.ShouldShowTutorial(TutorialScreen.MainWindow));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Planner));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Analytics));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Streak));

        // Mark Planner
        settings.MarkTutorialShown(TutorialScreen.Planner);
        Assert.True(settings.HasSeenPlannerTutorial);
        Assert.False(settings.ShouldShowTutorial(TutorialScreen.Planner));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Analytics));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Streak));

        // Mark Analytics
        settings.MarkTutorialShown(TutorialScreen.Analytics);
        Assert.True(settings.HasSeenAnalyticsTutorial);
        Assert.False(settings.ShouldShowTutorial(TutorialScreen.Analytics));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Streak));

        // Mark Streak
        settings.MarkTutorialShown(TutorialScreen.Streak);
        Assert.True(settings.HasSeenStreakTutorial);
        Assert.False(settings.ShouldShowTutorial(TutorialScreen.Streak));
    }

    [Fact]
    public void WorkRestSettings_ForceTutorialOnNextLaunch_OverridesHasSeenTutorial()
    {
        var settings = new WorkRestSettings
        {
            HasSeenTutorial = true,
            HasSeenMainWindowTutorial = true,
            HasSeenPlannerTutorial = true,
            HasSeenAnalyticsTutorial = true,
            HasSeenStreakTutorial = true,
            ForceTutorialOnNextLaunch = true
        };

        Assert.True(settings.ShouldShowTutorial(TutorialScreen.MainWindow));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Planner));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Analytics));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Streak));

        settings.MarkTutorialShown(TutorialScreen.MainWindow);
        Assert.False(settings.ForceTutorialOnNextLaunch);
        Assert.False(settings.ShouldShowTutorial(TutorialScreen.MainWindow));
    }

    [Fact]
    public void WorkRestSettings_ResetTutorial_ReEnablesPopup()
    {
        var settings = new WorkRestSettings();
        settings.MarkTutorialShown(TutorialScreen.General);
        Assert.False(settings.ShouldShowTutorial(TutorialScreen.MainWindow));
        Assert.False(settings.ShouldShowTutorial(TutorialScreen.Planner));
        Assert.False(settings.ShouldShowTutorial(TutorialScreen.Analytics));
        Assert.False(settings.ShouldShowTutorial(TutorialScreen.Streak));

        settings.ResetTutorial();

        Assert.False(settings.HasSeenTutorial);
        Assert.False(settings.HasSeenMainWindowTutorial);
        Assert.False(settings.HasSeenPlannerTutorial);
        Assert.False(settings.HasSeenAnalyticsTutorial);
        Assert.False(settings.HasSeenStreakTutorial);
        Assert.True(settings.ForceTutorialOnNextLaunch);

        Assert.True(settings.ShouldShowTutorial(TutorialScreen.MainWindow));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Planner));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Analytics));
        Assert.True(settings.ShouldShowTutorial(TutorialScreen.Streak));
    }

    [Fact]
    public void WorkRestSettings_Clone_PreservesAllTutorialFlags()
    {
        var original = new WorkRestSettings
        {
            HasSeenTutorial = true,
            HasSeenMainWindowTutorial = true,
            HasSeenPlannerTutorial = false,
            HasSeenAnalyticsTutorial = true,
            HasSeenStreakTutorial = false,
            ForceTutorialOnNextLaunch = true
        };

        var clone = original.Clone();

        Assert.True(clone.HasSeenTutorial);
        Assert.True(clone.HasSeenMainWindowTutorial);
        Assert.False(clone.HasSeenPlannerTutorial);
        Assert.True(clone.HasSeenAnalyticsTutorial);
        Assert.False(clone.HasSeenStreakTutorial);
        Assert.True(clone.ForceTutorialOnNextLaunch);
    }

    [Fact]
    public void FocusCompanionStorage_DeserializeSettings_HandlesPerScreenJsonAndAliases()
    {
        // 1. Standard JSON property names
        string json1 = """
        {
            "hasSeenTutorial": true,
            "hasSeenMainWindowTutorial": true,
            "hasSeenPlannerTutorial": true,
            "hasSeenAnalyticsTutorial": false,
            "hasSeenStreakTutorial": true,
            "forceTutorialOnNextLaunch": false
        }
        """;
        var s1 = FocusCompanionStorage.DeserializeSettings(json1);
        Assert.True(s1.HasSeenTutorial);
        Assert.True(s1.HasSeenMainWindowTutorial);
        Assert.True(s1.HasSeenPlannerTutorial);
        Assert.False(s1.HasSeenAnalyticsTutorial);
        Assert.True(s1.HasSeenStreakTutorial);
        Assert.False(s1.ForceTutorialOnNextLaunch);

        // 2. Snake_case aliases
        string json2 = """
        {
            "has_seen_main_window_tutorial": true,
            "has_seen_planner_tutorial": true,
            "has_seen_analytics_tutorial": true,
            "has_seen_streak_tutorial": true,
            "force_tutorial_on_next_launch": true
        }
        """;
        var s2 = FocusCompanionStorage.DeserializeSettings(json2);
        Assert.True(s2.HasSeenMainWindowTutorial);
        Assert.True(s2.HasSeenPlannerTutorial);
        Assert.True(s2.HasSeenAnalyticsTutorial);
        Assert.True(s2.HasSeenStreakTutorial);
        Assert.True(s2.ForceTutorialOnNextLaunch);

        // 3. Alternate aliases (calendar, dashboard, reports, streaks)
        string json3 = """
        {
            "hasSeenDashboardTutorial": true,
            "hasSeenCalendarTutorial": true,
            "hasSeenReportsTutorial": true,
            "hasSeenStreaksTutorial": true
        }
        """;
        var s3 = FocusCompanionStorage.DeserializeSettings(json3);
        Assert.True(s3.HasSeenMainWindowTutorial);
        Assert.True(s3.HasSeenPlannerTutorial);
        Assert.True(s3.HasSeenAnalyticsTutorial);
        Assert.True(s3.HasSeenStreakTutorial);
    }

    [Fact]
    public void FocusCompanionStorage_SaveAndLoad_PreservesPerScreenTutorialState()
    {
        var (tempDir, _) = SetupSandbox();
        try
        {
            var settings = new WorkRestSettings
            {
                HasSeenTutorial = true,
                HasSeenMainWindowTutorial = true,
                HasSeenPlannerTutorial = true,
                HasSeenAnalyticsTutorial = false,
                HasSeenStreakTutorial = true,
                ForceTutorialOnNextLaunch = false,
                UserName = "Vitor"
            };

            FocusCompanionStorage.Save(settings);

            var loaded = FocusCompanionStorage.Load();
            Assert.True(loaded.HasSeenTutorial);
            Assert.True(loaded.HasSeenMainWindowTutorial);
            Assert.True(loaded.HasSeenPlannerTutorial);
            Assert.False(loaded.HasSeenAnalyticsTutorial);
            Assert.True(loaded.HasSeenStreakTutorial);
            Assert.False(loaded.ForceTutorialOnNextLaunch);
            Assert.Equal("Vitor", loaded.UserName);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void TutorialDialog_STA_InitialState_ShowsPage0AndCorrectControls()
    {
        RunInSta(() =>
        {
            var dialog = new TutorialDialog(TutorialScreen.General);

            Assert.Equal(TutorialScreen.General, dialog.ScreenMode);
            Assert.Equal(0, dialog.CurrentPageIndex);
            Assert.Equal("Step 1 of 5", dialog.StepIndicatorTextBlock.Text);
            Assert.False(dialog.PreviousButton.IsEnabled);
            Assert.Equal(Visibility.Visible, dialog.NextButton.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.FinishButton.Visibility);
            Assert.Equal(Visibility.Visible, dialog.Page0.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.Page1.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.Page2.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.Page3.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.Page4.Visibility);
            Assert.Equal(Visibility.Visible, dialog.DotsPanel.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.ViewFullTourButton.Visibility);
            Assert.Equal("●", dialog.Dot0.Content);
            Assert.Equal("○", dialog.Dot1.Content);
        });
    }

    [Fact]
    public void TutorialDialog_STA_NextPageAndPreviousPage_NavigatesThroughAll5Steps()
    {
        RunInSta(() =>
        {
            var dialog = new TutorialDialog(TutorialScreen.General);

            // Page 0 -> Page 1
            dialog.NextPage();
            Assert.Equal(1, dialog.CurrentPageIndex);
            Assert.Equal("Step 2 of 5", dialog.StepIndicatorTextBlock.Text);
            Assert.True(dialog.PreviousButton.IsEnabled);
            Assert.Equal(Visibility.Visible, dialog.NextButton.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.FinishButton.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.Page0.Visibility);
            Assert.Equal(Visibility.Visible, dialog.Page1.Visibility);

            // Page 1 -> Page 2
            dialog.NextPage();
            Assert.Equal(2, dialog.CurrentPageIndex);
            Assert.Equal("Step 3 of 5", dialog.StepIndicatorTextBlock.Text);
            Assert.Equal(Visibility.Visible, dialog.Page2.Visibility);

            // Page 2 -> Page 3
            dialog.NextPage();
            Assert.Equal(3, dialog.CurrentPageIndex);
            Assert.Equal("Step 4 of 5", dialog.StepIndicatorTextBlock.Text);
            Assert.Equal(Visibility.Visible, dialog.Page3.Visibility);

            // Page 3 -> Page 4 (Final step)
            dialog.NextPage();
            Assert.Equal(4, dialog.CurrentPageIndex);
            Assert.Equal("Step 5 of 5", dialog.StepIndicatorTextBlock.Text);
            Assert.True(dialog.PreviousButton.IsEnabled);
            Assert.Equal(Visibility.Collapsed, dialog.NextButton.Visibility);
            Assert.Equal(Visibility.Visible, dialog.FinishButton.Visibility);
            Assert.Equal(Visibility.Visible, dialog.Page4.Visibility);

            // Attempting NextPage at step 4 stays at step 4
            dialog.NextPage();
            Assert.Equal(4, dialog.CurrentPageIndex);

            // Step 4 -> Step 3
            dialog.PreviousPage();
            Assert.Equal(3, dialog.CurrentPageIndex);
            Assert.Equal(Visibility.Visible, dialog.NextButton.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.FinishButton.Visibility);
        });
    }

    [Fact]
    public void TutorialDialog_STA_GoToPage_JumpsDirectlyToPage()
    {
        RunInSta(() =>
        {
            var dialog = new TutorialDialog(TutorialScreen.General);

            dialog.GoToPage(3);
            Assert.Equal(3, dialog.CurrentPageIndex);
            Assert.Equal("Step 4 of 5", dialog.StepIndicatorTextBlock.Text);
            Assert.Equal(Visibility.Visible, dialog.Page3.Visibility);

            dialog.GoToPage(0);
            Assert.Equal(0, dialog.CurrentPageIndex);
            Assert.Equal(Visibility.Visible, dialog.Page0.Visibility);

            // Out-of-bounds index should be ignored
            dialog.GoToPage(-1);
            Assert.Equal(0, dialog.CurrentPageIndex);

            dialog.GoToPage(10);
            Assert.Equal(0, dialog.CurrentPageIndex);
        });
    }

    [Fact]
    public void TutorialDialog_STA_MainWindowMode_ShowsDedicatedGuide()
    {
        RunInSta(() =>
        {
            var dialog = new TutorialDialog(TutorialScreen.MainWindow);

            Assert.Equal(TutorialScreen.MainWindow, dialog.ScreenMode);
            Assert.Equal("Dashboard Guide", dialog.StepIndicatorTextBlock.Text);
            Assert.Equal(Visibility.Visible, dialog.PageMainWindow.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.Page0.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.DotsPanel.Visibility);
            Assert.Equal(Visibility.Visible, dialog.ViewFullTourButton.Visibility);
            Assert.Equal(Visibility.Visible, dialog.FinishButton.Visibility);
            Assert.Equal("✓ Got It!", dialog.FinishButton.Content);
        });
    }

    [Fact]
    public void TutorialDialog_STA_PlannerMode_ShowsDedicatedGuide()
    {
        RunInSta(() =>
        {
            var dialog = new TutorialDialog(TutorialScreen.Planner);

            Assert.Equal(TutorialScreen.Planner, dialog.ScreenMode);
            Assert.Equal("Planner Guide", dialog.StepIndicatorTextBlock.Text);
            Assert.Equal(Visibility.Visible, dialog.PagePlanner.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.Page0.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.DotsPanel.Visibility);
            Assert.Equal(Visibility.Visible, dialog.ViewFullTourButton.Visibility);
            Assert.Equal("✓ Got It!", dialog.FinishButton.Content);
        });
    }

    [Fact]
    public void TutorialDialog_STA_AnalyticsMode_ShowsDedicatedGuide()
    {
        RunInSta(() =>
        {
            var dialog = new TutorialDialog(TutorialScreen.Analytics);

            Assert.Equal(TutorialScreen.Analytics, dialog.ScreenMode);
            Assert.Equal("Analytics Guide", dialog.StepIndicatorTextBlock.Text);
            Assert.Equal(Visibility.Visible, dialog.PageAnalytics.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.Page0.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.DotsPanel.Visibility);
            Assert.Equal(Visibility.Visible, dialog.ViewFullTourButton.Visibility);
            Assert.Equal("✓ Got It!", dialog.FinishButton.Content);
        });
    }

    [Fact]
    public void TutorialDialog_STA_StreakMode_ShowsDedicatedGuide()
    {
        RunInSta(() =>
        {
            var dialog = new TutorialDialog(TutorialScreen.Streak);

            Assert.Equal(TutorialScreen.Streak, dialog.ScreenMode);
            Assert.Equal("Streak Guide", dialog.StepIndicatorTextBlock.Text);
            Assert.Equal(Visibility.Visible, dialog.PageStreak.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.Page0.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.DotsPanel.Visibility);
            Assert.Equal(Visibility.Visible, dialog.ViewFullTourButton.Visibility);
            Assert.Equal("✓ Got It!", dialog.FinishButton.Content);
        });
    }

    [Fact]
    public void TutorialDialog_STA_SwitchToFullTour_TransitionsToCarousel()
    {
        RunInSta(() =>
        {
            var dialog = new TutorialDialog(TutorialScreen.Planner);
            Assert.Equal(TutorialScreen.Planner, dialog.ScreenMode);
            Assert.Equal(Visibility.Visible, dialog.PagePlanner.Visibility);

            dialog.SwitchToFullTour(3);

            Assert.Equal(TutorialScreen.General, dialog.ScreenMode);
            Assert.Equal(3, dialog.CurrentPageIndex);
            Assert.Equal(Visibility.Visible, dialog.Page3.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.PagePlanner.Visibility);
            Assert.Equal(Visibility.Visible, dialog.DotsPanel.Visibility);
            Assert.Equal(Visibility.Collapsed, dialog.ViewFullTourButton.Visibility);
        });
    }

    [Fact]
    public void WorkRestSettingsDialog_STA_InitializesTutorialButtons()
    {
        RunInSta(() =>
        {
            var settings = new WorkRestSettings
            {
                ForceTutorialOnNextLaunch = false,
                HasSeenTutorial = true
            };

            var dialog = new WorkRestSettingsDialog(settings);

            Assert.NotNull(dialog.ShowTutorialNowButton);
            Assert.NotNull(dialog.ShowMainGuideButton);
            Assert.NotNull(dialog.ShowPlannerGuideButton);
            Assert.NotNull(dialog.ShowAnalyticsGuideButton);
            Assert.NotNull(dialog.ShowStreakGuideButton);
            Assert.NotNull(dialog.ResetAllTutorialsButton);
            Assert.NotNull(dialog.ForceTutorialNextLaunchButton);
            Assert.Equal("Show Tutorials on next launch", dialog.ForceTutorialNextLaunchButton.Content);
        });
    }

    [Fact]
    public void MainWindow_STA_GuideButtonHasIconOnlyContent()
    {
        RunInSta(() =>
        {
            var (tempDir, _) = SetupSandbox();
            try
            {
                var window = new MainWindow();
                Assert.NotNull(window.MainWindowGuideButton);
                Assert.Equal("🎓", window.MainWindowGuideButton.Content);
                Assert.True(window.MainWindowGuideButton.ToolTip?.ToString()?.Contains("Guide"));
            }
            finally
            {
                CleanupSandbox(tempDir);
            }
        });
    }

    [Fact]
    public void StreakDialog_STA_GuideButtonLocatedRightOfActiveToggle()
    {
        RunInSta(() =>
        {
            var (tempDir, _) = SetupSandbox();
            try
            {
                FocusCompanionStorage.Save(new WorkRestSettings
                {
                    HasSeenTutorial = true,
                    HasSeenStreakTutorial = true
                });

                var dialog = new StreakDialog();
                Assert.NotNull(dialog.StreakGuideButton);
                Assert.Equal("🎓", dialog.StreakGuideButton.Content);
                Assert.True(dialog.StreakGuideButton.ToolTip?.ToString()?.Contains("Guide"));

                var parentPanel = dialog.StreakGuideButton.Parent as StackPanel;
                Assert.NotNull(parentPanel);
                Assert.Equal(Orientation.Horizontal, parentPanel.Orientation);

                var checkboxIndex = parentPanel.Children.IndexOf(dialog.StreakEnabledCheckBox);
                var buttonIndex = parentPanel.Children.IndexOf(dialog.StreakGuideButton);

                Assert.True(checkboxIndex >= 0);
                Assert.True(buttonIndex > checkboxIndex, "Guide button must be placed to the right of the Streak System Active checkbox");
            }
            finally
            {
                CleanupSandbox(tempDir);
            }
        });
    }
}
