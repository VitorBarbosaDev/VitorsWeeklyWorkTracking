using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

[Collection("StorageTests")]
public class SettingsUnsavedChangesTests
{
    private static (string tempDir, string dataDir) SetupSandbox()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SettingsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;
        var dataDir = StoragePathHelper.GetDataFolder();
        return (tempDir, dataDir);
    }

    private static void CleanupSandbox(string tempDir)
    {
        StoragePathHelper.OverrideAppDataFolder = null;
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
    public void WorkRestSettingsDialog_HasUnsavedChanges_ReturnsFalse_WhenNothingChanged()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                var initial = new WorkRestSettings
                {
                    FocusMinutes = 25,
                    ShortBreakMinutes = 5,
                    LongBreakMinutes = 15,
                    CyclesBeforeLongBreak = 4,
                    SoundVolume = 80,
                    UserName = "Vitor"
                };

                var dialog = new WorkRestSettingsDialog(initial);
                Assert.False(dialog.HasUnsavedChanges());
                dialog.Close();
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void WorkRestSettingsDialog_HasUnsavedChanges_TracksFocusAndBreakSliders()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                var initial = new WorkRestSettings { FocusMinutes = 25, ShortBreakMinutes = 5 };
                var dialog = new WorkRestSettingsDialog(initial);
                dialog.PromptUnsavedChangesHandler = () => MessageBoxResult.No;

                Assert.False(dialog.HasUnsavedChanges());

                // Change FocusMinutes slider
                dialog.FocusSlider.Value = 35;
                Assert.True(dialog.HasUnsavedChanges());

                // Revert FocusMinutes back
                dialog.FocusSlider.Value = 25;
                Assert.False(dialog.HasUnsavedChanges());

                // Change ShortBreak slider
                dialog.ShortBreakSlider.Value = 8;
                Assert.True(dialog.HasUnsavedChanges());

                dialog.Close();
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void WorkRestSettingsDialog_HasUnsavedChanges_TracksUserNameAndToggles()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                var initial = new WorkRestSettings
                {
                    UserName = "Alex",
                    IntervalModeEnabled = true,
                    SoundAlertEnabled = true
                };
                var dialog = new WorkRestSettingsDialog(initial);
                dialog.PromptUnsavedChangesHandler = () => MessageBoxResult.No;

                Assert.False(dialog.HasUnsavedChanges());

                // Change username
                dialog.UserNameTextBox.Text = "Alexandre";
                Assert.True(dialog.HasUnsavedChanges());

                dialog.UserNameTextBox.Text = "Alex";
                Assert.False(dialog.HasUnsavedChanges());

                // Toggle interval mode
                dialog.IntervalModeEnabledCheckBox.IsChecked = false;
                Assert.True(dialog.HasUnsavedChanges());

                dialog.IntervalModeEnabledCheckBox.IsChecked = true;
                Assert.False(dialog.HasUnsavedChanges());

                // Toggle sound alert
                dialog.SoundAlertCheckBox.IsChecked = false;
                Assert.True(dialog.HasUnsavedChanges());

                dialog.Close();
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void WorkRestSettingsDialog_HasUnsavedChanges_TracksSoundVariations()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                var initial = new WorkRestSettings
                {
                    EnabledSoundVariations = new List<string> { "start_ascending", "end_completion" }
                };
                var dialog = new WorkRestSettingsDialog(initial);
                dialog.PromptUnsavedChangesHandler = () => MessageBoxResult.No;

                Assert.False(dialog.HasUnsavedChanges());

                // Toggle variation
                if (dialog.Var_start_pop != null)
                {
                    bool origState = dialog.Var_start_pop.IsChecked == true;
                    dialog.Var_start_pop.IsChecked = !origState;
                    Assert.True(dialog.HasUnsavedChanges());

                    dialog.Var_start_pop.IsChecked = origState;
                    Assert.False(dialog.HasUnsavedChanges());
                }

                dialog.Close();
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void WorkRestSettingsDialog_HasUnsavedChanges_TracksStreakSettings()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                StreakService.Instance.Profile.Enabled = true;
                StreakService.Instance.Profile.ExcludedDays = new List<DayOfWeek> { DayOfWeek.Sunday };

                var initial = new WorkRestSettings();
                var dialog = new WorkRestSettingsDialog(initial);
                dialog.PromptUnsavedChangesHandler = () => MessageBoxResult.No;

                Assert.False(dialog.HasUnsavedChanges());

                // Exclude Saturday
                if (dialog.SettingsExcludeSatCheckBox != null)
                {
                    dialog.SettingsExcludeSatCheckBox.IsChecked = true;
                    Assert.True(dialog.HasUnsavedChanges());

                    dialog.SettingsExcludeSatCheckBox.IsChecked = false;
                    Assert.False(dialog.HasUnsavedChanges());
                }

                // Toggle streak enabled
                if (dialog.StreakSystemEnabledCheckBox != null)
                {
                    dialog.StreakSystemEnabledCheckBox.IsChecked = false;
                    Assert.True(dialog.HasUnsavedChanges());

                    dialog.StreakSystemEnabledCheckBox.IsChecked = true;
                    Assert.False(dialog.HasUnsavedChanges());
                }

                dialog.Close();
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void WorkRestSettingsDialog_OnClosing_WithUnsavedChanges_UserChoosesSave_SavesSettings()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                var initial = new WorkRestSettings { FocusMinutes = 25, UserName = "Original" };
                var dialog = new WorkRestSettingsDialog(initial);

                dialog.FocusSlider.Value = 45;
                dialog.UserNameTextBox.Text = "Updated User";
                Assert.True(dialog.HasUnsavedChanges());

                // Mock user answering "Yes" (Save)
                bool promptCalled = false;
                dialog.PromptUnsavedChangesHandler = () =>
                {
                    promptCalled = true;
                    return MessageBoxResult.Yes;
                };

                // Trigger close
                dialog.Close();

                Assert.True(promptCalled);
                Assert.Equal(45, dialog.Settings.FocusMinutes);
                Assert.Equal("Updated User", dialog.Settings.UserName);
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void WorkRestSettingsDialog_OnClosing_WithUnsavedChanges_UserChoosesDiscard_DoesNotSave()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                var initial = new WorkRestSettings { FocusMinutes = 25, UserName = "Original" };
                var dialog = new WorkRestSettingsDialog(initial);

                dialog.FocusSlider.Value = 55;
                dialog.UserNameTextBox.Text = "Changed User";
                Assert.True(dialog.HasUnsavedChanges());

                // Mock user answering "No" (Discard / Don't Save)
                bool promptCalled = false;
                dialog.PromptUnsavedChangesHandler = () =>
                {
                    promptCalled = true;
                    return MessageBoxResult.No;
                };

                dialog.Close();

                Assert.True(promptCalled);
                // Settings model was NOT updated
                Assert.Equal(25, dialog.Settings.FocusMinutes);
                Assert.Equal("Original", dialog.Settings.UserName);
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void WorkRestSettingsDialog_OnClosing_WithUnsavedChanges_UserChoosesCancel_AbortsClosing()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                var initial = new WorkRestSettings { FocusMinutes = 25 };
                var dialog = new WorkRestSettingsDialog(initial);

                dialog.FocusSlider.Value = 50;
                Assert.True(dialog.HasUnsavedChanges());

                // Mock user answering "Cancel" (Keep Editing)
                bool promptCalled = false;
                dialog.PromptUnsavedChangesHandler = () =>
                {
                    promptCalled = true;
                    return MessageBoxResult.Cancel;
                };

                dialog.Close();

                Assert.True(promptCalled);
                // Window remains not explicitly closed/saved, changes are still in UI
                Assert.True(dialog.HasUnsavedChanges());

                // Now mock user answering Discard so it can close cleanly
                dialog.PromptUnsavedChangesHandler = () => MessageBoxResult.No;
                dialog.Close();
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void WorkRestSettingsDialog_OnClosing_WithoutChanges_ClosesDirectlyWithoutPrompt()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            RunInSta(() =>
            {
                var initial = new WorkRestSettings { FocusMinutes = 25 };
                var dialog = new WorkRestSettingsDialog(initial);

                bool promptCalled = false;
                dialog.PromptUnsavedChangesHandler = () =>
                {
                    promptCalled = true;
                    return MessageBoxResult.Cancel;
                };

                dialog.Close();

                Assert.False(promptCalled);
            });
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }
}
