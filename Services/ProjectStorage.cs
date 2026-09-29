using System.IO;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class ProjectStorage
{
    private readonly string _filePath;

    public ProjectStorage()
    {
        _filePath = StoragePathHelper.GetFilePath("projects.json");
    }

    public List<Project> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new List<Project>();

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ProjectStorage.Load");
            return new List<Project>();
        }
    }

    public void Save(List<Project> projects)
    {
        try
        {
            string json = JsonSerializer.Serialize(projects, new JsonSerializerOptions
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
            StoragePathHelper.LogError(ex, "ProjectStorage.Save");
        }
    }
}
