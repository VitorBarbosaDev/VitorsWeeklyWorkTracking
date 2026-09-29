using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public class TimeEntryStorage
{
    private readonly string _filePath;

    public TimeEntryStorage()
    {
        _filePath = StoragePathHelper.GetFilePath("time-entries.json");
    }

    public List<TimeEntry> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new List<TimeEntry>();

            string json = File.ReadAllText(_filePath);

            return JsonSerializer.Deserialize<List<TimeEntry>>(json)
                ?? new List<TimeEntry>();
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "TimeEntryStorage.Load");
            return new List<TimeEntry>();
        }
    }

    public void Save(List<TimeEntry> entries)
    {
        try
        {
            string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
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
            StoragePathHelper.LogError(ex, "TimeEntryStorage.Save");
        }
    }
}