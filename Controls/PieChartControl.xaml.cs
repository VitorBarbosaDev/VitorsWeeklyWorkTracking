using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking.Controls;

public enum ChartStyle
{
    Donut,
    SolidPie
}

public enum ChartPalette
{
    ModernVivid,
    NeonCyber,
    PastelCandy,
    OceanBreeze,
    SunsetWarmth,
    ForestNature
}

public class PieSliceItem
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string FormattedValue { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public Brush? Color { get; set; }
}

public partial class PieChartControl : UserControl
{
    public static readonly IReadOnlyList<(ChartPalette Palette, string DisplayName)> AvailablePalettes = new List<(ChartPalette, string)>
    {
        (ChartPalette.ModernVivid, "✨ Modern Vivid"),
        (ChartPalette.NeonCyber, "⚡ Neon Cyber"),
        (ChartPalette.PastelCandy, "🍬 Pastel Candy"),
        (ChartPalette.OceanBreeze, "🌊 Ocean Breeze"),
        (ChartPalette.SunsetWarmth, "🌅 Sunset Warmth"),
        (ChartPalette.ForestNature, "🌲 Forest Nature")
    };

    public static readonly Dictionary<ChartPalette, List<string>> Palettes = new()
    {
        [ChartPalette.ModernVivid] = new List<string>
        {
            "#3B82F6", "#10B981", "#F59E0B", "#8B5CF6", "#EC4899", "#06B6D4", "#F97316", "#84CC16", "#14B8A6", "#E11D48", "#A855F7", "#6366F1"
        },
        [ChartPalette.NeonCyber] = new List<string>
        {
            "#00F0FF", "#FF007F", "#39FF14", "#FFE600", "#A855F7", "#FF5252", "#00FFCC", "#FF7700", "#7C3AED", "#06B6D4"
        },
        [ChartPalette.PastelCandy] = new List<string>
        {
            "#F472B6", "#60A5FA", "#34D399", "#FBBF24", "#A78BFA", "#F87171", "#38BDF8", "#4ADE80", "#FB923C", "#C084FC"
        },
        [ChartPalette.OceanBreeze] = new List<string>
        {
            "#0077B6", "#00B4D8", "#48CAE4", "#90E0EF", "#0096C7", "#023E8A", "#5390D9", "#5E60CE", "#64DFDF", "#72EFDD"
        },
        [ChartPalette.SunsetWarmth] = new List<string>
        {
            "#EA580C", "#F97316", "#FB923C", "#FBBF24", "#DC2626", "#BE123C", "#E11D48", "#F59E0B", "#D97706", "#C2410C"
        },
        [ChartPalette.ForestNature] = new List<string>
        {
            "#059669", "#10B981", "#34D399", "#6EE7B7", "#047857", "#84CC16", "#14B8A6", "#15803D", "#4D7C0F", "#0D9488"
        }
    };

    public static ChartStyle GlobalChartStyle { get; set; } = ChartStyle.Donut;
    public static ChartPalette GlobalChartPalette { get; set; } = ChartPalette.ModernVivid;
    public static event Action? ChartPreferencesChanged;

    public static void SetGlobalPreferences(ChartStyle style, ChartPalette palette)
    {
        GlobalChartStyle = style;
        GlobalChartPalette = palette;
        ChartPreferencesChanged?.Invoke();
    }

    private readonly List<PieSliceItem> _slices = new();
    private readonly List<UIElement> _renderedShapes = new();
    private readonly List<Border> _legendItems = new();
    private string _defaultTotalFormatted = "0h 00m";
    private double _defaultTotalHours = 0;
    private IEnumerable<PieSliceItem>? _lastRawItems;
    private string? _lastTitle;

    public PieChartControl()
    {
        InitializeComponent();
        
        ThemeManager.ThemeChanged += _ =>
        {
            ReapplyData();
        };

        ChartPreferencesChanged += () =>
        {
            ReapplyData();
        };
    }

    public void SetData(IEnumerable<PieSliceItem> items, string? title = null)
    {
        _lastRawItems = items;
        _lastTitle = title;
        ReapplyData();
    }

