using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

/// <summary>
/// Handles persistent storage of Streak settings and profile data (streaks.json).
/// Guarantees that scheduled holidays, sick days, gold stars spent, and all streak settings
/// are reliably preserved and migrated across application updates and legacy installations.
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

    public static void SetInstanceForTesting(StreakStorage? instance)
    {
        _instance = instance;
    }

    public static void ResetInstance()
    {
        _instance = null;
    }

    public event Action? StreakDataChanged;

    public StreakStorage(string? filePath = null)
    {
        _customFilePath = filePath;
    }

    public StreakProfile Load()
    {
        StreakProfile? profile = null;
        bool loadedFromPrimary = false;

        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath).Trim();
                if (!string.IsNullOrWhiteSpace(json))
                {
                    profile = DeserializeProfile(json);
                    if (profile != null)
                    {
                        loadedFromPrimary = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "StreakStorage.LoadPrimary");
        }

        profile ??= new StreakProfile();

        // Scan candidate legacy files to restore scheduled holidays, sick days, gold stars spent, etc. across updates
        if (_customFilePath == null)
        {
            try
            {
                bool hasMerged = MergeFromCandidateLegacyStreaks(profile);
                if (!loadedFromPrimary || hasMerged)
                {
                    Save(profile);
                }
            }
            catch (Exception ex)
            {
                StoragePathHelper.LogError(ex, "StreakStorage.MergeLegacy");
            }
        }

        EnsureProfileDefaults(profile);
        return profile;
    }

    public void Save(StreakProfile profile)
    {
        try
        {
            EnsureProfileDefaults(profile);

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

    private static void EnsureProfileDefaults(StreakProfile profile)
    {
        profile.ExcludedDays ??= new() { DayOfWeek.Saturday, DayOfWeek.Sunday };
        profile.Holidays ??= new();
        profile.UsedSickDates ??= new();
        profile.RevokedSickDates ??= new();
        profile.RestDayPurchases ??= new();
        profile.CelebratedMilestoneDays ??= new();
    }

    public static StreakProfile DeserializeProfile(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new StreakProfile();
        }

        StreakProfile profile;
        try
        {
            profile = JsonSerializer.Deserialize<StreakProfile>(json, JsonOptions) ?? new StreakProfile();
        }
        catch
        {
            profile = new StreakProfile();
        }

        EnsureProfileDefaults(profile);

        // Fallback parse using JsonDocument for nested structures or property aliases
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement targetElement = root;
            if (root.ValueKind == JsonValueKind.Object)
            {
                var candidateContainers = new[] { "Streak", "streak", "StreakProfile", "streakProfile", "StreakSettings", "streakSettings", "Profile", "profile" };
                foreach (var container in candidateContainers)
                {
                    if (root.TryGetProperty(container, out var nested) && nested.ValueKind == JsonValueKind.Object)
                    {
                        targetElement = nested;
                        break;
                    }
                }
            }

            if (targetElement.ValueKind == JsonValueKind.Object)
            {
                // Holidays alias parsing
                if (profile.Holidays == null || profile.Holidays.Count == 0)
                {
                    var holidayAliases = new[] { "Holidays", "holidays", "HolidayList", "holidayList", "ScheduledHolidays", "scheduledHolidays", "Vacations", "vacations" };
                    foreach (var alias in holidayAliases)
                    {
                        if (targetElement.TryGetProperty(alias, out var holProp) && holProp.ValueKind == JsonValueKind.Array)
                        {
                            profile.Holidays ??= new();
                            foreach (var item in holProp.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.Object)
                                {
                                    DateTime dateVal = DateTime.MinValue;
                                    string descVal = string.Empty;
                                    string idVal = Guid.NewGuid().ToString("N");

                                    if (item.TryGetProperty("Date", out var dP) || item.TryGetProperty("date", out dP))
                                    {
                                        if (dP.TryGetDateTime(out var dt)) dateVal = dt;
                                        else if (DateTime.TryParse(dP.GetString(), out dt)) dateVal = dt;
                                    }

                                    if (item.TryGetProperty("Description", out var descP) || item.TryGetProperty("description", out descP) || item.TryGetProperty("Name", out descP) || item.TryGetProperty("name", out descP))
                                    {
                                        descVal = descP.GetString() ?? string.Empty;
                                    }

                                    if (item.TryGetProperty("Id", out var idP) || item.TryGetProperty("id", out idP))
                                    {
                                        idVal = idP.GetString() ?? idVal;
                                    }

                                    if (dateVal != DateTime.MinValue)
                                    {
                                        profile.Holidays.Add(new HolidayItem
                                        {
                                            Id = idVal,
                                            Date = dateVal.Date,
                                            Description = descVal
                                        });
                                    }
                                }
                                else if (item.ValueKind == JsonValueKind.String)
                                {
                                    if (DateTime.TryParse(item.GetString(), out var dt))
                                    {
                                        profile.Holidays.Add(new HolidayItem
                                        {
                                            Date = dt.Date,
                                            Description = "Scheduled Holiday"
                                        });
                                    }
                                }
                            }
                            if (profile.Holidays.Count > 0) break;
                        }
                    }
                }

                // GoldStarsSpent alias parsing
                if (profile.GoldStarsSpent == 0)
                {
                    var goldSpentAliases = new[] { "GoldStarsSpent", "goldStarsSpent", "gold_stars_spent", "SpentGoldStars", "spentGoldStars", "StarsSpent", "starsSpent" };
                    foreach (var alias in goldSpentAliases)
                    {
                        if (targetElement.TryGetProperty(alias, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var val) && val > 0)
                        {
                            profile.GoldStarsSpent = val;
                            break;
                        }
                    }
                }

                // BonusGoldStars alias parsing
                if (profile.BonusGoldStars == 0)
                {
                    var bonusAliases = new[] { "BonusGoldStars", "bonusGoldStars", "bonus_gold_stars", "BonusStars", "bonusStars" };
                    foreach (var alias in bonusAliases)
                    {
                        if (targetElement.TryGetProperty(alias, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var val) && val > 0)
                        {
                            profile.BonusGoldStars = val;
                            break;
                        }
                    }
                }

                // UsedSickDates alias parsing
                if (profile.UsedSickDates == null || profile.UsedSickDates.Count == 0)
                {
                    var sickAliases = new[] { "UsedSickDates", "usedSickDates", "used_sick_dates", "SickDates", "sickDates", "UsedSickDays", "usedSickDays" };
                    foreach (var alias in sickAliases)
                    {
                        if (targetElement.TryGetProperty(alias, out var prop) && prop.ValueKind == JsonValueKind.Array)
                        {
                            profile.UsedSickDates ??= new();
                            foreach (var item in prop.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String && DateTime.TryParse(item.GetString(), out var dt))
                                {
                                    profile.UsedSickDates.Add(dt.Date);
                                }
                            }
                            if (profile.UsedSickDates.Count > 0) break;
                        }
                    }
                }

                // RevokedSickDates alias parsing
                if (profile.RevokedSickDates == null || profile.RevokedSickDates.Count == 0)
                {
                    var revokedAliases = new[] { "RevokedSickDates", "revokedSickDates", "revoked_sick_dates", "RevokedSickDays", "revokedSickDays" };
                    foreach (var alias in revokedAliases)
                    {
                        if (targetElement.TryGetProperty(alias, out var prop) && prop.ValueKind == JsonValueKind.Array)
                        {
                            profile.RevokedSickDates ??= new();
                            foreach (var item in prop.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String && DateTime.TryParse(item.GetString(), out var dt))
                                {
                                    profile.RevokedSickDates.Add(dt.Date);
                                }
                            }
                            if (profile.RevokedSickDates.Count > 0) break;
                        }
                    }
                }

                // ReturnedSickDays alias parsing
                if (profile.ReturnedSickDays == 0)
                {
                    var returnedAliases = new[] { "ReturnedSickDays", "returnedSickDays", "returned_sick_days", "ReturnedSickCredits", "returnedSickCredits" };
                    foreach (var alias in returnedAliases)
                    {
                        if (targetElement.TryGetProperty(alias, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var val) && val > 0)
                        {
                            profile.ReturnedSickDays = val;
                            break;
                        }
                    }
                }

                // RestDayPurchases alias parsing
                if (profile.RestDayPurchases == null || profile.RestDayPurchases.Count == 0)
                {
                    var restPurchasesAliases = new[] { "RestDayPurchases", "restDayPurchases", "rest_day_purchases", "PurchasedRestDays", "purchasedRestDays" };
                    foreach (var alias in restPurchasesAliases)
                    {
                        if (targetElement.TryGetProperty(alias, out var prop) && prop.ValueKind == JsonValueKind.Array)
                        {
                            profile.RestDayPurchases ??= new();
                            foreach (var item in prop.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.Object)
                                {
                                    var purchase = new RestDayPurchase();
                                    if (item.TryGetProperty("Id", out var idP) || item.TryGetProperty("id", out idP))
                                    {
                                        purchase.Id = idP.GetString() ?? purchase.Id;
                                    }
                                    if (item.TryGetProperty("PurchasedAt", out var atP) || item.TryGetProperty("purchasedAt", out atP))
                                    {
                                        if (atP.TryGetDateTime(out var dt)) purchase.PurchasedAt = dt;
                                        else if (DateTime.TryParse(atP.GetString(), out dt)) purchase.PurchasedAt = dt;
                                    }
                                    if (item.TryGetProperty("CostInGoldStars", out var costP) || item.TryGetProperty("costInGoldStars", out costP))
                                    {
                                        if (costP.TryGetInt32(out var cost)) purchase.CostInGoldStars = cost;
                                    }
                                    profile.RestDayPurchases.Add(purchase);
                                }
                            }
                            if (profile.RestDayPurchases.Count > 0) break;
                        }
                    }
                }
            }
        }
        catch { }

        EnsureProfileDefaults(profile);
        return profile;
    }

    public static bool MergeFromCandidateLegacyStreaks(StreakProfile targetProfile, IEnumerable<string>? customLegacyDirectories = null)
    {
        bool anyMerged = false;
        var candidateNames = StoragePathHelper.GetCandidateFileNames("streaks.json", "streak-settings.json");
        List<string> legacyDirs;

        if (customLegacyDirectories != null)
        {
            legacyDirs = customLegacyDirectories.ToList();
        }
        else
        {
            legacyDirs = StoragePathHelper.GetCandidateLegacyDirectories().ToList();
            var currentFolder = StoragePathHelper.GetDataFolder();
            legacyDirs.Insert(0, currentFolder);
        }

        var discoveredFiles = new List<(string FilePath, DateTime LastWriteTime)>();

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
                        var fullPath = Path.GetFullPath(candidateFile);
                        if (!discoveredFiles.Any(f => string.Equals(f.FilePath, fullPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            discoveredFiles.Add((fullPath, File.GetLastWriteTimeUtc(fullPath)));
                        }
                    }
                    catch { }
                }
            }
        }

        targetProfile.Holidays ??= new();
        targetProfile.UsedSickDates ??= new();
        targetProfile.RevokedSickDates ??= new();
        targetProfile.RestDayPurchases ??= new();
        targetProfile.CelebratedMilestoneDays ??= new();
        targetProfile.ExcludedDays ??= new() { DayOfWeek.Saturday, DayOfWeek.Sunday };

        // Process discovered files in descending order of last write time
        foreach (var (filePath, _) in discoveredFiles.OrderByDescending(f => f.LastWriteTime))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var legacyProfile = DeserializeProfile(json);

                // 1. Merge Scheduled Holidays
                if (legacyProfile.Holidays != null && legacyProfile.Holidays.Count > 0)
                {
                    foreach (var legacyH in legacyProfile.Holidays)
                    {
                        var existing = targetProfile.Holidays.FirstOrDefault(h => h.Date.Date == legacyH.Date.Date);
                        if (existing == null)
                        {
                            targetProfile.Holidays.Add(new HolidayItem
                            {
                                Id = legacyH.Id,
                                Date = legacyH.Date.Date,
                                Description = legacyH.Description
                            });
                            anyMerged = true;
                        }
                        else if (string.IsNullOrWhiteSpace(existing.Description) && !string.IsNullOrWhiteSpace(legacyH.Description))
                        {
                            existing.Description = legacyH.Description;
                            anyMerged = true;
                        }
                    }
                }

                // 2. Merge Sick Days (UsedSickDates, RevokedSickDates, ReturnedSickDays)
                if (legacyProfile.UsedSickDates != null && legacyProfile.UsedSickDates.Count > 0)
                {
                    foreach (var sickDate in legacyProfile.UsedSickDates)
                    {
                        var dateOnly = sickDate.Date;
                        if (!targetProfile.UsedSickDates.Any(d => d.Date == dateOnly))
                        {
                            targetProfile.UsedSickDates.Add(dateOnly);
                            anyMerged = true;
                        }
                    }
                }

                if (legacyProfile.RevokedSickDates != null && legacyProfile.RevokedSickDates.Count > 0)
                {
                    foreach (var revokedDate in legacyProfile.RevokedSickDates)
                    {
                        var dateOnly = revokedDate.Date;
                        if (!targetProfile.RevokedSickDates.Any(d => d.Date == dateOnly))
                        {
                            targetProfile.RevokedSickDates.Add(dateOnly);
                            anyMerged = true;
                        }
                    }
                }

                if (legacyProfile.ReturnedSickDays > targetProfile.ReturnedSickDays)
                {
                    targetProfile.ReturnedSickDays = legacyProfile.ReturnedSickDays;
                    anyMerged = true;
                }

                // 3. Merge Gold Stars Spent and Bonus Stars
                if (legacyProfile.GoldStarsSpent > targetProfile.GoldStarsSpent)
                {
                    targetProfile.GoldStarsSpent = legacyProfile.GoldStarsSpent;
                    anyMerged = true;
                }

                if (legacyProfile.BonusGoldStars > targetProfile.BonusGoldStars)
                {
                    targetProfile.BonusGoldStars = legacyProfile.BonusGoldStars;
                    anyMerged = true;
                }

                // 4. Merge Rest Day Purchases
                if (legacyProfile.RestDayPurchases != null && legacyProfile.RestDayPurchases.Count > 0)
                {
                    foreach (var purchase in legacyProfile.RestDayPurchases)
                    {
                        if (!targetProfile.RestDayPurchases.Any(p => p.Id == purchase.Id || (p.PurchasedAt == purchase.PurchasedAt && p.CostInGoldStars == purchase.CostInGoldStars)))
                        {
                            targetProfile.RestDayPurchases.Add(new RestDayPurchase
                            {
                                Id = purchase.Id,
                                PurchasedAt = purchase.PurchasedAt,
                                CostInGoldStars = purchase.CostInGoldStars
                            });
                            anyMerged = true;
                        }
                    }
                }

                // 5. Merge Excluded Days (Rest days)
                if (legacyProfile.ExcludedDays != null && legacyProfile.ExcludedDays.Count > 0)
                {
                    var isDefaultTarget = targetProfile.ExcludedDays.Count == 2 &&
                                          targetProfile.ExcludedDays.Contains(DayOfWeek.Saturday) &&
                                          targetProfile.ExcludedDays.Contains(DayOfWeek.Sunday);
                    var isDefaultLegacy = legacyProfile.ExcludedDays.Count == 2 &&
                                          legacyProfile.ExcludedDays.Contains(DayOfWeek.Saturday) &&
                                          legacyProfile.ExcludedDays.Contains(DayOfWeek.Sunday);

                    if (isDefaultTarget && !isDefaultLegacy)
                    {
                        targetProfile.ExcludedDays = legacyProfile.ExcludedDays.Distinct().ToList();
                        anyMerged = true;
                    }
                }

                // 6. Merge AutoApplySickDays and Enabled
                if (targetProfile.AutoApplySickDays && !legacyProfile.AutoApplySickDays)
                {
                    targetProfile.AutoApplySickDays = false;
                    anyMerged = true;
                }

                if (targetProfile.Enabled && !legacyProfile.Enabled)
                {
                    targetProfile.Enabled = false;
                    anyMerged = true;
                }

                // 7. Merge Celebrated Milestone Days
                if (legacyProfile.CelebratedMilestoneDays != null && legacyProfile.CelebratedMilestoneDays.Count > 0)
                {
                    foreach (var m in legacyProfile.CelebratedMilestoneDays)
                    {
                        if (!targetProfile.CelebratedMilestoneDays.Contains(m))
                        {
                            targetProfile.CelebratedMilestoneDays.Add(m);
                            anyMerged = true;
                        }
                    }
                }

                // 8. Merge Monthly Sick Days Allowance if customized
                if (targetProfile.MonthlySickDaysAllowance == 3 && legacyProfile.MonthlySickDaysAllowance != 3 && legacyProfile.MonthlySickDaysAllowance > 0)
                {
                    targetProfile.MonthlySickDaysAllowance = legacyProfile.MonthlySickDaysAllowance;
                    anyMerged = true;
                }
            }
            catch { }
        }

        return anyMerged;
    }
}
