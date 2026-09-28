using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class CalendarPlannerWindow : Window
{
    private readonly List<TimeEntry> _allEntries;
    private readonly List<Project> _allProjects;
    private readonly List<ActivityItem> _allActivities;

    private DateTime _currentMonday;
    private DateTime _selectedPlanDate;
    private string? _editingPlanId = null;
    private bool _isInitializing = true;

    public CalendarPlannerWindow(
        List<TimeEntry> entries,
        List<Project> projects,
        List<ActivityItem>? activities = null,
        DateTime? initialDate = null)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        _allEntries = entries ?? new List<TimeEntry>();
        _allProjects = projects ?? new List<Project>();
        _allActivities = activities ?? new List<ActivityItem>();

        var targetDate = initialDate ?? DateTime.Today;
        _currentMonday = CalendarPlanStorage.GetWeekBounds(targetDate).Monday;
        _selectedPlanDate = targetDate.Date;

        PopulateDropdowns();

        PlanDatePicker.SelectedDate = targetDate.Date;
        WeekDatePicker.SelectedDate = targetDate.Date;

        _isInitializing = false;
        LoadWeekData();
    }

    private void PopulateDropdowns()
    {
        PlanProjectComboBox.Items.Clear();
        var projects = _allProjects.Where(p => p.IsActive).Select(p => p.Name)
            .Union(_allProjects.Select(p => p.Name))
            .Union(_allEntries.Select(e => e.ProjectName))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        if (projects.Count == 0)
        {
            projects.Add("General Freelance");
        }

        foreach (var proj in projects)
        {
            PlanProjectComboBox.Items.Add(proj);
        }
        PlanProjectComboBox.SelectedIndex = 0;

        PlanActivityComboBox.Items.Clear();
        var activities = (_allActivities.Count > 0 ? _allActivities.Where(a => a.IsActive).Select(a => a.Name) : ActivityStorage.DefaultActivities)
            .Union(_allEntries.Select(e => e.Activity))
            .Where(act => !string.IsNullOrWhiteSpace(act))
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        if (activities.Count == 0)
        {
            activities.Add("Development");
        }

        foreach (var act in activities)
        {
            PlanActivityComboBox.Items.Add(act);
        }
        PlanActivityComboBox.SelectedIndex = 0;
    }

    private void LoadWeekData()
    {
        if (_isInitializing) return;

        var sunday = _currentMonday.AddDays(6);
        WeekRangeLabel.Text = $"{_currentMonday:ddd, dd MMM} - {sunday:ddd, dd MMM yyyy}";
        WeekDatePicker.SelectedDate = _currentMonday;

        var weeklySummary = CalendarPlanStorage.Instance.CalculateWeeklySummary(_currentMonday, _allEntries);

        WeeklyPlannedHoursText.Text = $"{weeklySummary.TotalPlannedHours:F1} hrs";
        WeeklyActualHoursText.Text = $"{weeklySummary.TotalActualHours:F2} hrs";
        WeeklyProgressPctText.Text = weeklySummary.FormattedCompletionPercentage;
        WeeklyRemainingText.Text = weeklySummary.FormattedRemainingHours;
        WeeklyProgressBar.Value = Math.Min(100, weeklySummary.CompletionPercentage);

        // Update 7 day columns (Monday = 0 ... Sunday = 6)
        var dayBorders = new[] { Day0Border, Day1Border, Day2Border, Day3Border, Day4Border, Day5Border, Day6Border };
        var dayNameTexts = new[] { Day0NameText, Day1NameText, Day2NameText, Day3NameText, Day4NameText, Day5NameText, Day6NameText };
        var dayDateTexts = new[] { Day0DateText, Day1DateText, Day2DateText, Day3DateText, Day4DateText, Day5DateText, Day6DateText };
        var dayStatsTexts = new[] { Day0StatsText, Day1StatsText, Day2StatsText, Day3StatsText, Day4StatsText, Day5StatsText, Day6StatsText };
        var dayProgressBars = new[] { Day0ProgressBar, Day1ProgressBar, Day2ProgressBar, Day3ProgressBar, Day4ProgressBar, Day5ProgressBar, Day6ProgressBar };
        var dayPanels = new[] { Day0PlansPanel, Day1PlansPanel, Day2PlansPanel, Day3PlansPanel, Day4PlansPanel, Day5PlansPanel, Day6PlansPanel };

        for (int i = 0; i < 7; i++)
        {
            var dayDate = _currentMonday.AddDays(i);
            var daySummary = weeklySummary.DailySummaries.FirstOrDefault(d => d.Date.Date == dayDate.Date)
                ?? CalendarPlanStorage.Instance.CalculateDailySummary(dayDate, _allEntries);

            bool isToday = dayDate.Date == DateTime.Today;

            dayNameTexts[i].Text = dayDate.ToString("dddd").ToUpperInvariant();
            dayDateTexts[i].Text = dayDate.ToString("dd MMM yyyy") + (isToday ? " • TODAY" : "");
            
            if (isToday)
            {
                dayBorders[i].BorderBrush = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
                dayBorders[i].BorderThickness = new Thickness(2);
                dayNameTexts[i].Foreground = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
                dayDateTexts[i].Foreground = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
            }
            else
            {
                dayBorders[i].BorderBrush = TryFindResource("Theme.CardBorder") as Brush ?? Brushes.LightGray;
                dayBorders[i].BorderThickness = new Thickness(1);
                dayNameTexts[i].Foreground = i < 5 
                    ? (TryFindResource("Theme.Foreground") as Brush ?? Brushes.Black) 
                    : (TryFindResource("Theme.ForegroundSubtle") as Brush ?? Brushes.Gray);
                dayDateTexts[i].Foreground = TryFindResource("Theme.ForegroundMuted") as Brush ?? Brushes.Gray;
            }

            dayStatsTexts[i].Text = $"Plan: {daySummary.PlannedHours:F1}h • Act: {daySummary.ActualHours:F1}h";
            dayProgressBars[i].Value = Math.Min(100, daySummary.CompletionPercentage);

            // Populate day plans panel
            dayPanels[i].Children.Clear();

            if (daySummary.PlannedItems.Count == 0)
            {
                var emptyBlock = new TextBlock
                {
                    Text = isToday ? "No plans yet for today." : "No plans scheduled.",
                    FontSize = 9.5,
                    FontStyle = FontStyles.Italic,
                    Foreground = (Brush)FindResource("Theme.ForegroundMuted"),
                    Margin = new Thickness(2, 6, 2, 6),
                    TextWrapping = TextWrapping.Wrap
                };
                dayPanels[i].Children.Add(emptyBlock);
            }
            else
            {
                foreach (var plan in daySummary.PlannedItems)
                {
                    var planCard = CreatePlanCardElement(plan);
                    dayPanels[i].Children.Add(planCard);
                }
            }
        }

        // Update All Plans DataGrid (week plans)
        var weekPlans = CalendarPlanStorage.Instance.GetPlansForRange(_currentMonday, sunday);
        AllPlansDataGrid.ItemsSource = null;
        AllPlansDataGrid.ItemsSource = weekPlans;
        AllPlansEmptyStatePanel.Visibility = weekPlans.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // Update Project Goals DataGrid
        var projectComparisons = weeklySummary.ProjectComparisons ?? new List<ProjectGoalComparison>();
        ProjectGoalsDataGrid.ItemsSource = null;
        ProjectGoalsDataGrid.ItemsSource = projectComparisons;
        ProjectGoalsEmptyStatePanel.Visibility = projectComparisons.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        UpdateDaySelectionUI();
    }

    private void UpdateDaySelectionUI()
    {
        var dayBorders = new[] { Day0Border, Day1Border, Day2Border, Day3Border, Day4Border, Day5Border, Day6Border };
        var daySelectedBadges = new[] { Day0SelectedBadge, Day1SelectedBadge, Day2SelectedBadge, Day3SelectedBadge, Day4SelectedBadge, Day5SelectedBadge, Day6SelectedBadge };
        var dayButtons = new[] { Day0AddPlanButton, Day1AddPlanButton, Day2AddPlanButton, Day3AddPlanButton, Day4AddPlanButton, Day5AddPlanButton, Day6AddPlanButton };
        var dayNameTexts = new[] { Day0NameText, Day1NameText, Day2NameText, Day3NameText, Day4NameText, Day5NameText, Day6NameText };
        var dayDateTexts = new[] { Day0DateText, Day1DateText, Day2DateText, Day3DateText, Day4DateText, Day5DateText, Day6DateText };

        var selectedDate = _selectedPlanDate.Date;

        if (_editingPlanId != null)
        {
            EditorHeaderLabel.Text = $"✏ Editing Plan for: {selectedDate:dddd, dd MMM}";
        }
        else
        {
            EditorHeaderLabel.Text = $"➕ Plan Session for: {selectedDate:dddd, dd MMM}";
        }

        for (int i = 0; i < 7; i++)
        {
            var dayDate = _currentMonday.AddDays(i);
            bool isToday = dayDate.Date == DateTime.Today;
            bool isSelected = dayDate.Date == selectedDate;

            if (isSelected)
            {
                dayBorders[i].BorderBrush = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
                dayBorders[i].BorderThickness = new Thickness(2.5);
                dayBorders[i].Background = TryFindResource("Theme.CardBackgroundAlt") as Brush ?? Brushes.LightCyan;
                daySelectedBadges[i].Visibility = Visibility.Visible;
                dayButtons[i].Content = "✓ Planning Day";
                dayButtons[i].Style = (Style)FindResource("PrimaryButton");
                dayNameTexts[i].Foreground = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
                dayDateTexts[i].Foreground = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
            }
            else if (isToday)
            {
                dayBorders[i].BorderBrush = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
                dayBorders[i].BorderThickness = new Thickness(1.5);
                dayBorders[i].Background = TryFindResource("Theme.CardBackground") as Brush ?? Brushes.White;
                daySelectedBadges[i].Visibility = Visibility.Collapsed;
                dayButtons[i].Content = "➕ Add Plan";
                dayButtons[i].Style = (Style)FindResource("GhostButton");
                dayNameTexts[i].Foreground = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
                dayDateTexts[i].Foreground = TryFindResource("Theme.ForegroundMuted") as Brush ?? Brushes.Gray;
            }
            else
            {
                dayBorders[i].BorderBrush = TryFindResource("Theme.CardBorder") as Brush ?? Brushes.LightGray;
                dayBorders[i].BorderThickness = new Thickness(1);
                dayBorders[i].Background = TryFindResource("Theme.CardBackground") as Brush ?? Brushes.White;
                daySelectedBadges[i].Visibility = Visibility.Collapsed;
                dayButtons[i].Content = "➕ Add Plan";
                dayButtons[i].Style = (Style)FindResource("GhostButton");
                dayNameTexts[i].Foreground = i < 5 
                    ? (TryFindResource("Theme.Foreground") as Brush ?? Brushes.Black) 
                    : (TryFindResource("Theme.ForegroundSubtle") as Brush ?? Brushes.Gray);
                dayDateTexts[i].Foreground = TryFindResource("Theme.ForegroundMuted") as Brush ?? Brushes.Gray;
            }
        }
    }

    private UIElement CreatePlanCardElement(PlannedWorkItem plan)
    {
        var card = new Border
        {
            Background = (Brush)FindResource("Theme.CardBackgroundAlt"),
            BorderBrush = (Brush)FindResource("Theme.CardBorder"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(6, 5, 6, 5),
            Margin = new Thickness(0, 0, 0, 4)
        };

        var stack = new StackPanel();

        // Top line: Project name + Planned hours badge
        var topLine = new Grid();
        topLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var projText = new TextBlock
        {
            Text = plan.ProjectName,
            FontWeight = FontWeights.Bold,
            FontSize = 10,
            Foreground = (Brush)FindResource("Theme.Foreground"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(projText, 0);
        topLine.Children.Add(projText);

        var hoursBadge = new Border
        {
            Background = (Brush)FindResource("Theme.CardBackground"),
            BorderBrush = (Brush)FindResource("Theme.Primary"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(4, 1, 4, 1)
        };
        var hoursText = new TextBlock
        {
            Text = plan.FormattedHours,
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("Theme.Primary")
        };
        hoursBadge.Child = hoursText;
        Grid.SetColumn(hoursBadge, 1);
        topLine.Children.Add(hoursBadge);

        stack.Children.Add(topLine);

        // Activity and Notes
        if (!string.IsNullOrWhiteSpace(plan.Activity))
        {
            var actText = new TextBlock
            {
                Text = $"🏷️ {plan.Activity}",
                FontSize = 9,
                Foreground = (Brush)FindResource("Theme.ForegroundMuted"),
                Margin = new Thickness(0, 2, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            stack.Children.Add(actText);
        }

        if (!string.IsNullOrWhiteSpace(plan.Note))
        {
            var noteText = new TextBlock
            {
                Text = plan.Note,
                FontSize = 9,
                FontStyle = FontStyles.Italic,
                Foreground = (Brush)FindResource("Theme.ForegroundSubtle"),
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 30
            };
            stack.Children.Add(noteText);
        }

        // Action buttons: Edit & Delete
        var actionsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var editBtn = new Button
        {
            Content = "✏",
            Style = (Style)FindResource("GhostButton"),
            FontSize = 9,
            Padding = new Thickness(4, 1, 4, 1),
            Height = 18,
            Margin = new Thickness(0, 0, 3, 0),
            Tag = plan
        };
        editBtn.Click += (s, e) => StartEditPlan(plan);
        actionsPanel.Children.Add(editBtn);

        var delBtn = new Button
        {
            Content = "✕",
            Style = (Style)FindResource("GhostDangerButton"),
            FontSize = 9,
            Padding = new Thickness(4, 1, 4, 1),
            Height = 18,
            Tag = plan
        };
        delBtn.Click += (s, e) => DeletePlan(plan);
        actionsPanel.Children.Add(delBtn);

        stack.Children.Add(actionsPanel);

        card.Child = stack;
        return card;
    }

    private bool ConfirmDiscardUnsavedNotes(string actionDescription = "start a new plan")
    {
        if (!string.IsNullOrWhiteSpace(PlanNoteTextBox.Text))
        {
            var result = MessageBox.Show(
                $"You have unsaved text in Notes:\n\"{PlanNoteTextBox.Text.Trim()}\"\n\nDo you want to discard it and {actionDescription}?",
                "Unsaved Notes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }
        return true;
    }

    private void StartEditPlan(PlannedWorkItem plan)
    {
        if (_editingPlanId != plan.Id && !ConfirmDiscardUnsavedNotes($"edit plan for '{plan.ProjectName}'"))
        {
            return;
        }

        _editingPlanId = plan.Id;
        _selectedPlanDate = plan.Date;
        PlanDatePicker.SelectedDate = plan.Date;

        if (!PlanProjectComboBox.Items.Contains(plan.ProjectName))
        {
            PlanProjectComboBox.Items.Add(plan.ProjectName);
        }
        PlanProjectComboBox.SelectedItem = plan.ProjectName;

        if (!string.IsNullOrWhiteSpace(plan.Activity))
        {
            if (!PlanActivityComboBox.Items.Contains(plan.Activity))
            {
                PlanActivityComboBox.Items.Add(plan.Activity);
            }
            PlanActivityComboBox.SelectedItem = plan.Activity;
        }

        PlanHoursTextBox.Text = plan.PlannedHours.ToString("0.##");
        PlanNoteTextBox.Text = plan.Note;

        SavePlanButton.Content = "💾 Update Plan";
        CancelEditButton.Visibility = Visibility.Visible;
        UpdateDaySelectionUI();
    }

    private void CancelEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardUnsavedNotes("cancel editing"))
        {
            return;
        }
        ResetEditor(clearNotes: true);
    }

    private void ResetEditor(bool clearNotes = true)
    {
        _editingPlanId = null;
        if (clearNotes)
        {
            PlanNoteTextBox.Text = string.Empty;
        }
        PlanHoursTextBox.Text = "4.0";
        SavePlanButton.Content = "💾 Save Plan";
        CancelEditButton.Visibility = Visibility.Collapsed;
        UpdateDaySelectionUI();
    }

    private void SavePlanButton_Click(object sender, RoutedEventArgs e)
    {
        var date = PlanDatePicker.SelectedDate ?? DateTime.Today;
        var project = PlanProjectComboBox.SelectedItem?.ToString() ?? PlanProjectComboBox.Text?.Trim() ?? string.Empty;
        var activity = PlanActivityComboBox.SelectedItem?.ToString() ?? PlanActivityComboBox.Text?.Trim() ?? string.Empty;
        var note = PlanNoteTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(project))
        {
            MessageBox.Show("Please select or enter a project name.", "Project Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(PlanHoursTextBox.Text.Trim(), out double hours) || hours <= 0 || hours > 24)
        {
            MessageBox.Show("Please enter a valid planned hours amount between 0.1 and 24 hours.", "Invalid Hours", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var planItem = new PlannedWorkItem
        {
            Id = _editingPlanId ?? Guid.NewGuid().ToString("N"),
            Date = date.Date,
            ProjectName = project,
            Activity = activity,
            PlannedHours = Math.Round(hours, 2),
            Note = note,
            CreatedAt = DateTime.Now
        };

        CalendarPlanStorage.Instance.AddOrUpdatePlan(planItem);
        ResetEditor(clearNotes: true);
        LoadWeekData();
    }

    private void DeletePlan(PlannedWorkItem plan)
    {
        var result = MessageBox.Show(
            $"Are you sure you want to delete planned session for '{plan.ProjectName}' on {plan.Date:yyyy-MM-dd} ({plan.FormattedHours})?",
            "Confirm Delete Plan",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            CalendarPlanStorage.Instance.DeletePlan(plan.Id);
            if (_editingPlanId == plan.Id)
            {
                ResetEditor(clearNotes: true);
            }
            LoadWeekData();
        }
    }

    private void EditPlanItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PlannedWorkItem plan })
        {
            StartEditPlan(plan);
        }
        else if (sender is Button btn && btn.DataContext is PlannedWorkItem contextPlan)
        {
            StartEditPlan(contextPlan);
        }
    }

    private void DeletePlanItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PlannedWorkItem plan })
        {
            DeletePlan(plan);
        }
        else if (sender is Button btn && btn.DataContext is PlannedWorkItem contextPlan)
        {
            DeletePlan(contextPlan);
        }
    }

    private void DayAddPlan_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tagStr } && int.TryParse(tagStr, out int dayOffset))
        {
            var targetDay = _currentMonday.AddDays(dayOffset);
            if ((_selectedPlanDate.Date != targetDay.Date || _editingPlanId != null) && !string.IsNullOrWhiteSpace(PlanNoteTextBox.Text))
            {
                if (!ConfirmDiscardUnsavedNotes($"plan for {targetDay:dddd, dd MMM}"))
                {
                    return;
                }
            }

            _selectedPlanDate = targetDay;
            PlanDatePicker.SelectedDate = targetDay;
            ResetEditor(clearNotes: true);
            UpdateDaySelectionUI();
            PlanHoursTextBox.Focus();
            PlanHoursTextBox.SelectAll();
        }
    }

    private void DayBorder_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button || e.OriginalSource is TextBox || e.OriginalSource is ComboBox)
            return;

        if (sender is Border { Tag: string tagStr } && int.TryParse(tagStr, out int dayOffset))
        {
            var targetDay = _currentMonday.AddDays(dayOffset);
            if ((_selectedPlanDate.Date != targetDay.Date || _editingPlanId != null) && !string.IsNullOrWhiteSpace(PlanNoteTextBox.Text))
            {
                if (!ConfirmDiscardUnsavedNotes($"plan for {targetDay:dddd, dd MMM}"))
                {
                    return;
                }
            }

            _selectedPlanDate = targetDay;
            PlanDatePicker.SelectedDate = targetDay;
            ResetEditor(clearNotes: true);
            UpdateDaySelectionUI();
        }
    }

    private void PlanDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (PlanDatePicker.SelectedDate.HasValue)
        {
            _selectedPlanDate = PlanDatePicker.SelectedDate.Value.Date;
            UpdateDaySelectionUI();
        }
    }

    private void HoursPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string hoursStr })
        {
            PlanHoursTextBox.Text = hoursStr;
        }
    }

    private void PrevWeekButton_Click(object sender, RoutedEventArgs e)
    {
        _currentMonday = _currentMonday.AddDays(-7);
        LoadWeekData();
    }

    private void NextWeekButton_Click(object sender, RoutedEventArgs e)
    {
        _currentMonday = _currentMonday.AddDays(7);
        LoadWeekData();
    }

    private void ThisWeekButton_Click(object sender, RoutedEventArgs e)
    {
        _currentMonday = CalendarPlanStorage.GetWeekBounds(DateTime.Today).Monday;
        LoadWeekData();
    }

    private void WeekDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (WeekDatePicker.SelectedDate.HasValue)
        {
            var newMonday = CalendarPlanStorage.GetWeekBounds(WeekDatePicker.SelectedDate.Value).Monday;
            if (newMonday != _currentMonday)
            {
                _currentMonday = newMonday;
                LoadWeekData();
            }
        }
    }

    private void Template40Hours_Click(object sender, RoutedEventArgs e)
    {
        ApplyWeeklyTemplate(8.0, 5, "Mon-Fri 8h/day (40h total)");
    }

    private void Template25Hours_Click(object sender, RoutedEventArgs e)
    {
        ApplyWeeklyTemplate(5.0, 5, "Mon-Fri 5h/day (25h total)");
    }

    private void ApplyWeeklyTemplate(double hoursPerDay, int dayCount, string templateName)
    {
        var project = PlanProjectComboBox.SelectedItem?.ToString() ?? "General Freelance";
        var activity = PlanActivityComboBox.SelectedItem?.ToString() ?? "Development";

        var result = MessageBox.Show(
            $"Apply '{templateName}' to project '{project}' for week {_currentMonday:yyyy-MM-dd} to {_currentMonday.AddDays(dayCount - 1):yyyy-MM-dd}?",
            "Apply Weekly Template",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        for (int i = 0; i < dayCount; i++)
        {
            var date = _currentMonday.AddDays(i);
            var planItem = new PlannedWorkItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Date = date,
                ProjectName = project,
                Activity = activity,
                PlannedHours = hoursPerDay,
                Note = $"{templateName} goal"
            };
            CalendarPlanStorage.Instance.AddOrUpdatePlan(planItem);
        }

        LoadWeekData();
    }

    private void CopyPrevWeekButton_Click(object sender, RoutedEventArgs e)
    {
        var prevMonday = _currentMonday.AddDays(-7);
        var prevSunday = prevMonday.AddDays(6);
        var prevPlans = CalendarPlanStorage.Instance.GetPlansForRange(prevMonday, prevSunday);

        if (prevPlans.Count == 0)
        {
            MessageBox.Show("No planned sessions found in the previous week to copy.", "Nothing to Copy", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Copy {prevPlans.Count} planned sessions from previous week ({prevMonday:dd MMM} - {prevSunday:dd MMM}) to this week?",
            "Copy Previous Week Plans",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        foreach (var p in prevPlans)
        {
            int dayOffset = (int)(p.Date.Date - prevMonday.Date).TotalDays;
            var newDate = _currentMonday.AddDays(dayOffset);

            var copied = new PlannedWorkItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Date = newDate,
                ProjectName = p.ProjectName,
                Activity = p.Activity,
                PlannedHours = p.PlannedHours,
                Note = p.Note
            };
            CalendarPlanStorage.Instance.AddOrUpdatePlan(copied);
        }

        LoadWeekData();
    }

    private void ClearWeekButton_Click(object sender, RoutedEventArgs e)
    {
        var sunday = _currentMonday.AddDays(6);
        var weekPlans = CalendarPlanStorage.Instance.GetPlansForRange(_currentMonday, sunday);

        if (weekPlans.Count == 0)
        {
            MessageBox.Show("No planned sessions exist in this week to clear.", "Week is Empty", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to delete all {weekPlans.Count} planned sessions for this week ({_currentMonday:dd MMM} - {sunday:dd MMM})?",
            "Confirm Clear Week",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        foreach (var p in weekPlans)
        {
            CalendarPlanStorage.Instance.DeletePlan(p.Id);
        }

        ResetEditor();
        LoadWeekData();
    }

    private void PlannerTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Keep tables updated
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
