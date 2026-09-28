using System.IO;
using VitorsWeeklyWorkTracking.Models;
namespace VitorsWeeklyWorkTracking;

using System.Text.Json;

public class TimeEntryStorage
{
    private readonly string _filePath = "time-entries.json";

    public List<TimeEntry> Load()
    {
        if (!File.Exists(_filePath))
            return new List<TimeEntry>();

        string json = File.ReadAllText(_filePath);

        return JsonSerializer.Deserialize<List<TimeEntry>>(json)
            ?? new List<TimeEntry>();
    }

    public void Save(List<TimeEntry> entries)
    {
        string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_filePath, json);
    }
}