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

            // Check if existing projects list contains dummy placeholders (e.g. "Alpha", "Beta", "Long Milestone", "Quick Sync")
            // while real non-dummy projects exist. Clean up the dummy placeholders.
            bool hasRealProjects = discoveredProjects.Any(p => !IsDummyPlaceholder(p)) || list.Any(p => !IsDummyPlaceholder(p.Name));
            if (hasRealProjects)
            {
                int removedCount = list.RemoveAll(p => IsDummyPlaceholder(p.Name));
                if (removedCount > 0)
                {
                    needsSave = true;
                }
            }

            // Add any discovered projects that are not yet in the list
            foreach (var projName in discoveredProjects)
            {
                if (!list.Any(p => p.Name.Equals(projName, StringComparison.OrdinalIgnoreCase)))
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

        var nonDummy = result.Where(p => !IsDummyPlaceholder(p)).ToList();
        if (nonDummy.Count > 0)
        {
            return nonDummy.OrderBy(x => x).ToList();
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
               clean.Equals("End of Year Review", StringComparison.OrdinalIgnoreCase);
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
