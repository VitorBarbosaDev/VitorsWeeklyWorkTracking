using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public enum SoundEffectType
{
    Start,
    End,
    GoalAchieved,
    DailyGoalAchieved,
    WeeklyGoalAchieved,
    BreakStart,
    BreakEnd,
    Cheer
}

public class SoundVariationInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public SoundEffectType EventType { get; init; }
    public string? SceneId { get; init; }
    public string Description { get; init; } = string.Empty;
    internal SoundEffectManager.AudioNote[] Notes { get; init; } = Array.Empty<SoundEffectManager.AudioNote>();

    public string DisplayName => $"{Icon} {Name}";
}

public static class SoundEffectManager
{
    private const int SampleRate = 44100;

    internal readonly record struct AudioNote(
        double Frequency,
        double DurationMs,
        double OffsetMs,
        double Amplitude = 1.0,
        double Harmonic2 = 0.2,
        double Harmonic3 = 0.05,
        double AttackMs = 8.0,
        double DecayExponent = 3.5);

    public static readonly IReadOnlyList<SoundVariationInfo> Variations = new List<SoundVariationInfo>
    {
        // ==================== SESSION START VARIATIONS ====================
        new()
        {
            Id = "start_ascending",
            Name = "Ascending Chime",
            Icon = "🎵",
            EventType = SoundEffectType.Start,
            Description = "Upbeat rising 3-note melody (C5 → E5 → G5)",
            Notes = new AudioNote[]
            {
                new(523.25, 100, 0, Amplitude: 0.85, Harmonic2: 0.3, Harmonic3: 0.08, AttackMs: 6, DecayExponent: 3.2),
                new(659.25, 100, 80, Amplitude: 0.90, Harmonic2: 0.3, Harmonic3: 0.08, AttackMs: 6, DecayExponent: 3.2),
                new(783.99, 260, 160, Amplitude: 1.00, Harmonic2: 0.25, Harmonic3: 0.1, AttackMs: 6, DecayExponent: 2.8)
            }
        },
        new()
        {
            Id = "start_pop",
            Name = "Quick Pop-Brite",
            Icon = "✨",
            EventType = SoundEffectType.Start,
            Description = "Snappy high double-ping (G5 → C6)",
            Notes = new AudioNote[]
            {
                new(783.99, 70, 0, Amplitude: 0.90, Harmonic2: 0.35, Harmonic3: 0.12, AttackMs: 4, DecayExponent: 4.0),
                new(1046.50, 180, 60, Amplitude: 1.00, Harmonic2: 0.30, Harmonic3: 0.15, AttackMs: 5, DecayExponent: 3.0)
            }
        },
        new()
        {
            Id = "start_marimba",
            Name = "Warm Marimba",
            Icon = "🪵",
            EventType = SoundEffectType.Start,
            Description = "Rich acoustic wooden timbre (A4 → C#5 → E5 → A5)",
            Notes = new AudioNote[]
            {
                new(440.00, 90, 0, Amplitude: 0.85, Harmonic2: 0.15, Harmonic3: 0.03, AttackMs: 5, DecayExponent: 3.5),
                new(554.37, 90, 65, Amplitude: 0.88, Harmonic2: 0.15, Harmonic3: 0.03, AttackMs: 5, DecayExponent: 3.5),
                new(659.25, 90, 130, Amplitude: 0.92, Harmonic2: 0.15, Harmonic3: 0.03, AttackMs: 5, DecayExponent: 3.5),
                new(880.00, 240, 195, Amplitude: 1.00, Harmonic2: 0.18, Harmonic3: 0.05, AttackMs: 6, DecayExponent: 2.8)
            }
        },
        new()
        {
            Id = "start_digital",
            Name = "Digital Power-Up",
            Icon = "⚡",
            EventType = SoundEffectType.Start,
            Description = "Arcade retro power surge (D5 → F#5 → A5 → D6)",
            Notes = new AudioNote[]
            {
                new(587.33, 70, 0, Amplitude: 0.80, Harmonic2: 0.40, Harmonic3: 0.18, AttackMs: 4, DecayExponent: 3.8),
                new(739.99, 70, 50, Amplitude: 0.85, Harmonic2: 0.40, Harmonic3: 0.18, AttackMs: 4, DecayExponent: 3.8),
                new(880.00, 70, 100, Amplitude: 0.90, Harmonic2: 0.40, Harmonic3: 0.18, AttackMs: 4, DecayExponent: 3.8),
                new(1174.66, 260, 150, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.15, AttackMs: 5, DecayExponent: 2.6)
            }
        },

        // ==================== SESSION END VARIATIONS ====================
        new()
        {
            Id = "end_completion",
            Name = "Gentle Completion Bell",
            Icon = "🔔",
            EventType = SoundEffectType.End,
            Description = "Soft descending peaceful finish (C6 → G5 → E5 → C5)",
            Notes = new AudioNote[]
            {
                new(1046.50, 110, 0, Amplitude: 0.80, Harmonic2: 0.20, Harmonic3: 0.05, AttackMs: 8, DecayExponent: 3.0),
                new(783.99, 110, 85, Amplitude: 0.85, Harmonic2: 0.20, Harmonic3: 0.05, AttackMs: 8, DecayExponent: 3.0),
                new(659.25, 120, 170, Amplitude: 0.90, Harmonic2: 0.20, Harmonic3: 0.05, AttackMs: 8, DecayExponent: 2.8),
                new(523.25, 420, 255, Amplitude: 1.00, Harmonic2: 0.25, Harmonic3: 0.08, AttackMs: 10, DecayExponent: 2.0)
            }
        },
        new()
        {
            Id = "end_accomplished",
            Name = "Mission Accomplished",
            Icon = "🏆",
            EventType = SoundEffectType.End,
            Description = "Satisfying two-tone warm chime (E5 → A4)",
            Notes = new AudioNote[]
            {
                new(659.25, 180, 0, Amplitude: 0.90, Harmonic2: 0.22, Harmonic3: 0.06, AttackMs: 8, DecayExponent: 2.8),
                new(440.00, 480, 140, Amplitude: 1.00, Harmonic2: 0.28, Harmonic3: 0.10, AttackMs: 12, DecayExponent: 1.8)
            }
        },
        new()
        {
            Id = "end_sparkle",
            Name = "Sparkle Wind-Down",
            Icon = "✨",
            EventType = SoundEffectType.End,
            Description = "Glinting soft chime cascade (G5 → E5 → C5 → A4)",
            Notes = new AudioNote[]
            {
                new(783.99, 100, 0, Amplitude: 0.80, Harmonic2: 0.30, Harmonic3: 0.10, AttackMs: 6, DecayExponent: 3.2),
                new(659.25, 100, 75, Amplitude: 0.85, Harmonic2: 0.30, Harmonic3: 0.10, AttackMs: 6, DecayExponent: 3.2),
                new(523.25, 110, 150, Amplitude: 0.90, Harmonic2: 0.30, Harmonic3: 0.10, AttackMs: 6, DecayExponent: 3.0),
                new(440.00, 380, 225, Amplitude: 1.00, Harmonic2: 0.25, Harmonic3: 0.08, AttackMs: 10, DecayExponent: 2.2)
            }
        },
        new()
        {
            Id = "end_calm",
            Name = "Calm Acoustic Chord",
            Icon = "🌿",
            EventType = SoundEffectType.End,
            Description = "Peaceful relaxing major 7th chord (C4 + E4 + G4 + B4)",
            Notes = new AudioNote[]
            {
                new(261.63, 550, 0, Amplitude: 0.75, Harmonic2: 0.15, Harmonic3: 0.04, AttackMs: 15, DecayExponent: 1.8),
                new(329.63, 550, 0, Amplitude: 0.75, Harmonic2: 0.15, Harmonic3: 0.04, AttackMs: 15, DecayExponent: 1.8),
                new(392.00, 550, 0, Amplitude: 0.85, Harmonic2: 0.15, Harmonic3: 0.04, AttackMs: 15, DecayExponent: 1.8),
                new(493.88, 550, 0, Amplitude: 0.95, Harmonic2: 0.18, Harmonic3: 0.05, AttackMs: 15, DecayExponent: 1.8)
            }
        },

        // ==================== SESSION GOAL ACHIEVED VARIATIONS ====================
        new()
        {
            Id = "goal_victory",
            Name = "Victory Fanfare",
            Icon = "🎉",
            EventType = SoundEffectType.GoalAchieved,
            Description = "Celebratory triumph fanfare & sparkle (C5 → E5 → G5 → C6 → E6 + G6)",
            Notes = new AudioNote[]
            {
                new(523.25, 100, 0, Amplitude: 0.80, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 3.0),
                new(659.25, 100, 80, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 3.0),
                new(783.99, 100, 160, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 3.0),
                new(1046.50, 140, 240, Amplitude: 0.95, Harmonic2: 0.30, AttackMs: 6, DecayExponent: 2.6),
                new(1318.51, 350, 360, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.12, AttackMs: 8, DecayExponent: 2.2),
                new(1567.98, 380, 400, Amplitude: 0.75, Harmonic2: 0.20, Harmonic3: 0.08, AttackMs: 10, DecayExponent: 2.0)
            }
        },
        new()
        {
            Id = "goal_levelup",
            Name = "Level-Up Triumph",
            Icon = "🌟",
            EventType = SoundEffectType.GoalAchieved,
            Description = "Retro video game achievement burst (D5 → F#5 → A5 → D6 → F#6)",
            Notes = new AudioNote[]
            {
                new(587.33, 80, 0, Amplitude: 0.80, Harmonic2: 0.35, AttackMs: 5, DecayExponent: 3.2),
                new(739.99, 80, 65, Amplitude: 0.85, Harmonic2: 0.35, AttackMs: 5, DecayExponent: 3.2),
                new(880.00, 80, 130, Amplitude: 0.90, Harmonic2: 0.35, AttackMs: 5, DecayExponent: 3.2),
                new(1174.66, 120, 195, Amplitude: 0.95, Harmonic2: 0.35, AttackMs: 5, DecayExponent: 2.8),
                new(1479.98, 360, 290, Amplitude: 1.00, Harmonic2: 0.30, Harmonic3: 0.10, AttackMs: 6, DecayExponent: 2.2)
            }
        },
        new()
        {
            Id = "goal_bell",
            Name = "Golden Bell Melody",
            Icon = "🔔",
            EventType = SoundEffectType.GoalAchieved,
            Description = "Cathedral celebration bells (G5 → B5 → D6 → G6)",
            Notes = new AudioNote[]
            {
                new(783.99, 130, 0, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 2.6),
                new(987.77, 130, 100, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 2.6),
                new(1174.66, 150, 200, Amplitude: 0.95, Harmonic2: 0.28, AttackMs: 6, DecayExponent: 2.4),
                new(1567.98, 480, 320, Amplitude: 1.00, Harmonic2: 0.32, Harmonic3: 0.12, AttackMs: 8, DecayExponent: 1.8)
            }
        },
        new()
        {
            Id = "goal_cosmic",
            Name = "Cosmic Sparkles",
            Icon = "🪐",
            EventType = SoundEffectType.GoalAchieved,
            Description = "Magical cascading arpeggio (E5 → G#5 → B5 → E6 → G#6)",
            Notes = new AudioNote[]
            {
                new(659.25, 90, 0, Amplitude: 0.75, Harmonic2: 0.25, AttackMs: 5, DecayExponent: 3.2),
                new(830.61, 90, 70, Amplitude: 0.80, Harmonic2: 0.25, AttackMs: 5, DecayExponent: 3.2),
                new(987.77, 90, 140, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 5, DecayExponent: 3.2),
                new(1318.51, 120, 210, Amplitude: 0.95, Harmonic2: 0.30, AttackMs: 5, DecayExponent: 2.6),
                new(1661.22, 380, 300, Amplitude: 1.00, Harmonic2: 0.25, Harmonic3: 0.10, AttackMs: 6, DecayExponent: 2.0)
            }
        },

        // ==================== DAILY WORK GOAL ACHIEVED VARIATIONS ====================
        new()
        {
            Id = "daily_triumph",
            Name = "Daily Triumph Fanfare",
            Icon = "🏆",
            EventType = SoundEffectType.DailyGoalAchieved,
            Description = "Uplifting golden brass & harp arpeggio (C5 → E5 → G5 → C6 → E6 → G6 + C7 shimmer)",
            Notes = new AudioNote[]
            {
                new(523.25, 100, 0, Amplitude: 0.80, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 3.0),
                new(659.25, 100, 75, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 3.0),
                new(783.99, 100, 150, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 3.0),
                new(1046.50, 120, 225, Amplitude: 0.95, Harmonic2: 0.30, AttackMs: 6, DecayExponent: 2.6),
                new(1318.51, 150, 310, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.12, AttackMs: 8, DecayExponent: 2.2),
                new(1567.98, 460, 410, Amplitude: 1.00, Harmonic2: 0.30, Harmonic3: 0.10, AttackMs: 8, DecayExponent: 2.0),
                new(2093.00, 400, 430, Amplitude: 0.65, Harmonic2: 0.15, AttackMs: 10, DecayExponent: 2.0)
            }
        },
        new()
        {
            Id = "daily_sunburst",
            Name = "Sunburst Achievement",
            Icon = "🌅",
            EventType = SoundEffectType.DailyGoalAchieved,
            Description = "Warm glowing sunset chords & sunburst flourish (D4/A4 → D5 → F#5 → A5 → D6 → F#6)",
            Notes = new AudioNote[]
            {
                new(293.66, 420, 0, Amplitude: 0.70, Harmonic2: 0.20, AttackMs: 12, DecayExponent: 2.0),
                new(440.00, 420, 40, Amplitude: 0.75, Harmonic2: 0.20, AttackMs: 12, DecayExponent: 2.0),
                new(587.33, 110, 90, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 3.0),
                new(739.99, 110, 170, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 3.0),
                new(880.00, 120, 250, Amplitude: 0.95, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 2.8),
                new(1174.66, 150, 330, Amplitude: 1.00, Harmonic2: 0.30, AttackMs: 6, DecayExponent: 2.4),
                new(1479.98, 520, 420, Amplitude: 1.00, Harmonic2: 0.25, Harmonic3: 0.08, AttackMs: 8, DecayExponent: 1.8)
            }
        },
        new()
        {
            Id = "daily_chime",
            Name = "Master Day Chimes",
            Icon = "🔔",
            EventType = SoundEffectType.DailyGoalAchieved,
            Description = "Grand crystal master chime sequence (G5 → B5 → D6 → G6 → B6 + deep pedal chime)",
            Notes = new AudioNote[]
            {
                new(783.99, 140, 0, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 2.8),
                new(987.77, 140, 110, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 2.8),
                new(1174.66, 160, 220, Amplitude: 0.95, Harmonic2: 0.28, AttackMs: 6, DecayExponent: 2.4),
                new(1567.98, 200, 340, Amplitude: 1.00, Harmonic2: 0.30, AttackMs: 6, DecayExponent: 2.2),
                new(1975.53, 520, 470, Amplitude: 1.00, Harmonic2: 0.28, Harmonic3: 0.10, AttackMs: 8, DecayExponent: 1.8),
                new(783.99, 620, 470, Amplitude: 0.65, Harmonic2: 0.20, AttackMs: 12, DecayExponent: 1.6)
            }
        },
        new()
        {
            Id = "daily_quest",
            Name = "Daily Quest Complete",
            Icon = "🌟",
            EventType = SoundEffectType.DailyGoalAchieved,
            Description = "Retro adventure quest victory flourish (E5 → G#5 → B5 → E6 → G#6 → B6 → E7)",
            Notes = new AudioNote[]
            {
                new(659.25, 80, 0, Amplitude: 0.80, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.5),
                new(830.61, 80, 60, Amplitude: 0.85, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.5),
                new(987.77, 80, 120, Amplitude: 0.90, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.5),
                new(1318.51, 100, 180, Amplitude: 0.95, Harmonic2: 0.35, AttackMs: 5, DecayExponent: 3.0),
                new(1661.22, 110, 250, Amplitude: 1.00, Harmonic2: 0.35, AttackMs: 5, DecayExponent: 2.8),
                new(1975.53, 120, 320, Amplitude: 1.00, Harmonic2: 0.35, AttackMs: 5, DecayExponent: 2.6),
                new(2637.02, 420, 400, Amplitude: 1.00, Harmonic2: 0.30, Harmonic3: 0.10, AttackMs: 6, DecayExponent: 2.2)
            }
        },

        // ==================== WEEKLY WORK GOAL ACHIEVED VARIATIONS ====================
        new()
        {
            Id = "weekly_grand",
            Name = "Grand Weekly Victory",
            Icon = "👑",
            EventType = SoundEffectType.WeeklyGoalAchieved,
            Description = "Majestic orchestral multi-chord symphony with grand crescendo & glittering sparkles",
            Notes = new AudioNote[]
            {
                // Chord 1: C-major
                new(261.63, 140, 0, Amplitude: 0.70, Harmonic2: 0.20, AttackMs: 10, DecayExponent: 2.6),
                new(392.00, 140, 0, Amplitude: 0.75, Harmonic2: 0.20, AttackMs: 10, DecayExponent: 2.6),
                new(523.25, 140, 0, Amplitude: 0.80, Harmonic2: 0.20, AttackMs: 10, DecayExponent: 2.6),
                // Chord 2: E-major
                new(329.63, 140, 120, Amplitude: 0.75, Harmonic2: 0.20, AttackMs: 10, DecayExponent: 2.6),
                new(493.88, 140, 120, Amplitude: 0.80, Harmonic2: 0.20, AttackMs: 10, DecayExponent: 2.6),
                new(659.25, 140, 120, Amplitude: 0.85, Harmonic2: 0.20, AttackMs: 10, DecayExponent: 2.6),
                // Chord 3: G-major
                new(392.00, 160, 240, Amplitude: 0.80, Harmonic2: 0.25, AttackMs: 10, DecayExponent: 2.4),
                new(587.33, 160, 240, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 10, DecayExponent: 2.4),
                new(783.99, 160, 240, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 10, DecayExponent: 2.4),
                // Grand Final Chord (C-major multi-octave victory resolution)
                new(261.63, 750, 380, Amplitude: 0.85, Harmonic2: 0.20, AttackMs: 14, DecayExponent: 1.6),
                new(392.00, 750, 380, Amplitude: 0.85, Harmonic2: 0.20, AttackMs: 14, DecayExponent: 1.6),
                new(523.25, 750, 380, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 14, DecayExponent: 1.6),
                new(659.25, 750, 380, Amplitude: 0.95, Harmonic2: 0.30, AttackMs: 14, DecayExponent: 1.6),
                new(783.99, 750, 380, Amplitude: 0.95, Harmonic2: 0.30, AttackMs: 14, DecayExponent: 1.6),
                new(1046.50, 750, 380, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.12, AttackMs: 12, DecayExponent: 1.8),
                new(1318.51, 620, 450, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 10, DecayExponent: 1.8),
                new(1567.98, 580, 520, Amplitude: 0.80, Harmonic2: 0.20, AttackMs: 10, DecayExponent: 1.8),
                new(2093.00, 520, 590, Amplitude: 0.70, Harmonic2: 0.15, AttackMs: 10, DecayExponent: 2.0)
            }
        },
        new()
        {
            Id = "weekly_champions",
            Name = "Champions Heroic Anthem",
            Icon = "🏆",
            EventType = SoundEffectType.WeeklyGoalAchieved,
            Description = "Heroic royal fanfare with triumphant brass flourishes and harmonic resolution",
            Notes = new AudioNote[]
            {
                new(293.66, 120, 0, Amplitude: 0.75, Harmonic2: 0.25, AttackMs: 8, DecayExponent: 2.8),
                new(440.00, 120, 0, Amplitude: 0.80, Harmonic2: 0.25, AttackMs: 8, DecayExponent: 2.8),
                new(369.99, 120, 100, Amplitude: 0.80, Harmonic2: 0.25, AttackMs: 8, DecayExponent: 2.8),
                new(587.33, 120, 100, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 8, DecayExponent: 2.8),
                new(440.00, 130, 200, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 8, DecayExponent: 2.6),
                new(739.99, 130, 200, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 8, DecayExponent: 2.6),
                new(587.33, 140, 310, Amplitude: 0.90, Harmonic2: 0.30, AttackMs: 8, DecayExponent: 2.4),
                new(880.00, 140, 310, Amplitude: 0.95, Harmonic2: 0.30, AttackMs: 8, DecayExponent: 2.4),
                // Final Champion Chord
                new(293.66, 800, 440, Amplitude: 0.90, Harmonic2: 0.20, AttackMs: 14, DecayExponent: 1.5),
                new(440.00, 800, 440, Amplitude: 0.90, Harmonic2: 0.20, AttackMs: 14, DecayExponent: 1.5),
                new(587.33, 800, 440, Amplitude: 0.95, Harmonic2: 0.25, AttackMs: 14, DecayExponent: 1.5),
                new(739.99, 800, 440, Amplitude: 1.00, Harmonic2: 0.30, AttackMs: 14, DecayExponent: 1.5),
                new(880.00, 800, 440, Amplitude: 1.00, Harmonic2: 0.30, AttackMs: 14, DecayExponent: 1.5),
                new(1174.66, 800, 440, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.12, AttackMs: 12, DecayExponent: 1.7),
                new(1479.98, 650, 510, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 10, DecayExponent: 1.8),
                new(1760.00, 600, 580, Amplitude: 0.75, Harmonic2: 0.20, AttackMs: 10, DecayExponent: 1.8),
                new(2349.32, 540, 650, Amplitude: 0.70, Harmonic2: 0.15, AttackMs: 10, DecayExponent: 2.0)
            }
        },
        new()
        {
            Id = "weekly_fiesta",
            Name = "Weekly Fiesta Sparkle",
            Icon = "🎉",
            EventType = SoundEffectType.WeeklyGoalAchieved,
            Description = "Cascading celebration bells with rapid ascending golden arpeggio flourishes across octaves",
            Notes = new AudioNote[]
            {
                new(698.46, 80, 0, Amplitude: 0.80, Harmonic2: 0.25, AttackMs: 5, DecayExponent: 3.2),
                new(880.00, 80, 60, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 5, DecayExponent: 3.2),
                new(1046.50, 80, 120, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 5, DecayExponent: 3.2),
                new(1318.51, 80, 180, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 5, DecayExponent: 3.2),
                new(1396.91, 90, 240, Amplitude: 0.95, Harmonic2: 0.30, AttackMs: 5, DecayExponent: 3.0),
                new(1760.00, 100, 300, Amplitude: 1.00, Harmonic2: 0.30, AttackMs: 5, DecayExponent: 2.8),
                new(2093.00, 110, 360, Amplitude: 1.00, Harmonic2: 0.30, AttackMs: 5, DecayExponent: 2.6),
                // Resolving celebration bell ring
                new(523.25, 600, 420, Amplitude: 0.85, Harmonic2: 0.20, AttackMs: 10, DecayExponent: 1.8),
                new(698.46, 600, 420, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 10, DecayExponent: 1.8),
                new(880.00, 600, 420, Amplitude: 0.95, Harmonic2: 0.25, AttackMs: 10, DecayExponent: 1.8),
                new(1046.50, 600, 420, Amplitude: 1.00, Harmonic2: 0.30, AttackMs: 10, DecayExponent: 1.8),
                new(2793.83, 500, 420, Amplitude: 0.85, Harmonic2: 0.15, AttackMs: 8, DecayExponent: 2.0)
            }
        },
        new()
        {
            Id = "weekly_celestial",
            Name = "Celestial Masterpiece Chord",
            Icon = "✨",
            EventType = SoundEffectType.WeeklyGoalAchieved,
            Description = "Lush major-9th harmonic swell with rich multi-octave resonance & shimmering overtones",
            Notes = new AudioNote[]
            {
                new(130.81, 900, 0, Amplitude: 0.80, Harmonic2: 0.20, AttackMs: 25, DecayExponent: 1.4),
                new(196.00, 900, 50, Amplitude: 0.80, Harmonic2: 0.20, AttackMs: 25, DecayExponent: 1.4),
                new(329.63, 900, 100, Amplitude: 0.85, Harmonic2: 0.20, AttackMs: 20, DecayExponent: 1.4),
                new(392.00, 900, 150, Amplitude: 0.90, Harmonic2: 0.20, AttackMs: 20, DecayExponent: 1.4),
                new(493.88, 900, 200, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 18, DecayExponent: 1.4),
                new(587.33, 900, 250, Amplitude: 0.95, Harmonic2: 0.25, AttackMs: 16, DecayExponent: 1.4),
                new(783.99, 900, 300, Amplitude: 1.00, Harmonic2: 0.25, AttackMs: 14, DecayExponent: 1.5),
                new(987.77, 800, 380, Amplitude: 0.85, Harmonic2: 0.20, AttackMs: 12, DecayExponent: 1.6),
                new(1174.66, 750, 460, Amplitude: 0.80, Harmonic2: 0.18, AttackMs: 10, DecayExponent: 1.8),
                new(1567.98, 700, 540, Amplitude: 0.75, Harmonic2: 0.15, AttackMs: 10, DecayExponent: 1.8),
                new(1975.53, 650, 620, Amplitude: 0.70, Harmonic2: 0.12, AttackMs: 10, DecayExponent: 2.0)
            }
        },

        // ==================== BREAK START VARIATIONS ====================
        new()
        {
            Id = "breakstart_zen",
            Name = "Peaceful Zen Bell",
            Icon = "🧘",
            EventType = SoundEffectType.BreakStart,
            Description = "Soothing acoustic relaxation bell (E5 → B4 → G4 + warm E4 chord)",
            Notes = new AudioNote[]
            {
                new(659.25, 280, 0, Amplitude: 0.85, Harmonic2: 0.18, AttackMs: 14, DecayExponent: 2.4),
                new(493.88, 320, 200, Amplitude: 0.90, Harmonic2: 0.18, AttackMs: 14, DecayExponent: 2.2),
                new(392.00, 520, 420, Amplitude: 1.00, Harmonic2: 0.22, Harmonic3: 0.05, AttackMs: 18, DecayExponent: 1.8),
                new(329.63, 560, 420, Amplitude: 0.70, Harmonic2: 0.15, Harmonic3: 0.05, AttackMs: 20, DecayExponent: 1.6)
            }
        },
        new()
        {
            Id = "breakstart_lofi",
            Name = "Cozy Lo-Fi Drop",
            Icon = "☕",
            EventType = SoundEffectType.BreakStart,
            Description = "Gentle warm soft mallet drop (A4 → E4 → C4)",
            Notes = new AudioNote[]
            {
                new(440.00, 240, 0, Amplitude: 0.90, Harmonic2: 0.12, AttackMs: 10, DecayExponent: 2.6),
                new(329.63, 280, 180, Amplitude: 0.95, Harmonic2: 0.12, AttackMs: 12, DecayExponent: 2.2),
                new(261.63, 500, 360, Amplitude: 1.00, Harmonic2: 0.15, Harmonic3: 0.04, AttackMs: 15, DecayExponent: 1.8)
            }
        },
        new()
        {
            Id = "breakstart_bowl",
            Name = "Singing Bowl Resonator",
            Icon = "🍵",
            EventType = SoundEffectType.BreakStart,
            Description = "Warm lingering meditation bowl chime (D4 → A4 harmonic)",
            Notes = new AudioNote[]
            {
                new(293.66, 680, 0, Amplitude: 0.95, Harmonic2: 0.35, Harmonic3: 0.10, AttackMs: 25, DecayExponent: 1.4),
                new(440.00, 650, 80, Amplitude: 0.75, Harmonic2: 0.25, Harmonic3: 0.08, AttackMs: 30, DecayExponent: 1.5)
            }
        },
        new()
        {
            Id = "breakstart_waterdrop",
            Name = "Gentle Waterdrop Chime",
            Icon = "💧",
            EventType = SoundEffectType.BreakStart,
            Description = "Soft crystalline drops (F5 → C5 → A4)",
            Notes = new AudioNote[]
            {
                new(698.46, 160, 0, Amplitude: 0.90, Harmonic2: 0.20, Harmonic3: 0.05, AttackMs: 6, DecayExponent: 3.2),
                new(523.25, 200, 140, Amplitude: 0.95, Harmonic2: 0.18, Harmonic3: 0.05, AttackMs: 8, DecayExponent: 2.8),
                new(440.00, 480, 280, Amplitude: 1.00, Harmonic2: 0.15, Harmonic3: 0.04, AttackMs: 12, DecayExponent: 2.0)
            }
        },

        // ==================== BREAK END VARIATIONS ====================
        new()
        {
            Id = "breakend_wakeup",
            Name = "Energetic Wake-Up",
            Icon = "⚡",
            EventType = SoundEffectType.BreakEnd,
            Description = "Bright uplifting focus prompt (G4 → C5 → E5 → G5)",
            Notes = new AudioNote[]
            {
                new(392.00, 110, 0, Amplitude: 0.80, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 3.2),
                new(523.25, 110, 95, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 3.2),
                new(659.25, 130, 190, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 3.0),
                new(783.99, 360, 290, Amplitude: 1.00, Harmonic2: 0.30, Harmonic3: 0.1, AttackMs: 6, DecayExponent: 2.4)
            }
        },
        new()
        {
            Id = "breakend_ready",
            Name = "Ready-Set-Go Ping",
            Icon = "⏱️",
            EventType = SoundEffectType.BreakEnd,
            Description = "Sharp, motivating triple-tap pulse (C5 → C5 → G5)",
            Notes = new AudioNote[]
            {
                new(523.25, 80, 0, Amplitude: 0.85, Harmonic2: 0.30, AttackMs: 5, DecayExponent: 3.6),
                new(523.25, 80, 90, Amplitude: 0.90, Harmonic2: 0.30, AttackMs: 5, DecayExponent: 3.6),
                new(783.99, 280, 180, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.12, AttackMs: 5, DecayExponent: 2.6)
            }
        },
        new()
        {
            Id = "breakend_morning",
            Name = "Morning Sunbeam",
            Icon = "🌅",
            EventType = SoundEffectType.BreakEnd,
            Description = "Warm rising morning arpeggio (D4 → G4 → B4 → D5)",
            Notes = new AudioNote[]
            {
                new(293.66, 120, 0, Amplitude: 0.80, Harmonic2: 0.20, AttackMs: 8, DecayExponent: 3.0),
                new(392.00, 120, 90, Amplitude: 0.85, Harmonic2: 0.20, AttackMs: 8, DecayExponent: 3.0),
                new(493.88, 140, 180, Amplitude: 0.90, Harmonic2: 0.20, AttackMs: 8, DecayExponent: 2.8),
                new(587.33, 400, 280, Amplitude: 1.00, Harmonic2: 0.25, Harmonic3: 0.08, AttackMs: 8, DecayExponent: 2.2)
            }
        },
        new()
        {
            Id = "breakend_focusbell",
            Name = "Focus Bell Resonator",
            Icon = "🔔",
            EventType = SoundEffectType.BreakEnd,
            Description = "Clear bell ring (E5 → B5) with crisp attack",
            Notes = new AudioNote[]
            {
                new(659.25, 140, 0, Amplitude: 0.90, Harmonic2: 0.28, AttackMs: 6, DecayExponent: 3.0),
                new(987.77, 440, 110, Amplitude: 1.00, Harmonic2: 0.32, Harmonic3: 0.10, AttackMs: 6, DecayExponent: 2.0)
            }
        },

        // ==================== SCENE COMPANION CHEER / PET VARIATIONS ====================

        // --- 1. Boy Cycling Home ("cycling") ---
        new()
        {
            Id = "cheer_cycling_bell",
            Name = "Bicycle Bell 'Ding-Ding!'",
            Icon = "🔔",
            EventType = SoundEffectType.Cheer,
            SceneId = "cycling",
            Description = "Dual metallic brass bicycle bell ring (E6 → E6 + G6 overtone)",
            Notes = new AudioNote[]
            {
                new(1318.51, 100, 0, Amplitude: 0.95, Harmonic2: 0.45, Harmonic3: 0.15, AttackMs: 3, DecayExponent: 3.5),
                new(1318.51, 280, 90, Amplitude: 1.00, Harmonic2: 0.45, Harmonic3: 0.15, AttackMs: 3, DecayExponent: 2.8),
                new(1567.98, 240, 90, Amplitude: 0.50, Harmonic2: 0.20, AttackMs: 4, DecayExponent: 3.0)
            }
        },
        new()
        {
            Id = "cheer_cycling_bark",
            Name = "Puppy Basket Bark",
            Icon = "🐶",
            EventType = SoundEffectType.Cheer,
            SceneId = "cycling",
            Description = "Cute bouncy puppy bark chime (F#5 → A5 → D6 → F#6)",
            Notes = new AudioNote[]
            {
                new(739.99, 65, 0, Amplitude: 0.85, Harmonic2: 0.30, AttackMs: 4, DecayExponent: 3.5),
                new(880.00, 75, 45, Amplitude: 0.90, Harmonic2: 0.30, AttackMs: 4, DecayExponent: 3.5),
                new(1174.66, 180, 100, Amplitude: 1.00, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.0),
                new(1479.98, 120, 150, Amplitude: 0.70, Harmonic2: 0.20, AttackMs: 4, DecayExponent: 3.2)
            }
        },
        new()
        {
            Id = "cheer_cycling_whistle",
            Name = "Meadow Trail Whistle",
            Icon = "🎶",
            EventType = SoundEffectType.Cheer,
            SceneId = "cycling",
            Description = "Joyful country meadow trail whistle (E5 → G5 → B5 → E6)",
            Notes = new AudioNote[]
            {
                new(659.25, 70, 0, Amplitude: 0.80, Harmonic2: 0.20, AttackMs: 5, DecayExponent: 3.5),
                new(783.99, 70, 55, Amplitude: 0.85, Harmonic2: 0.20, AttackMs: 5, DecayExponent: 3.5),
                new(987.77, 80, 110, Amplitude: 0.90, Harmonic2: 0.20, AttackMs: 5, DecayExponent: 3.5),
                new(1318.51, 240, 165, Amplitude: 1.00, Harmonic2: 0.20, AttackMs: 6, DecayExponent: 2.8)
            }
        },

        // --- 2. Rainy Window Coffee ("cafe") ---
        new()
        {
            Id = "cheer_cafe_cup",
            Name = "Porcelain Mug Clink",
            Icon = "☕",
            EventType = SoundEffectType.Cheer,
            SceneId = "cafe",
            Description = "Ceramic coffee cup clink & warm chime (A5 + E6 + warm C5)",
            Notes = new AudioNote[]
            {
                new(880.00, 70, 0, Amplitude: 0.95, Harmonic2: 0.50, Harmonic3: 0.15, AttackMs: 2, DecayExponent: 3.8),
                new(1318.51, 180, 20, Amplitude: 0.90, Harmonic2: 0.30, AttackMs: 3, DecayExponent: 3.2),
                new(523.25, 380, 40, Amplitude: 0.75, Harmonic2: 0.15, AttackMs: 8, DecayExponent: 2.0)
            }
        },
        new()
        {
            Id = "cheer_cafe_raindrop",
            Name = "Crystalline Raindrop",
            Icon = "💧",
            EventType = SoundEffectType.Cheer,
            SceneId = "cafe",
            Description = "Glassy waterdrop sliding down window (C6 → G5 → E5 → C5)",
            Notes = new AudioNote[]
            {
                new(1046.50, 80, 0, Amplitude: 0.90, Harmonic2: 0.15, AttackMs: 3, DecayExponent: 3.6),
                new(783.99, 100, 60, Amplitude: 0.90, Harmonic2: 0.15, AttackMs: 4, DecayExponent: 3.2),
                new(659.25, 120, 120, Amplitude: 0.95, Harmonic2: 0.15, AttackMs: 5, DecayExponent: 2.8),
                new(523.25, 320, 180, Amplitude: 1.00, Harmonic2: 0.18, AttackMs: 8, DecayExponent: 2.2)
            }
        },
        new()
        {
            Id = "cheer_cafe_steam",
            Name = "Cozy Steam Swell",
            Icon = "🌿",
            EventType = SoundEffectType.Cheer,
            SceneId = "cafe",
            Description = "Mellow warm acoustic chord swell (C4 + E4 + G4 + C5)",
            Notes = new AudioNote[]
            {
                new(261.63, 440, 0, Amplitude: 0.70, Harmonic2: 0.15, AttackMs: 15, DecayExponent: 1.8),
                new(329.63, 440, 30, Amplitude: 0.80, Harmonic2: 0.15, AttackMs: 15, DecayExponent: 1.8),
                new(392.00, 440, 60, Amplitude: 0.90, Harmonic2: 0.15, AttackMs: 15, DecayExponent: 1.8),
                new(523.25, 480, 90, Amplitude: 1.00, Harmonic2: 0.18, AttackMs: 15, DecayExponent: 1.8)
            }
        },

        // --- 3. Coffee Jazz Window ("coffeejazz") ---
        new()
        {
            Id = "cheer_jazz_sax",
            Name = "Smooth Sax Lick",
            Icon = "🎷",
            EventType = SoundEffectType.Cheer,
            SceneId = "coffeejazz",
            Description = "Jazzy blues slide lick (D5 → F5 → F#5 → A5 → C6)",
            Notes = new AudioNote[]
            {
                new(587.33, 70, 0, Amplitude: 0.80, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.5),
                new(698.46, 70, 50, Amplitude: 0.85, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.5),
                new(739.99, 70, 100, Amplitude: 0.90, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.5),
                new(880.00, 80, 150, Amplitude: 0.95, Harmonic2: 0.35, AttackMs: 5, DecayExponent: 3.2),
                new(1046.50, 280, 205, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.12, AttackMs: 6, DecayExponent: 2.5)
            }
        },
        new()
        {
            Id = "cheer_jazz_piano",
            Name = "Rhodes E-Piano Chord",
            Icon = "🎹",
            EventType = SoundEffectType.Cheer,
            SceneId = "coffeejazz",
            Description = "Warm Rhodes electric piano major-9th chord (F4 + A4 + C5 + E5 + G5)",
            Notes = new AudioNote[]
            {
                new(349.23, 500, 0, Amplitude: 0.80, Harmonic2: 0.25, AttackMs: 8, DecayExponent: 1.9),
                new(440.00, 500, 20, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 8, DecayExponent: 1.9),
                new(523.25, 500, 40, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 8, DecayExponent: 1.9),
                new(659.25, 500, 60, Amplitude: 0.95, Harmonic2: 0.25, AttackMs: 8, DecayExponent: 1.9),
                new(783.99, 500, 80, Amplitude: 1.00, Harmonic2: 0.28, Harmonic3: 0.08, AttackMs: 8, DecayExponent: 1.9)
            }
        },
        new()
        {
            Id = "cheer_jazz_sparkle",
            Name = "Autumn Sparkle Cascade",
            Icon = "🍂",
            EventType = SoundEffectType.Cheer,
            SceneId = "coffeejazz",
            Description = "Drifting golden autumn dusk sparkles (B5 → G#5 → E5 → B4)",
            Notes = new AudioNote[]
            {
                new(987.77, 80, 0, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 4, DecayExponent: 3.5),
                new(830.61, 80, 65, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 4, DecayExponent: 3.5),
                new(659.25, 90, 130, Amplitude: 0.95, Harmonic2: 0.25, AttackMs: 5, DecayExponent: 3.0),
                new(493.88, 320, 195, Amplitude: 1.00, Harmonic2: 0.25, Harmonic3: 0.08, AttackMs: 8, DecayExponent: 2.2)
            }
        },

        // --- 4. Pastel Ice Cream Truck ("icecream") ---
        new()
        {
            Id = "cheer_icecream_jingle",
            Name = "Carousel Music Box",
            Icon = "🍦",
            EventType = SoundEffectType.Cheer,
            SceneId = "icecream",
            Description = "Sweet ice cream truck carousel melody (E5 → G5 → A5 → G5 → E5 → C5)",
            Notes = new AudioNote[]
            {
                new(659.25, 80, 0, Amplitude: 0.85, Harmonic2: 0.30, AttackMs: 4, DecayExponent: 3.5),
                new(783.99, 80, 65, Amplitude: 0.88, Harmonic2: 0.30, AttackMs: 4, DecayExponent: 3.5),
                new(880.00, 80, 130, Amplitude: 0.92, Harmonic2: 0.30, AttackMs: 4, DecayExponent: 3.5),
                new(783.99, 80, 195, Amplitude: 0.92, Harmonic2: 0.30, AttackMs: 4, DecayExponent: 3.5),
                new(659.25, 80, 260, Amplitude: 0.95, Harmonic2: 0.30, AttackMs: 4, DecayExponent: 3.5),
                new(523.25, 260, 325, Amplitude: 1.00, Harmonic2: 0.30, Harmonic3: 0.10, AttackMs: 5, DecayExponent: 2.6)
            }
        },
        new()
        {
            Id = "cheer_icecream_pop",
            Name = "Pastel Candy Pop",
            Icon = "🍬",
            EventType = SoundEffectType.Cheer,
            SceneId = "icecream",
            Description = "Cute bouncy candy pops (C6 → E6 → G6)",
            Notes = new AudioNote[]
            {
                new(1046.50, 50, 0, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 2, DecayExponent: 4.5),
                new(1318.51, 60, 45, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 2, DecayExponent: 4.0),
                new(1567.98, 180, 90, Amplitude: 1.00, Harmonic2: 0.25, Harmonic3: 0.08, AttackMs: 3, DecayExponent: 3.2)
            }
        },
        new()
        {
            Id = "cheer_icecream_bell",
            Name = "Shop Counter Ding",
            Icon = "🔔",
            EventType = SoundEffectType.Cheer,
            SceneId = "icecream",
            Description = "Bright friendly ice cream parlor counter bell (D6 → A6)",
            Notes = new AudioNote[]
            {
                new(1174.66, 90, 0, Amplitude: 0.90, Harmonic2: 0.35, AttackMs: 3, DecayExponent: 3.6),
                new(1760.00, 340, 40, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.12, AttackMs: 3, DecayExponent: 2.2)
            }
        },

        // --- 5. Tokyo Metro Lo-Fi Girl ("metro") ---
        new()
        {
            Id = "cheer_metro_melody",
            Name = "Tokyo Departure Tune",
            Icon = "🎧",
            EventType = SoundEffectType.Cheer,
            SceneId = "metro",
            Description = "Japanese train station departure melody (E5 → G5 → D5 → G5 → B5 → A5)",
            Notes = new AudioNote[]
            {
                new(659.25, 75, 0, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 4, DecayExponent: 3.5),
                new(783.99, 75, 65, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 4, DecayExponent: 3.5),
                new(587.33, 75, 130, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 4, DecayExponent: 3.5),
                new(783.99, 75, 195, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 4, DecayExponent: 3.5),
                new(987.77, 90, 260, Amplitude: 0.95, Harmonic2: 0.25, AttackMs: 4, DecayExponent: 3.2),
                new(880.00, 260, 335, Amplitude: 1.00, Harmonic2: 0.25, Harmonic3: 0.08, AttackMs: 5, DecayExponent: 2.6)
            }
        },
        new()
        {
            Id = "cheer_metro_lofi",
            Name = "Lo-Fi Headphone Beat",
            Icon = "🎵",
            EventType = SoundEffectType.Cheer,
            SceneId = "metro",
            Description = "Dreamy lo-fi headphone chord drop (A4 + C5 + E5 + G#5)",
            Notes = new AudioNote[]
            {
                new(440.00, 450, 0, Amplitude: 0.85, Harmonic2: 0.20, AttackMs: 12, DecayExponent: 2.0),
                new(523.25, 450, 25, Amplitude: 0.90, Harmonic2: 0.20, AttackMs: 12, DecayExponent: 2.0),
                new(659.25, 450, 50, Amplitude: 0.95, Harmonic2: 0.20, AttackMs: 12, DecayExponent: 2.0),
                new(830.61, 450, 75, Amplitude: 1.00, Harmonic2: 0.20, AttackMs: 12, DecayExponent: 2.0)
            }
        },
        new()
        {
            Id = "cheer_metro_doors",
            Name = "Subway Dual Chime",
            Icon = "🚇",
            EventType = SoundEffectType.Cheer,
            SceneId = "metro",
            Description = "Tokyo metro door closing dual chime (C6 → G5)",
            Notes = new AudioNote[]
            {
                new(1046.50, 130, 0, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 4, DecayExponent: 3.0),
                new(783.99, 300, 100, Amplitude: 1.00, Harmonic2: 0.25, Harmonic3: 0.08, AttackMs: 5, DecayExponent: 2.4)
            }
        },

        // --- 6. Playful Focus Kitty ("cat") ---
        new()
        {
            Id = "cheer_cat_meow",
            Name = "Kawaii Kitty Meow",
            Icon = "🐱",
            EventType = SoundEffectType.Cheer,
            SceneId = "cat",
            Description = "Adorable cheerful kitty meow arpeggio (A5 → C#6 → E6 → C#6)",
            Notes = new AudioNote[]
            {
                new(880.00, 70, 0, Amplitude: 0.80, Harmonic2: 0.30, AttackMs: 4, DecayExponent: 3.5),
                new(1108.73, 90, 50, Amplitude: 0.95, Harmonic2: 0.30, AttackMs: 5, DecayExponent: 3.2),
                new(1318.51, 100, 120, Amplitude: 1.00, Harmonic2: 0.30, AttackMs: 5, DecayExponent: 3.0),
                new(1108.73, 220, 190, Amplitude: 0.90, Harmonic2: 0.25, AttackMs: 6, DecayExponent: 2.6)
            }
        },
        new()
        {
            Id = "cheer_cat_purr",
            Name = "Purr & Collar Bell",
            Icon = "💖",
            EventType = SoundEffectType.Cheer,
            SceneId = "cat",
            Description = "Warm purr vibration with tiny collar bell (G5 + D6 + G6 + warm G3 rumble)",
            Notes = new AudioNote[]
            {
                new(196.00, 360, 0, Amplitude: 0.50, Harmonic2: 0.15, AttackMs: 15, DecayExponent: 1.5),
                new(783.99, 90, 0, Amplitude: 0.75, Harmonic2: 0.30, AttackMs: 3, DecayExponent: 3.2),
                new(1174.66, 120, 60, Amplitude: 0.85, Harmonic2: 0.30, AttackMs: 3, DecayExponent: 3.0),
                new(1567.98, 260, 130, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.10, AttackMs: 4, DecayExponent: 2.5)
            }
        },
        new()
        {
            Id = "cheer_cat_bounce",
            Name = "Playful Yarn Squeak",
            Icon = "🧶",
            EventType = SoundEffectType.Cheer,
            SceneId = "cat",
            Description = "Bouncy yarn ball toy squeak chirp (E6 → G6 → A6)",
            Notes = new AudioNote[]
            {
                new(1318.51, 50, 0, Amplitude: 0.85, Harmonic2: 0.30, AttackMs: 2, DecayExponent: 4.5),
                new(1567.98, 60, 40, Amplitude: 0.90, Harmonic2: 0.30, AttackMs: 2, DecayExponent: 4.0),
                new(1760.00, 180, 85, Amplitude: 1.00, Harmonic2: 0.30, Harmonic3: 0.10, AttackMs: 3, DecayExponent: 3.2)
            }
        },

        // --- 7. Space Rocket Launch ("rocket") ---
        new()
        {
            Id = "cheer_rocket_laser",
            Name = "Cosmic Laser Boost",
            Icon = "🚀",
            EventType = SoundEffectType.Cheer,
            SceneId = "rocket",
            Description = "Sci-Fi futuristic laser pulse & booster chirp (C6 → G6 → C7)",
            Notes = new AudioNote[]
            {
                new(1046.50, 45, 0, Amplitude: 0.85, Harmonic2: 0.45, Harmonic3: 0.20, AttackMs: 2, DecayExponent: 4.5),
                new(1567.98, 55, 35, Amplitude: 0.95, Harmonic2: 0.45, Harmonic3: 0.20, AttackMs: 2, DecayExponent: 4.0),
                new(2093.00, 200, 75, Amplitude: 1.00, Harmonic2: 0.45, Harmonic3: 0.20, AttackMs: 2, DecayExponent: 3.2)
            }
        },
        new()
        {
            Id = "cheer_rocket_beacon",
            Name = "Satellite Radar Ping",
            Icon = "🪐",
            EventType = SoundEffectType.Cheer,
            SceneId = "rocket",
            Description = "Planetary satellite radar ping echo (A6 → E6 echo)",
            Notes = new AudioNote[]
            {
                new(1760.00, 110, 0, Amplitude: 0.90, Harmonic2: 0.30, AttackMs: 3, DecayExponent: 3.2),
                new(1318.51, 260, 80, Amplitude: 1.00, Harmonic2: 0.30, Harmonic3: 0.08, AttackMs: 3, DecayExponent: 2.6)
            }
        },
        new()
        {
            Id = "cheer_rocket_warp",
            Name = "Hyperspace Warp Sweep",
            Icon = "✨",
            EventType = SoundEffectType.Cheer,
            SceneId = "rocket",
            Description = "Ascending hypersonic space warp sweep (D5 → A5 → D6 → A6 → D7)",
            Notes = new AudioNote[]
            {
                new(587.33, 50, 0, Amplitude: 0.80, Harmonic2: 0.35, AttackMs: 3, DecayExponent: 4.0),
                new(880.00, 50, 40, Amplitude: 0.85, Harmonic2: 0.35, AttackMs: 3, DecayExponent: 4.0),
                new(1174.66, 50, 80, Amplitude: 0.90, Harmonic2: 0.35, AttackMs: 3, DecayExponent: 4.0),
                new(1760.00, 60, 120, Amplitude: 0.95, Harmonic2: 0.35, AttackMs: 3, DecayExponent: 3.5),
                new(2349.32, 240, 165, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.12, AttackMs: 4, DecayExponent: 2.6)
            }
        },

        // --- 8. Chibi Marathon Runner ("runner") ---
        new()
        {
            Id = "cheer_runner_whistle",
            Name = "Coach Sport Whistle",
            Icon = "🏃",
            EventType = SoundEffectType.Cheer,
            SceneId = "runner",
            Description = "Sharp athletic double-whistle tweet (G6 → G6 rapid vibrato chirp)",
            Notes = new AudioNote[]
            {
                new(1567.98, 70, 0, Amplitude: 0.95, Harmonic2: 0.40, Harmonic3: 0.15, AttackMs: 2, DecayExponent: 4.0),
                new(1567.98, 200, 80, Amplitude: 1.00, Harmonic2: 0.45, Harmonic3: 0.15, AttackMs: 2, DecayExponent: 2.8),
                new(1174.66, 200, 80, Amplitude: 0.50, Harmonic2: 0.20, AttackMs: 2, DecayExponent: 2.8)
            }
        },
        new()
        {
            Id = "cheer_runner_horn",
            Name = "Stadium Victory Horn",
            Icon = "🎺",
            EventType = SoundEffectType.Cheer,
            SceneId = "runner",
            Description = "Athletic finish line fanfare trumpet (C5 → E5 → G5 → C6)",
            Notes = new AudioNote[]
            {
                new(523.25, 75, 0, Amplitude: 0.80, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.5),
                new(659.25, 75, 60, Amplitude: 0.85, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.5),
                new(783.99, 85, 120, Amplitude: 0.90, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.2),
                new(1046.50, 260, 185, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.12, AttackMs: 5, DecayExponent: 2.5)
            }
        },
        new()
        {
            Id = "cheer_runner_squeak",
            Name = "Sprint Sneaker Squeak",
            Icon = "👟",
            EventType = SoundEffectType.Cheer,
            SceneId = "runner",
            Description = "High-energy bouncy sneaker squeak chime (A5 → D6 → F#6)",
            Notes = new AudioNote[]
            {
                new(880.00, 50, 0, Amplitude: 0.85, Harmonic2: 0.30, AttackMs: 2, DecayExponent: 4.5),
                new(1174.66, 60, 40, Amplitude: 0.90, Harmonic2: 0.30, AttackMs: 2, DecayExponent: 4.0),
                new(1479.98, 160, 85, Amplitude: 1.00, Harmonic2: 0.30, Harmonic3: 0.10, AttackMs: 3, DecayExponent: 3.2)
            }
        },

        // --- 9. Focus Pet Tamagotchi ("tamagotchi") ---
        new()
        {
            Id = "cheer_pet_happy",
            Name = "8-Bit Happy Chirp",
            Icon = "👾",
            EventType = SoundEffectType.Cheer,
            SceneId = "tamagotchi",
            Description = "Classic retro Game Boy happy chirp (C6 → E6 → G6 → C7)",
            Notes = new AudioNote[]
            {
                new(1046.50, 45, 0, Amplitude: 0.85, Harmonic2: 0.50, Harmonic3: 0.25, AttackMs: 2, DecayExponent: 4.5),
                new(1318.51, 45, 40, Amplitude: 0.90, Harmonic2: 0.50, Harmonic3: 0.25, AttackMs: 2, DecayExponent: 4.5),
                new(1567.98, 50, 80, Amplitude: 0.95, Harmonic2: 0.50, Harmonic3: 0.25, AttackMs: 2, DecayExponent: 4.0),
                new(2093.00, 180, 125, Amplitude: 1.00, Harmonic2: 0.50, Harmonic3: 0.25, AttackMs: 3, DecayExponent: 3.5)
            }
        },
        new()
        {
            Id = "cheer_pet_levelup",
            Name = "Pixel Item Power-Up",
            Icon = "🌟",
            EventType = SoundEffectType.Cheer,
            SceneId = "tamagotchi",
            Description = "Chiptune item pickup fanfare (F5 → A5 → C6 → F6)",
            Notes = new AudioNote[]
            {
                new(698.46, 50, 0, Amplitude: 0.85, Harmonic2: 0.45, AttackMs: 2, DecayExponent: 4.0),
                new(880.00, 50, 45, Amplitude: 0.90, Harmonic2: 0.45, AttackMs: 2, DecayExponent: 4.0),
                new(1046.50, 50, 90, Amplitude: 0.95, Harmonic2: 0.45, AttackMs: 2, DecayExponent: 4.0),
                new(1396.91, 200, 135, Amplitude: 1.00, Harmonic2: 0.45, Harmonic3: 0.20, AttackMs: 3, DecayExponent: 3.0)
            }
        },
        new()
        {
            Id = "cheer_pet_heart",
            Name = "Pixel Pet Love Beep",
            Icon = "💖",
            EventType = SoundEffectType.Cheer,
            SceneId = "tamagotchi",
            Description = "Cute double heart blip (G6 → C7)",
            Notes = new AudioNote[]
            {
                new(1567.98, 60, 0, Amplitude: 0.90, Harmonic2: 0.40, AttackMs: 2, DecayExponent: 4.5),
                new(2093.00, 180, 60, Amplitude: 1.00, Harmonic2: 0.40, Harmonic3: 0.15, AttackMs: 3, DecayExponent: 3.2)
            }
        },

        // --- 10. Lumberjack Wood Chopping ("lumberjack") ---
        new()
        {
            Id = "cheer_lumberjack_chop",
            Name = "Axe Strike & Wood Splinter",
            Icon = "🪓",
            EventType = SoundEffectType.Cheer,
            SceneId = "lumberjack",
            Description = "Crisp woody axe strike impact & timber splinter (E3 + A4 + E5)",
            Notes = new AudioNote[]
            {
                new(164.81, 120, 0, Amplitude: 0.90, Harmonic2: 0.20, AttackMs: 2, DecayExponent: 3.0),
                new(440.00, 140, 20, Amplitude: 0.85, Harmonic2: 0.25, AttackMs: 3, DecayExponent: 3.2),
                new(659.25, 180, 40, Amplitude: 0.95, Harmonic2: 0.35, AttackMs: 2, DecayExponent: 3.5),
                new(1318.51, 200, 70, Amplitude: 1.00, Harmonic2: 0.30, AttackMs: 3, DecayExponent: 3.0)
            }
        },
        new()
        {
            Id = "cheer_lumberjack_timber",
            Name = "'Timber!' Forest Echo",
            Icon = "🌲",
            EventType = SoundEffectType.Cheer,
            SceneId = "lumberjack",
            Description = "Resonant timber call & forest cascade chime (G5 → E5 → C5 → G4 → C4)",
            Notes = new AudioNote[]
            {
                new(783.99, 70, 0, Amplitude: 0.85, Harmonic2: 0.20, AttackMs: 4, DecayExponent: 3.5),
                new(659.25, 75, 55, Amplitude: 0.88, Harmonic2: 0.20, AttackMs: 4, DecayExponent: 3.5),
                new(523.25, 80, 115, Amplitude: 0.92, Harmonic2: 0.20, AttackMs: 4, DecayExponent: 3.2),
                new(392.00, 90, 180, Amplitude: 0.95, Harmonic2: 0.20, AttackMs: 5, DecayExponent: 3.0),
                new(261.63, 340, 250, Amplitude: 1.00, Harmonic2: 0.25, Harmonic3: 0.08, AttackMs: 8, DecayExponent: 2.2)
            }
        },
        new()
        {
            Id = "cheer_lumberjack_van",
            Name = "Timber Van Horn & Rev",
            Icon = "🚚",
            EventType = SoundEffectType.Cheer,
            SceneId = "lumberjack",
            Description = "Friendly vintage van dual horn beep (F4 → A4 + C5 → F5)",
            Notes = new AudioNote[]
            {
                new(349.23, 90, 0, Amplitude: 0.85, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.5),
                new(440.00, 90, 0, Amplitude: 0.85, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 3.5),
                new(523.25, 260, 95, Amplitude: 0.95, Harmonic2: 0.35, AttackMs: 4, DecayExponent: 2.8),
                new(698.46, 260, 95, Amplitude: 1.00, Harmonic2: 0.35, Harmonic3: 0.12, AttackMs: 4, DecayExponent: 2.8)
            }
        }
    };