    private void ReapplyData()
    {
        if (!string.IsNullOrWhiteSpace(_lastTitle))
        {
            ChartTitleTextBlock.Text = _lastTitle;
            ChartTitleTextBlock.Visibility = Visibility.Visible;
        }
        else
        {
            ChartTitleTextBlock.Visibility = Visibility.Collapsed;
        }

        var rawList = _lastRawItems?.Where(x => x.Value > 0).ToList() ?? new List<PieSliceItem>();
        var totalSeconds = rawList.Sum(x => x.Value);
        var totalTimeSpan = TimeSpan.FromSeconds(totalSeconds);
        _defaultTotalHours = totalTimeSpan.TotalHours;

        _defaultTotalFormatted = $"{(int)totalTimeSpan.TotalHours}h {totalTimeSpan.Minutes:D2}m {totalTimeSpan.Seconds:D2}s";

        _slices.Clear();
        int colorIdx = 0;

        var paletteColors = Palettes.TryGetValue(GlobalChartPalette, out var colors) ? colors : Palettes[ChartPalette.ModernVivid];

        foreach (var item in rawList)
        {
            var percentage = totalSeconds > 0 ? (item.Value / totalSeconds) * 100.0 : 0.0;
            var hex = paletteColors[colorIdx % paletteColors.Count];
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            colorIdx++;

            _slices.Add(new PieSliceItem
            {
                Label = string.IsNullOrWhiteSpace(item.Label) ? "(Unassigned)" : item.Label,
                Value = item.Value,
                FormattedValue = item.FormattedValue,
                Percentage = percentage,
                Color = brush
            });
        }

        RenderChart();
        RenderLegend();
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderChart();
    }

    private void RenderChart()
    {
        ChartCanvas.Children.Clear();
        _renderedShapes.Clear();

        if (_slices.Count == 0)
        {
            EmptyMessagePanel.Visibility = Visibility.Visible;
            CenterHubBorder.Visibility = Visibility.Collapsed;
            return;
        }

        EmptyMessagePanel.Visibility = Visibility.Collapsed;

        double width = ChartCanvas.ActualWidth;
        double height = ChartCanvas.ActualHeight;

        if (width <= 15 || height <= 15)
        {
            // Canvas not measured yet; defer until layout is ready
            return;
        }

        double centerX = width / 2.0;
        double centerY = height / 2.0;
        double radius = Math.Max(15, Math.Min(centerX, centerY) - 10);
        double innerRadius = GlobalChartStyle == ChartStyle.Donut ? radius * 0.58 : 0;

        // Reset center hub
        if (GlobalChartStyle == ChartStyle.Donut)
        {
            CenterHubBorder.Visibility = Visibility.Visible;
            CenterHubLabel.Text = "TOTAL";
            CenterHubValue.Text = _defaultTotalFormatted;
            CenterHubPercent.Text = $"{_slices.Count} {(_slices.Count == 1 ? "item" : "items")}";
            CenterHubBorder.Width = Math.Max(60, innerRadius * 1.85);
            CenterHubBorder.Height = Math.Max(60, innerRadius * 1.85);
        }
        else
        {
            CenterHubBorder.Visibility = Visibility.Collapsed;
        }

        if (_slices.Count == 1)
        {
            var singleSlice = _slices[0];
            UIElement shape;

            if (GlobalChartStyle == ChartStyle.Donut)
            {
                var outerGeom = new EllipseGeometry(new Point(centerX, centerY), radius, radius);
                var innerGeom = new EllipseGeometry(new Point(centerX, centerY), innerRadius, innerRadius);
                var donutGeom = new CombinedGeometry(GeometryCombineMode.Exclude, outerGeom, innerGeom);

                var donutPath = new Path
                {
                    Data = donutGeom,
                    Fill = singleSlice.Color,
                    StrokeThickness = 2,
                    ToolTip = CreateToolTip(singleSlice),
                    Cursor = Cursors.Hand
                };
                donutPath.SetResourceReference(Shape.StrokeProperty, "Theme.CardBackgroundAlt");
                
                donutPath.MouseEnter += (s, e) => HighlightSlice(0);
                donutPath.MouseLeave += (s, e) => ResetHighlight();
                shape = donutPath;
            }
            else
            {
                var circle = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Fill = singleSlice.Color,
                    StrokeThickness = 2,
                    ToolTip = CreateToolTip(singleSlice),
                    Cursor = Cursors.Hand
                };
                circle.SetResourceReference(Shape.StrokeProperty, "Theme.CardBackgroundAlt");
                Canvas.SetLeft(circle, centerX - radius);
                Canvas.SetTop(circle, centerY - radius);

                circle.MouseEnter += (s, e) => HighlightSlice(0);
                circle.MouseLeave += (s, e) => ResetHighlight();
                shape = circle;
            }

            _renderedShapes.Add(shape);
            ChartCanvas.Children.Add(shape);
            return;
        }

