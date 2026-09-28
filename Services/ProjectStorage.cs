using System.IO;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class ProjectStorage
{
    private readonly string _filePath = "projects.json";

    public List<Project> Load()
    {
        if (!File.Exists(_filePath))
            return new List<Project>();

        try
        {
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
        }
        catch
        {
            return new List<Project>();
        }
    }

    public void Save(List<Project> projects)
    {
        string json = JsonSerializer.Serialize(projects, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_filePath, json);
    }
}
