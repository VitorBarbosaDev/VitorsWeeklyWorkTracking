using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace VitorsWeeklyWorkTracking.Models;

/// <summary>
/// Metadata and statistics describing a data backup snapshot.
/// </summary>
public class BackupInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string BackupDirectory { get; set; } = string.Empty;

    public string? ZipPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string Reason { get; set; } = "Auto Backup";

    public string Note { get; set; } = string.Empty;

    public bool IsManual { get; set; } = false;

    public int TimeEntryCount { get; set; }

    public int ProjectCount { get; set; }

    public int ActivityCount { get; set; }

    public int CalendarPlanCount { get; set; }

    public long TotalSizeBytes { get; set; }

    public List<string> IncludedFiles { get; set; } = new();

    [JsonIgnore]
    public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

    [JsonIgnore]
    public string DisplayTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Note))
            {
                return $"{Reason}: {Note}";
            }
            return Reason;
        }
    }

    [JsonIgnore]
    public string BadgeText
    {
        get
        {
            if (IsManual) return "Manual";
            if (Reason.Contains("Safety", StringComparison.OrdinalIgnoreCase)) return "Safety";
            if (Reason.Contains("Pre-Update", StringComparison.OrdinalIgnoreCase)) return "Pre-Update";
            if (Reason.Contains("Import", StringComparison.OrdinalIgnoreCase)) return "Imported";
            return "Auto";
        }
    }

    [JsonIgnore]
    public string BadgeBackgroundHex
    {
        get
        {
            if (IsManual) return "#2563EB"; // Blue
            if (Reason.Contains("Safety", StringComparison.OrdinalIgnoreCase)) return "#D97706"; // Amber
            if (Reason.Contains("Pre-Update", StringComparison.OrdinalIgnoreCase)) return "#9333EA"; // Purple
            if (Reason.Contains("Import", StringComparison.OrdinalIgnoreCase)) return "#0D9488"; // Teal
            return "#64748B"; // Slate
        }
    }

    [JsonIgnore]
    public string FormattedSize
    {
        get
        {
            if (TotalSizeBytes < 1024) return $"{TotalSizeBytes} B";
            if (TotalSizeBytes < 1024 * 1024) return $"{TotalSizeBytes / 1024.0:F1} KB";
            return $"{TotalSizeBytes / (1024.0 * 1024.0):F2} MB";
        }
    }

    [JsonIgnore]
    public string SummaryText =>
        $"{TimeEntryCount} entries • {ProjectCount} projects • {ActivityCount} activities • {CalendarPlanCount} plans";

    [JsonIgnore]
    public string FullDetailsText =>
        $"Created: {FormattedCreatedAt}\nTrigger: {Reason}\nSize: {FormattedSize}\nContents: {TimeEntryCount} time entries, {ProjectCount} projects, {ActivityCount} activities, {CalendarPlanCount} calendar plans\nFiles: {string.Join(", ", IncludedFiles)}";
}
