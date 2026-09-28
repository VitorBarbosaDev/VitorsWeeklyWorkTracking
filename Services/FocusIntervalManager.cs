using System;
using System.Media;
using System.Threading.Tasks;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class IntervalAlertEventArgs : EventArgs
{
    public IntervalPhase PreviousPhase { get; set; }
    public IntervalPhase NewPhase { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty;
}

public class FocusIntervalManager
{
    public WorkRestSettings Settings { get; private set; }
    public IntervalPhase CurrentPhase { get; private set; } = IntervalPhase.None;
    public int CurrentCycle { get; private set; } = 1;
    public DateTime? PhaseStartTime { get; private set; }
    public TimeSpan ExtraBreakTime { get; private set; } = TimeSpan.Zero;
    public bool IsAlertPending { get; set; } = false;
    public IntervalAlertEventArgs? LastAlert { get; private set; }

    public event EventHandler<IntervalAlertEventArgs>? AlertTriggered;
    public event EventHandler<IntervalPhase>? PhaseChanged;

    private static readonly string[] RestTips =
    {
        "💧 Grab a refreshing glass of water and hydrate!",
        "👀 20-20-20 Rule: Look at an object 20 feet away for 20 seconds.",
        "🧘 Stand up, stretch your shoulders, neck, and back.",
        "🚶 Take a short stroll around your room or office.",
        "🌿 Take 3 slow, deep breaths to reset your mind.",
        "☕ Sip your favorite tea or coffee and relax your eyes."
    };

    private static readonly string[] FocusTips =
    {
        "🎯 Clear distractions and lock into your single main task!",
        "🚀 Flow state engaged! Let's make steady, focused progress.",
        "✨ One step at a time — great work happens in small focused sprints.",
        "💡 Focus on quality and take pride in your craft."
    };

    public FocusIntervalManager(WorkRestSettings settings)
    {
        Settings = settings;
    }

    public void UpdateSettings(WorkRestSettings settings)
    {
        Settings = settings;
        FocusCompanionStorage.Save(Settings);
    }

    public bool IsRestPhase => CurrentPhase == IntervalPhase.ShortBreak || CurrentPhase == IntervalPhase.LongBreak;

    public TimeSpan CurrentPhaseTargetDuration
    {
        get
        {
            return CurrentPhase switch
            {
                IntervalPhase.Focus => TimeSpan.FromMinutes(Settings.FocusMinutes),
                IntervalPhase.ShortBreak => TimeSpan.FromMinutes(Settings.ShortBreakMinutes) + ExtraBreakTime,
                IntervalPhase.LongBreak => TimeSpan.FromMinutes(Settings.LongBreakMinutes) + ExtraBreakTime,
                _ => TimeSpan.Zero
            };
        }
    }

    public TimeSpan Elapsed
    {
        get
        {
            if (PhaseStartTime == null) return TimeSpan.Zero;
            return DateTime.Now - PhaseStartTime.Value;
        }
    }

    public TimeSpan Remaining
    {
        get
        {
            var target = CurrentPhaseTargetDuration;
            if (target <= TimeSpan.Zero) return TimeSpan.Zero;
            var elapsed = Elapsed;
            return elapsed < target ? target - elapsed : TimeSpan.Zero;
        }
    }

    public double ProgressPercentage
    {
        get
        {
            var target = CurrentPhaseTargetDuration.TotalSeconds;
            if (target <= 0) return 0.0;
            var elapsed = Elapsed.TotalSeconds;
            return Math.Clamp((elapsed / target) * 100.0, 0.0, 100.0);
        }
    }

    public void StartTracking(DateTime startTime)
    {
        if (Settings.IntervalModeEnabled)
        {
            CurrentPhase = IntervalPhase.Focus;
            PhaseStartTime = startTime;
            ExtraBreakTime = TimeSpan.Zero;
            IsAlertPending = false;
            PhaseChanged?.Invoke(this, CurrentPhase);
        }
        else
        {
            CurrentPhase = IntervalPhase.None;
            PhaseStartTime = startTime;
        }
    }

    public void StopTracking()
    {
        CurrentPhase = IntervalPhase.None;
        PhaseStartTime = null;
        ExtraBreakTime = TimeSpan.Zero;
        IsAlertPending = false;
    }

    public void CheckTick(DateTime now)
    {
        if (!Settings.IntervalModeEnabled || PhaseStartTime == null || CurrentPhase == IntervalPhase.None)
            return;

        var elapsed = now - PhaseStartTime.Value;
        var target = CurrentPhaseTargetDuration;

        if (elapsed >= target && !IsAlertPending)
        {
            IsAlertPending = true;
            TriggerPhaseEndAlert();

            if (Settings.AutoAdvancePhases)
            {
                AdvanceToNextPhase(now);
            }
        }
    }

    public void AdvanceToNextPhase(DateTime? atTime = null)
    {
        var prev = CurrentPhase;
        ExtraBreakTime = TimeSpan.Zero;
        IsAlertPending = false;

        if (prev == IntervalPhase.Focus)
        {
            Settings.TotalSessionsCompleted++;
            Settings.FocusXp += 25;
            FocusCompanionStorage.Save(Settings);

            if (CurrentCycle >= Settings.CyclesBeforeLongBreak)
            {
                CurrentPhase = IntervalPhase.LongBreak;
                CurrentCycle = 1;
            }
            else
            {
                CurrentPhase = IntervalPhase.ShortBreak;
            }
        }
        else if (prev == IntervalPhase.ShortBreak)
        {
            CurrentCycle++;
            CurrentPhase = IntervalPhase.Focus;
        }
        else if (prev == IntervalPhase.LongBreak)
        {
            CurrentCycle = 1;
            CurrentPhase = IntervalPhase.Focus;
        }
        else
        {
            CurrentPhase = IntervalPhase.Focus;
        }

        PhaseStartTime = atTime ?? DateTime.Now;
        PhaseChanged?.Invoke(this, CurrentPhase);
    }

    public void SkipToNextPhase()
    {
        AdvanceToNextPhase(DateTime.Now);
    }

    public void AddExtraBreak(int extraMinutes)
    {
        if (IsRestPhase)
        {
            ExtraBreakTime += TimeSpan.FromMinutes(extraMinutes);
            IsAlertPending = false;
        }
    }

    public void DismissAlert()
    {
        IsAlertPending = false;
    }

    private void TriggerPhaseEndAlert()
    {
        var random = new Random();
        IntervalAlertEventArgs alert;

        if (CurrentPhase == IntervalPhase.Focus)
        {
            bool isLongBreak = CurrentCycle >= Settings.CyclesBeforeLongBreak;
            var breakType = isLongBreak ? "Long Rest Break" : "Short Rest Break";
            var breakMinutes = isLongBreak ? Settings.LongBreakMinutes : Settings.ShortBreakMinutes;
            var tip = RestTips[random.Next(RestTips.Length)];

            alert = new IntervalAlertEventArgs
            {
                PreviousPhase = IntervalPhase.Focus,
                NewPhase = isLongBreak ? IntervalPhase.LongBreak : IntervalPhase.ShortBreak,
                Title = "🔔 Rest Time! Great Job!",
                Message = $"You've finished your {Settings.FocusMinutes}-minute focus session (Cycle {CurrentCycle}/{Settings.CyclesBeforeLongBreak}). Time for a well-deserved {breakMinutes}-minute {breakType}!",
                Tip = tip
            };
        }
        else
        {
            var tip = FocusTips[random.Next(FocusTips.Length)];
            alert = new IntervalAlertEventArgs
            {
                PreviousPhase = CurrentPhase,
                NewPhase = IntervalPhase.Focus,
                Title = "⚡ Break Finished! Ready to Focus?",
                Message = $"Rest period complete! Let's begin focus session {CurrentCycle} of {Settings.CyclesBeforeLongBreak} ({Settings.FocusMinutes} min).",
                Tip = tip
            };
        }

        LastAlert = alert;

        if (Settings.SoundAlertEnabled)
        {
            PlayAlertSound();
        }

        AlertTriggered?.Invoke(this, alert);
    }

    public static void PlayAlertSound()
    {
        Task.Run(() =>
        {
            try
            {
                // Play pleasant system chime
                SystemSounds.Asterisk.Play();
            }
            catch
            {
                // Ignore audio playback exceptions if system has no sound device
            }
        });
    }
}
