using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VitorsWeeklyWorkTracking;

public partial class CustomGoalDialog : Window
{
    public int SelectedMinutes { get; private set; } = 30;

    public CustomGoalDialog(int initialMinutes = 30)
    {
        InitializeComponent();
        SelectedMinutes = initialMinutes > 0 ? initialMinutes : 30;
        MinutesTextBox.Text = SelectedMinutes.ToString();
        Loaded += (s, e) =>
        {
            MinutesTextBox.Focus();
            MinutesTextBox.SelectAll();
        };
    }

    private void SetGoalButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(MinutesTextBox.Text.Trim(), out int minutes) && minutes > 0)
        {
            SelectedMinutes = minutes;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Please enter a valid positive number of minutes.", "Invalid Goal", MessageBoxButton.OK, MessageBoxImage.Warning);
            MinutesTextBox.Focus();
            MinutesTextBox.SelectAll();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void QuickPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int mins))
        {
            MinutesTextBox.Text = mins.ToString();
            SelectedMinutes = mins;
            DialogResult = true;
            Close();
        }
    }

    private void MinutesTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow digits
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    private void MinutesTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SetGoalButton_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            CancelButton_Click(sender, e);
        }
    }
}
