using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WpfColor = System.Windows.Media.Color;

namespace PixelDogReminders.Views;

public partial class ClassFlagWindow : Window
{
    private string _label = "Class";
    private string _countdown = "in 10 min";
    private string _accentColor = "#64B5F6";

    public ClassFlagWindow()
    {
        InitializeComponent();
        Loaded += ClassFlagWindow_Loaded;
    }

    public void SetContent(string label, string countdown, string accentColor)
    {
        _label = label;
        _countdown = countdown;
        _accentColor = accentColor;

        TxtSubjectName.Text = _label;
        TxtCountdown.Text = _countdown;

        try
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(_accentColor)!;
            FlagBorder.Background = brush;

            var c = brush.Color;
            // Calculate luminance to decide text foreground
            double luminance = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
            if (luminance > 140)
            {
                TxtSubjectName.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#2D1E14")!;
            }
            else
            {
                TxtSubjectName.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#FAF7F2")!;
            }
        }
        catch
        {
            // Fallback
            FlagBorder.Background = new SolidColorBrush(WpfColor.FromRgb(100, 181, 246));
            TxtSubjectName.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#2D1E14")!;
        }
    }

    private void ClassFlagWindow_Loaded(object sender, RoutedEventArgs e)
    {
        double startLeft = -ActualWidth - 30;
        double endLeft = SystemParameters.WorkArea.Width + 30;

        Left = startLeft;

        var animation = new DoubleAnimation
        {
            From = startLeft,
            To = endLeft,
            Duration = TimeSpan.FromSeconds(15.0),
            FillBehavior = FillBehavior.Stop
        };

        animation.Completed += (s, ev) =>
        {
            Close();
        };

        BeginAnimation(LeftProperty, animation);
    }
}
