using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace VitorsWeeklyWorkTracking.Services;

/// <summary>
/// Centralized helper for managing persistent data paths in %LOCALAPPDATA%.
/// Prevents write permission errors when installed in Program Files and provides seamless
/// migration from legacy working directory files and prior application versions.
/// </summary>
public static class StoragePathHelper
{
    private const string AppFolderName = "VitorsWeeklyWorkTracking";
    private static readonly string AppDataFolder;
    private static readonly AsyncLocal<string?> _asyncOverrideFolder = new();
    private static volatile string? _globalOverrideFolder;
    private static string? _defaultTestSandboxFolder;
    private static readonly object _fileWriteLock = new();

    /// <summary>
    /// Standard application data and settings files monitored for updates and backups.
    /// </summary>
    public static readonly string[] MonitoredFiles = new[]
    {
        "time-entries.json",
        "projects.json",
        "activities.json",
        "calendar-plans.json",
        "focus-settings.json",
        "theme.json",
        "streaks.json"
    };

    /// <summary>
    /// For testing purposes only. Allows overriding the data folder.
    /// </summary>
    public static string? OverrideAppDataFolder
    {
        get => _asyncOverrideFolder.Value ?? _globalOverrideFolder;
        set
        {
            _asyncOverrideFolder.Value = value;
            _globalOverrideFolder = value;
        }
    }

    /// <summary>
    /// For testing purposes only. Allows enabling legacy migration when OverrideAppDataFolder is set.
    /// </summary>
    public static bool EnableLegacyMigrationInTest { get; set; } = false;

    /// <summary>
    /// For testing purposes only. Allows specifying custom legacy directories to test migration from.
    /// </summary>
    public static List<string>? CustomLegacyDirectoriesForTesting { get; set; }

