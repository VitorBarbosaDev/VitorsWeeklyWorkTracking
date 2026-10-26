using System;
using System.Collections.Generic;
using System.Linq;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class MilestoneService
{
    private static readonly Lazy<MilestoneService> _lazyInstance = new(() => new MilestoneService());
    public static MilestoneService Instance => _lazyInstance.Value;

    private readonly ProjectStorage _projectStorage;
    private readonly object _lock = new();

    public event Action? MilestonesChanged;

    public MilestoneService(ProjectStorage? projectStorage = null)
    {
        _projectStorage = projectStorage ?? new ProjectStorage();
    }

    /// <summary>
    /// Gets all milestones across projects.
    /// </summary>
    public List<ProjectMilestone> GetAllMilestones(bool includeInactive = false)
    {
        lock (_lock)
        {
            var projects = _projectStorage.Load();
            var list = new List<ProjectMilestone>();

            foreach (var proj in projects)
            {
                if (!includeInactive && !proj.IsActive)
                    continue;

                if (proj.Milestones != null)
                {
                    foreach (var m in proj.Milestones)
                    {
                        // Ensure project name matches parent project
                        m.ProjectName = proj.Name;
                        list.Add(m);
                    }
                }
            }

            return list.OrderBy(m => m.ProjectName)
                       .ThenBy(m => m.Order)
                       .ThenBy(m => m.TargetDate)
                       .ToList();
        }
    }

    /// <summary>
    /// Gets milestones for a specific project name.
    /// </summary>
    public List<ProjectMilestone> GetMilestonesForProject(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return new List<ProjectMilestone>();

        lock (_lock)
        {
            var projects = _projectStorage.Load();
            var proj = projects.FirstOrDefault(p => p.Name.Equals(projectName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (proj == null || proj.Milestones == null)
                return new List<ProjectMilestone>();

            foreach (var m in proj.Milestones)
            {
                m.ProjectName = proj.Name;
            }

            return proj.Milestones.OrderBy(m => m.Order)
                                  .ThenBy(m => m.TargetDate)
                                  .ToList();
        }
    }

    /// <summary>
    /// Returns the active reminder milestones for daily popups and home page widgets.
    /// In Queue Mode (forceNextOnly = true or per project), only the next upcoming uncompleted milestone per project is returned.
    /// In All mode (forceNextOnly = false), all uncompleted milestones are returned.
    /// </summary>
    public List<ProjectMilestone> GetActiveReminders(bool? forceNextOnly = null)
    {
        lock (_lock)
        {
            var projects = _projectStorage.Load();
            var reminders = new List<ProjectMilestone>();

            foreach (var proj in projects.Where(p => p.IsActive))
            {
                if (proj.Milestones == null || proj.Milestones.Count == 0)
                    continue;

                var uncompleted = proj.Milestones
                    .Where(m => !m.IsCompleted)
                    .OrderBy(m => m.Order)
                    .ThenBy(m => m.TargetDate)
                    .ToList();

                if (uncompleted.Count == 0)
                    continue;

                bool nextOnly = forceNextOnly ?? proj.ShowOnlyNextMilestone;

                if (nextOnly)
                {
                    var next = uncompleted.First();
                    next.ProjectName = proj.Name;
                    reminders.Add(next);
                }
                else
                {
                    foreach (var m in uncompleted)
                    {
                        m.ProjectName = proj.Name;
                        reminders.Add(m);
                    }
                }
            }

            return reminders.OrderBy(m => m.TargetDate)
                            .ThenBy(m => m.Order)
                            .ToList();
        }
    }

    /// <summary>
    /// Gets milestone by its unique ID.
    /// </summary>
    public ProjectMilestone? GetMilestoneById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        lock (_lock)
        {
            var projects = _projectStorage.Load();
            foreach (var p in projects)
            {
                if (p.Milestones == null) continue;
                var found = p.Milestones.FirstOrDefault(m => m.Id == id);
                if (found != null)
                {
                    found.ProjectName = p.Name;
                    return found;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Adds a new milestone to the target project.
    /// </summary>
    public void AddMilestone(ProjectMilestone milestone)
    {
        if (milestone == null || string.IsNullOrWhiteSpace(milestone.ProjectName))
            return;

        lock (_lock)
        {
            var projects = _projectStorage.Load();
            var proj = projects.FirstOrDefault(p => p.Name.Equals(milestone.ProjectName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (proj == null)
            {
                // Create project if missing
                proj = new Project
                {
                    Name = milestone.ProjectName.Trim(),
                    IsActive = true
                };
                projects.Add(proj);
            }

            proj.Milestones ??= new List<ProjectMilestone>();

            if (string.IsNullOrWhiteSpace(milestone.Id))
            {
                milestone.Id = Guid.NewGuid().ToString("N");
            }

            // Assign order if not set
            if (milestone.Order == 0 && proj.Milestones.Count > 0)
            {
                milestone.Order = proj.Milestones.Max(m => m.Order) + 1;
            }

            milestone.ProjectName = proj.Name;
            proj.Milestones.Add(milestone);

            _projectStorage.Save(projects);
        }

        NotifyChanged();
    }

    /// <summary>
    /// Updates an existing milestone.
    /// </summary>
    public bool UpdateMilestone(ProjectMilestone updated)
    {
        if (updated == null || string.IsNullOrWhiteSpace(updated.Id))
            return false;

        bool success = false;
        lock (_lock)
        {
            var projects = _projectStorage.Load();
            
            // Check if project name changed
            Project? originalProject = null;
            ProjectMilestone? existingMilestone = null;

            foreach (var p in projects)
            {
                if (p.Milestones == null) continue;
                existingMilestone = p.Milestones.FirstOrDefault(m => m.Id == updated.Id);
                if (existingMilestone != null)
                {
                    originalProject = p;
                    break;
                }
            }

            if (originalProject == null || existingMilestone == null)
                return false;

            string targetProjectName = string.IsNullOrWhiteSpace(updated.ProjectName) ? originalProject.Name : updated.ProjectName.Trim();

            if (originalProject.Name.Equals(targetProjectName, StringComparison.OrdinalIgnoreCase))
            {
                // In-place update
                existingMilestone.Title = updated.Title;
                existingMilestone.TargetDate = updated.TargetDate;
                existingMilestone.Description = updated.Description;
                existingMilestone.IsCompleted = updated.IsCompleted;
                existingMilestone.CompletedAt = updated.IsCompleted ? (updated.CompletedAt ?? DateTime.Now) : null;
                existingMilestone.Order = updated.Order;
                success = true;
            }
            else
            {
                // Moved to another project
                originalProject.Milestones.Remove(existingMilestone);

                var targetProj = projects.FirstOrDefault(p => p.Name.Equals(targetProjectName, StringComparison.OrdinalIgnoreCase));
                if (targetProj == null)
                {
                    targetProj = new Project { Name = targetProjectName, IsActive = true };
                    projects.Add(targetProj);
                }
                targetProj.Milestones ??= new List<ProjectMilestone>();

                updated.ProjectName = targetProj.Name;
                targetProj.Milestones.Add(updated);
                success = true;
            }

            if (success)
            {
                _projectStorage.Save(projects);
            }
        }

        if (success)
        {
            NotifyChanged();
        }

        return success;
    }

    /// <summary>
    /// Deletes a milestone by ID.
    /// </summary>
    public bool DeleteMilestone(string milestoneId)
    {
        if (string.IsNullOrWhiteSpace(milestoneId))
            return false;

        bool deleted = false;
        lock (_lock)
        {
            var projects = _projectStorage.Load();
            foreach (var p in projects)
            {
                if (p.Milestones == null) continue;
                int count = p.Milestones.RemoveAll(m => m.Id == milestoneId);
                if (count > 0)
                {
                    deleted = true;
                    break;
                }
            }

            if (deleted)
            {
                _projectStorage.Save(projects);
            }
        }

        if (deleted)
        {
            NotifyChanged();
        }

        return deleted;
    }

    /// <summary>
    /// Toggles the completion state of a milestone.
    /// </summary>
    public bool ToggleMilestoneCompletion(string milestoneId)
    {
        if (string.IsNullOrWhiteSpace(milestoneId))
            return false;

        bool updated = false;
        lock (_lock)
        {
            var projects = _projectStorage.Load();
            foreach (var p in projects)
            {
                if (p.Milestones == null) continue;
                var m = p.Milestones.FirstOrDefault(x => x.Id == milestoneId);
                if (m != null)
                {
                    m.IsCompleted = !m.IsCompleted;
                    m.CompletedAt = m.IsCompleted ? DateTime.Now : null;
                    updated = true;
                    break;
                }
            }

            if (updated)
            {
                _projectStorage.Save(projects);
            }
        }

        if (updated)
        {
            NotifyChanged();
        }

        return updated;
    }

    /// <summary>
    /// Reorders a milestone within its project (direction: -1 for up, +1 for down).
    /// </summary>
    public bool ReorderMilestone(string projectName, string milestoneId, int direction)
    {
        if (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(milestoneId))
            return false;

        bool changed = false;
        lock (_lock)
        {
            var projects = _projectStorage.Load();
            var proj = projects.FirstOrDefault(p => p.Name.Equals(projectName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (proj == null || proj.Milestones == null || proj.Milestones.Count <= 1)
                return false;

            // Sort milestones by current Order
            var ordered = proj.Milestones.OrderBy(m => m.Order).ThenBy(m => m.TargetDate).ToList();
            int currentIndex = ordered.FindIndex(m => m.Id == milestoneId);
            if (currentIndex == -1)
                return false;

            int targetIndex = currentIndex + direction;
            if (targetIndex < 0 || targetIndex >= ordered.Count)
                return false;

            // Swap
            var item = ordered[currentIndex];
            ordered.RemoveAt(currentIndex);
            ordered.Insert(targetIndex, item);

            // Reassign continuous Order values
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].Order = i;
            }

            proj.Milestones = ordered;
            _projectStorage.Save(projects);
            changed = true;
        }

        if (changed)
        {
            NotifyChanged();
        }

        return changed;
    }

    /// <summary>
    /// Updates the queue mode for a specific project.
    /// </summary>
    public void SetProjectQueueMode(string projectName, bool showOnlyNext)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return;

        lock (_lock)
        {
            var projects = _projectStorage.Load();
            var proj = projects.FirstOrDefault(p => p.Name.Equals(projectName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (proj != null)
            {
                proj.ShowOnlyNextMilestone = showOnlyNext;
                _projectStorage.Save(projects);
            }
        }

        NotifyChanged();
    }

    public void NotifyChanged()
    {
        MilestonesChanged?.Invoke();
    }
}
