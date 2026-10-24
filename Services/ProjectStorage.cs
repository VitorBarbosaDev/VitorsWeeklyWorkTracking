using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class ProjectStorage
{
    private readonly string _filePath;

    public ProjectStorage(string? filePath = null)
    {
        _filePath = filePath ?? StoragePathHelper.GetFilePath("projects.json");
    }

    public List<Project> Load()
    {
        try
        {
            List<Project> list;
            bool needsSave = false;

            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                list = JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
            }
            else
            {
                list = new List<Project>();
            }

            // Auto-discover pre-existing projects from time entries and calendar plans
            var discoveredProjects = DiscoverProjectsFromData();

            // Safe Repair: Remove dummy placeholders only if they are NOT used in real data 
            // AND don't have any custom user-assigned colors. This cleans up accidental 
            // template data without losing user customizations.
            int removed = list.RemoveAll(p => IsDummyPlaceholder(p.Name) && 
                                              !p.HasCustomColor && 
                                              !discoveredProjects.Contains(p.Name, StringComparer.OrdinalIgnoreCase));
            if (removed > 0)
            {
                needsSave = true;
            }

            // Only add discovered projects that are not yet in the list and are not dummies
            foreach (var projName in discoveredProjects)
            {
                if (!IsDummyPlaceholder(projName) && !list.Any(p => p.Name.Equals(projName, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(new Project
                    {
                        Name = projName,
                        IsActive = true,
                        ColorHex = null // Default grey
                    });
                    needsSave = true;
                }
            }

            // If still completely empty, initialize with default project
            if (list.Count == 0)
            {
                list.Add(new Project
                {
                    Name = "General Freelance",
                    IsActive = true,
                    ColorHex = null
                });
                needsSave = true;
            }

            if (needsSave && list.Count > 0)
            {
                Save(list);
            }

            ProjectColorHelper.RegisterProjects(list);
            return list;
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ProjectStorage.Load");
            return new List<Project>();
        }
    }

    public List<Project> SyncProjects(List<Project> currentProjects, List<TimeEntry>? entries = null, List<PlannedWorkItem>? plans = null)
    {
        var discovered = DiscoverProjects(entries, plans);
        var added = new List<Project>();

        foreach (var projName in discovered)
        {
            if (!currentProjects.Any(p => p.Name.Equals(projName, StringComparison.OrdinalIgnoreCase)))
            {
                var newProj = new Project
                {
                    Name = projName,
                    IsActive = true,
                    ColorHex = null
                };
                currentProjects.Add(newProj);
                added.Add(newProj);
            }
        }

        if (added.Count > 0)
        {
            Save(currentProjects);
        }

        return added;
    }

    public List<string> DiscoverProjectsFromData()
    {
        return DiscoverProjects(null, null);
    }

    private List<string> DiscoverProjects(List<TimeEntry>? entries, List<PlannedWorkItem>? plans)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (!string.IsNullOrWhiteSpace(e.ProjectName))
                        result.Add(e.ProjectName.Trim());
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
                            if (!string.IsNullOrWhiteSpace(e.ProjectName))
                                result.Add(e.ProjectName.Trim());
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ProjectStorage.DiscoverProjects.TimeEntries");
        }

        try
        {
            if (plans != null)
            {
                foreach (var p in plans)
                {
                    if (!string.IsNullOrWhiteSpace(p.ProjectName))
                        result.Add(p.ProjectName.Trim());
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
                            if (!string.IsNullOrWhiteSpace(p.ProjectName))
                                result.Add(p.ProjectName.Trim());
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ProjectStorage.DiscoverProjects.CalendarPlans");
        }

        return result.OrderBy(x => x).ToList();
    }

    private static bool IsDummyPlaceholder(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;
        var clean = name.Trim();
        return clean.Equals("Alpha", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("Beta", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("Project Alpha", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("Project Beta", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("Quick Sync", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("Long Milestone", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("Storage Query Test", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("Test Project", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("Client Website", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("End of Year Review", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("General Freelance", StringComparison.OrdinalIgnoreCase);
    }

    public void Save(List<Project> projects)
    {
        try
        {
            ProjectColorHelper.RegisterProjects(projects);
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