        double startAngle = -90.0; // Start at 12 o'clock

        for (int i = 0; i < _slices.Count; i++)
        {
            var slice = _slices[i];
            double sweepAngle = (slice.Percentage / 100.0) * 360.0;
            if (sweepAngle <= 0.001)
                continue;

            double endAngle = startAngle + sweepAngle;
            var path = CreateSlicePath(centerX, centerY, radius, innerRadius, startAngle, endAngle, slice, GlobalChartStyle == ChartStyle.Donut);

            int sliceIndex = i;
            path.MouseEnter += (s, e) => HighlightSlice(sliceIndex);
            path.MouseLeave += (s, e) => ResetHighlight();

            _renderedShapes.Add(path);
            ChartCanvas.Children.Add(path);

            startAngle = endAngle;
        }
    }

    private static Path CreateSlicePath(double cx, double cy, double rOut, double rIn, double startAngleDeg, double endAngleDeg, PieSliceItem slice, bool isDonut)
    {
        double startRad = startAngleDeg * Math.PI / 180.0;
        double endRad = endAngleDeg * Math.PI / 180.0;

        Point pOutStart = new Point(cx + rOut * Math.Cos(startRad), cy + rOut * Math.Sin(startRad));
        Point pOutEnd = new Point(cx + rOut * Math.Cos(endRad), cy + rOut * Math.Sin(endRad));

        bool isLargeArc = (endAngleDeg - startAngleDeg) > 180.0;

        var figure = new PathFigure();

        if (isDonut && rIn > 0)
        {
            Point pInEnd = new Point(cx + rIn * Math.Cos(endRad), cy + rIn * Math.Sin(endRad));
            Point pInStart = new Point(cx + rIn * Math.Cos(startRad), cy + rIn * Math.Sin(startRad));

            figure.StartPoint = pOutStart;
            figure.Segments.Add(new ArcSegment(pOutEnd, new Size(rOut, rOut), 0, isLargeArc, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(pInEnd, true));
            figure.Segments.Add(new ArcSegment(pInStart, new Size(rIn, rIn), 0, isLargeArc, SweepDirection.Counterclockwise, true));
            figure.IsClosed = true;
        }
        else
        {
            figure.StartPoint = new Point(cx, cy);
            figure.Segments.Add(new LineSegment(pOutStart, true));
            figure.Segments.Add(new ArcSegment(pOutEnd, new Size(rOut, rOut), 0, isLargeArc, SweepDirection.Clockwise, true));
            figure.IsClosed = true;
        }

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        var path = new Path
        {
            Fill = slice.Color,
            StrokeThickness = 2.0,
            Data = geometry,
            ToolTip = CreateToolTip(slice),
            Cursor = Cursors.Hand
        };
        path.SetResourceReference(Shape.StrokeProperty, "Theme.CardBackgroundAlt");

        return path;
    }

    private static ToolTip CreateToolTip(PieSliceItem slice)
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["Theme.CardBackground"],
            BorderBrush = (Brush)Application.Current.Resources["Theme.CardBorder"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 6, 8, 6)
        };

        var sp = new StackPanel { Orientation = Orientation.Vertical };

        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        var dot = new Border
        {
            Width = 10,
            Height = 10,
            CornerRadius = new CornerRadius(5),
            Background = slice.Color,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var title = new TextBlock
        {
            Text = slice.Label,
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["Theme.Foreground"]
        };
        headerPanel.Children.Add(dot);
        headerPanel.Children.Add(title);

        var valueText = new TextBlock
        {
            Text = $"Duration: {slice.FormattedValue}",
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["Theme.ForegroundMuted"]
        };

        var percentText = new TextBlock
        {
            Text = $"Share: {slice.Percentage:F1}%",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["Theme.Accent"],
            Margin = new Thickness(0, 2, 0, 0)
        };

        sp.Children.Add(headerPanel);
        sp.Children.Add(valueText);
        sp.Children.Add(percentText);
        border.Child = sp;

        return new ToolTip
        {
            Content = border,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        };
    }

    private void HighlightSlice(int index)
    {
        if (index < 0 || index >= _slices.Count)
            return;

        var slice = _slices[index];

        if (GlobalChartStyle == ChartStyle.Donut)
        {
            CenterHubLabel.Text = slice.Label;
            CenterHubValue.Text = slice.FormattedValue;
            CenterHubPercent.Text = $"{slice.Percentage:F1}%";
        }

        if (index < _renderedShapes.Count && _renderedShapes[index] is Shape shape)
        {
            shape.StrokeThickness = 3.5;
            shape.SetResourceReference(Shape.StrokeProperty, "Theme.Accent");
            Panel.SetZIndex(shape, 100);
        }

        if (index < _legendItems.Count)
        {
            var legendBorder = _legendItems[index];
            legendBorder.SetResourceReference(Border.BackgroundProperty, "Theme.SecondaryHover");
            legendBorder.SetResourceReference(Border.BorderBrushProperty, "Theme.Accent");
        }
    }

    private void ResetHighlight()
    {
        if (GlobalChartStyle == ChartStyle.Donut)
        {
            CenterHubLabel.Text = "TOTAL";
            CenterHubValue.Text = _defaultTotalFormatted;
            CenterHubPercent.Text = $"{_slices.Count} {(_slices.Count == 1 ? "item" : "items")}";
        }

        for (int i = 0; i < _renderedShapes.Count; i++)
        {
            if (_renderedShapes[i] is Shape shape)
            {
                shape.StrokeThickness = 2.0;
                shape.SetResourceReference(Shape.StrokeProperty, "Theme.CardBackgroundAlt");
                Panel.SetZIndex(shape, 0);
            }
        }

        for (int i = 0; i < _legendItems.Count; i++)
        {
            _legendItems[i].Background = Brushes.Transparent;
            _legendItems[i].SetResourceReference(Border.BorderBrushProperty, "Theme.CardBorder");
        }
    }

    private void RenderLegend()
    {
        LegendPanel.Children.Clear();
        _legendItems.Clear();

        if (_slices.Count == 0)
            return;

        for (int i = 0; i < _slices.Count; i++)
        {
            var slice = _slices[i];
            int sliceIndex = i;

            var itemBorder = new Border
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(7, 4, 7, 4),
                Margin = new Thickness(4, 3, 4, 3),
                Cursor = Cursors.Hand,
                ToolTip = $"{slice.Label}: {slice.FormattedValue} ({slice.Percentage:F1}%)"
            };
            itemBorder.SetResourceReference(Border.BorderBrushProperty, "Theme.CardBorder");

            var itemStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var swatch = new Border
            {
                Width = 10,
                Height = 10,
                Background = slice.Color,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var labelText = new TextBlock
            {
                Text = slice.Label,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 110,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            labelText.SetResourceReference(TextBlock.ForegroundProperty, "Theme.Foreground");

            var pctBadge = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            pctBadge.SetResourceReference(Border.BackgroundProperty, "Theme.Secondary");

            var pctText = new TextBlock
            {
                Text = $"{slice.Percentage:F1}%",
                FontSize = 10,
                FontWeight = FontWeights.Bold
            };
            pctText.SetResourceReference(TextBlock.ForegroundProperty, "Theme.Accent");
            pctBadge.Child = pctText;

            itemStack.Children.Add(swatch);
            itemStack.Children.Add(labelText);
            itemStack.Children.Add(pctBadge);
            itemBorder.Child = itemStack;

            itemBorder.MouseEnter += (s, e) => HighlightSlice(sliceIndex);
            itemBorder.MouseLeave += (s, e) => ResetHighlight();

            _legendItems.Add(itemBorder);
            LegendPanel.Children.Add(itemBorder);
        }
    }
}
