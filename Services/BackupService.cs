using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

/// <summary>
/// Service responsible for managing automated snapshots, manual backups, data restorations,
/// and backup archives for the application.
/// </summary>
public class BackupService
{
    private static BackupService? _instance;
    public static BackupService Instance => _instance ??= new BackupService();

    public static void SetInstanceForTesting(BackupService? instance)
    {
        _instance = instance;
    }

    public static void ResetInstance()
    {
        _instance = null;
    }

    public static readonly string[] MonitoredDataFiles = new[]
    {
        "time-entries.json",
        "projects.json",
        "activities.json",
        "calendar-plans.json",
        "focus-settings.json",
        "theme.json"
    };

    /// <summary>
    /// Default retention period for automatic snapshots (approx. 2 months / 60 days).
    /// </summary>
    public static readonly TimeSpan DefaultAutoBackupRetention = TimeSpan.FromDays(60);

    /// <summary>
    /// Default maximum count of automatic snapshots to keep if not pruned by age.
    /// </summary>
    public const int DefaultMaxAutoBackups = 50;

    private const string BackupInfoFileName = "backup-info.json";
    private readonly object _lock = new();
    private DateTime _lastAutoBackupTime = DateTime.MinValue;
    private readonly TimeSpan _defaultAutoBackupThrottle = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Event fired whenever a backup is restored, allowing views and caches to reload data.
    /// </summary>
    public event EventHandler? DataRestored;

    /// <summary>
    /// Creates a complete snapshot of current active data files.
    /// </summary>
    public BackupInfo? CreateBackup(string reason = "Manual Backup", string note = "", bool isManual = false, bool pruneAfter = true)
    {
        lock (_lock)
        {
            try
            {
                var dataFolder = StoragePathHelper.GetDataFolder();
                var backupsFolder = StoragePathHelper.GetBackupFolderPath();

                // Find data files that currently exist
                var existingFiles = new List<string>();
                foreach (var fileName in MonitoredDataFiles)
                {
                    var sourcePath = Path.Combine(dataFolder, fileName);
                    if (File.Exists(sourcePath))
                    {
                        existingFiles.Add(fileName);
                    }
                }

                // If no data files exist at all yet, return null
                if (existingFiles.Count == 0)
                {
                    return null;
                }

                var timestamp = DateTime.Now;
                var folderName = $"backup_{timestamp:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}";
                var backupDir = Path.Combine(backupsFolder, folderName);
                StoragePathHelper.EnsureDirectoryExists(backupDir);

                long totalSize = 0;
                var copiedFiles = new List<string>();

                foreach (var fileName in existingFiles)
                {
                    var sourcePath = Path.Combine(dataFolder, fileName);
                    var destPath = Path.Combine(backupDir, fileName);
                    File.Copy(sourcePath, destPath, overwrite: true);
                    copiedFiles.Add(fileName);

                    var fileInfo = new FileInfo(destPath);
                    totalSize += fileInfo.Length;
                }

                // Calculate item counts
                int entryCount = CountItemsInJson<TimeEntry>(Path.Combine(backupDir, "time-entries.json"));
                int projectCount = CountItemsInJson<Project>(Path.Combine(backupDir, "projects.json"));
                int activityCount = CountItemsInJson<ActivityItem>(Path.Combine(backupDir, "activities.json"));
                int planCount = CountItemsInJson<PlannedWorkItem>(Path.Combine(backupDir, "calendar-plans.json"));

                var backupInfo = new BackupInfo
                {
                    Id = Guid.NewGuid().ToString("N"),
                    BackupDirectory = backupDir,
                    CreatedAt = timestamp,
                    Reason = reason,
                    Note = note,
                    IsManual = isManual,
                    TimeEntryCount = entryCount,
                    ProjectCount = projectCount,
                    ActivityCount = activityCount,
                    CalendarPlanCount = planCount,
                    TotalSizeBytes = totalSize,
                    IncludedFiles = copiedFiles
                };

                // Save backup-info.json inside the backup directory
                var metaJson = JsonSerializer.Serialize(backupInfo, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(backupDir, BackupInfoFileName), metaJson);

                if (!isManual)
                {
                    _lastAutoBackupTime = DateTime.UtcNow;
                }

                if (pruneAfter)
                {
                    PruneOldBackups();
                }

                return backupInfo;
            }
            catch (Exception ex)
            {
                StoragePathHelper.LogError(ex, $"BackupService.CreateBackup ({reason})");
                return null;
            }
        }
    }

