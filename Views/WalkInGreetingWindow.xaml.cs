using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PixelDogReminders.Views;

public partial class WalkInGreetingWindow : Window
{
    private readonly bool _isBirthday;
    private readonly BitmapImage[] _walkFrames = new BitmapImage[8];
    private readonly BitmapImage[] _idleFrames = new BitmapImage[5];
    private readonly BitmapImage[] _foodFrames = new BitmapImage[5];
    private readonly BitmapImage[] _restFrames = new BitmapImage[5];

    private readonly DispatcherTimer _spriteTimer;
    private readonly DispatcherTimer _autoDismissTimer;

    private int _currentFrame = 0;
    private bool _hasArrived = false;
    private bool _isReacting = false;
    private bool _isClosing = false;

    public WalkInGreetingWindow()
    {
        InitializeComponent();

        var now = DateTime.Now;
        _isBirthday = (now.Month == 8 && now.Day == 25);

        if (_isBirthday)
        {
            TxtTitle.Text = "🎂 BIRTHDAY COMPANION 🐶";
            TxtGreeting.Text = "Happy Birthday Abhishek! 🎉";
        }
        else
        {
            TxtTitle.Text = "GOOD MORNING ☀️";
            TxtGreeting.Text = "Good morning, Abhishek";
        }

        // 1. Preload 8 walking frames (birthday or standard)
        var walkPrefix = _isBirthday ? "birthday_walk" : "walking";
        for (int i = 0; i < 8; i++)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Assets/Sprites/{walkPrefix}_{i}.png", UriKind.Absolute);
                _walkFrames[i] = new BitmapImage(uri);
            }
            catch
            {
                // Fallback
            }
        }

        // 2. Preload 5 idle, food, rest frames
        for (int i = 0; i < 5; i++)
        {
            try
            {
                _idleFrames[i] = new BitmapImage(new Uri($"pack://application:,,,/Assets/Sprites/idle_{i}.png", UriKind.Absolute));
                _foodFrames[i] = new BitmapImage(new Uri($"pack://application:,,,/Assets/Sprites/food_{i}.png", UriKind.Absolute));
                _restFrames[i] = new BitmapImage(new Uri($"pack://application:,,,/Assets/Sprites/rest_{i}.png", UriKind.Absolute));
            }
            catch
            {
                // Fallback
            }
        }

        if (_walkFrames[0] != null)
        {
            ImgDogSprite.Source = _walkFrames[0];
        }

        // Sprite Animation Timer (125ms / 8 FPS)
        _spriteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(125) };
        _spriteTimer.Tick += SpriteTimer_Tick;

        // Auto-dismiss after 30 seconds
        _autoDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _autoDismissTimer.Tick += (s, e) =>
        {
            _autoDismissTimer.Stop();
            SlideOutAndClose();
        };

        Loaded += WalkInGreetingWindow_Loaded;
    }

    private void SpriteTimer_Tick(object? sender, EventArgs e)
    {
        if (!_hasArrived)
        {
            _currentFrame = (_currentFrame + 1) % 8;
            if (_walkFrames[_currentFrame] != null)
            {
                ImgDogSprite.Source = _walkFrames[_currentFrame];
            }
        }
        else if (!_isReacting)
        {
            _currentFrame = (_currentFrame + 1) % 5;
            if (_idleFrames[_currentFrame] != null)
            {
                ImgDogSprite.Source = _idleFrames[_currentFrame];
            }
        }
    }

    private void WalkInGreetingWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        double width = ActualWidth > 0 ? ActualWidth : 320;
        double height = ActualHeight > 0 ? ActualHeight : 340;

        // Start off-screen at bottom-left
        double startLeft = workArea.Left - width;
        double targetLeft = workArea.Right - width - 30;
        double bottomTop = workArea.Bottom - height - 10;

        Left = startLeft;
        Top = bottomTop;

        _spriteTimer.Start();

        // Animate Window.Left across the screen (4.5s linear walk)
        var walkAnim = new DoubleAnimation
        {
            From = startLeft,
            To = targetLeft,
            Duration = TimeSpan.FromSeconds(4.5)
        };

        walkAnim.Completed += (s, ev) =>
        {
            _hasArrived = true;
            _currentFrame = 0;

            // Fade in speech bubble and buttons
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400));
            PnlSpeechBubble.BeginAnimation(OpacityProperty, fadeIn);
            PnlButtons.BeginAnimation(OpacityProperty, fadeIn);

            _autoDismissTimer.Start();
        };

        BeginAnimation(LeftProperty, walkAnim);
    }

    private void BtnFeed_Click(object sender, RoutedEventArgs e)
    {
        if (_isClosing) return;
        _autoDismissTimer.Stop();
        _isReacting = true;

        // Play food eating reaction
        int reactionCount = 0;
        var reactionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(130) };
        reactionTimer.Tick += (s, ev) =>
        {
            ImgDogSprite.Source = _foodFrames[reactionCount % 5];
            reactionCount++;
            if (reactionCount >= 10) // ~1.3 seconds
            {
                reactionTimer.Stop();
                SlideOutAndClose();
            }
        };
        reactionTimer.Start();
    }

    private void BtnLetItBe_Click(object sender, RoutedEventArgs e)
    {
        if (_isClosing) return;
        _autoDismissTimer.Stop();
        _isReacting = true;

        // Play rest/stretch reaction
        int reactionCount = 0;
        var reactionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(130) };
        reactionTimer.Tick += (s, ev) =>
        {
            ImgDogSprite.Source = _restFrames[reactionCount % 5];
            reactionCount++;
            if (reactionCount >= 10) // ~1.3 seconds
            {
                reactionTimer.Stop();
                SlideOutAndClose();
            }
        };
        reactionTimer.Start();
    }

    private void SlideOutAndClose()
    {
        if (_isClosing) return;
        _isClosing = true;
        _spriteTimer.Stop();

        // Slide out / fade
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
        fadeOut.Completed += (s, ev) => Close();
        BeginAnimation(OpacityProperty, fadeOut);
    }
}
