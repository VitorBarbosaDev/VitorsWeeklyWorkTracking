using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class WelcomeBackDialog : Window
{
    public bool RequestOpenPlanner { get; private set; } = false;

    public class DisplayGoalItem
    {
        public string ProjectName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public double PlannedHours { get; set; }
        public string FormattedDuration => PlannedHours >= 1.0 ? $"{PlannedHours:F1} hrs" : $"{PlannedHours * 60:F0} min";
        public Visibility HasNoteVisibility => string.IsNullOrWhiteSpace(Note) ? Visibility.Collapsed : Visibility.Visible;
    }

    public WelcomeBackDialog(string? userName, List<PlannedWorkItem> todayGoals)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        SetupGreeting(userName);
        SetupDate();
        SetupGoals(todayGoals);
    }

    private void SetupGreeting(string? userName)
    {
        if (!string.IsNullOrWhiteSpace(userName))
        {
            GreetingTextBlock.Text = $"👋 Welcome back, {userName.Trim()}!";
        }
        else
        {
            GreetingTextBlock.Text = "👋 Welcome back!";
        }
    }

    private void SetupDate()
    {
        var now = DateTime.Today;
        DateTextBlock.Text = now.ToString("dddd, dd MMMM yyyy");
    }

    private void SetupGoals(List<PlannedWorkItem> todayGoals)
    {
        if (todayGoals != null && todayGoals.Count > 0)
        {
            GoalsPanel.Visibility = Visibility.Visible;
            NoGoalsPanel.Visibility = Visibility.Collapsed;

            var items = todayGoals.Select(g => new DisplayGoalItem
            {
                ProjectName = string.IsNullOrWhiteSpace(g.ProjectName) ? "General Work" : g.ProjectName,
                Note = g.Note ?? string.Empty,
                PlannedHours = g.PlannedHours
            }).ToList();

            GoalsItemsControl.ItemsSource = items;

            double totalHours = todayGoals.Sum(g => g.PlannedHours);
            TotalPlannedTextBlock.Text = $"Total: {totalHours:F1} hrs ({todayGoals.Count} task{(todayGoals.Count == 1 ? "" : "s")})";

            string[] motivationalQuotes = new[]
            {
                "💡 Focus on one goal at a time and celebrate your progress along the way!",
                "✨ You've got clear goals scheduled. Let's make today a fantastic and productive day!",
                "🎯 Step by step, minute by minute — you're ready to crush today's roadmap!",
                "🚀 Steady pace, clear mind. Let's get things done today!"
            };
            int index = Math.Abs(DateTime.Today.DayOfYear) % motivationalQuotes.Length;
            MotivationTextBlock.Text = motivationalQuotes[index];
        }
        else
        {
            GoalsPanel.Visibility = Visibility.Collapsed;
            NoGoalsPanel.Visibility = Visibility.Visible;
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OpenPlannerButton_Click(object sender, RoutedEventArgs e)
    {
        RequestOpenPlanner = true;
        DialogResult = true;
        Close();
    }

    private void PlanDayButton_Click(object sender, RoutedEventArgs e)
    {
        RequestOpenPlanner = true;
        DialogResult = true;
        Close();
    }
}
