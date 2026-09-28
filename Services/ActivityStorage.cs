using System.IO;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class ActivityStorage
{
    private readonly string _filePath = "activities.json";

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
        if (!File.Exists(_filePath))
        {
            var defaults = DefaultActivities.Select(name => new ActivityItem
            {
                Name = name,
                IsActive = true
            }).ToList();

            Save(defaults);
            return defaults;
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<List<ActivityItem>>(json);
            if (loaded == null || loaded.Count == 0)
            {
                var defaults = DefaultActivities.Select(name => new ActivityItem
                {
                    Name = name,
                    IsActive = true
                }).ToList();
                return defaults;
            }
            return loaded;
        }
        catch
        {
            return DefaultActivities.Select(name => new ActivityItem
            {
                Name = name,
                IsActive = true
            }).ToList();
        }
    }

    public void Save(List<ActivityItem> activities)
    {
        string json = JsonSerializer.Serialize(activities, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_filePath, json);
    }
}
