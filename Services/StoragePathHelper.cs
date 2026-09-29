using System;
using System.IO;

namespace VitorsWeeklyWorkTracking.Services;

/// <summary>
/// Centralized helper for managing persistent data paths in %LOCALAPPDATA%.
/// Prevents write permission errors when installed in Program Files and provides seamless
/// migration from legacy working directory files.
/// </summary>
public static class StoragePathHelper
{
    private const string AppFolderName = "VitorsWeeklyWorkTracking";
    private static readonly string AppDataFolder;

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
    /// Gets the full path to a file in the user's LocalApplicationData folder,
    /// automatically creating directories and migrating legacy files if present.
    /// </summary>
    public static string GetFilePath(string fileName, string? legacyFileName = null)
    {
        EnsureDirectoryExists(AppDataFolder);
        var targetPath = Path.Combine(AppDataFolder, fileName);

        // Migrate legacy file if destination does not exist
        try
        {
            if (!File.Exists(targetPath))
            {
                var candidateNames = new List<string> { fileName };
                if (!string.IsNullOrWhiteSpace(legacyFileName) && !candidateNames.Contains(legacyFileName))
                {
                    candidateNames.Add(legacyFileName);
                }

                foreach (var name in candidateNames)
                {
                    // Check base directory
                    var baseDirPath = Path.Combine(AppContext.BaseDirectory, name);
                    if (File.Exists(baseDirPath))
                    {
                        File.Copy(baseDirPath, targetPath, overwrite: false);
                        break;
                    }

                    // Check working directory
                    if (File.Exists(name))
                    {
                        File.Copy(name, targetPath, overwrite: false);
                        break;
                    }
                }
            }
        }
        catch
        {
            // Ignore migration failure and continue with target path
        }

        return targetPath;
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
            var logPath = Path.Combine(AppDataFolder, "error.log");
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
            File.AppendAllText(logPath, message);
        }
        catch
        {
            // Ignore logging failure
        }
    }
}
