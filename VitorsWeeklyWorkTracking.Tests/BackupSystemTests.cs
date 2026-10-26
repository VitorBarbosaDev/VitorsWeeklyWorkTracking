using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

[Collection("StorageTests")]
public class BackupSystemTests
{
    private (string tempDir, string dataDir, string backupDir) SetupSandbox()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"backup_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;
        BackupService.ResetInstance();
        CalendarPlanStorage.ResetInstance();

        var backupDir = StoragePathHelper.GetBackupFolderPath();
        return (tempDir, tempDir, backupDir);
    }

    private void CleanupSandbox(string tempDir)
    {
        StoragePathHelper.OverrideAppDataFolder = null;
        BackupService.ResetInstance();
        CalendarPlanStorage.ResetInstance();
        try
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
        catch
        {
            // Ignore temp cleanup errors
        }
    }

    private void PopulateSampleData(string dataFolder)
    {
        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "Project Alpha", Activity = "Coding", StartTime = DateTime.Today, EndTime = DateTime.Today.AddHours(2) },
            new() { ProjectName = "Project Beta", Activity = "Design", StartTime = DateTime.Today, EndTime = DateTime.Today.AddHours(1.5) }
        };
        File.WriteAllText(Path.Combine(dataFolder, "time-entries.json"), JsonSerializer.Serialize(entries));

        var projects = new List<Project>
        {
            new() { Name = "Project Alpha", IsActive = true, ColorHex = "#3B82F6" },
            new() { Name = "Project Beta", IsActive = true, ColorHex = "#10B981" }
        };
        File.WriteAllText(Path.Combine(dataFolder, "projects.json"), JsonSerializer.Serialize(projects));

        var activities = new List<ActivityItem>
        {
            new() { Name = "Coding", IsActive = true, ColorHex = "#6366F1" },
            new() { Name = "Design", IsActive = true, ColorHex = "#EC4899" }
        };
        File.WriteAllText(Path.Combine(dataFolder, "activities.json"), JsonSerializer.Serialize(activities));

        var plans = new List<PlannedWorkItem>
        {
            new() { Id = Guid.NewGuid().ToString("N"), ProjectName = "Project Alpha", Activity = "Coding", PlannedHours = 3.0, Date = DateTime.Today }
        };
        File.WriteAllText(Path.Combine(dataFolder, "calendar-plans.json"), JsonSerializer.Serialize(plans));
    }

    [Fact]
    public void CreateBackup_CreatesValidSnapshotWithAllDataFilesAndMetadata()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            var backup = BackupService.Instance.CreateBackup(
                reason: "Manual Backup",
                note: "Test manual note",
                isManual: true,
                pruneAfter: false);

            Assert.NotNull(backup);
            Assert.True(Directory.Exists(backup.BackupDirectory));
            Assert.Equal("Manual Backup", backup.Reason);
            Assert.Equal("Test manual note", backup.Note);
            Assert.True(backup.IsManual);
            Assert.Equal(2, backup.TimeEntryCount);
            Assert.Equal(2, backup.ProjectCount);
            Assert.Equal(2, backup.ActivityCount);
            Assert.Equal(1, backup.CalendarPlanCount);
            Assert.True(backup.TotalSizeBytes > 0);
            Assert.Contains("time-entries.json", backup.IncludedFiles);
            Assert.Contains("projects.json", backup.IncludedFiles);
            Assert.Contains("activities.json", backup.IncludedFiles);
            Assert.Contains("calendar-plans.json", backup.IncludedFiles);

            // Verify file exists on disk
            var infoPath = Path.Combine(backup.BackupDirectory, "backup-info.json");
            Assert.True(File.Exists(infoPath));
            var readInfo = JsonSerializer.Deserialize<BackupInfo>(File.ReadAllText(infoPath));
            Assert.NotNull(readInfo);
            Assert.Equal(backup.Id, readInfo.Id);
            Assert.Equal(2, readInfo.TimeEntryCount);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void RestoreBackup_SafelyRestoresFilesAndCreatesPreRestoreSafetyBackup()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            // Step 1: Create initial backup of original data
            var originalBackup = BackupService.Instance.CreateBackup("Original State", isManual: true, pruneAfter: false);
            Assert.NotNull(originalBackup);

            // Step 2: Overwrite live data with modified / corrupted data
            var modifiedEntries = new List<TimeEntry>
            {
                new() { ProjectName = "Overwritten Project", Activity = "Overwritten Activity", StartTime = DateTime.Today, EndTime = DateTime.Today.AddHours(5) }
            };
            File.WriteAllText(Path.Combine(dataDir, "time-entries.json"), JsonSerializer.Serialize(modifiedEntries));

            // Verify live data was overwritten
            var liveStorage = new TimeEntryStorage();
            var loadedBeforeRestore = liveStorage.Load();
            Assert.Single(loadedBeforeRestore);
            Assert.Equal("Overwritten Project", loadedBeforeRestore[0].ProjectName);

            // Step 3: Restore original backup
            bool restoredEventFired = false;
            BackupService.Instance.DataRestored += (s, e) => restoredEventFired = true;

            var restoreResult = BackupService.Instance.RestoreBackup(originalBackup, createSafetyBackup: true);

            Assert.True(restoreResult.Success);
            Assert.True(restoreResult.SafetyBackupCreated);
            Assert.NotNull(restoreResult.SafetyBackupPath);
            Assert.True(Directory.Exists(restoreResult.SafetyBackupPath));
            Assert.True(restoredEventFired);

            // Step 4: Verify live data is restored back to 2 entries
            var restoredEntries = liveStorage.Load();
            Assert.Equal(2, restoredEntries.Count);
            Assert.Contains(restoredEntries, e => e.ProjectName == "Project Alpha");
            Assert.Contains(restoredEntries, e => e.ProjectName == "Project Beta");

            // Step 5: Verify the safety backup contains the "Overwritten Project"
            var safetyEntriesFile = Path.Combine(restoreResult.SafetyBackupPath, "time-entries.json");
            Assert.True(File.Exists(safetyEntriesFile));
            var safetyEntries = JsonSerializer.Deserialize<List<TimeEntry>>(File.ReadAllText(safetyEntriesFile));
            Assert.NotNull(safetyEntries);
            Assert.Single(safetyEntries);
            Assert.Equal("Overwritten Project", safetyEntries[0].ProjectName);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void ExportBackupToZip_And_ImportBackupFromZip_WorksCorrectly()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            var backup = BackupService.Instance.CreateBackup("To Export", isManual: true, pruneAfter: false);
            Assert.NotNull(backup);

            var zipPath = Path.Combine(tempDir, "exported_backup.zip");
            BackupService.Instance.ExportBackupToZip(backup, zipPath);

            Assert.True(File.Exists(zipPath));
            using var archive = ZipFile.OpenRead(zipPath);
            Assert.True(archive.Entries.Count > 0);
            Assert.Contains(archive.Entries, e => e.Name == "time-entries.json");
            Assert.Contains(archive.Entries, e => e.Name == "projects.json");

            // Overwrite live data with empty data
            File.WriteAllText(Path.Combine(dataDir, "time-entries.json"), "[]");
            var liveStorage = new TimeEntryStorage();
            Assert.Empty(liveStorage.Load());

            // Import from zip with immediate restore
            var imported = BackupService.Instance.ImportBackupFromZip(zipPath, restoreImmediately: true);
            Assert.NotNull(imported);
            Assert.Equal(2, imported.TimeEntryCount);
            Assert.Equal(2, imported.ProjectCount);

            // Check that live data is now restored
            var reloaded = liveStorage.Load();
            Assert.Equal(2, reloaded.Count);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void ExportCurrentDataToZip_CreatesArchiveOfLiveDataset()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            var zipPath = Path.Combine(tempDir, "live_export.zip");
            BackupService.Instance.ExportCurrentDataToZip(zipPath, "Live backup test");

            Assert.True(File.Exists(zipPath));
            using var archive = ZipFile.OpenRead(zipPath);
            Assert.Contains(archive.Entries, e => e.Name == "time-entries.json");
            Assert.Contains(archive.Entries, e => e.Name == "projects.json");
            Assert.Contains(archive.Entries, e => e.Name == "activities.json");
            Assert.Contains(archive.Entries, e => e.Name == "calendar-plans.json");
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void PruneOldBackups_PreservesManualBackupsAndDeletesExcessAutoBackups()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            // Create 3 manual backups
            var m1 = BackupService.Instance.CreateBackup("Manual 1", isManual: true, pruneAfter: false);
            var m2 = BackupService.Instance.CreateBackup("Manual 2", isManual: true, pruneAfter: false);
            var m3 = BackupService.Instance.CreateBackup("Manual 3", isManual: true, pruneAfter: false);

            // Create 5 auto backups with small time diffs
            var autoList = new List<BackupInfo>();
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(20);
                var a = BackupService.Instance.CreateBackup($"Auto {i}", isManual: false, pruneAfter: false);
                if (a != null) autoList.Add(a);
            }

            var allBefore = BackupService.Instance.GetAvailableBackups();
            Assert.Equal(8, allBefore.Count);

            // Prune to keep only 2 auto backups
            BackupService.Instance.PruneOldBackups(maxAutoBackups: 2);

            var allAfter = BackupService.Instance.GetAvailableBackups();
            var manualAfter = allAfter.Where(b => b.IsManual).ToList();
            var autoAfter = allAfter.Where(b => !b.IsManual).ToList();

            // All 3 manual backups should remain
            Assert.Equal(3, manualAfter.Count);
            // Exactly 2 auto backups should remain
            Assert.Equal(2, autoAfter.Count);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void DeleteBackup_RemovesDirectoryAndFilesFromDisk()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            var backup = BackupService.Instance.CreateBackup("To Delete", isManual: true, pruneAfter: false);
            Assert.NotNull(backup);
            Assert.True(Directory.Exists(backup.BackupDirectory));

            bool deleted = BackupService.Instance.DeleteBackup(backup);
            Assert.True(deleted);
            Assert.False(Directory.Exists(backup.BackupDirectory));

            var remaining = BackupService.Instance.GetAvailableBackups();
            Assert.DoesNotContain(remaining, b => b.Id == backup.Id);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void GetAvailableBackups_DiscoversAndReconstructsMissingMetadata()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            var backup = BackupService.Instance.CreateBackup("Metadata test", isManual: true, pruneAfter: false);
            Assert.NotNull(backup);

            // Delete backup-info.json to test reconstruction
            var infoPath = Path.Combine(backup.BackupDirectory, "backup-info.json");
            File.Delete(infoPath);
            Assert.False(File.Exists(infoPath));

            var backups = BackupService.Instance.GetAvailableBackups();
            var discovered = backups.FirstOrDefault(b => b.BackupDirectory == backup.BackupDirectory);

            Assert.NotNull(discovered);
            Assert.Equal("Discovered Backup", discovered.Reason);
            Assert.Equal(2, discovered.TimeEntryCount);
            Assert.Equal(2, discovered.ProjectCount);
            Assert.Equal(2, discovered.ActivityCount);
            Assert.Equal(1, discovered.CalendarPlanCount);
            Assert.True(discovered.TotalSizeBytes > 0);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void CreateAutoBackupIfNeeded_RespectsIntervalThrottle()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            var first = BackupService.Instance.CreateAutoBackupIfNeeded("First Auto", TimeSpan.FromSeconds(10));
            Assert.NotNull(first);

            // Immediate subsequent call should be throttled (returns null)
            var second = BackupService.Instance.CreateAutoBackupIfNeeded("Second Auto", TimeSpan.FromSeconds(10));
            Assert.Null(second);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void StorageSave_AutomaticallyTriggersAutoBackup()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            // Calling TimeEntryStorage.Save should trigger auto-backup
            var entryStorage = new TimeEntryStorage();
            var entries = entryStorage.Load();
            entries.Add(new TimeEntry { ProjectName = "New Entry", StartTime = DateTime.Today, EndTime = DateTime.Today.AddHours(1) });
            entryStorage.Save(entries);

            var backups = BackupService.Instance.GetAvailableBackups();
            Assert.NotEmpty(backups);
            Assert.Contains(backups, b => b.Reason.Contains("Time Entries"));
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void RestoreBackup_FailsGracefully_WhenDirectoryDoesNotExist()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            var nonExistentBackup = new BackupInfo
            {
                BackupDirectory = Path.Combine(tempDir, "does_not_exist")
            };

            var result = BackupService.Instance.RestoreBackup(nonExistentBackup, createSafetyBackup: false);
            Assert.False(result.Success);
            Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void BackupInfo_FormattingProperties_DisplayAccurately()
    {
        var info = new BackupInfo
        {
            Reason = "Pre-Update (Project Rename)",
            Note = "Renaming Alpha to Beta",
            IsManual = false,
            CreatedAt = new DateTime(2026, 10, 26, 14, 30, 0),
            TimeEntryCount = 42,
            ProjectCount = 5,
            ActivityCount = 8,
            CalendarPlanCount = 12,
            TotalSizeBytes = 1024 * 50 // 50 KB
        };

        Assert.Equal("2026-10-26 14:30:00", info.FormattedCreatedAt);
        Assert.Equal("Pre-Update (Project Rename): Renaming Alpha to Beta", info.DisplayTitle);
        Assert.Equal("Pre-Update", info.BadgeText);
        Assert.Equal("#9333EA", info.BadgeBackgroundHex);
        Assert.Equal("50.0 KB", info.FormattedSize);
        Assert.Equal("42 entries • 5 projects • 8 activities • 12 plans", info.SummaryText);
    }

    [Fact]
    public void BackupManagementWindow_STA_CanLoadAndDisplayBackups()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            var (tempDir, dataDir, backupDir) = SetupSandbox();
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                PopulateSampleData(dataDir);

                // Create a manual backup
                var b1 = BackupService.Instance.CreateBackup("Manual Baseline", "Baseline snapshot", isManual: true, pruneAfter: false);
                Assert.NotNull(b1);

                var window = new BackupManagementWindow();
                window.RefreshBackupsList();

                Assert.NotNull(window.BackupsListBox);
                Assert.NotNull(window.CreateBackupButton);
                Assert.NotNull(window.RestoreSelectedButton);
                Assert.NotNull(window.ExportBackupButton);
                Assert.NotNull(window.ImportBackupButton);

                Assert.True(window.BackupsListBox.Items.Count >= 1);
                Assert.Equal("Manual Baseline: Baseline snapshot", window.DetailTitleTextBlock.Text);
                Assert.Equal("• Time Entries: 2", window.DetailEntriesText.Text);
                Assert.Equal("• Projects: 2", window.DetailProjectsText.Text);

                // Test filter
                window.SearchFilterTextBox.Text = "NonExistentFilter";
                Assert.Empty(window.BackupsListBox.Items);
                Assert.Equal(System.Windows.Visibility.Visible, window.EmptyStateBorder.Visibility);

                window.SearchFilterTextBox.Text = "";
                Assert.True(window.BackupsListBox.Items.Count >= 1);
                Assert.Equal(System.Windows.Visibility.Collapsed, window.EmptyStateBorder.Visibility);

                window.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
            finally
            {
                CleanupSandbox(tempDir);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }

    [Fact]
    public void WorkRestSettingsDialog_STA_HasBackupsButtonAndDataCard()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            var (tempDir, dataDir, backupDir) = SetupSandbox();
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                PopulateSampleData(dataDir);

                var settings = new WorkRestSettings();
                var dialog = new WorkRestSettingsDialog(settings);

                Assert.NotNull(dialog.BackupsButton);
                Assert.NotNull(dialog.BackupCardButton);
                Assert.NotNull(dialog.ManageBackupsCardButton);

                Assert.Equal("💾 Backups", dialog.BackupsButton.Content.ToString());
                Assert.Equal("💾 Create Backup", dialog.BackupCardButton.Content.ToString());
                Assert.Equal("📂 Manage & Restore Backups...", dialog.ManageBackupsCardButton.Content.ToString());

                // Ensure BackupsButton is in Row 2 (the bottom action bar of settings)
                Assert.Equal(2, System.Windows.Controls.Grid.GetRow(dialog.BackupsButton.Parent as System.Windows.FrameworkElement ?? dialog.BackupsButton));

                dialog.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
            finally
            {
                CleanupSandbox(tempDir);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }

    [Fact]
    public void ManualSettingsBackup_CreatesSnapshotWithSettingsNote()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            var backup = BackupService.Instance.CreateBackup(
                reason: "Manual Backup",
                note: "Created from Settings",
                isManual: true,
                pruneAfter: false);

            Assert.NotNull(backup);
            Assert.True(backup.IsManual);
            Assert.Equal("Manual Backup", backup.Reason);
            Assert.Equal("Created from Settings", backup.Note);
            Assert.Equal(2, backup.TimeEntryCount);
            Assert.Equal(2, backup.ProjectCount);
            Assert.Equal(2, backup.ActivityCount);
            Assert.Equal(1, backup.CalendarPlanCount);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void PruneOldBackups_DeletesAutoBackupsOlderThanTwoMonths_PreservesRecentAndManualBackups()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            // 1. Old manual backup from 90 days ago (should be kept because it is manual)
            var oldManual = BackupService.Instance.CreateBackup("Old Manual Backup", isManual: true, pruneAfter: false);
            Assert.NotNull(oldManual);
            SetBackupDate(oldManual, DateTime.Now.AddDays(-90));

            // 2. Old auto backup from 90 days ago (should be deleted)
            var oldAuto1 = BackupService.Instance.CreateBackup("Auto Backup 90d", isManual: false, pruneAfter: false);
            Assert.NotNull(oldAuto1);
            SetBackupDate(oldAuto1, DateTime.Now.AddDays(-90));

            // 3. Old auto backup from 65 days ago (should be deleted, > 60 days)
            var oldAuto2 = BackupService.Instance.CreateBackup("Auto Backup 65d", isManual: false, pruneAfter: false);
            Assert.NotNull(oldAuto2);
            SetBackupDate(oldAuto2, DateTime.Now.AddDays(-65));

            // 4. Recent auto backup from 45 days ago (should be kept, < 60 days)
            var recentAuto1 = BackupService.Instance.CreateBackup("Auto Backup 45d", isManual: false, pruneAfter: false);
            Assert.NotNull(recentAuto1);
            SetBackupDate(recentAuto1, DateTime.Now.AddDays(-45));

            // 5. Recent auto backup from 10 days ago (should be kept)
            var recentAuto2 = BackupService.Instance.CreateBackup("Auto Backup 10d", isManual: false, pruneAfter: false);
            Assert.NotNull(recentAuto2);
            SetBackupDate(recentAuto2, DateTime.Now.AddDays(-10));

            // 6. Recent auto backup from today (should be kept)
            var recentAutoToday = BackupService.Instance.CreateBackup("Auto Backup Today", isManual: false, pruneAfter: false);
            Assert.NotNull(recentAutoToday);

            // 7. Recent manual backup from today (should be kept)
            var recentManual = BackupService.Instance.CreateBackup("Manual Backup Today", isManual: true, pruneAfter: false);
            Assert.NotNull(recentManual);

            var allBefore = BackupService.Instance.GetAvailableBackups();
            Assert.Equal(7, allBefore.Count);

            // Run pruning with default 2-month (60-day) retention threshold
            int deleted = BackupService.Instance.PruneOldBackups();

            // Exactly 2 old auto backups should be deleted (90d and 65d)
            Assert.Equal(2, deleted);

            var allAfter = BackupService.Instance.GetAvailableBackups();
            Assert.Equal(5, allAfter.Count);

            // Ensure old manual backup is still present
            Assert.Contains(allAfter, b => b.Id == oldManual.Id && b.IsManual);

            // Ensure recent manual backup is still present
            Assert.Contains(allAfter, b => b.Id == recentManual.Id && b.IsManual);

            // Ensure recent auto backups are still present
            Assert.Contains(allAfter, b => b.Id == recentAuto1.Id);
            Assert.Contains(allAfter, b => b.Id == recentAuto2.Id);
            Assert.Contains(allAfter, b => b.Id == recentAutoToday.Id);

            // Ensure old auto backups were pruned from disk
            Assert.DoesNotContain(allAfter, b => b.Id == oldAuto1.Id);
            Assert.DoesNotContain(allAfter, b => b.Id == oldAuto2.Id);
            Assert.False(Directory.Exists(oldAuto1.BackupDirectory));
            Assert.False(Directory.Exists(oldAuto2.BackupDirectory));
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void PruneOldBackups_ReconstructedFolderMetadata_PrunesOldDiscoveredAutoBackups()
    {
        var (tempDir, dataDir, backupDir) = SetupSandbox();
        try
        {
            PopulateSampleData(dataDir);

            // Create a discovered folder from 75 days ago without backup-info.json
            var oldDateStr = DateTime.Now.AddDays(-75).ToString("yyyyMMdd_HHmmss");
            var oldFolder = Path.Combine(backupDir, $"backup_{oldDateStr}_test01");
            Directory.CreateDirectory(oldFolder);
            File.WriteAllText(Path.Combine(oldFolder, "time-entries.json"), "[]");

            // Create a recent discovered folder from 5 days ago without backup-info.json
            var recentDateStr = DateTime.Now.AddDays(-5).ToString("yyyyMMdd_HHmmss");
            var recentFolder = Path.Combine(backupDir, $"backup_{recentDateStr}_test02");
            Directory.CreateDirectory(recentFolder);
            File.WriteAllText(Path.Combine(oldFolder, "time-entries.json"), "[]");

            var discovered = BackupService.Instance.GetAvailableBackups();
            Assert.Equal(2, discovered.Count);

            // Run pruning
            int deleted = BackupService.Instance.PruneOldBackups();
            Assert.Equal(1, deleted);

            var remaining = BackupService.Instance.GetAvailableBackups();
            Assert.Single(remaining);
            Assert.False(Directory.Exists(oldFolder));
            Assert.True(Directory.Exists(recentFolder));
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    private static void SetBackupDate(BackupInfo backup, DateTime date)
    {
        backup.CreatedAt = date;
        var infoPath = Path.Combine(backup.BackupDirectory, "backup-info.json");
        if (File.Exists(infoPath))
        {
            var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(infoPath, json);
        }
        var dirInfo = new DirectoryInfo(backup.BackupDirectory);
        if (dirInfo.Exists)
        {
            dirInfo.CreationTime = date;
            dirInfo.LastWriteTime = date;
        }
    }
}