    static StoragePathHelper()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
            {
                localAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }

            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                AppDataFolder = Path.Combine(localAppData, AppFolderName);
            }
            else
            {
                AppDataFolder = Path.Combine(AppContext.BaseDirectory, "Data");
            }

            EnsureDirectoryExists(AppDataFolder);
        }
        catch
        {
            AppDataFolder = Path.Combine(Path.GetTempPath(), AppFolderName);
            EnsureDirectoryExists(AppDataFolder);
        }
    }

    /// <summary>
    /// Returns true if execution is taking place inside a test runner environment (e.g., xUnit, VSTest, testhost).
    /// </summary>
    public static bool IsTestEnvironment()
    {
        try
        {
            var friendlyName = AppDomain.CurrentDomain.FriendlyName;
            if (friendlyName.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                friendlyName.Contains("xunit", StringComparison.OrdinalIgnoreCase) ||
                friendlyName.Contains("vstest", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSTEST_EXECUTION_ENGINE")))
            {
                return true;
            }

            if (AppDomain.CurrentDomain.GetAssemblies().Any(a =>
            {
                var name = a.GetName().Name;
                return name != null && (
                    name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("testhost", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Microsoft.VisualStudio.TestPlatform", StringComparison.OrdinalIgnoreCase));
            }))
            {
                return true;
            }
        }
        catch { }

        return false;
    }

    /// <summary>
    /// Gets the root data folder for application data.
    /// In test runner environments without explicit override, safely isolates data to a dedicated sandbox.
    /// </summary>
    public static string GetDataFolder()
    {
        var overrideFolder = OverrideAppDataFolder;
        if (!string.IsNullOrWhiteSpace(overrideFolder))
        {
            EnsureDirectoryExists(overrideFolder);
            return overrideFolder;
        }

        if (IsTestEnvironment())
        {
            if (string.IsNullOrWhiteSpace(_defaultTestSandboxFolder))
            {
                _defaultTestSandboxFolder = Path.Combine(Path.GetTempPath(), "VitorsWorkTracking_TestSandbox_" + Process.GetCurrentProcess().Id);
            }
            EnsureDirectoryExists(_defaultTestSandboxFolder);
            return _defaultTestSandboxFolder;
        }

        EnsureDirectoryExists(AppDataFolder);
        return AppDataFolder;
    }

    /// <summary>
    /// Atomically and safely writes text to the specified destination path.
    /// Writes to a temporary file first, maintains a .bak backup file, and replaces the target file atomically.
    /// </summary>
    public static void SaveTextAtomic(string filePath, string content)
    {
        lock (_fileWriteLock)
        {
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    EnsureDirectoryExists(dir);
                }

                var tempPath = filePath + $".tmp_{Guid.NewGuid():N}";
                var bakPath = filePath + ".bak";

                File.WriteAllText(tempPath, content);

                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Copy(filePath, bakPath, overwrite: true);
                    }
                    catch { }

                    try
                    {
                        File.Replace(tempPath, filePath, null, ignoreMetadataErrors: true);
                    }
                    catch
                    {
                        File.Copy(tempPath, filePath, overwrite: true);
                        try { File.Delete(tempPath); } catch { }
                    }
                }
                else
                {
                    try
                    {
                        File.Move(tempPath, filePath);
                    }
                    catch
                    {
                        File.Copy(tempPath, filePath, overwrite: true);
                        try { File.Delete(tempPath); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, $"StoragePathHelper.SaveTextAtomic({filePath})");
                try
                {
                    File.WriteAllText(filePath, content);
                }
                catch (Exception fallbackEx)
                {
                    LogError(fallbackEx, $"StoragePathHelper.SaveTextAtomic.Fallback({filePath})");
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Safely reads text from the target file. If missing, 0 bytes, or corrupted,
    /// falls back to reading from .bak copy or from the latest valid BackupService snapshot,
    /// and automatically restores the recovered content.
    /// </summary>
    public static string? ReadTextSafe(string filePath, bool fallbackToBackups = true)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath).Trim();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return content;
                }
            }
        }
        catch (Exception ex)
        {
            LogError(ex, $"StoragePathHelper.ReadTextSafe.Primary({filePath})");
        }

        if (!fallbackToBackups) return null;

        // 1. Try .bak file
        var bakPath = filePath + ".bak";
        try
        {
            if (File.Exists(bakPath))
            {
                var bakContent = File.ReadAllText(bakPath).Trim();
                if (!string.IsNullOrWhiteSpace(bakContent))
                {
                    try
                    {
                        SaveTextAtomic(filePath, bakContent);
                    }
                    catch { }
                    return bakContent;
                }
            }
        }
        catch { }

        // 2. Try latest snapshot from BackupService
        try
        {
            var fileName = Path.GetFileName(filePath);
            var recoveredContent = BackupService.Instance.TryGetFileContentFromLatestBackup(fileName);
            if (!string.IsNullOrWhiteSpace(recoveredContent))
            {
                try
                {
                    SaveTextAtomic(filePath, recoveredContent);
                }
                catch { }
                return recoveredContent;
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Gets the backups folder path, ensuring it exists.
    /// </summary>
    public static string GetBackupFolderPath()
    {
        var backupFolder = Path.Combine(GetDataFolder(), "backups");
        EnsureDirectoryExists(backupFolder);
        return backupFolder;
    }

    /// <summary>
    /// Returns all candidate directories where legacy data or settings from previous versions
    /// or portable runs may be located.
    /// </summary>
    public static IReadOnlyList<string> GetCandidateLegacyDirectories()
    {
        if (CustomLegacyDirectoriesForTesting != null)
        {
            return CustomLegacyDirectoriesForTesting;
        }

        if (OverrideAppDataFolder != null && !EnableLegacyMigrationInTest)
        {
            return Array.Empty<string>();
        }

        var currentFolder = GetDataFolder();
        var candidates = new List<string>();

        // 1. Base directory and base Data subfolder (including parent directories if installed in subfolder)
        try
        {
            if (!string.IsNullOrWhiteSpace(AppContext.BaseDirectory))
            {
                candidates.Add(Path.Combine(AppContext.BaseDirectory, "Data"));
                candidates.Add(AppContext.BaseDirectory);

                var parentDir = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (!string.IsNullOrWhiteSpace(parentDir))
                {
                    candidates.Add(Path.Combine(parentDir, "Data"));
                    candidates.Add(parentDir);
                }
            }
        }
        catch { }

        // 2. Current working directory and Data subfolder
        try
        {
            var curDir = Environment.CurrentDirectory;
            if (!string.IsNullOrWhiteSpace(curDir))
            {
                candidates.Add(Path.Combine(curDir, "Data"));
                candidates.Add(curDir);
            }
        }
        catch { }

        // 3. LocalAppData variants (Inno Setup name, WiX name, plain name, Programs subfolder, VirtualStore)
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                var folderNames = new[]
                {
                    "Vitor's Weekly Work Tracking",
                    "Vitors Weekly Work Tracking",
                    "VitorWeeklyWorkTracking",
                    "VitorsWeeklyWorkTracking",
                    "Programs\\VitorsWeeklyWorkTracking",
                    "Programs\\Vitors Weekly Work Tracking",
                    "Programs\\Vitor's Weekly Work Tracking",
                    "Programs\\VitorWeeklyWorkTracking",
                    "VirtualStore\\Program Files\\VitorsWeeklyWorkTracking",
                    "VirtualStore\\Program Files\\Vitors Weekly Work Tracking",
                    "VirtualStore\\Program Files\\Vitor's Weekly Work Tracking",
                    "VirtualStore\\Program Files (x86)\\VitorsWeeklyWorkTracking",
                    "VirtualStore\\Program Files (x86)\\Vitors Weekly Work Tracking",
                    "VirtualStore\\Program Files (x86)\\Vitor's Weekly Work Tracking",
                    "VirtualStore\\ProgramData\\VitorsWeeklyWorkTracking",
                    "VirtualStore\\ProgramData\\Vitor's Weekly Work Tracking"
                };

                foreach (var name in folderNames)
                {
                    var dir = Path.Combine(localAppData, name);
                    candidates.Add(Path.Combine(dir, "Data"));
                    candidates.Add(dir);
                }
            }
        }
        catch { }

        // 4. Roaming AppData variants
        try
        {
            var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrWhiteSpace(roamingAppData))
            {
                var folderNames = new[]
                {
                    "VitorsWeeklyWorkTracking",
                    "Vitor's Weekly Work Tracking",
                    "Vitors Weekly Work Tracking",
                    "VitorWeeklyWorkTracking"
                };

                foreach (var name in folderNames)
                {
                    var dir = Path.Combine(roamingAppData, name);
                    candidates.Add(Path.Combine(dir, "Data"));
                    candidates.Add(dir);
                }
            }
        }
        catch { }

        // 5. CommonApplicationData (ProgramData) variants
        try
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (!string.IsNullOrWhiteSpace(programData))
            {
                var folderNames = new[]
                {
                    "VitorsWeeklyWorkTracking",
                    "Vitor's Weekly Work Tracking",
                    "Vitors Weekly Work Tracking",
                    "VitorWeeklyWorkTracking"
                };

                foreach (var name in folderNames)
                {
                    var dir = Path.Combine(programData, name);
                    candidates.Add(Path.Combine(dir, "Data"));
                    candidates.Add(dir);
                }
            }
        }
        catch { }

        // 6. Program Files installation directories
        try
        {
            var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(progFiles))
            {
                var folderNames = new[]
                {
                    "Vitor's Weekly Work Tracking",
                    "Vitors Weekly Work Tracking",
                    "VitorsWeeklyWorkTracking",
                    "VitorWeeklyWorkTracking"
                };
                foreach (var name in folderNames)
                {
                    var dir = Path.Combine(progFiles, name);
                    candidates.Add(Path.Combine(dir, "Data"));
                    candidates.Add(dir);
                }
            }

            var progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrWhiteSpace(progFilesX86))
            {
                var folderNames = new[]
                {
                    "Vitor's Weekly Work Tracking",
                    "Vitors Weekly Work Tracking",
                    "VitorsWeeklyWorkTracking",
                    "VitorWeeklyWorkTracking"
                };
                foreach (var name in folderNames)
                {
                    var dir = Path.Combine(progFilesX86, name);
                    candidates.Add(Path.Combine(dir, "Data"));
                    candidates.Add(dir);
                }
            }
        }
        catch { }

        // 7. User Profile folder variants
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userProfile))
            {
                var folderNames = new[]
                {
                    "VitorsWeeklyWorkTracking",
                    "Vitor's Weekly Work Tracking",
                    "Vitors Weekly Work Tracking",
                    "VitorWeeklyWorkTracking",
                    ".vitorsweeklyworktracking",
                    "Downloads\\VitorsWeeklyWorkTracking",
                    "Downloads\\Vitor's Weekly Work Tracking",
                    "Desktop\\VitorsWeeklyWorkTracking",
                    "Desktop\\Vitor's Weekly Work Tracking"
                };
                foreach (var name in folderNames)
                {
                    var dir = Path.Combine(userProfile, name);
                    candidates.Add(Path.Combine(dir, "Data"));
                    candidates.Add(dir);
                }
            }
        }
        catch { }

        // Deduplicate and filter out the current active folder
        var distinct = new List<string>();
        foreach (var c in candidates)
        {
            if (string.IsNullOrWhiteSpace(c)) continue;
            var fullPath = Path.GetFullPath(c);
            if (string.Equals(fullPath, Path.GetFullPath(currentFolder), StringComparison.OrdinalIgnoreCase))
                continue;

            if (!distinct.Any(d => string.Equals(d, fullPath, StringComparison.OrdinalIgnoreCase)))
            {
                distinct.Add(fullPath);
            }
        }

        return distinct;
    }

    /// <summary>
    /// Returns candidate file names including common legacy naming variations.
    /// </summary>
    public static List<string> GetCandidateFileNames(string fileName, string? legacyFileName = null)
    {
        var names = new List<string> { fileName };

        if (!string.IsNullOrWhiteSpace(legacyFileName) && !names.Contains(legacyFileName, StringComparer.OrdinalIgnoreCase))
        {
            names.Add(legacyFileName);
        }

        var normalized = fileName.ToLowerInvariant();
        if (normalized.Contains("focus") || normalized.Contains("setting"))
        {
            var aliases = new[]
            {
                "focus-settings.json",
                "focus_companion_settings.json",
                "focus-companion-settings.json",
                "focus_settings.json",
                "work-rest-settings.json",
                "work_rest_settings.json",
                "settings.json"
            };
            foreach (var alias in aliases)
            {
                if (!names.Contains(alias, StringComparer.OrdinalIgnoreCase))
                    names.Add(alias);
            }
        }
        else if (normalized.Contains("theme"))
        {
            var aliases = new[]
            {
                "theme.json",
                "theme-settings.json",
                "theme_settings.json"
            };
            foreach (var alias in aliases)
            {
                if (!names.Contains(alias, StringComparer.OrdinalIgnoreCase))
                    names.Add(alias);
            }
        }
        else if (normalized.Contains("streak"))
        {
            var aliases = new[]
            {
                "streaks.json",
                "streak-settings.json",
                "streak_settings.json",
                "streak.json"
            };
            foreach (var alias in aliases)
            {
                if (!names.Contains(alias, StringComparer.OrdinalIgnoreCase))
                    names.Add(alias);
            }
        }
        else if (normalized.Contains("time") || normalized.Contains("entri") || normalized.Contains("entry"))
        {
            var aliases = new[]
            {
                "time-entries.json",
                "time_entries.json",
                "entries.json",
                "time-tracking.json",
                "time_tracking.json"
            };
            foreach (var alias in aliases)
            {
                if (!names.Contains(alias, StringComparer.OrdinalIgnoreCase))
                    names.Add(alias);
            }
        }
        else if (normalized.Contains("calendar") || normalized.Contains("plan"))
        {
            var aliases = new[]
            {
                "calendar-plans.json",
                "calendar_plans.json",
                "plans.json",
                "planner.json",
                "calendar-planner.json"
            };
            foreach (var alias in aliases)
            {
                if (!names.Contains(alias, StringComparer.OrdinalIgnoreCase))
                    names.Add(alias);
            }
        }
        else if (normalized.Contains("project"))
        {
            var aliases = new[]
            {
                "projects.json",
                "project-list.json",
                "projects_data.json",
                "project_list.json"
            };
            foreach (var alias in aliases)
            {
                if (!names.Contains(alias, StringComparer.OrdinalIgnoreCase))
                    names.Add(alias);
            }
        }
        else if (normalized.Contains("activit"))
        {
            var aliases = new[]
            {
                "activities.json",
                "activity-list.json",
                "activities_data.json",
                "activity_list.json"
            };
            foreach (var alias in aliases)
            {
                if (!names.Contains(alias, StringComparer.OrdinalIgnoreCase))
                    names.Add(alias);
            }
        }

        return names;
    }

    /// <summary>
    /// Gets the full path to a file in the user's LocalApplicationData folder,
    /// automatically creating directories and migrating legacy files if present.
    /// </summary>
    public static string GetFilePath(string fileName, string? legacyFileName = null)
    {
        var folder = GetDataFolder();
        var targetPath = Path.Combine(folder, fileName);

        // Migrate legacy file if destination does not exist or is empty
        try
        {
            bool destinationNeedsFile = !File.Exists(targetPath) || new FileInfo(targetPath).Length == 0;
            if (destinationNeedsFile)
            {
                var candidateNames = GetCandidateFileNames(fileName, legacyFileName);

                // 1. Check if candidate file exists under an alias name within the current data folder itself
                foreach (var name in candidateNames)
                {
                    var localSource = Path.Combine(folder, name);
                    if (!string.Equals(Path.GetFullPath(localSource), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase)
                        && File.Exists(localSource) && new FileInfo(localSource).Length > 0)
                    {
                        EnsureDirectoryExists(Path.GetDirectoryName(targetPath));
                        File.Copy(localSource, targetPath, overwrite: true);
                        destinationNeedsFile = false;
                        break;
                    }
                }

                // 2. Check candidate legacy directories
                if (destinationNeedsFile)
                {
                    var legacyDirs = GetCandidateLegacyDirectories();

                    foreach (var dir in legacyDirs)
                    {
                        if (!Directory.Exists(dir)) continue;

                        foreach (var name in candidateNames)
                        {
                            var sourcePath = Path.Combine(dir, name);
                            if (File.Exists(sourcePath) && new FileInfo(sourcePath).Length > 0)
                            {
                                EnsureDirectoryExists(Path.GetDirectoryName(targetPath));
                                File.Copy(sourcePath, targetPath, overwrite: true);
                                destinationNeedsFile = false;
                                break;
                            }
                        }

                        if (!destinationNeedsFile) break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogError(ex, $"StoragePathHelper.GetFilePath({fileName})");
        }

        return targetPath;
    }

    /// <summary>
    /// Scans all candidate legacy locations and imports missing data files and backup snapshots
    /// into the current user data folder to guarantee seamless updates across all versions.
    /// </summary>
    public static int MigrateAllLegacyData()
    {
        int migratedCount = 0;
        try
        {
            // 1. Migrate all monitored data files
            foreach (var file in MonitoredFiles)
            {
                var targetPath = Path.Combine(GetDataFolder(), file);
                bool wasMissing = !File.Exists(targetPath) || new FileInfo(targetPath).Length == 0;
                var resolved = GetFilePath(file);
                if (wasMissing && File.Exists(resolved) && new FileInfo(resolved).Length > 0)
                {
                    migratedCount++;
                }
            }

            // 2. Discover and migrate legacy backup snapshots if any
            var targetBackupsDir = GetBackupFolderPath();
            var legacyDirs = GetCandidateLegacyDirectories();

            foreach (var dir in legacyDirs)
            {
                var legacyBackupsDir = Path.Combine(dir, "backups");
                if (Directory.Exists(legacyBackupsDir) && !string.Equals(Path.GetFullPath(legacyBackupsDir), Path.GetFullPath(targetBackupsDir), StringComparison.OrdinalIgnoreCase))
                {
                    var backupSubDirs = Directory.GetDirectories(legacyBackupsDir);
                    foreach (var subDir in backupSubDirs)
                    {
                        var dirName = Path.GetFileName(subDir);
                        var targetSubDir = Path.Combine(targetBackupsDir, dirName);
                        if (!Directory.Exists(targetSubDir))
                        {
                            try
                            {
                                CopyDirectoryRecursively(subDir, targetSubDir);
                                migratedCount++;
                            }
                            catch { }
                        }
                    }
                }
            }

            // 3. Ensure streak data and companion settings are merged from legacy stores
            try
            {
                StreakStorage.Instance.Load();
            }
            catch { }

            try
            {
                FocusCompanionStorage.Load();
            }
            catch { }
        }
        catch (Exception ex)
        {
            LogError(ex, "StoragePathHelper.MigrateAllLegacyData");
        }

        return migratedCount;
    }

    private static void CopyDirectoryRecursively(string sourceDir, string targetDir)
    {
        EnsureDirectoryExists(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
            CopyDirectoryRecursively(subDir, destSubDir);
        }
    }

    /// <summary>
    /// Ensures that the specified directory exists.
    /// </summary>
    public static void EnsureDirectoryExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch
        {
            // Ignore directory creation failure
        }
    }

    /// <summary>
    /// Logs unhandled exceptions to error.log in the user data folder.
    /// </summary>
    public static void LogError(Exception ex, string context = "")
    {
        try
        {
            var logPath = Path.Combine(GetDataFolder(), "error.log");
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
            File.AppendAllText(logPath, message);
        }
        catch
        {
            // Ignore logging failure
        }
    }
}
