using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PixelDogReminders.Models;
using WpfColor = System.Windows.Media.Color;

namespace PixelDogReminders.Views;

public partial class ClassFlagWindow : Window
{
    private string _label = "Class";
    private string _countdown = "in 10 min";
    private string _accentColor = "#64B5F6";
    private ClassReminderStyle _style = ClassReminderStyle.Simple;
    private FlagPosition _position = FlagPosition.Top;

    public ClassFlagWindow()
    {
        InitializeComponent();
        Loaded += ClassFlagWindow_Loaded;
    }

    public void SetContent(string label, string countdown, string accentColor, ClassReminderStyle style = ClassReminderStyle.Simple, FlagPosition position = FlagPosition.Top)
    {
        _label = label;
        _countdown = countdown;
        _accentColor = accentColor;
        _style = style;
        _position = position;

        // 1. Configure Simple Flag
        TxtSubjectName.Text = _label;
        TxtCountdown.Text = _countdown;

        // 2. Configure Cloud
        TxtCloudSubject.Text = _label;
        TxtCloudCountdown.Text = _countdown;

        // 3. Configure Banner
        TxtBannerSubject.Text = _label;
        TxtBannerCountdown.Text = _countdown;

        // Apply accent color
        try
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(_accentColor)!;
            FlagBorder.Background = brush;
            BannerSubjectPlaque.Background = brush;

            var c = brush.Color;
            double luminance = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
            var textBrush = (luminance > 140)
                ? (SolidColorBrush)new BrushConverter().ConvertFromString("#2D1E14")!
                : (SolidColorBrush)new BrushConverter().ConvertFromString("#FAF7F2")!;

            TxtSubjectName.Foreground = textBrush;
            TxtBannerSubject.Foreground = textBrush;
        }
        catch
        {
            FlagBorder.Background = new SolidColorBrush(WpfColor.FromRgb(100, 181, 246));
            BannerSubjectPlaque.Background = new SolidColorBrush(WpfColor.FromRgb(100, 181, 246));
        }

        // Toggle container visibility
        SimpleContainer.Visibility = (_style == ClassReminderStyle.Simple) ? Visibility.Visible : Visibility.Collapsed;
        CloudContainer.Visibility = (_style == ClassReminderStyle.Cloud) ? Visibility.Visible : Visibility.Collapsed;
        BannerContainer.Visibility = (_style == ClassReminderStyle.Banner) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ClassFlagWindow_Loaded(object sender, RoutedEventArgs e)
    {
        switch (_style)
        {
            case ClassReminderStyle.Cloud:
                StartCloudAnimation();
                break;
            case ClassReminderStyle.Banner:
                StartBannerAnimation();
                break;
            case ClassReminderStyle.Simple:
            default:
                StartSimpleAnimation();
                break;
        }
    }

    private double ComputeTargetTop()
    {
        var workArea = SystemParameters.WorkArea;
        return _position switch
        {
            FlagPosition.Top => workArea.Top + (workArea.Height * 0.18),
            FlagPosition.Middle => workArea.Top + (workArea.Height * 0.48) - 30,
            FlagPosition.Bottom => workArea.Bottom - 120,
            _ => workArea.Top + (workArea.Height * 0.18)
        };
    }

    private void StartSimpleAnimation()
    {
        double startLeft = -ActualWidth - 30;
        double endLeft = SystemParameters.WorkArea.Width + 30;

        Top = ComputeTargetTop();
        Left = startLeft;

        var animation = new DoubleAnimation
        {
            From = startLeft,
            To = endLeft,
            Duration = TimeSpan.FromSeconds(10.0),
            FillBehavior = FillBehavior.Stop
        };

        animation.Completed += (s, ev) => Close();
        BeginAnimation(LeftProperty, animation);
    }

    private void StartCloudAnimation()
    {
        double startLeft = -ActualWidth - 30;
        double endLeft = SystemParameters.WorkArea.Width + 30;
        double baseTop = ComputeTargetTop();

        Top = baseTop;
        Left = startLeft;

        // Horizontal float
        var moveAnim = new DoubleAnimation
        {
            From = startLeft,
            To = endLeft,
            Duration = TimeSpan.FromSeconds(12.0),
            FillBehavior = FillBehavior.Stop
        };
        moveAnim.Completed += (s, ev) => Close();

        // Subtle vertical sine bobbing (±8px)
        var bobAnim = new DoubleAnimation
        {
            From = baseTop - 8,
            To = baseTop + 8,
            Duration = TimeSpan.FromMilliseconds(1400),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        BeginAnimation(LeftProperty, moveAnim);
        BeginAnimation(TopProperty, bobAnim);
    }

    private void StartBannerAnimation()
    {
        var workArea = SystemParameters.WorkArea;
        double finalTop = ComputeTargetTop();
        double startTop = -ActualHeight - 20;

        Left = workArea.Left + (workArea.Width - ActualWidth) / 2.0;
        Top = startTop;

        // 1. Drop down with slight bounce (0.8s)
        var dropIn = new DoubleAnimation
        {
            From = startTop,
            To = finalTop,
            Duration = TimeSpan.FromMilliseconds(800),
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
        };

        dropIn.Completed += (s, ev) =>
        {
            Top = finalTop;

            // 2. Hold in place for 10 seconds
            var holdTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            holdTimer.Tick += (st, evt) =>
            {
                holdTimer.Stop();

                // 3. Snap strings and plummet off-screen (0.5s fast gravity fall)
                var fadeStrings = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(150));
                PnlHangingStrings.BeginAnimation(OpacityProperty, fadeStrings);

                var fallDown = new DoubleAnimation
                {
                    From = finalTop,
                    To = workArea.Bottom + 50,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                fallDown.Completed += (s2, ev2) => Close();
                BeginAnimation(TopProperty, fallDown);
            };
            holdTimer.Start();
        };

        BeginAnimation(TopProperty, dropIn);
    }
}