    /// <summary>
    /// Creates an automatic backup if sufficient time has elapsed since the last auto backup.
    /// </summary>
    public BackupInfo? CreateAutoBackupIfNeeded(string reason = "Auto Backup", TimeSpan? minInterval = null)
    {
        lock (_lock)
        {
            var interval = minInterval ?? _defaultAutoBackupThrottle;
            if (DateTime.UtcNow - _lastAutoBackupTime < interval)
            {
                return null;
            }

            return CreateBackup(reason, note: string.Empty, isManual: false, pruneAfter: true);
        }
    }

    /// <summary>
    /// Returns a list of all discovered backups, sorted by newest first.
    /// </summary>
    public List<BackupInfo> GetAvailableBackups()
    {
        lock (_lock)
        {
            var result = new List<BackupInfo>();
            try
            {
                var backupsFolder = StoragePathHelper.GetBackupFolderPath();
                if (!Directory.Exists(backupsFolder))
                {
                    return result;
                }

                var dirs = Directory.GetDirectories(backupsFolder);
                foreach (var dir in dirs)
                {
                    try
                    {
                        var infoPath = Path.Combine(dir, BackupInfoFileName);
                        BackupInfo? info = null;

                        if (File.Exists(infoPath))
                        {
                            var json = File.ReadAllText(infoPath);
                            info = JsonSerializer.Deserialize<BackupInfo>(json);
                        }

                        if (info == null)
                        {
                            // Reconstruct metadata from folder
                            var dirInfo = new DirectoryInfo(dir);
                            var createdAt = dirInfo.CreationTime;

                            var dirName = Path.GetFileName(dir);
                            if (dirName.StartsWith("backup_") || dirName.StartsWith("imported_"))
                            {
                                var parts = dirName.Split('_');
                                if (parts.Length >= 3 && DateTime.TryParseExact($"{parts[1]}_{parts[2]}", "yyyyMMdd_HHmmss", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                                {
                                    createdAt = parsedDate;
                                }
                            }

                            var files = Directory.GetFiles(dir)
                                .Where(f => !Path.GetFileName(f).Equals(BackupInfoFileName, StringComparison.OrdinalIgnoreCase))
                                .Select(Path.GetFileName)
                                .Where(f => !string.IsNullOrEmpty(f))
                                .Select(f => f!)
                                .ToList();

                            long size = 0;
                            foreach (var f in Directory.GetFiles(dir))
                            {
                                size += new FileInfo(f).Length;
                            }

                            int entryCount = CountItemsInJson<TimeEntry>(Path.Combine(dir, "time-entries.json"));
                            int projectCount = CountItemsInJson<Project>(Path.Combine(dir, "projects.json"));
                            int activityCount = CountItemsInJson<ActivityItem>(Path.Combine(dir, "activities.json"));
                            int planCount = CountItemsInJson<PlannedWorkItem>(Path.Combine(dir, "calendar-plans.json"));

                            info = new BackupInfo
                            {
                                Id = Guid.NewGuid().ToString("N"),
                                BackupDirectory = dir,
                                CreatedAt = createdAt,
                                Reason = "Discovered Backup",
                                IsManual = false,
                                TimeEntryCount = entryCount,
                                ProjectCount = projectCount,
                                ActivityCount = activityCount,
                                CalendarPlanCount = planCount,
                                TotalSizeBytes = size,
                                IncludedFiles = files
                            };
                        }
                        else
                        {
                            // Always ensure absolute directory path is up-to-date
                            info.BackupDirectory = dir;
                        }

                        result.Add(info);
                    }
                    catch (Exception ex)
                    {
                        StoragePathHelper.LogError(ex, $"BackupService.GetAvailableBackups.ProcessDir ({dir})");
                    }
                }
            }
            catch (Exception ex)
            {
                StoragePathHelper.LogError(ex, "BackupService.GetAvailableBackups");
            }

            return result.OrderByDescending(b => b.CreatedAt).ToList();
        }
    }

    /// <summary>
    /// Restores data from the specified backup snapshot, optionally creating a pre-restore safety backup first.
    /// </summary>
    public RestoreResult RestoreBackup(BackupInfo backupInfo, bool createSafetyBackup = true)
    {
        lock (_lock)
        {
            if (backupInfo == null)
            {
                return RestoreResult.Fail("Backup info was null.");
            }

            if (!Directory.Exists(backupInfo.BackupDirectory))
            {
                return RestoreResult.Fail($"Backup directory not found: {backupInfo.BackupDirectory}");
            }

            string? safetyBackupDir = null;
            bool safetyCreated = false;

            if (createSafetyBackup)
            {
                var safety = CreateBackup(
                    reason: "Pre-Restore Safety Backup",
                    note: $"Before restoring snapshot from {backupInfo.FormattedCreatedAt}",
                    isManual: false,
                    pruneAfter: false);

                if (safety != null)
                {
                    safetyCreated = true;
                    safetyBackupDir = safety.BackupDirectory;
                }
            }

            try
            {
                var dataFolder = StoragePathHelper.GetDataFolder();
                var restoredFiles = new List<string>();

                var files = Directory.GetFiles(backupInfo.BackupDirectory);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    if (fileName.Equals(BackupInfoFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var targetPath = Path.Combine(dataFolder, fileName);
                    File.Copy(file, targetPath, overwrite: true);
                    restoredFiles.Add(fileName);
                }

                // Invalidate any storage caches
                CalendarPlanStorage.Instance.InvalidateCache();

                // Fire event so open windows and view models can refresh
                try
                {
                    DataRestored?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    StoragePathHelper.LogError(ex, "BackupService.RestoreBackup.DataRestoredEvent");
                }

                return RestoreResult.Ok(
                    message: $"Successfully restored {restoredFiles.Count} files from backup ({backupInfo.FormattedCreatedAt}).",
                    filesCount: restoredFiles.Count,
                    files: restoredFiles,
                    safetyCreated: safetyCreated,
                    safetyPath: safetyBackupDir);
            }
            catch (Exception ex)
            {
                StoragePathHelper.LogError(ex, $"BackupService.RestoreBackup ({backupInfo.BackupDirectory})");
                return RestoreResult.Fail($"Failed to restore backup: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Exports a backup directory to a compressed .zip archive.
    /// </summary>
    public void ExportBackupToZip(BackupInfo backupInfo, string targetZipPath)
    {
        lock (_lock)
        {
            if (backupInfo == null || !Directory.Exists(backupInfo.BackupDirectory))
            {
                throw new DirectoryNotFoundException("Backup directory does not exist.");
            }

            var targetDir = Path.GetDirectoryName(targetZipPath);
            if (!string.IsNullOrEmpty(targetDir))
            {
                StoragePathHelper.EnsureDirectoryExists(targetDir);
            }

            if (File.Exists(targetZipPath))
            {
                File.Delete(targetZipPath);
            }

            ZipFile.CreateFromDirectory(backupInfo.BackupDirectory, targetZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }
    }

    /// <summary>
    /// Exports the current live active dataset directly to a compressed .zip archive.
    /// </summary>
    public string ExportCurrentDataToZip(string targetZipPath, string note = "")
    {
        lock (_lock)
        {
            // First create a temporary or manual backup snapshot
            var backup = CreateBackup("Export Archive", note, isManual: true, pruneAfter: false);
            if (backup == null || !Directory.Exists(backup.BackupDirectory))
            {
                throw new InvalidOperationException("Could not snapshot current active data for export.");
            }

            ExportBackupToZip(backup, targetZipPath);
            return targetZipPath;
        }
    }

    /// <summary>
    /// Imports a backup from a .zip archive, extracts it into the backups folder,
    /// and optionally restores it immediately.
    /// </summary>
    public BackupInfo ImportBackupFromZip(string zipFilePath, bool restoreImmediately = false)
    {
        lock (_lock)
        {
            if (!File.Exists(zipFilePath))
            {
                throw new FileNotFoundException($"Zip archive file not found: {zipFilePath}");
            }

            var backupsFolder = StoragePathHelper.GetBackupFolderPath();
            var targetDirName = $"imported_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}";
            var targetDir = Path.Combine(backupsFolder, targetDirName);
            StoragePathHelper.EnsureDirectoryExists(targetDir);

            ZipFile.ExtractToDirectory(zipFilePath, targetDir, overwriteFiles: true);

            var infoPath = Path.Combine(targetDir, BackupInfoFileName);
            BackupInfo? info = null;

            if (File.Exists(infoPath))
            {
                try
                {
                    var json = File.ReadAllText(infoPath);
                    info = JsonSerializer.Deserialize<BackupInfo>(json);
                }
                catch
                {
                    // Ignore parse error and rebuild metadata
                }
            }

            var files = Directory.GetFiles(targetDir)
                .Where(f => !Path.GetFileName(f).Equals(BackupInfoFileName, StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .Where(f => !string.IsNullOrEmpty(f))
                .Select(f => f!)
                .ToList();

            long size = 0;
            foreach (var f in Directory.GetFiles(targetDir))
            {
                size += new FileInfo(f).Length;
            }

            int entryCount = CountItemsInJson<TimeEntry>(Path.Combine(targetDir, "time-entries.json"));
            int projectCount = CountItemsInJson<Project>(Path.Combine(targetDir, "projects.json"));
            int activityCount = CountItemsInJson<ActivityItem>(Path.Combine(targetDir, "activities.json"));
            int planCount = CountItemsInJson<PlannedWorkItem>(Path.Combine(targetDir, "calendar-plans.json"));

            info ??= new BackupInfo();
            info.Id = Guid.NewGuid().ToString("N");
            info.BackupDirectory = targetDir;
            info.CreatedAt = DateTime.Now;
            info.Reason = "Imported Backup";
            info.Note = Path.GetFileName(zipFilePath);
            info.IsManual = true;
            info.TimeEntryCount = entryCount;
            info.ProjectCount = projectCount;
            info.ActivityCount = activityCount;
            info.CalendarPlanCount = planCount;
            info.TotalSizeBytes = size;
            info.IncludedFiles = files;

            var metaJson = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(infoPath, metaJson);

            if (restoreImmediately)
            {
                RestoreBackup(info, createSafetyBackup: true);
            }

            return info;
        }
    }

    /// <summary>
    /// Deletes a specific backup directory or archive.
    /// </summary>
    public bool DeleteBackup(BackupInfo backupInfo)
    {
        lock (_lock)
        {
            if (backupInfo == null) return false;
            try
            {
                if (!string.IsNullOrEmpty(backupInfo.BackupDirectory) && Directory.Exists(backupInfo.BackupDirectory))
                {
                    Directory.Delete(backupInfo.BackupDirectory, recursive: true);
                    return true;
                }

                if (!string.IsNullOrEmpty(backupInfo.ZipPath) && File.Exists(backupInfo.ZipPath))
                {
                    File.Delete(backupInfo.ZipPath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                StoragePathHelper.LogError(ex, $"BackupService.DeleteBackup ({backupInfo.BackupDirectory})");
                return false;
            }
        }
    }

    /// <summary>
    /// Prunes older automatic backups when they exceed the retention period (default 60 days / 2 months)
    /// or when the total auto-backup count exceeds max limit.
    /// Preserves all manual backups.
    /// </summary>
    public int PruneOldBackups(int maxAutoBackups = DefaultMaxAutoBackups, TimeSpan? maxAutoBackupAge = null)
    {
        lock (_lock)
        {
            int deletedCount = 0;
            try
            {
                var maxAge = maxAutoBackupAge ?? DefaultAutoBackupRetention;
                var cutoffTime = DateTime.Now - maxAge;

                var backups = GetAvailableBackups();
                var autoBackups = backups.Where(b => !b.IsManual).OrderBy(b => b.CreatedAt).ToList();

                var deletedIds = new HashSet<string>();

                // 1. Delete automatic backups older than the retention threshold (2 months / 60 days)
                foreach (var autoBackup in autoBackups)
                {
                    if (autoBackup.CreatedAt < cutoffTime)
                    {
                        if (DeleteBackup(autoBackup))
                        {
                            deletedIds.Add(autoBackup.Id);
                            deletedCount++;
                        }
                    }
                }

                // 2. If remaining automatic backups exceed max count limit, remove the oldest excess
                var remainingAuto = autoBackups.Where(b => !deletedIds.Contains(b.Id)).ToList();
                if (maxAutoBackups >= 0 && remainingAuto.Count > maxAutoBackups)
                {
                    int excessCount = remainingAuto.Count - maxAutoBackups;
                    for (int i = 0; i < excessCount; i++)
                    {
                        if (DeleteBackup(remainingAuto[i]))
                        {
                            deletedCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StoragePathHelper.LogError(ex, "BackupService.PruneOldBackups");
            }

            return deletedCount;
        }
    }

    private static int CountItemsInJson<T>(string filePath)
    {
        if (!File.Exists(filePath)) return 0;
        try
        {
            var json = File.ReadAllText(filePath);
            var list = JsonSerializer.Deserialize<List<T>>(json);
            return list?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
