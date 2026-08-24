using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PixelDogReminders.Models;

namespace PixelDogReminders.Views;

public partial class ReminderPopupWindow : Window
{
    private readonly SpriteVariant _variant;
    private readonly PopupPosition _position;
    private readonly DispatcherTimer _animationTimer;
    private readonly DispatcherTimer _autoDismissTimer;
    private int _currentFrame = 0;
    private readonly BitmapImage[] _frames = new BitmapImage[5];
    private bool _isClosing = false;

    public event EventHandler? SnoozeClicked;
    public event EventHandler? OkiiClicked;
    public event EventHandler? AutoDismissed;

    public ReminderPopupWindow(string title, string message, SpriteVariant variant, PopupPosition position)
    {
        InitializeComponent();

        _variant = variant;
        _position = position;

        TxtTitle.Text = title.ToUpperInvariant();
        TxtMessage.Text = message;

        // Preload the 5 animation frames for the variant
        var key = variant.ToKey();
        for (int i = 0; i < 5; i++)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Assets/Sprites/{key}_{i}.png", UriKind.Absolute);
                _frames[i] = new BitmapImage(uri);
            }
            catch
            {
                // Fallback to idle if not found
                var fallbackUri = new Uri($"pack://application:,,,/Assets/Sprites/idle_{i}.png", UriKind.Absolute);
                _frames[i] = new BitmapImage(fallbackUri);
            }
        }

        ImgDogSprite.Source = _frames[0];

        // 8 FPS animation loop
        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(125)
        };
        _animationTimer.Tick += (s, e) =>
        {
            _currentFrame = (_currentFrame + 1) % 5;
            ImgDogSprite.Source = _frames[_currentFrame];
        };

        // 2-minute neutral auto-dismiss timer (Spec v4 §1)
        _autoDismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(2)
        };
        _autoDismissTimer.Tick += (s, e) =>
        {
            _autoDismissTimer.Stop();
            PlaySlideOutAndClose(() => AutoDismissed?.Invoke(this, EventArgs.Empty));
        };

        Loaded += ReminderPopupWindow_Loaded;
    }

    private void ReminderPopupWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
        _animationTimer.Start();
        _autoDismissTimer.Start();
        PlaySlideInAnimation();
    }

    private void PositionWindow()
    {
        var workArea = SystemParameters.WorkArea;
        double width = ActualWidth > 0 ? ActualWidth : 320;
        double height = ActualHeight > 0 ? ActualHeight : 340;
        double margin = 20;

        switch (_position)
        {
            case PopupPosition.TopLeft:
                Left = workArea.Left + margin;
                Top = workArea.Top + margin;
                break;
            case PopupPosition.TopCenter:
                Left = workArea.Left + (workArea.Width - width) / 2;
                Top = workArea.Top + margin;
                break;
            case PopupPosition.TopRight:
                Left = workArea.Right - width - margin;
                Top = workArea.Top + margin;
                break;
            case PopupPosition.BottomLeft:
                Left = workArea.Left + margin;
                Top = workArea.Bottom - height - margin;
                break;
            case PopupPosition.BottomCenter:
                Left = workArea.Left + (workArea.Width - width) / 2;
                Top = workArea.Bottom - height - margin;
                break;
            case PopupPosition.BottomRight:
            default:
                Left = workArea.Right - width - margin;
                Top = workArea.Bottom - height - margin;
                break;
        }
    }

    private void PlaySlideInAnimation()
    {
        var isTop = _position is PopupPosition.TopLeft or PopupPosition.TopCenter or PopupPosition.TopRight;
        double startY = isTop ? -250 : 250;

        WindowTranslateTransform.Y = startY;
        var anim = new DoubleAnimation
        {
            From = startY,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        WindowTranslateTransform.BeginAnimation(TranslateTransform.YProperty, anim);
    }

    public void PlaySlideOutAndClose(Action? onCompleted = null)
    {
        if (_isClosing) return;
        _isClosing = true;
        _autoDismissTimer.Stop();
        _animationTimer.Stop();

        var isTop = _position is PopupPosition.TopLeft or PopupPosition.TopCenter or PopupPosition.TopRight;
        double endY = isTop ? -300 : 300;

        var anim = new DoubleAnimation
        {
            To = endY,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };

        anim.Completed += (s, e) =>
        {
            onCompleted?.Invoke();
            Close();
        };

        WindowTranslateTransform.BeginAnimation(TranslateTransform.YProperty, anim);
    }

    private void BtnSnooze_Click(object sender, RoutedEventArgs e)
    {
        _autoDismissTimer.Stop();
        PlaySlideOutAndClose(() => SnoozeClicked?.Invoke(this, EventArgs.Empty));
    }

    private void BtnOkii_Click(object sender, RoutedEventArgs e)
    {
        _autoDismissTimer.Stop();
        PlaySlideOutAndClose(() => OkiiClicked?.Invoke(this, EventArgs.Empty));
    }
}