    private static readonly AudioNote[] DefaultCheerNotes = new AudioNote[]
    {
        new(1318.51, 80, 0, Amplitude: 0.80, Harmonic2: 0.15, AttackMs: 5, DecayExponent: 3.5),
        new(1567.98, 80, 65, Amplitude: 0.85, Harmonic2: 0.15, AttackMs: 5, DecayExponent: 3.5),
        new(1975.53, 90, 130, Amplitude: 0.90, Harmonic2: 0.15, AttackMs: 5, DecayExponent: 3.5),
        new(2637.02, 220, 195, Amplitude: 1.00, Harmonic2: 0.10, AttackMs: 5, DecayExponent: 2.8)
    };

    public static List<SoundVariationInfo> GetVariationsForEvent(SoundEffectType type)
    {
        return Variations.Where(v => v.EventType == type).ToList();
    }

    public static List<SoundVariationInfo> GetVariationsForScene(string sceneId)
    {
        if (string.IsNullOrWhiteSpace(sceneId))
            return new List<SoundVariationInfo>();

        return Variations
            .Where(v => string.Equals(v.SceneId, sceneId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static void PlayStartSound(WorkRestSettings? settings = null)
    {
        PlaySound(SoundEffectType.Start, settings);
    }

    public static void PlayEndSound(WorkRestSettings? settings = null)
    {
        PlaySound(SoundEffectType.End, settings);
    }

    public static void PlayGoalAchievedSound(WorkRestSettings? settings = null)
    {
        PlaySound(SoundEffectType.GoalAchieved, settings);
    }

    public static void PlayDailyGoalAchievedSound(WorkRestSettings? settings = null)
    {
        PlaySound(SoundEffectType.DailyGoalAchieved, settings);
    }

    public static void PlayWeeklyGoalAchievedSound(WorkRestSettings? settings = null)
    {
        PlaySound(SoundEffectType.WeeklyGoalAchieved, settings);
    }

    public static void PlayBreakStartSound(WorkRestSettings? settings = null)
    {
        PlaySound(SoundEffectType.BreakStart, settings);
    }

    public static void PlayBreakEndSound(WorkRestSettings? settings = null)
    {
        PlaySound(SoundEffectType.BreakEnd, settings);
    }

    public static void PlayCheerSound(WorkRestSettings? settings = null)
    {
        PlayCheerSound(settings?.SelectedSceneId, settings);
    }

    public static void PlayCheerSound(string? sceneId, WorkRestSettings? settings = null)
    {
        settings ??= FocusCompanionStorage.Load();

        if (!settings.SoundAlertEnabled || settings.SoundVolume <= 0 || !settings.SoundOnCheerEnabled)
            return;

        var volume = Math.Clamp(settings.SoundVolume, 0, 100);

        var targetSceneId = !string.IsNullOrWhiteSpace(sceneId) ? sceneId : settings.SelectedSceneId;
        var sceneVariations = GetVariationsForScene(targetSceneId);

        var enabledSceneVariations = sceneVariations
            .Where(v => settings.IsVariationEnabled(v.Id))
            .ToList();

        if (enabledSceneVariations.Count > 0)
        {
            var chosen = enabledSceneVariations[Random.Shared.Next(enabledSceneVariations.Count)];
            var wavBytes = GenerateWavBytesForNotes(chosen.Notes, volume);
            PlayWavBytes(wavBytes);
            return;
        }

        // If no variations enabled specifically for this scene, check all cheer variations enabled
        var allCheer = Variations.Where(v => v.EventType == SoundEffectType.Cheer).ToList();
        var enabledCheer = allCheer.Where(v => settings.IsVariationEnabled(v.Id)).ToList();
        if (enabledCheer.Count > 0)
        {
            var chosen = enabledCheer[Random.Shared.Next(enabledCheer.Count)];
            var wavBytes = GenerateWavBytesForNotes(chosen.Notes, volume);
            PlayWavBytes(wavBytes);
            return;
        }

        // Fallback default cheer chime
        var defaultWav = GenerateWavBytesForNotes(DefaultCheerNotes, volume);
        PlayWavBytes(defaultWav);
    }

    public static void PlayVariationPreview(string variationId, int volume = 75)
    {
        var clampedVolume = Math.Clamp(volume, 0, 100);
        if (clampedVolume <= 0) return;

        var variation = Variations.FirstOrDefault(v => v.Id == variationId);
        if (variation == null) return;

        var wavBytes = GenerateWavBytesForNotes(variation.Notes, clampedVolume);
        PlayWavBytes(wavBytes);
    }

    public static void PlayPreview(SoundEffectType type, int volume = 75)
    {
        var clampedVolume = Math.Clamp(volume, 0, 100);
        if (clampedVolume <= 0) return;

        var list = GetVariationsForEvent(type);
        if (list.Count == 0)
        {
            var defaultWav = GenerateWavBytesForNotes(DefaultCheerNotes, clampedVolume);
            PlayWavBytes(defaultWav);
            return;
        }

        var chosen = list[Random.Shared.Next(list.Count)];
        var bytes = GenerateWavBytesForNotes(chosen.Notes, clampedVolume);
        PlayWavBytes(bytes);
    }

    public static void PlayScenePreview(string sceneId, int volume = 75)
    {
        var clampedVolume = Math.Clamp(volume, 0, 100);
        if (clampedVolume <= 0) return;

        var list = GetVariationsForScene(sceneId);
        if (list.Count == 0)
        {
            var defaultWav = GenerateWavBytesForNotes(DefaultCheerNotes, clampedVolume);
            PlayWavBytes(defaultWav);
            return;
        }

        var chosen = list[Random.Shared.Next(list.Count)];
        var bytes = GenerateWavBytesForNotes(chosen.Notes, clampedVolume);
        PlayWavBytes(bytes);
    }

    public static void PlaySound(SoundEffectType type, WorkRestSettings? settings = null)
    {
        settings ??= FocusCompanionStorage.Load();

        if (!settings.SoundAlertEnabled || settings.SoundVolume <= 0)
            return;

        // Check master event toggle
        var isEventEnabled = type switch
        {
            SoundEffectType.Start => settings.SoundOnStartEnabled,
            SoundEffectType.End => settings.SoundOnEndEnabled,
            SoundEffectType.GoalAchieved => settings.SoundOnGoalAchievedEnabled,
            SoundEffectType.DailyGoalAchieved => settings.SoundOnDailyGoalAchievedEnabled,
            SoundEffectType.WeeklyGoalAchieved => settings.SoundOnWeeklyGoalAchievedEnabled,
            SoundEffectType.BreakStart => settings.SoundOnBreakStartEnabled,
            SoundEffectType.BreakEnd => settings.SoundOnBreakEndEnabled,
            SoundEffectType.Cheer => settings.SoundOnCheerEnabled,
            _ => true
        };

        if (!isEventEnabled)
            return;

        var volume = Math.Clamp(settings.SoundVolume, 0, 100);

        if (type == SoundEffectType.Cheer)
        {
            PlayCheerSound(settings.SelectedSceneId, settings);
            return;
        }

        var allForEvent = GetVariationsForEvent(type);
        if (allForEvent.Count == 0) return;

        var enabledVariations = allForEvent
            .Where(v => settings.IsVariationEnabled(v.Id))
            .ToList();

        if (enabledVariations.Count == 0)
            return;

        var selectedVariation = enabledVariations[Random.Shared.Next(enabledVariations.Count)];
        var soundBytes = GenerateWavBytesForNotes(selectedVariation.Notes, volume);
        PlayWavBytes(soundBytes);
    }

    private static void PlayWavBytes(byte[] wavBytes)
    {
        if (wavBytes.Length <= 44) return;

        Task.Run(() =>
        {
            try
            {
                using var ms = new MemoryStream(wavBytes);
                using var player = new SoundPlayer(ms);
                player.PlaySync();
            }
            catch
            {
                // Fallback or ignore audio playback exceptions if system has no sound device
            }
        });
    }

    private static byte[] GenerateWavBytesForNotes(IReadOnlyList<AudioNote> notes, int volumePercent)
    {
        if (notes.Count == 0) return Array.Empty<byte>();

        // Calculate total duration in samples
        double totalDurationMs = 0;
        foreach (var note in notes)
        {
            var endMs = note.OffsetMs + note.DurationMs;
            if (endMs > totalDurationMs) totalDurationMs = endMs;
        }

        totalDurationMs += 80; // Small tail for natural decay
        int totalSamples = (int)(SampleRate * (totalDurationMs / 1000.0));
        if (totalSamples <= 0) return Array.Empty<byte>();

        var mixBuffer = new float[totalSamples];
        var volumeScale = volumePercent / 100.0f;

        foreach (var note in notes)
        {
            int startSample = (int)(SampleRate * (note.OffsetMs / 1000.0));
            int noteSamples = (int)(SampleRate * (note.DurationMs / 1000.0));
            int attackSamples = Math.Max(1, (int)(SampleRate * (note.AttackMs / 1000.0)));

            double freq = note.Frequency;
            double twoPi = 2.0 * Math.PI;

            for (int i = 0; i < noteSamples; i++)
            {
                int bufferIndex = startSample + i;
                if (bufferIndex >= totalSamples) break;

                double t = i / (double)SampleRate;

                // Envelope calculation: linear attack, exponential decay
                double envelope;
                if (i < attackSamples)
                {
                    envelope = i / (double)attackSamples;
                }
                else
                {
                    double decayT = (i - attackSamples) / (double)(noteSamples - attackSamples);
                    envelope = Math.Max(0.0, 1.0 - Math.Pow(decayT, 1.0 / Math.Max(0.1, note.DecayExponent)));
                }

                // Add fundamental and harmonics
                double fundamental = Math.Sin(twoPi * freq * t);
                double harmonic2 = note.Harmonic2 * Math.Sin(twoPi * freq * 2.0 * t);
                double harmonic3 = note.Harmonic3 * Math.Sin(twoPi * freq * 3.0 * t);

                double sample = (fundamental + harmonic2 + harmonic3) * note.Amplitude * envelope;
                mixBuffer[bufferIndex] += (float)sample;
            }
        }

        // Find peak for soft normalisation / clipping avoidance
        float peak = 0.0f;
        for (int i = 0; i < totalSamples; i++)
        {
            float abs = Math.Abs(mixBuffer[i]);
            if (abs > peak) peak = abs;
        }

        float normaliseFactor = peak > 1.0f ? (1.0f / peak) : 1.0f;

        // Convert to 16-bit PCM WAV
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        int byteRate = SampleRate * 1 * 2; // SampleRate * NumChannels * BitsPerSample/8
        int subChunk2Size = totalSamples * 1 * 2;
        int chunkSize = 36 + subChunk2Size;

        // RIFF header
        writer.Write("RIFF"u8.ToArray());
        writer.Write(chunkSize);
        writer.Write("WAVE"u8.ToArray());

        // "fmt " sub-chunk
        writer.Write("fmt "u8.ToArray());
        writer.Write(16); // Subchunk1Size for PCM
        writer.Write((short)1); // AudioFormat: 1 = PCM
        writer.Write((short)1); // NumChannels: 1 = Mono
        writer.Write(SampleRate); // SampleRate
        writer.Write(byteRate); // ByteRate
        writer.Write((short)2); // BlockAlign: NumChannels * BitsPerSample/8
        writer.Write((short)16); // BitsPerSample: 16-bit

        // "data" sub-chunk
        writer.Write("data"u8.ToArray());
        writer.Write(subChunk2Size);

        for (int i = 0; i < totalSamples; i++)
        {
            float sample = mixBuffer[i] * normaliseFactor * (float)volumeScale;
            sample = Math.Clamp(sample, -1.0f, 1.0f);
            short pcmSample = (short)(sample * 32767.0f);
            writer.Write(pcmSample);
        }

        writer.Flush();
        return ms.ToArray();
    }
}
