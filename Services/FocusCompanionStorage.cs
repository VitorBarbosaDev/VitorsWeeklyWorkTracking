using System;
using System.IO;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class FocusCompanionStorage
{
    private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "focus-settings.json");

    public static WorkRestSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<WorkRestSettings>(json);
                if (settings != null)
                {
                    if (settings.EnabledSoundVariations == null || settings.EnabledSoundVariations.Count == 0)
                    {
                        settings.EnabledSoundVariations = new WorkRestSettings().EnabledSoundVariations;
                    }
                    else
                    {
                        var defaultVariations = new WorkRestSettings().EnabledSoundVariations;
                        foreach (var def in defaultVariations)
                        {
                            var prefix = def.Split('_')[0];
                            if (!settings.EnabledSoundVariations.Any(v => v.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase)))
                            {
                                settings.EnabledSoundVariations.Add(def);
                            }
                        }
                    }
                    return settings;
                }
            }
        }
        catch
        {
            // Fallback to default
        }

        return new WorkRestSettings();
    }

    public static void Save(WorkRestSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Ignore error
        }
    }
}
