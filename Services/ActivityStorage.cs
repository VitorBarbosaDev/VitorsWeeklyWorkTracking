using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class ActivityStorage
{
    private readonly string _filePath;

    public ActivityStorage(string? filePath = null)
    {
        _filePath = filePath ?? StoragePathHelper.GetFilePath("activities.json");
    }

    public static readonly string[] DefaultActivities = new[]
    {
        "Game Dev",
        "Coding",
        "Art",
        "Web Dev",
        "Research",
        "Design",
        "Writing",
        "Audio / Sound",
        "Testing",
        "Planning / Admin",
        "Other"
    };

    public List<ActivityItem> Load()
    {
        try
        {
            List<ActivityItem> list;
            bool needsSave = false;

            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                list = JsonSerializer.Deserialize<List<ActivityItem>>(json) ?? new List<ActivityItem>();
            }
            else
            {
                list = new List<ActivityItem>();
            }

            // Auto-discover pre-existing activities from time entries and calendar plans
            var discovered = DiscoverActivitiesFromData();

            // Safe Repair: Remove dummy placeholders only if they are NOT used in real data 
            // AND don't have any custom user-assigned colors. This cleans up accidental 
            // template data without losing user customizations.
            int removed = list.RemoveAll(a => IsDummyActivityPlaceholder(a.Name) && 
                                              !a.HasCustomColor && 
                                              !discovered.Contains(a.Name, StringComparer.OrdinalIgnoreCase));
            if (removed > 0)
            {
                needsSave = true;
            }

            // Add any discovered activities that are not yet in the list and are not dummies
            foreach (var actName in discovered)
            {
                if (!IsDummyActivityPlaceholder(actName) && !list.Any(a => a.Name.Equals(actName, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(new ActivityItem
                    {
                        Name = actName,
                        IsActive = true,
                        ColorHex = null // Default grey
                    });
                    needsSave = true;
                }
            }

            // If still completely empty, initialize with default activities
            if (list.Count == 0)
            {
                foreach (var defName in DefaultActivities)
                {
                    list.Add(new ActivityItem
                    {
                        Name = defName,
                        IsActive = true,
                        ColorHex = null
                    });
                }
                needsSave = true;
            }

            if (needsSave && list.Count > 0)
            {
                Save(list);
            }

            ActivityColorHelper.RegisterActivities(list);
            return list;
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ActivityStorage.Load");
            var fallback = DefaultActivities.Select(name => new ActivityItem
            {
                Name = name,
                IsActive = true
            }).ToList();
            ActivityColorHelper.RegisterActivities(fallback);
            return fallback;
        }
    }

    public List<ActivityItem> SyncActivities(List<ActivityItem> currentActivities, List<TimeEntry>? entries = null, List<PlannedWorkItem>? plans = null)
    {
        var discovered = DiscoverActivities(entries, plans);
        var added = new List<ActivityItem>();

        foreach (var actName in discovered)
        {
            if (!currentActivities.Any(a => a.Name.Equals(actName, StringComparison.OrdinalIgnoreCase)))
            {
                var newAct = new ActivityItem
                {
                    Name = actName,
                    IsActive = true,
                    ColorHex = null
                };
                currentActivities.Add(newAct);
                added.Add(newAct);
            }
        }

        if (added.Count > 0)
        {
            Save(currentActivities);
        }

        return added;
    }

    public List<string> DiscoverActivitiesFromData()
    {
        return DiscoverActivities(null, null);
    }

    private List<string> DiscoverActivities(List<TimeEntry>? entries, List<PlannedWorkItem>? plans)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (!string.IsNullOrWhiteSpace(e.Activity))
                        result.Add(e.Activity.Trim());
                }
            }
            else
            {
                var entriesPath = StoragePathHelper.GetFilePath("time-entries.json");
                if (File.Exists(entriesPath))
                {
                    string json = File.ReadAllText(entriesPath);
                    var list = JsonSerializer.Deserialize<List<TimeEntry>>(json);
                    if (list != null)
                    {
                        foreach (var e in list)
                        {
                            if (!string.IsNullOrWhiteSpace(e.Activity))
                                result.Add(e.Activity.Trim());
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ActivityStorage.DiscoverActivities.TimeEntries");
        }

        try
        {
            if (plans != null)
            {
                foreach (var p in plans)
                {
                    if (!string.IsNullOrWhiteSpace(p.Activity))
                        result.Add(p.Activity.Trim());
                }
            }
            else
            {
                var plansPath = StoragePathHelper.GetFilePath("calendar-plans.json");
                if (File.Exists(plansPath))
                {
                    string json = File.ReadAllText(plansPath);
                    var list = JsonSerializer.Deserialize<List<PlannedWorkItem>>(json);
                    if (list != null)
                    {
                        foreach (var p in list)
                        {
                            if (!string.IsNullOrWhiteSpace(p.Activity))
                                result.Add(p.Activity.Trim());
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ActivityStorage.DiscoverActivities.CalendarPlans");
        }

        return result.OrderBy(x => x).ToList();
    }

    private static bool IsDummyActivityPlaceholder(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;
        var clean = name.Trim();
        
        // Examples and placeholders used in tests or legacy versions
        if (clean.Equals("Marketing", StringComparison.OrdinalIgnoreCase) ||
            clean.Equals("Activity Alpha", StringComparison.OrdinalIgnoreCase) ||
            clean.Equals("Activity Beta", StringComparison.OrdinalIgnoreCase) ||
            clean.Equals("Development", StringComparison.OrdinalIgnoreCase) ||
            clean.Equals("Study", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Current default activity set - these should be considered removable if unused and unstyled
        return DefaultActivities.Contains(clean, StringComparer.OrdinalIgnoreCase);
    }

    public void Save(List<ActivityItem> activities)
    {
        try
        {
            ActivityColorHelper.RegisterActivities(activities);
            string json = JsonSerializer.Serialize(activities, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                StoragePathHelper.EnsureDirectoryExists(dir);
            }

            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ActivityStorage.Save");
        }
    }
}
