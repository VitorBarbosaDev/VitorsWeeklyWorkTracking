using System;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public enum ThemedDialogType
{
    Information,
    Success,
    Warning,
    Question,
    Danger,
    Exit
}

/// <summary>
/// A modern, fully theme-aware dialog replacement for the native Win32 MessageBox.
/// Supports styled title bars, theme colors, icons, badges, custom action buttons, and keyboard navigation.
/// </summary>
public partial class ThemedMessageBox : Window
{
    private MessageBoxButton _buttons = MessageBoxButton.OK;
    private MessageBoxResult _defaultResult = MessageBoxResult.None;

    public Border IconBadgeBorder { get; private set; } = null!;
    public TextBlock IconBadgeText { get; private set; } = null!;
    public TextBlock DialogTitleText { get; private set; } = null!;
    public TextBlock DialogSubtitleText { get; private set; } = null!;
    public TextBlock DialogMessageText { get; private set; } = null!;
    public Border DetailCardBorder { get; private set; } = null!;
    public TextBlock DetailCardText { get; private set; } = null!;
    public Button SecondaryCancelButton { get; private set; } = null!;
    public Button NegativeButton { get; private set; } = null!;
    public Button PositiveButton { get; private set; } = null!;

    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    public ThemedMessageBox()
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);
        KeyDown += ThemedMessageBox_KeyDown;
    }

    public void InitializeComponent()
    {
        Title = "Confirmation";
        Width = 460;
        MinWidth = 380;
        MaxWidth = 560;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;

        SetResourceReference(BackgroundProperty, "Theme.WindowBackground");
        SetResourceReference(ForegroundProperty, "Theme.Foreground");

        try
        {
            var iconUri = new Uri("pack://application:,,,/VitorWeeklyWorkTracking.ico", UriKind.RelativeOrAbsolute);
            Icon = BitmapFrame.Create(iconUri);
        }
        catch
        {
            // Ignore if icon cannot be loaded in test context
        }

        var outerBorder = new Border { Padding = new Thickness(16) };

        var cardBorder = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18, 16, 18, 16)
        };
        cardBorder.SetResourceReference(Border.BackgroundProperty, "Theme.CardBackground");
        cardBorder.SetResourceReference(Border.BorderBrushProperty, "Theme.CardBorder");

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Row 0: Header Area
        var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        IconBadgeBorder = new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(26, 37, 99, 235)),
            BorderThickness = new Thickness(1.5),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };
        IconBadgeBorder.SetResourceReference(Border.BorderBrushProperty, "Theme.Primary");

        IconBadgeText = new TextBlock
        {
            Text = "💬",
            FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        IconBadgeBorder.Child = IconBadgeText;
        Grid.SetColumn(IconBadgeBorder, 0);
        headerGrid.Children.Add(IconBadgeBorder);

        var titlePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        DialogTitleText = new TextBlock
        {
            Text = "Confirmation",
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        };
        DialogTitleText.SetResourceReference(TextBlock.ForegroundProperty, "Theme.Foreground");
        titlePanel.Children.Add(DialogTitleText);

        DialogSubtitleText = new TextBlock
        {
            Text = "",
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0),
            Visibility = Visibility.Collapsed,
            TextWrapping = TextWrapping.Wrap
        };
        DialogSubtitleText.SetResourceReference(TextBlock.ForegroundProperty, "Theme.ForegroundMuted");
        titlePanel.Children.Add(DialogSubtitleText);

        Grid.SetColumn(titlePanel, 1);
        headerGrid.Children.Add(titlePanel);

        Grid.SetRow(headerGrid, 0);
        mainGrid.Children.Add(headerGrid);

        // Row 1: Message Body
        var bodyPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };

        DialogMessageText = new TextBlock
        {
            Text = "Are you sure you want to proceed?",
            FontSize = 12.5,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 18
        };
        DialogMessageText.SetResourceReference(TextBlock.ForegroundProperty, "Theme.Foreground");
        bodyPanel.Children.Add(DialogMessageText);

        DetailCardBorder = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 10, 0, 0),
            Visibility = Visibility.Collapsed
        };
        DetailCardBorder.SetResourceReference(Border.BackgroundProperty, "Theme.CardBackgroundAlt");
        DetailCardBorder.SetResourceReference(Border.BorderBrushProperty, "Theme.CardBorder");

        DetailCardText = new TextBlock
        {
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap
        };
        DetailCardText.SetResourceReference(TextBlock.ForegroundProperty, "Theme.ForegroundMuted");
        DetailCardBorder.Child = DetailCardText;
        bodyPanel.Children.Add(DetailCardBorder);

        Grid.SetRow(bodyPanel, 1);
        mainGrid.Children.Add(bodyPanel);

        // Row 2: Action Buttons
        var actionsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };

        SecondaryCancelButton = new Button
        {
            Content = "Cancel",
            MinWidth = 84,
            Height = 32,
            Padding = new Thickness(12, 4, 12, 4),
            Margin = new Thickness(0, 0, 8, 0),
            Visibility = Visibility.Collapsed
        };
        SecondaryCancelButton.Click += SecondaryCancelButton_Click;
        actionsPanel.Children.Add(SecondaryCancelButton);

        NegativeButton = new Button
        {
            Content = "No",
            MinWidth = 84,
            Height = 32,
            Padding = new Thickness(12, 4, 12, 4),
            Margin = new Thickness(0, 0, 8, 0),
            Visibility = Visibility.Collapsed
        };
        NegativeButton.Click += NegativeButton_Click;
        actionsPanel.Children.Add(NegativeButton);

        PositiveButton = new Button
        {
            Content = "Yes",
            MinWidth = 84,
            Height = 32,
            Padding = new Thickness(12, 4, 12, 4)
        };
        PositiveButton.SetResourceReference(StyleProperty, "PrimaryButton");
        PositiveButton.Click += PositiveButton_Click;
        actionsPanel.Children.Add(PositiveButton);

        Grid.SetRow(actionsPanel, 2);
        mainGrid.Children.Add(actionsPanel);

        cardBorder.Child = mainGrid;
        outerBorder.Child = cardBorder;
        Content = outerBorder;
    }

    private void ThemedMessageBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            if (SecondaryCancelButton.Visibility == Visibility.Visible)
            {
                SecondaryCancelButton_Click(this, new RoutedEventArgs());
            }
            else if (NegativeButton.Visibility == Visibility.Visible)
            {
                NegativeButton_Click(this, new RoutedEventArgs());
            }
            else
            {
                PositiveButton_Click(this, new RoutedEventArgs());
            }
        }
    }

    private void Configure(
        string message,
        string title,
        string? subtitle,
        ThemedDialogType dialogType,
        MessageBoxButton buttons,
        MessageBoxResult defaultResult,
        string? positiveButtonText,
        string? negativeButtonText,
        string? cancelButtonText,
        string? detailText)
    {
        _buttons = buttons;
        _defaultResult = defaultResult;
        Title = string.IsNullOrWhiteSpace(title) ? GetDefaultTitle(dialogType) : title;
        DialogTitleText.Text = Title;

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            DialogSubtitleText.Text = subtitle;
            DialogSubtitleText.Visibility = Visibility.Visible;
        }
        else
        {
            DialogSubtitleText.Visibility = Visibility.Collapsed;
        }

        DialogMessageText.Text = message;

        if (!string.IsNullOrWhiteSpace(detailText))
        {
            DetailCardText.Text = detailText;
            DetailCardBorder.Visibility = Visibility.Visible;
        }
        else
        {
            DetailCardBorder.Visibility = Visibility.Collapsed;
        }

        ApplyDialogTypeVisuals(dialogType);
        ConfigureButtons(buttons, defaultResult, positiveButtonText, negativeButtonText, cancelButtonText, dialogType);
    }

    private string GetDefaultTitle(ThemedDialogType dialogType) => dialogType switch
    {
        ThemedDialogType.Danger => "Confirm Delete",
        ThemedDialogType.Exit => "Confirm Exit",
        ThemedDialogType.Warning => "Warning",
        ThemedDialogType.Success => "Success",
        ThemedDialogType.Question => "Confirmation",
        ThemedDialogType.Information => "Information",
        _ => "Attention"
    };

    private void ApplyDialogTypeVisuals(ThemedDialogType dialogType)
    {
        switch (dialogType)
        {
            case ThemedDialogType.Danger:
                IconBadgeText.Text = "🗑️";
                IconBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(38, 220, 38, 38)); // ~15% Red
                IconBadgeBorder.SetResourceReference(Border.BorderBrushProperty, "Theme.Danger");
                PositiveButton.SetResourceReference(StyleProperty, "DangerButton");
                break;

            case ThemedDialogType.Exit:
                IconBadgeText.Text = "🚪";
                IconBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(38, 217, 119, 6)); // ~15% Amber
                IconBadgeBorder.SetResourceReference(Border.BorderBrushProperty, "Theme.Warning");
                PositiveButton.SetResourceReference(StyleProperty, "DangerButton");
                break;

            case ThemedDialogType.Warning:
                IconBadgeText.Text = "⚠️";
                IconBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(38, 217, 119, 6)); // ~15% Amber
                IconBadgeBorder.SetResourceReference(Border.BorderBrushProperty, "Theme.Warning");
                PositiveButton.SetResourceReference(StyleProperty, "PrimaryButton");
                break;

            case ThemedDialogType.Success:
                IconBadgeText.Text = "✅";
                IconBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(38, 22, 163, 74)); // ~15% Green
                IconBadgeBorder.SetResourceReference(Border.BorderBrushProperty, "Theme.Success");
                PositiveButton.SetResourceReference(StyleProperty, "SuccessButton");
                break;

            case ThemedDialogType.Question:
                IconBadgeText.Text = "❓";
                IconBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(38, 37, 99, 235)); // ~15% Blue
                IconBadgeBorder.SetResourceReference(Border.BorderBrushProperty, "Theme.Primary");
                PositiveButton.SetResourceReference(StyleProperty, "PrimaryButton");
                break;

            case ThemedDialogType.Information:
            default:
                IconBadgeText.Text = "ℹ️";
                IconBadgeBorder.Background = new SolidColorBrush(Color.FromArgb(38, 37, 99, 235)); // ~15% Blue
                IconBadgeBorder.SetResourceReference(Border.BorderBrushProperty, "Theme.Primary");
                PositiveButton.SetResourceReference(StyleProperty, "PrimaryButton");
                break;
        }
    }

    private void ConfigureButtons(
        MessageBoxButton buttons,
        MessageBoxResult defaultResult,
        string? positiveText,
        string? negativeText,
        string? cancelText,
        ThemedDialogType dialogType)
    {
        switch (buttons)
        {
            case MessageBoxButton.OK:
                PositiveButton.Content = positiveText ?? "OK";
                PositiveButton.Visibility = Visibility.Visible;
                PositiveButton.IsDefault = true;
                NegativeButton.Visibility = Visibility.Collapsed;
                SecondaryCancelButton.Visibility = Visibility.Collapsed;
                Loaded += (s, e) => PositiveButton.Focus();
                break;

            case MessageBoxButton.OKCancel:
                PositiveButton.Content = positiveText ?? "OK";
                PositiveButton.Visibility = Visibility.Visible;
                NegativeButton.Content = cancelText ?? negativeText ?? "Cancel";
                NegativeButton.Visibility = Visibility.Visible;
                NegativeButton.IsCancel = true;
                SecondaryCancelButton.Visibility = Visibility.Collapsed;

                if (defaultResult == MessageBoxResult.Cancel)
                {
                    NegativeButton.IsDefault = true;
                    Loaded += (s, e) => NegativeButton.Focus();
                }
                else
                {
                    PositiveButton.IsDefault = true;
                    Loaded += (s, e) => PositiveButton.Focus();
                }
                break;

            case MessageBoxButton.YesNo:
                PositiveButton.Content = positiveText ?? (dialogType == ThemedDialogType.Danger ? "🗑️ Delete" : "Yes");
                PositiveButton.Visibility = Visibility.Visible;
                NegativeButton.Content = negativeText ?? "No";
                NegativeButton.Visibility = Visibility.Visible;
                NegativeButton.IsCancel = true;
                SecondaryCancelButton.Visibility = Visibility.Collapsed;

                if (defaultResult == MessageBoxResult.No)
                {
                    NegativeButton.IsDefault = true;
                    Loaded += (s, e) => NegativeButton.Focus();
                }
                else
                {
                    PositiveButton.IsDefault = true;
                    Loaded += (s, e) => PositiveButton.Focus();
                }
                break;

            case MessageBoxButton.YesNoCancel:
                PositiveButton.Content = positiveText ?? "Yes";
                PositiveButton.Visibility = Visibility.Visible;
                NegativeButton.Content = negativeText ?? "No";
                NegativeButton.Visibility = Visibility.Visible;
                SecondaryCancelButton.Content = cancelText ?? "Cancel";
                SecondaryCancelButton.Visibility = Visibility.Visible;
                SecondaryCancelButton.IsCancel = true;

                if (defaultResult == MessageBoxResult.Cancel)
                {
                    SecondaryCancelButton.IsDefault = true;
                    Loaded += (s, e) => SecondaryCancelButton.Focus();
                }
                else if (defaultResult == MessageBoxResult.No)
                {
                    NegativeButton.IsDefault = true;
                    Loaded += (s, e) => NegativeButton.Focus();
                }
                else
                {
                    PositiveButton.IsDefault = true;
                    Loaded += (s, e) => PositiveButton.Focus();
                }
                break;
        }
    }

    private void PositiveButton_Click(object sender, RoutedEventArgs e)
    {
        Result = _buttons switch
        {
            MessageBoxButton.OK => MessageBoxResult.OK,
            MessageBoxButton.OKCancel => MessageBoxResult.OK,
            MessageBoxButton.YesNo => MessageBoxResult.Yes,
            MessageBoxButton.YesNoCancel => MessageBoxResult.Yes,
            _ => MessageBoxResult.OK
        };

        DialogResult = true;
        Close();
    }

    private void NegativeButton_Click(object sender, RoutedEventArgs e)
    {
        Result = _buttons switch
        {
            MessageBoxButton.OKCancel => MessageBoxResult.Cancel,
            MessageBoxButton.YesNo => MessageBoxResult.No,
            MessageBoxButton.YesNoCancel => MessageBoxResult.No,
            _ => MessageBoxResult.No
        };

        DialogResult = false;
        Close();
    }

    private void SecondaryCancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Cancel;
        DialogResult = false;
        Close();
    }

    #region Static Helper Methods

    /// <summary>
    /// Displays a themed message box with standard MessageBox parameters.
    /// </summary>
    public static MessageBoxResult Show(
        string message,
        string title = "",
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        return Show(null, message, title, buttons, icon, defaultResult);
    }

    /// <summary>
    /// Displays a themed message box with specified owner window.
    /// </summary>
    public static MessageBoxResult Show(
        Window? owner,
        string message,
        string title = "",
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        ThemedDialogType dialogType = MapIconToDialogType(icon, title);

        var dialog = new ThemedMessageBox();
        SetOwner(dialog, owner);
        dialog.Configure(
            message,
            title,
            null,
            dialogType,
            buttons,
            defaultResult,
            null,
            null,
            null,
            null);

        PlaySystemSound(dialogType);
        dialog.ShowDialog();
        return dialog.Result;
    }

    /// <summary>
    /// Displays a themed delete confirmation popup with danger styling.
    /// </summary>
    public static bool ConfirmDelete(
        Window? owner,
        string message,
        string title = "Confirm Delete",
        string deleteButtonText = "Delete",
        string cancelButtonText = "Cancel",
        string? detailText = null,
        string? subtitle = "This action cannot be undone")
    {
        var dialog = new ThemedMessageBox();
        SetOwner(dialog, owner);
        dialog.Configure(
            message,
            title,
            subtitle,
            ThemedDialogType.Danger,
            MessageBoxButton.YesNo,
            MessageBoxResult.No,
            deleteButtonText,
            cancelButtonText,
            null,
            detailText);

        PlaySystemSound(ThemedDialogType.Danger);
        dialog.ShowDialog();
        return dialog.Result == MessageBoxResult.Yes;
    }

    /// <summary>
    /// Displays a themed confirmation popup for closing the application while tracking.
    /// </summary>
    public static bool ConfirmClose(
        Window? owner,
        string message,
        string title = "Exit Application?",
        string exitButtonText = "Exit Anyway",
        string stayButtonText = "Keep Working",
        string? detailText = null,
        string? subtitle = "Active progress may be lost")
    {
        var dialog = new ThemedMessageBox();
        SetOwner(dialog, owner);
        dialog.Configure(
            message,
            title,
            subtitle,
            ThemedDialogType.Exit,
            MessageBoxButton.YesNo,
            MessageBoxResult.No,
            exitButtonText,
            stayButtonText,
            null,
            detailText);

        PlaySystemSound(ThemedDialogType.Exit);
        dialog.ShowDialog();
        return dialog.Result == MessageBoxResult.Yes;
    }

    /// <summary>
    /// Displays a standard yes/no question confirmation dialog.
    /// </summary>
    public static bool Confirm(
        Window? owner,
        string message,
        string title = "Confirm",
        string confirmButtonText = "Yes",
        string cancelButtonText = "No",
        string? detailText = null,
        bool isDanger = false,
        string? subtitle = null,
        ThemedDialogType dialogType = ThemedDialogType.Question)
    {
        if (isDanger)
        {
            dialogType = ThemedDialogType.Danger;
        }

        var dialog = new ThemedMessageBox();
        SetOwner(dialog, owner);
        dialog.Configure(
            message,
            title,
            subtitle,
            dialogType,
            MessageBoxButton.YesNo,
            MessageBoxResult.No,
            confirmButtonText,
            cancelButtonText,
            null,
            detailText);

        PlaySystemSound(dialogType);
        dialog.ShowDialog();
        return dialog.Result == MessageBoxResult.Yes;
    }

    /// <summary>
    /// Displays an informational dialog.
    /// </summary>
    public static void ShowInfo(Window? owner, string message, string title = "Information")
    {
        Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// Displays a success dialog.
    /// </summary>
    public static void ShowSuccess(Window? owner, string message, string title = "Success")
    {
        var dialog = new ThemedMessageBox();
        SetOwner(dialog, owner);
        dialog.Configure(
            message,
            title,
            null,
            ThemedDialogType.Success,
            MessageBoxButton.OK,
            MessageBoxResult.OK,
            "OK",
            null,
            null,
            null);

        PlaySystemSound(ThemedDialogType.Success);
        dialog.ShowDialog();
    }

    /// <summary>
    /// Displays a warning dialog.
    /// </summary>
    public static void ShowWarning(Window? owner, string message, string title = "Warning")
    {
        Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// Displays an error dialog.
    /// </summary>
    public static void ShowError(Window? owner, string message, string title = "Error")
    {
        Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static void SetOwner(Window dialog, Window? owner)
    {
        try
        {
            if (owner == null && Application.Current != null)
            {
                owner = Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w.IsVisible && w != dialog)
                    ?? Application.Current.MainWindow;
            }

            if (owner != null && owner.IsVisible)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }
        catch
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private static ThemedDialogType MapIconToDialogType(MessageBoxImage icon, string title)
    {
        var titleLower = title?.ToLowerInvariant() ?? "";
        if (titleLower.Contains("delete") || titleLower.Contains("remove") || titleLower.Contains("discard"))
        {
            return ThemedDialogType.Danger;
        }
        if (titleLower.Contains("exit") || titleLower.Contains("close"))
        {
            return ThemedDialogType.Exit;
        }

        return icon switch
        {
            MessageBoxImage.Hand => ThemedDialogType.Danger,
            MessageBoxImage.Question => ThemedDialogType.Question,
            MessageBoxImage.Exclamation => ThemedDialogType.Warning,
            MessageBoxImage.Asterisk => ThemedDialogType.Information,
            _ => ThemedDialogType.Information
        };
    }

    private static void PlaySystemSound(ThemedDialogType type)
    {
        try
        {
            switch (type)
            {
                case ThemedDialogType.Danger:
                case ThemedDialogType.Exit:
                case ThemedDialogType.Warning:
                    SystemSounds.Exclamation.Play();
                    break;
                case ThemedDialogType.Question:
                    SystemSounds.Question.Play();
                    break;
                case ThemedDialogType.Success:
                case ThemedDialogType.Information:
                    SystemSounds.Asterisk.Play();
                    break;
            }
        }
        catch
        {
            // Ignore sound errors
        }
    }

    #endregion
}
