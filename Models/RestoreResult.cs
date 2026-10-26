using System.Collections.Generic;

namespace VitorsWeeklyWorkTracking.Models;

/// <summary>
/// Result details of a backup restoration operation.
/// </summary>
public class RestoreResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public int RestoredFilesCount { get; set; }

    public bool SafetyBackupCreated { get; set; }

    public string? SafetyBackupPath { get; set; }

    public List<string> RestoredFiles { get; set; } = new();

    public static RestoreResult Ok(string message, int filesCount, List<string> files, bool safetyCreated = false, string? safetyPath = null)
    {
        return new RestoreResult
        {
            Success = true,
            Message = message,
            RestoredFilesCount = filesCount,
            RestoredFiles = files,
            SafetyBackupCreated = safetyCreated,
            SafetyBackupPath = safetyPath
        };
    }

    public static RestoreResult Fail(string message)
    {
        return new RestoreResult
        {
            Success = false,
            Message = message
        };
    }
}
