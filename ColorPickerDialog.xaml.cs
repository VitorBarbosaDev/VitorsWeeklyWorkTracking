using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class ColorPickerDialog : Window
{
    private double _hue = 0;        // 0 to 360
    private double _saturation = 1; // 0 to 1
    private double _value = 1;      // 0 to 1
    private bool _isUpdating = false;
    private bool _isDraggingCanvas = false;
    private readonly string _initialHex;

    public string SelectedColorHex { get; private set; } = "#6B7280";

    public static readonly string[] PresetColors = new[]
    {
        "#6B7280", // Default Grey
        "#EF4444", "#F97316", "#F59E0B", "#EAB308", "#84CC16", "#10B981", "#06B6D4",
        "#0EA5E9", "#3B82F6", "#6366F1", "#8B5CF6", "#A855F7", "#D946EF", "#EC4899",
        "#F43F5E", "#14B8A6", "#22C55E", "#2563EB", "#7C3AED", "#DB2777", "#475569"
    };

    public ColorPickerDialog(string? initialHex = null, string title = "🎨 Select Color")
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        try
        {
            var iconUri = new Uri("pack://application:,,,/VitorWeeklyWorkTracking.ico", UriKind.RelativeOrAbsolute);
            Icon = BitmapFrame.Create(iconUri);
        }
        catch { }

        _initialHex = NormalizeHex(initialHex) ?? ProjectColorHelper.DefaultProjectColorHex;
        SelectedColorHex = _initialHex;
        PickerTitleBlock.Text = title;
        Title = title;

        SetOldColorPreview(_initialHex);
        BuildPresetSwatches();

        Loaded += (s, e) =>
        {
            SetColorFromHex(_initialHex);
        };
    }

    public static string? PickColor(Window? owner, string? initialHex, string title = "🎨 Select Color")
    {
        var dialog = new ColorPickerDialog(initialHex, title);
        if (owner != null && owner != dialog && owner.IsVisible)
        {
            dialog.Owner = owner;
        }

        var result = dialog.ShowDialog();
        return result == true ? dialog.SelectedColorHex : null;
    }

    private void SetOldColorPreview(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            OldColorPreview.Background = new SolidColorBrush(color);
        }
        catch
        {
            OldColorPreview.Background = new SolidColorBrush(Colors.Gray);
        }
    }

    private void BuildPresetSwatches()
    {
        QuickPresetsPanel.Children.Clear();
        foreach (var hex in PresetColors)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var swatch = new Border
                {
                    Width = 22,
                    Height = 22,
                    CornerRadius = new CornerRadius(11),
                    Background = new SolidColorBrush(color),
                    BorderThickness = new Thickness(1.5),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = Cursors.Hand,
                    ToolTip = hex
                };

                swatch.MouseLeftButtonDown += (s, e) =>
                {
                    SetColorFromHex(hex);
                };

                QuickPresetsPanel.Children.Add(swatch);
            }
            catch { }
        }
    }

    private void SetColorFromHex(string hex)
    {
        var norm = NormalizeHex(hex);
        if (norm == null) return;

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(norm);
            RgbToHsv(color.R, color.G, color.B, out _hue, out _saturation, out _value);
            UpdateUIFromHsv();
        }
        catch { }
    }

    private void UpdateUIFromHsv()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            // Base hue color for background of SatValCanvas
            var hueColor = HsvToRgb(_hue, 1.0, 1.0);
            SatValCanvas.Background = new SolidColorBrush(hueColor);

            HueSlider.Value = _hue;

            // Current final color
            var currentColor = HsvToRgb(_hue, _saturation, _value);
            SelectedColorHex = $"#{currentColor.R:X2}{currentColor.G:X2}{currentColor.B:X2}";

            NewColorPreview.Background = new SolidColorBrush(currentColor);
            HexInputTextBox.Text = SelectedColorHex;
            RedInputTextBox.Text = currentColor.R.ToString();
            GreenInputTextBox.Text = currentColor.G.ToString();
            BlueInputTextBox.Text = currentColor.B.ToString();

            UpdateCrosshairPosition();
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void UpdateCrosshairPosition()
    {
        double width = SatValCanvas.ActualWidth > 0 ? SatValCanvas.ActualWidth : 374;
        double height = SatValCanvas.ActualHeight > 0 ? SatValCanvas.ActualHeight : 160;

        double x = _saturation * width;
        double y = (1.0 - _value) * height;

        Canvas.SetLeft(CrosshairHandle, Math.Max(0, Math.Min(width - CrosshairHandle.Width, x - CrosshairHandle.Width / 2)));
        Canvas.SetTop(CrosshairHandle, Math.Max(0, Math.Min(height - CrosshairHandle.Height, y - CrosshairHandle.Height / 2)));
    }

    private void SatValCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingCanvas = true;
        SatValCanvas.CaptureMouse();
        UpdateSatValFromMouse(e.GetPosition(SatValCanvas));
    }

    private void SatValCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingCanvas)
        {
            UpdateSatValFromMouse(e.GetPosition(SatValCanvas));
        }
    }

    private void SatValCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingCanvas)
        {
            _isDraggingCanvas = false;
            SatValCanvas.ReleaseMouseCapture();
        }
    }

    private void UpdateSatValFromMouse(Point pos)
    {
        double width = SatValCanvas.ActualWidth > 0 ? SatValCanvas.ActualWidth : 374;
        double height = SatValCanvas.ActualHeight > 0 ? SatValCanvas.ActualHeight : 160;

        double sat = Math.Max(0, Math.Min(1, pos.X / width));
        double val = Math.Max(0, Math.Min(1, 1.0 - (pos.Y / height)));

        _saturation = sat;
        _value = val;

        UpdateUIFromHsv();
    }

    private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdating) return;
        _hue = HueSlider.Value;
        UpdateUIFromHsv();
    }

    private void HexInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        var text = HexInputTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        var norm = NormalizeHex(text);
        if (norm != null)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(norm);
                RgbToHsv(color.R, color.G, color.B, out _hue, out _saturation, out _value);
                _isUpdating = true;
                try
                {
                    var hueColor = HsvToRgb(_hue, 1.0, 1.0);
                    SatValCanvas.Background = new SolidColorBrush(hueColor);
                    HueSlider.Value = _hue;
                    SelectedColorHex = norm;
                    NewColorPreview.Background = new SolidColorBrush(color);
                    RedInputTextBox.Text = color.R.ToString();
                    GreenInputTextBox.Text = color.G.ToString();
                    BlueInputTextBox.Text = color.B.ToString();
                    UpdateCrosshairPosition();
                }
                finally
                {
                    _isUpdating = false;
                }
            }
            catch { }
        }
    }

    private void RgbInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;

        if (byte.TryParse(RedInputTextBox.Text, out byte r) &&
            byte.TryParse(GreenInputTextBox.Text, out byte g) &&
            byte.TryParse(BlueInputTextBox.Text, out byte b))
        {
            RgbToHsv(r, g, b, out _hue, out _saturation, out _value);
            _isUpdating = true;
            try
            {
                var currentColor = Color.FromRgb(r, g, b);
                var hueColor = HsvToRgb(_hue, 1.0, 1.0);
                SatValCanvas.Background = new SolidColorBrush(hueColor);
                HueSlider.Value = _hue;
                SelectedColorHex = $"#{r:X2}{g:X2}{b:X2}";
                NewColorPreview.Background = new SolidColorBrush(currentColor);
                HexInputTextBox.Text = SelectedColorHex;
                UpdateCrosshairPosition();
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }

    private void ResetDefaultButton_Click(object sender, RoutedEventArgs e)
    {
        SetColorFromHex(ProjectColorHelper.DefaultProjectColorHex);
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    #region Color Conversion Helpers

    public static Color HsvToRgb(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value *= 255;
        byte v = (byte)Math.Max(0, Math.Min(255, value));
        byte p = (byte)Math.Max(0, Math.Min(255, value * (1 - saturation)));
        byte q = (byte)Math.Max(0, Math.Min(255, value * (1 - f * saturation)));
        byte t = (byte)Math.Max(0, Math.Min(255, value * (1 - (1 - f) * saturation)));

        return hi switch
        {
            0 => Color.FromRgb(v, t, p),
            1 => Color.FromRgb(q, v, p),
            2 => Color.FromRgb(p, v, t),
            3 => Color.FromRgb(p, q, v),
            4 => Color.FromRgb(t, p, v),
            _ => Color.FromRgb(v, p, q)
        };
    }

    public static void RgbToHsv(byte r, byte g, byte b, out double hue, out double saturation, out double value)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        value = max;
        saturation = max == 0 ? 0 : delta / max;

        if (delta == 0)
        {
            hue = 0;
        }
        else if (max == rd)
        {
            hue = 60 * (((gd - bd) / delta) % 6);
        }
        else if (max == gd)
        {
            hue = 60 * (((bd - rd) / delta) + 2);
        }
        else
        {
            hue = 60 * (((rd - gd) / delta) + 4);
        }

        if (hue < 0)
        {
            hue += 360;
        }
    }

    public static string? NormalizeHex(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var clean = input.Trim().TrimStart('#');
        if (clean.Length == 3 &&
            uint.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return $"#{clean[0]}{clean[0]}{clean[1]}{clean[1]}{clean[2]}{clean[2]}".ToUpperInvariant();
        }
        if (clean.Length == 6 &&
            uint.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return $"#{clean}".ToUpperInvariant();
        }
        return null;
    }

    #endregion
}