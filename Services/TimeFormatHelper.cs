using System;

namespace VitorsWeeklyWorkTracking.Services;

/// <summary>
/// Centralized helper for formatting duration and planned/actual work hours cleanly.
/// For durations under 1 hour, displays the amount in minutes (e.g., 48m or 48 mins) instead of decimals like 0.8h.
/// </summary>
public static class TimeFormatHelper
{
    /// <summary>
    /// Formats hours into a clean compact format:
    /// - 0 or negative: "0m"
    /// - Less than 1 hour (e.g. 0.8): "{minutes}m" (e.g. "48m", "30m", "15m")
    /// - Exactly whole hours (e.g. 2.0): "{hours}h" (e.g. "1h", "2h")
    /// - Hours with minutes (e.g. 1.5): "{hours}h {minutes}m" (e.g. "1h 30m")
    /// </summary>
    public static string FormatHours(double totalHours)
    {
        if (double.IsNaN(totalHours) || double.IsInfinity(totalHours) || totalHours <= 0)
            return "0m";

        int totalMinutes = (int)Math.Round(totalHours * 60.0);
        if (totalMinutes <= 0)
            return "0m";

        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        if (hours == 0)
            return $"{minutes}m";
        if (minutes == 0)
            return $"{hours}h";
        return $"{hours}h {minutes}m";
    }

    /// <summary>
    /// Formats hours with descriptive units ("mins" / "hrs"):
    /// - 0 or negative: fallback string (default "—")
    /// - Less than 1 hour (e.g. 0.8): "{minutes} mins" (e.g. "48 mins")
    /// - 1 hour or more (e.g. 1.5): "{totalHours:F1} hrs" (e.g. "1.5 hrs")
    /// </summary>
    public static string FormatHoursWithUnit(double totalHours, string fallback = "—")
    {
        if (double.IsNaN(totalHours) || double.IsInfinity(totalHours) || totalHours <= 0)
            return fallback;

        int totalMinutes = (int)Math.Round(totalHours * 60.0);
        if (totalMinutes <= 0)
            return fallback;

        if (totalMinutes < 60)
            return $"{totalMinutes} mins";

        return $"{totalHours:F1} hrs";
    }

    /// <summary>
    /// Formats remaining hours (e.g. "48 mins left" or "2.5 hrs left").
    /// </summary>
    public static string FormatRemainingHours(double remainingHours)
    {
        if (double.IsNaN(remainingHours) || double.IsInfinity(remainingHours) || remainingHours <= 0)
            return "0 mins left";

        int totalMinutes = (int)Math.Round(remainingHours * 60.0);
        if (totalMinutes < 60)
            return $"{totalMinutes} mins left";

        return $"{remainingHours:F1} hrs left";
    }

    /// <summary>
    /// Parses a user-entered duration string into total hours with full precision.
    /// Supports:
    /// - Minutes: "1m", "1 min", "1 mins", "1 minute", "45m", "30 mins"
    /// - Hours: "1h", "2hr", "2.5 hrs", "4 hours"
    /// - Mixed: "1h 30m", "1h30m", "1 hr 15 mins", "2 hours 45 mins"
    /// - Colon format: "1:30" (1h 30m), "0:45" (45m), "0:01" (1m)
    /// - Decimal numbers: "0.5", "1.5", "4", "0.01667", "0.01" (normalized to at least 1m)
    /// </summary>
    public static bool TryParseDurationHours(string? input, out double hours)
    {
        hours = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string s = input.Trim().ToLowerInvariant();

        // 1. Colon format: e.g. "1:30", "0:45", "0:01"
        var colonMatch = System.Text.RegularExpressions.Regex.Match(s, @"^(\d+):(\d{1,2})$");
        if (colonMatch.Success)
        {
            if (int.TryParse(colonMatch.Groups[1].Value, out int colHours) &&
                int.TryParse(colonMatch.Groups[2].Value, out int colMins))
            {
                double total = colHours + (colMins / 60.0);
                if (total > 0)
                {
                    double mins = Math.Round(total * 60.0);
                    if (mins < 1.0) mins = 1.0;
                    hours = mins / 60.0;
                    return true;
                }
            }
        }

        // 2. Pattern with units: e.g. "1h 30m", "45m", "1 min", "1.5 hours"
        var hmMatch = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^(?:(\d+(?:\.\d+)?)\s*(?:h|hr|hrs|hours?))?\s*(?:(\d+(?:\.\d+)?)\s*(?:m|min|mins|minutes?))?$");
        if (hmMatch.Success && (hmMatch.Groups[1].Success || hmMatch.Groups[2].Success))
        {
            double h = 0;
            double m = 0;
            if (hmMatch.Groups[1].Success && double.TryParse(hmMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsedH))
            {
                h = parsedH;
            }
            if (hmMatch.Groups[2].Success && double.TryParse(hmMatch.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsedM))
            {
                m = parsedM;
            }

            double total = h + (m / 60.0);
            if (total > 0)
            {
                double mins = Math.Round(total * 60.0);
                if (mins < 1.0) mins = 1.0;
                hours = mins / 60.0;
                return true;
            }
        }

        // 3. Direct decimal number parsing: e.g. "1.5", "4", "0.25", "0.01"
        if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsedDirect) ||
            double.TryParse(s, out parsedDirect))
        {
            if (parsedDirect > 0)
            {
                double mins = Math.Round(parsedDirect * 60.0);
                if (mins < 1.0) mins = 1.0;
                hours = mins / 60.0;
                return true;
            }
        }

        return false;
    }
}
