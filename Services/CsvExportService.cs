using System.IO;
using VitorsWeeklyWorkTracking.Models;
namespace VitorsWeeklyWorkTracking;

using System.Text;

public static class CsvExportService
{
    public static void Export(List<TimeEntry> entries, string filePath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Project,Activity,Description,Note,StartTime,EndTime,DurationMinutes"); 

        foreach (var entry in entries)
        {
            sb.AppendLine(
                $"{Escape(entry.ProjectName)}," +
                $"{Escape(entry.Activity)}," +
                $"{Escape(entry.Description)}," +
                $"{Escape(entry.Note)}," +
                $"{entry.StartTime}," +
                $"{entry.EndTime}," +
                $"{entry.Duration.TotalMinutes:F2}"
            );
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    private static string Escape(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}