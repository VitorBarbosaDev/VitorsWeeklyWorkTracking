using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class CopyToOtherDaysDialog : Window
{
    private readonly PlannedWorkItem _sourcePlan;
    public int SelectedDays { get; private set; } = 1;

    public CopyToOtherDaysDialog(PlannedWorkItem plan, int initialDays = 1)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        _sourcePlan = plan ?? throw new ArgumentNullException(nameof(plan));
        SelectedDays = initialDays > 0 ? initialDays : 1;

        PopulateSourceTask();
        DaysTextBox.Text = SelectedDays.ToString();
        UpdatePreview();

        Loaded += (s, e) =>
        {
            DaysTextBox.Focus();
            DaysTextBox.SelectAll();
        };
    }

    private void PopulateSourceTask()
    {
        ProjectNameText.Text = string.IsNullOrWhiteSpace(_sourcePlan.ProjectName) ? "General" : _sourcePlan.ProjectName;
        PlannedHoursText.Text = $"{_sourcePlan.PlannedHours:0.0} hrs/day";
        SourceDateText.Text = $"📅 {_sourcePlan.Date:dddd, dd MMM yyyy}";

        if (!string.IsNullOrWhiteSpace(_sourcePlan.Activity))
        {
            ActivityText.Text = $"🏷️ {_sourcePlan.Activity}";
            ActivityText.Visibility = Visibility.Visible;
        }
        else
        {
            ActivityText.Visibility = Visibility.Collapsed;
        }

        if (!string.IsNullOrWhiteSpace(_sourcePlan.Note))
        {
            NoteText.Text = $"📝 {_sourcePlan.Note}";
            NoteText.Visibility = Visibility.Visible;
        }
        else
        {
            NoteText.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdatePreview()
    {
        if (int.TryParse(DaysTextBox.Text.Trim(), out int days) && days > 0)
        {
            SelectedDays = days;
            CopySubmitButton.IsEnabled = true;
            CopySubmitButton.Content = days == 1 ? "📋 Copy Task (1 Day)" : $"📋 Copy Task ({days} Days)";

            var sb = new StringBuilder();
            sb.AppendLine($"Task will be copied to the next {days} day{(days > 1 ? "s" : "")}:");

            if (days <= 7)
            {
                for (int i = 1; i <= days; i++)
                {
                    var targetDate = _sourcePlan.Date.Date.AddDays(i);
                    sb.AppendLine($"  • {targetDate:ddd, dd MMM yyyy}");
                }
            }
            else
            {
                var firstTarget = _sourcePlan.Date.Date.AddDays(1);
                var lastTarget = _sourcePlan.Date.Date.AddDays(days);
                sb.AppendLine($"  • From: {firstTarget:ddd, dd MMM yyyy}");
                sb.AppendLine($"  • To:     {lastTarget:ddd, dd MMM yyyy} ({days} consecutive days)");
            }

            TargetDatesText.Text = sb.ToString().TrimEnd();
            double totalHours = _sourcePlan.PlannedHours * days;
            SummaryTotalText.Text = $"Total planned hours added: {totalHours:0.0} hrs ({days} × {_sourcePlan.PlannedHours:0.0}h)";
        }
        else
        {
            CopySubmitButton.IsEnabled = false;
            TargetDatesText.Text = "Please enter a valid positive number of days (at least 1).";
            SummaryTotalText.Text = "Total planned hours added: 0.0 hrs";
        }
    }

    private void IncrementDaysButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(DaysTextBox.Text.Trim(), out int days))
        {
            if (days < 365)
            {
                DaysTextBox.Text = (days + 1).ToString();
            }
        }
        else
        {
            DaysTextBox.Text = "1";
        }
    }

    private void DecrementDaysButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(DaysTextBox.Text.Trim(), out int days))
        {
            if (days > 1)
            {
                DaysTextBox.Text = (days - 1).ToString();
            }
        }
        else
        {
            DaysTextBox.Text = "1";
        }
    }

    private void QuickPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int days))
        {
            DaysTextBox.Text = days.ToString();
            DaysTextBox.Focus();
            DaysTextBox.SelectAll();
        }
    }

    private void DaysTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TargetDatesText != null && SummaryTotalText != null && CopySubmitButton != null)
        {
            UpdatePreview();
        }
    }

    private void DaysTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    private void DaysTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CopyButton_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            CancelButton_Click(sender, e);
        }
        else if (e.Key == Key.Up)
        {
            IncrementDaysButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            DecrementDaysButton_Click(sender, e);
            e.Handled = true;
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(DaysTextBox.Text.Trim(), out int days) && days > 0)
        {
            if (days > 365)
            {
                ThemedMessageBox.ShowWarning(this, "Please enter a reasonable number of days (maximum 365).", "Number of Days Too Large");
                DaysTextBox.Focus();
                DaysTextBox.SelectAll();
                return;
            }

            SelectedDays = days;
            DialogResult = true;
            Close();
        }
        else
        {
            ThemedMessageBox.ShowWarning(this, "Please enter a valid positive number of days to extend.", "Invalid Days");
            DaysTextBox.Focus();
            DaysTextBox.SelectAll();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
