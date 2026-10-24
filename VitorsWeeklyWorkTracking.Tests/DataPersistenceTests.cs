using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

[Collection("StorageTests")]
public class DataPersistenceTests
{
    [Fact]
    public void Load_DoesNotRemoveUserProjectsEvenIfTheyMatchDummyNames()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var projectsFile = Path.Combine(tempDir, "projects.json");
            var entriesFile = Path.Combine(tempDir, "time-entries.json");
            
            // Create a projects file with a "dummy" name but custom color
            var initialProjects = new List<Project>
            {
                new Project { Name = "Alpha", IsActive = true, ColorHex = "#FF0000" },
                new Project { Name = "Real Project", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(projectsFile, JsonSerializer.Serialize(initialProjects));

            // Create an empty entries file so discovery doesn't find "Alpha"
            File.WriteAllText(entriesFile, "[]");

            // We need to trick StoragePathHelper to use our temp files, but ProjectStorage 
            // allows passing a path directly. However, it might still call StoragePathHelper
            // for discovery. 
            // Actually ProjectStorage calls StoragePathHelper.GetFilePath("time-entries.json")
            // inside DiscoverProjects. This is hard to mock without changing StoragePathHelper.
            
            // But wait, the Load() method in ProjectStorage uses the _filePath passed in constructor
            // for loading projects, but it uses StoragePathHelper for discovery.
            
            var storage = new ProjectStorage(projectsFile);
            var loaded = storage.Load();

            // REPRODUCTION OF BUG: "Alpha" should be there, but it will be removed by current code.
            var alpha = loaded.FirstOrDefault(p => p.Name == "Alpha");
            Assert.NotNull(alpha);
            Assert.Equal("#FF0000", alpha.ColorHex);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_DoesNotRemoveUserActivitiesEvenIfTheyMatchDummyNames()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_act_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var activitiesFile = Path.Combine(tempDir, "activities.json");
            
            var initialActivities = new List<ActivityItem>
            {
                new ActivityItem { Name = "Marketing", IsActive = true, ColorHex = "#FF0000" },
                new ActivityItem { Name = "Real Activity", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(activitiesFile, JsonSerializer.Serialize(initialActivities));

            var storage = new ActivityStorage(activitiesFile);
            var loaded = storage.Load();

            var marketing = loaded.FirstOrDefault(a => a.Name == "Marketing");
            Assert.NotNull(marketing);
            Assert.Equal("#FF0000", marketing.ColorHex);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_RepairsListByRemovingUnusedDummyItemsWithoutColors()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_repair_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var projectsFile = Path.Combine(tempDir, "projects.json");
            
            // Create a projects file with a dummy name, NO color
            var initialProjects = new List<Project>
            {
                new Project { Name = "Alpha", IsActive = true, ColorHex = null },
                new Project { Name = "Real Project", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(projectsFile, JsonSerializer.Serialize(initialProjects));

            var storage = new ProjectStorage(projectsFile);
            var loaded = storage.Load();

            // "Alpha" should be removed because it's a dummy, has no color, and is not discovered from data
            Assert.DoesNotContain(loaded, p => p.Name == "Alpha");
            Assert.Contains(loaded, p => p.Name == "Real Project");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_RemovesUnusedDefaultActivitiesWithoutColors()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_act_repair_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;

        try
        {
            var activitiesFile = Path.Combine(tempDir, "activities.json");
            
            // Create an activities file with a default name, NO color
            var initialActivities = new List<ActivityItem>
            {
                new ActivityItem { Name = "Game Dev", IsActive = true, ColorHex = null }, // Default activity
                new ActivityItem { Name = "Real Activity", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(activitiesFile, JsonSerializer.Serialize(initialActivities));

            var storage = new ActivityStorage(activitiesFile);
            var loaded = storage.Load();

            // "Game Dev" should be removed because it's now in IsDummyActivityPlaceholder
            Assert.DoesNotContain(loaded, a => a.Name == "Game Dev");
            Assert.Contains(loaded, a => a.Name == "Real Activity");
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_RemovesUnusedGeneralFreelanceProjectWithoutColors()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_proj_repair_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;

        try
        {
            var projectsFile = Path.Combine(tempDir, "projects.json");
            
            // Create a projects file with "General Freelance", NO color
            var initialProjects = new List<Project>
            {
                new Project { Name = "General Freelance", IsActive = true, ColorHex = null },
                new Project { Name = "Real Project", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(projectsFile, JsonSerializer.Serialize(initialProjects));

            var storage = new ProjectStorage(projectsFile);
            var loaded = storage.Load();

            // "General Freelance" should be removed if we update IsDummyPlaceholder to include it
            Assert.DoesNotContain(loaded, p => p.Name == "General Freelance");
            Assert.Contains(loaded, p => p.Name == "Real Project");
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
    [Fact]
    public void Load_RemovesUnusedDefaultActivitiesEvenWithDefaultColor()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_act_color_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;

        try
        {
            var activitiesFile = Path.Combine(tempDir, "activities.json");
            
            // Create an activities file with a default name, and the DEFAULT color hex
            var initialActivities = new List<ActivityItem>
            {
                new ActivityItem { Name = "Coding", IsActive = true, ColorHex = "#6B7280" }, // Default activity with default color
                new ActivityItem { Name = "Real Activity", IsActive = true, ColorHex = "#00FF00" }
            };
            File.WriteAllText(activitiesFile, JsonSerializer.Serialize(initialActivities));

            var storage = new ActivityStorage(activitiesFile);
            var loaded = storage.Load();

            // "Coding" should be removed because although it has ColorHex, it's the default color
            Assert.DoesNotContain(loaded, a => a.Name == "Coding");
            Assert.Contains(loaded, a => a.Name == "Real Activity");
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_InitializesWithGeneralFreelanceIfProjectListIsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"persistence_test_proj_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            StoragePathHelper.OverrideAppDataFolder = tempDir;
            var projectsFile = Path.Combine(tempDir, "projects.json");
            
            // Create an empty projects file
            File.WriteAllText(projectsFile, "[]");

            var storage = new ProjectStorage(projectsFile);
            var loaded = storage.Load();

            // Should be initialized with "General Freelance"
            Assert.Single(loaded);
            Assert.Equal("General Freelance", loaded[0].Name);
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
