using System;
using System.IO;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

/// <summary>
/// Handles persistent storage of Streak settings and profile data (streaks.json).
/// </summary>
public class StreakStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    private readonly string? _customFilePath;
    private string FilePath => _customFilePath ?? StoragePathHelper.GetFilePath("streaks.json", "streak-settings.json");

    private static StreakStorage? _instance;
    public static StreakStorage Instance => _instance ??= new StreakStorage();

    public event Action? StreakDataChanged;

    public StreakStorage(string? filePath = null)
    {
        _customFilePath = filePath;
    }

    public StreakProfile Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath).Trim();
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var profile = JsonSerializer.Deserialize<StreakProfile>(json, JsonOptions);
                    if (profile != null)
                    {
                        profile.ExcludedDays ??= new() { DayOfWeek.Saturday, DayOfWeek.Sunday };
                        profile.Holidays ??= new();
                        profile.UsedSickDates ??= new();
                        profile.RestDayPurchases ??= new();
                        profile.CelebratedMilestoneDays ??= new();
                        return profile;
                    }
                }
            }

            // Fallback scan candidate legacy directories
            var candidateNames = StoragePathHelper.GetCandidateFileNames("streaks.json", "streak-settings.json");
            var legacyDirs = StoragePathHelper.GetCandidateLegacyDirectories();

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
                            var json = File.ReadAllText(candidateFile).Trim();
                            if (!string.IsNullOrWhiteSpace(json))
                            {
                                var profile = JsonSerializer.Deserialize<StreakProfile>(json, JsonOptions);
                                if (profile != null)
                                {
                                    profile.ExcludedDays ??= new() { DayOfWeek.Saturday, DayOfWeek.Sunday };
                                    profile.Holidays ??= new();
                                    profile.UsedSickDates ??= new();
                                    profile.RestDayPurchases ??= new();
                                    profile.CelebratedMilestoneDays ??= new();
                                    Save(profile);
                                    return profile;
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "StreakStorage.Load");
        }

        return new StreakProfile();
    }

    public void Save(StreakProfile profile)
    {
        try
        {
            profile.ExcludedDays ??= new() { DayOfWeek.Saturday, DayOfWeek.Sunday };
            profile.Holidays ??= new();
            profile.UsedSickDates ??= new();
            profile.RestDayPurchases ??= new();
            profile.CelebratedMilestoneDays ??= new();

            var json = JsonSerializer.Serialize(profile, JsonOptions);
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                StoragePathHelper.EnsureDirectoryExists(dir);
            }

            File.WriteAllText(FilePath, json);
            StreakDataChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "StreakStorage.Save");
        }
    }
}
