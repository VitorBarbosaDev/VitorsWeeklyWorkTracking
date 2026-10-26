using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

/// <summary>
/// Interaction logic for BackupManagementWindow.xaml
/// </summary>
public partial class BackupManagementWindow : Window
{
    private readonly BackupService _backupService;
    private List<BackupInfo> _allBackups = new();

    /// <summary>
    /// Indicates whether a restore operation was performed during this dialog session.
    /// </summary>
    public bool WasRestored { get; private set; }

    public BackupManagementWindow(BackupService? backupService = null)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        _backupService = backupService ?? BackupService.Instance;
        Loaded += BackupManagementWindow_Loaded;
    }

    private void BackupManagementWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshBackupsList();
    }

    public void RefreshBackupsList(string? selectId = null)
    {
        try
        {
            _backupService.PruneOldBackups();
            _allBackups = _backupService.GetAvailableBackups();
            ApplyFilter(selectId);
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "BackupManagementWindow.RefreshBackupsList");
        }
    }

    private void ApplyFilter(string? selectId = null)
    {
        var filter = SearchFilterTextBox?.Text?.Trim() ?? string.Empty;
        var filtered = _allBackups;

        if (!string.IsNullOrEmpty(filter))
        {
            filtered = _allBackups.Where(b =>
                b.Reason.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                b.Note.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                b.FormattedCreatedAt.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                b.BadgeText.Contains(filter, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        BackupsListBox.ItemsSource = null;
        BackupsListBox.ItemsSource = filtered;

        long totalSize = _allBackups.Sum(b => b.TotalSizeBytes);
        string formattedTotalSize = totalSize < 1024 * 1024
            ? $"{totalSize / 1024.0:F1} KB"
            : $"{totalSize / (1024.0 * 1024.0):F2} MB";

        HeaderStatsTextBlock.Text = $"{_allBackups.Count} backups available ({formattedTotalSize})";
        EmptyStateBorder.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (!string.IsNullOrEmpty(selectId))
        {
            var target = filtered.FirstOrDefault(b => b.Id == selectId);
            if (target != null)
            {
                BackupsListBox.SelectedItem = target;
                BackupsListBox.ScrollIntoView(target);
                return;
            }
        }

        if (BackupsListBox.SelectedItem == null && filtered.Count > 0)
        {
            BackupsListBox.SelectedIndex = 0;
        }
        else if (filtered.Count == 0)
        {
            UpdateDetailsPane(null);
        }
    }

    private BackupInfo? GetSelectedBackup()
    {
        return BackupsListBox.SelectedItem as BackupInfo;
    }

    private void BackupsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = GetSelectedBackup();
        UpdateDetailsPane(selected);
    }

    private void BackupsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var selected = GetSelectedBackup();
        if (selected != null)
        {
            RestoreBackup(selected);
        }
    }

    private void UpdateDetailsPane(BackupInfo? backup)
    {
        bool hasSelection = backup != null;

        RestoreSelectedButton.IsEnabled = hasSelection;
        ExportBackupButton.IsEnabled = hasSelection;
        DeleteBackupButton.IsEnabled = hasSelection;
        DetailRestoreButton.IsEnabled = hasSelection;
        DetailExportButton.IsEnabled = hasSelection;

        if (backup == null)
        {
            DetailTitleTextBlock.Text = "No backup selected";
            DetailDateTextBlock.Text = string.Empty;
            DetailReasonTextBlock.Text = string.Empty;
            DetailSizeTextBlock.Text = string.Empty;
            DetailEntriesText.Text = "• Time Entries: 0";
            DetailProjectsText.Text = "• Projects: 0";
            DetailActivitiesText.Text = "• Activities: 0";
            DetailPlansText.Text = "• Calendar Plans: 0";
            DetailFilesTextBlock.Text = "None";
            return;
        }

        DetailTitleTextBlock.Text = backup.DisplayTitle;
        DetailDateTextBlock.Text = $"Created: {backup.FormattedCreatedAt}";
        DetailReasonTextBlock.Text = $"Trigger: {backup.Reason}";
        DetailSizeTextBlock.Text = $"Size: {backup.FormattedSize}";
        DetailEntriesText.Text = $"• Time Entries: {backup.TimeEntryCount}";
        DetailProjectsText.Text = $"• Projects: {backup.ProjectCount}";
        DetailActivitiesText.Text = $"• Activities: {backup.ActivityCount}";
        DetailPlansText.Text = $"• Calendar Plans: {backup.CalendarPlanCount}";

        if (backup.IncludedFiles.Count > 0)
        {
            DetailFilesTextBlock.Text = string.Join("\n", backup.IncludedFiles.Select(f => $"📄 {f}"));
        }
        else
        {
            DetailFilesTextBlock.Text = "No files recorded";
        }
    }

    private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        bool confirmed = ThemedMessageBox.Confirm(
            this,
            "Are you sure you want to create a new manual backup snapshot now?",
            "Confirm Backup",
            "Backup Now",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        string? note = ThemedMessageBox.Prompt(
            this,
            "Enter an optional label or note for this manual backup snapshot (leave blank for none):",
            "Create Manual Backup",
            "",
            validator: _ => null);

        if (note == null)
        {
            // User cancelled
            return;
        }

        var created = _backupService.CreateBackup(
            reason: "Manual Backup",
            note: note.Trim(),
            isManual: true,
            pruneAfter: false);

        if (created != null)
        {
            RefreshBackupsList(created.Id);
            ThemedMessageBox.ShowSuccess(
                this,
                $"Backup created successfully!\n\nSnapshot contains {created.TimeEntryCount} entries, {created.ProjectCount} projects, {created.ActivityCount} activities, and {created.CalendarPlanCount} plans.",
                "Backup Created");
        }
        else
        {
            ThemedMessageBox.ShowWarning(
                this,
                "No active data was found to back up.",
                "Backup Incomplete");
        }
    }

    private void RestoreSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedBackup();
        if (selected == null)
        {
            ThemedMessageBox.ShowInfo(this, "Please select a backup from the list to restore.", "Restore Backup");
            return;
        }

        RestoreBackup(selected);
    }

    private void RestoreBackup(BackupInfo backup)
    {
        string promptMessage =
            $"Are you sure you want to restore the backup from {backup.FormattedCreatedAt}?\n\n" +
            $"This snapshot contains:\n" +
            $"• {backup.TimeEntryCount} Time Entries\n" +
            $"• {backup.ProjectCount} Projects\n" +
            $"• {backup.ActivityCount} Activities\n" +
            $"• {backup.CalendarPlanCount} Calendar Plans\n\n" +
            $"Restoring will replace your current active data with this snapshot.\n" +
            $"A safety backup of your current active data will be saved automatically before restoring.";

        bool confirmed = ThemedMessageBox.Confirm(
            this,
            promptMessage,
            "Confirm Data Restoration",
            "Restore Backup",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        var result = _backupService.RestoreBackup(backup, createSafetyBackup: true);

        if (result.Success)
        {
            WasRestored = true;
            RefreshBackupsList();

            ThemedMessageBox.ShowSuccess(
                this,
                $"Backup restored successfully!\n\n" +
                $"Data was restored from snapshot ({backup.FormattedCreatedAt}).\n" +
                $"A safety backup of your previous live data was saved in case you need to undo.",
                "Restore Completed");
        }
        else
        {
            ThemedMessageBox.ShowError(
                this,
                $"Failed to restore backup:\n\n{result.Message}",
                "Restore Failed");
        }
    }

    private void ExportBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedBackup();
        if (selected == null)
        {
            ThemedMessageBox.ShowInfo(this, "Please select a backup to export.", "Export Backup");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export Backup Snapshot to Zip Archive",
            Filter = "Zip Archive (*.zip)|*.zip",
            FileName = $"WorkTracker_Backup_{selected.CreatedAt:yyyyMMdd_HHmmss}.zip"
        };

        if (dialog.ShowDialog(this) == true)
        {
            try
            {
                _backupService.ExportBackupToZip(selected, dialog.FileName);
                ThemedMessageBox.ShowSuccess(
                    this,
                    $"Backup successfully exported to:\n{dialog.FileName}",
                    "Export Succeeded");
            }
            catch (Exception ex)
            {
                ThemedMessageBox.ShowError(
                    this,
                    $"Failed to export backup: {ex.Message}",
                    "Export Failed");
            }
        }
    }

    private void ImportBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Backup Zip Archive to Import",
            Filter = "Zip Archive (*.zip)|*.zip|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            try
            {
                bool restoreImmediately = ThemedMessageBox.Confirm(
                    this,
                    "Would you like to restore the data immediately to replace your active tracker data?\n\nChoose 'Restore Now' to restore active files, or 'Just Import' to add it to your backup history.",
                    "Import Backup",
                    confirmButtonText: "Restore Now",
                    cancelButtonText: "Just Import");

                var imported = _backupService.ImportBackupFromZip(dialog.FileName, restoreImmediately: restoreImmediately);

                if (restoreImmediately)
                {
                    WasRestored = true;
                }

                RefreshBackupsList(imported.Id);

                ThemedMessageBox.ShowSuccess(
                    this,
                    restoreImmediately
                        ? $"Backup imported and restored successfully!\n({imported.SummaryText})"
                        : $"Backup successfully imported to your history!\n({imported.SummaryText})",
                    "Import Succeeded");
            }
            catch (Exception ex)
            {
                ThemedMessageBox.ShowError(
                    this,
                    $"Failed to import backup: {ex.Message}",
                    "Import Failed");
            }
        }
    }

    private void DeleteBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedBackup();
        if (selected == null)
        {
            ThemedMessageBox.ShowInfo(this, "Please select a backup to delete.", "Delete Backup");
            return;
        }

        bool confirmed = ThemedMessageBox.ConfirmDelete(
            this,
            $"Are you sure you want to delete this backup snapshot from {selected.FormattedCreatedAt} ({selected.DisplayTitle})?",
            "Delete Backup Snapshot",
            "Delete Backup",
            "Cancel");

        if (confirmed)
        {
            _backupService.DeleteBackup(selected);
            RefreshBackupsList();
        }
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = StoragePathHelper.GetBackupFolderPath();
            if (Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            ThemedMessageBox.ShowWarning(this, $"Could not open backups folder: {ex.Message}", "Open Folder");
        }
    }

    private void SearchFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshBackupsList();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
