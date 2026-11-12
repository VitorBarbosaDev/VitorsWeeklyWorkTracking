using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class TutorialDialog : Window
{
    private const int TotalPages = 5;
    private int _currentPageIndex = 0;
    private TutorialScreen _screenMode = TutorialScreen.General;

    private readonly string[] _pageSubtitles =
    {
        "Discover everything CosyQueue has to offer for your daily productivity.",
        "Master focus sprints, timers, custom countdowns, and floating widgets.",
        "Level up animated companion scenes, earn XP rewards, and customize sounds.",
        "Schedule weekly workloads, duplicate plans, and track milestone deadlines.",
        "Build daily habit streaks, analyze charts, and keep your snapshots safe."
    };

    private StackPanel[] _pages = Array.Empty<StackPanel>();
    private Button[] _dots = Array.Empty<Button>();

    public int CurrentPageIndex => _currentPageIndex;
    public TutorialScreen ScreenMode => _screenMode;

    public TutorialDialog() : this(TutorialScreen.General)
    {
    }

    public TutorialDialog(TutorialScreen screen)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        _pages = new[] { Page0, Page1, Page2, Page3, Page4 };
        _dots = new[] { Dot0, Dot1, Dot2, Dot3, Dot4 };

        _screenMode = screen;

        KeyDown += TutorialDialog_KeyDown;
        UpdateDisplay();
    }

    public void SetScreen(TutorialScreen screen)
    {
        _screenMode = screen;
        UpdateDisplay();
    }

    public void SwitchToFullTour(int startPage = 0)
    {
        _screenMode = TutorialScreen.General;
        _currentPageIndex = Math.Clamp(startPage, 0, TotalPages - 1);
        UpdateDisplay();
    }

    private void TutorialDialog_KeyDown(object sender, KeyEventArgs e)
    {
        if (_screenMode != TutorialScreen.General)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                FinishTour();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                SkipTour();
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.Right || (e.Key == Key.Enter && _currentPageIndex < TotalPages - 1))
        {
            NextPage();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && _currentPageIndex == TotalPages - 1)
        {
            FinishTour();
            e.Handled = true;
        }
        else if (e.Key == Key.Left)
        {
            PreviousPage();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SkipTour();
            e.Handled = true;
        }
    }

    public void GoToPage(int index)
    {
        if (index < 0 || index >= TotalPages)
            return;

        _screenMode = TutorialScreen.General;
        _currentPageIndex = index;
        UpdateDisplay();
    }

    public void NextPage()
    {
        if (_screenMode != TutorialScreen.General)
            return;

        if (_currentPageIndex < TotalPages - 1)
        {
            _currentPageIndex++;
            UpdateDisplay();
        }
    }

    public void PreviousPage()
    {
        if (_screenMode != TutorialScreen.General)
            return;

        if (_currentPageIndex > 0)
        {
            _currentPageIndex--;
            UpdateDisplay();
        }
    }

    private void HideAllPages()
    {
        if (_pages != null)
        {
            foreach (var p in _pages)
            {
                if (p != null) p.Visibility = Visibility.Collapsed;
            }
        }
        if (PageMainWindow != null) PageMainWindow.Visibility = Visibility.Collapsed;
        if (PagePlanner != null) PagePlanner.Visibility = Visibility.Collapsed;
        if (PageAnalytics != null) PageAnalytics.Visibility = Visibility.Collapsed;
        if (PageStreak != null) PageStreak.Visibility = Visibility.Collapsed;
    }

    private void UpdateDisplay()
    {
        HideAllPages();

        if (_screenMode == TutorialScreen.General)
        {
            Title = "Welcome to CosyQueue - Feature Guide";
            if (HeaderIconTextBlock != null) HeaderIconTextBlock.Text = "✨";
            if (HeaderTitleTextBlock != null) HeaderTitleTextBlock.Text = "CosyQueue Feature Tour";
            if (StepSubtitleTextBlock != null && _currentPageIndex >= 0 && _currentPageIndex < _pageSubtitles.Length)
            {
                StepSubtitleTextBlock.Text = _pageSubtitles[_currentPageIndex];
            }
            if (StepIndicatorTextBlock != null)
            {
                StepIndicatorTextBlock.Text = $"Step {_currentPageIndex + 1} of {TotalPages}";
            }

            if (_pages != null && _currentPageIndex >= 0 && _currentPageIndex < _pages.Length && _pages[_currentPageIndex] != null)
            {
                _pages[_currentPageIndex].Visibility = Visibility.Visible;
            }

            // Dots & Navigation
            if (DotsPanel != null) DotsPanel.Visibility = Visibility.Visible;
            if (ViewFullTourButton != null) ViewFullTourButton.Visibility = Visibility.Collapsed;

            for (int i = 0; i < _dots.Length; i++)
            {
                if (_dots[i] != null)
                {
                    bool isActive = (i == _currentPageIndex);
                    _dots[i].Content = isActive ? "●" : "○";
                    _dots[i].FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal;
                    _dots[i].SetResourceReference(TextBlock.ForegroundProperty, isActive ? "Theme.Primary" : "Theme.ForegroundMuted");
                }
            }

            if (PreviousButton != null)
            {
                PreviousButton.Visibility = Visibility.Visible;
                PreviousButton.IsEnabled = _currentPageIndex > 0;
                PreviousButton.Opacity = _currentPageIndex > 0 ? 1.0 : 0.45;
            }

            if (NextButton != null)
            {
                NextButton.Visibility = _currentPageIndex < TotalPages - 1 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (FinishButton != null)
            {
                FinishButton.Visibility = _currentPageIndex == TotalPages - 1 ? Visibility.Visible : Visibility.Collapsed;
                FinishButton.Content = "🚀 Get Started";
            }
        }
        else
        {
            if (DotsPanel != null) DotsPanel.Visibility = Visibility.Collapsed;
            if (ViewFullTourButton != null) ViewFullTourButton.Visibility = Visibility.Visible;
            if (PreviousButton != null) PreviousButton.Visibility = Visibility.Collapsed;
            if (NextButton != null) NextButton.Visibility = Visibility.Collapsed;
            if (FinishButton != null)
            {
                FinishButton.Visibility = Visibility.Visible;
                FinishButton.Content = "✓ Got It!";
            }

            switch (_screenMode)
            {
                case TutorialScreen.MainWindow:
                    Title = "CosyQueue - Main Dashboard Guide";
                    if (HeaderIconTextBlock != null) HeaderIconTextBlock.Text = "⏱️";
                    if (HeaderTitleTextBlock != null) HeaderTitleTextBlock.Text = "Main Dashboard & Workspace Guide";
                    if (StepSubtitleTextBlock != null) StepSubtitleTextBlock.Text = "Track focus sessions, monitor live stats, customize companion art,\nand access all tools.";
                    if (StepIndicatorTextBlock != null) StepIndicatorTextBlock.Text = "Dashboard Guide";
                    if (PageMainWindow != null) PageMainWindow.Visibility = Visibility.Visible;
                    break;

                case TutorialScreen.Planner:
                    Title = "CosyQueue - Work Planner Guide";
                    if (HeaderIconTextBlock != null) HeaderIconTextBlock.Text = "📅";
                    if (HeaderTitleTextBlock != null) HeaderTitleTextBlock.Text = "Work Planner & Goal Calendar Guide";
                    if (StepSubtitleTextBlock != null) StepSubtitleTextBlock.Text = "Schedule project hours across the week, set milestone deadlines,\nand track weekly goals.";
                    if (StepIndicatorTextBlock != null) StepIndicatorTextBlock.Text = "Planner Guide";
                    if (PagePlanner != null) PagePlanner.Visibility = Visibility.Visible;
                    break;

                case TutorialScreen.Analytics:
                    Title = "CosyQueue - Work Analytics Guide";
                    if (HeaderIconTextBlock != null) HeaderIconTextBlock.Text = "📊";
                    if (HeaderTitleTextBlock != null) HeaderTitleTextBlock.Text = "Work Analytics & Reports Guide";
                    if (StepSubtitleTextBlock != null) StepSubtitleTextBlock.Text = "Gain deep insights into your focus patterns, project breakdowns,\nand historical trends.";
                    if (StepIndicatorTextBlock != null) StepIndicatorTextBlock.Text = "Analytics Guide";
                    if (PageAnalytics != null) PageAnalytics.Visibility = Visibility.Visible;
                    break;

                case TutorialScreen.Streak:
                    Title = "CosyQueue - Streak System Guide";
                    if (HeaderIconTextBlock != null) HeaderIconTextBlock.Text = "🔥";
                    if (HeaderTitleTextBlock != null) HeaderTitleTextBlock.Text = "Streak System & Tier Rewards Guide";
                    if (StepSubtitleTextBlock != null) StepSubtitleTextBlock.Text = "Build daily focus momentum, unlock achievement tier badges,\nand protect your streak.";
                    if (StepIndicatorTextBlock != null) StepIndicatorTextBlock.Text = "Streak Guide";
                    if (PageStreak != null) PageStreak.Visibility = Visibility.Visible;
                    break;
            }
        }
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        PreviousPage();
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        NextPage();
    }

    private void FinishButton_Click(object sender, RoutedEventArgs e)
    {
        FinishTour();
    }

    private void SkipTourButton_Click(object sender, RoutedEventArgs e)
    {
        SkipTour();
    }

    private void ViewFullTourButton_Click(object sender, RoutedEventArgs e)
    {
        int initialPage = _screenMode switch
        {
            TutorialScreen.Planner => 3,
            TutorialScreen.Analytics => 4,
            TutorialScreen.Streak => 4,
            _ => 0
        };
        SwitchToFullTour(initialPage);
    }

    private void DotButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int targetPage))
        {
            GoToPage(targetPage);
        }
    }

    private void FinishTour()
    {
        DialogResult = true;
        Close();
    }

    private void SkipTour()
    {
        DialogResult = false;
        Close();
    }
}
