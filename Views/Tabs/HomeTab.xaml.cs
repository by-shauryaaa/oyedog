using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PixelDogReminders.Models;
using PixelDogReminders.Services;
using WpfControl = System.Windows.Controls.UserControl;

namespace PixelDogReminders.Views.Tabs;

public partial class HomeTab : WpfControl
{
    private readonly PersistenceService _persistence;
    private readonly PopupService _popupService;
    private readonly WalkInService _walkInService;

    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _spriteTimer;
    private readonly DispatcherTimer _greetingTimer;

    private int _currentFrame = 0;
    private readonly BitmapImage[] _idleFrames = new BitmapImage[5];
    private readonly BitmapImage[] _foodFrames = new BitmapImage[5];
    private readonly BitmapImage[] _restFrames = new BitmapImage[5];

    private bool _isCustomAnimation = false;
    private DispatcherTimer? _resetReactionTimer;

    public HomeTab(PersistenceService persistence, PopupService popupService, WalkInService walkInService)
    {
        InitializeComponent();
        _persistence = persistence;
        _popupService = popupService;
        _walkInService = walkInService;

        // Preload frames
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

        if (_idleFrames[0] != null)
        {
            ImgHomeDog.Source = _idleFrames[0];
        }

        // Live Clock (1 second interval)
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => UpdateClock();

        // Dog Animation (125ms / 8 FPS)
        _spriteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(125) };
        _spriteTimer.Tick += (s, e) =>
        {
            if (!_isCustomAnimation)
            {
                _currentFrame = (_currentFrame + 1) % 5;
                if (_idleFrames[_currentFrame] != null)
                {
                    ImgHomeDog.Source = _idleFrames[_currentFrame];
                }
            }
        };

        // Greeting update timer (1 minute interval)
        _greetingTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _greetingTimer.Tick += (s, e) => UpdateGreeting();

        Loaded += (s, e) =>
        {
            UpdateClock();
            UpdateGreeting();
            _clockTimer.Start();
            _spriteTimer.Start();
            _greetingTimer.Start();
        };

        Unloaded += (s, e) =>
        {
            _clockTimer.Stop();
            _spriteTimer.Stop();
            _greetingTimer.Stop();
        };
    }

    private void UpdateClock()
    {
        TxtLiveClock.Text = DateTime.Now.ToString("hh:mm:ss tt").ToUpperInvariant();
    }

    public void UpdateGreeting()
    {
        var now = DateTime.Now;

        // Birthday Easter Egg on August 25
        if (now.Month == 8 && now.Day == 25)
        {
            TxtGreeting.Text = "🎂 Happy Birthday Abhishek ♥ 🐶";
            TxtSubGreeting.Text = "Wishing you a legendary year ahead filled with joy, goals, and fast laps!";
            TxtGreeting.Foreground = System.Windows.Media.Brushes.DarkRed;
            return;
        }

        TxtGreeting.Foreground = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#8B0000")
        );

        if (now.Hour >= 5 && now.Hour < 12)
        {
            TxtGreeting.Text = "Good morning, Abhishek ☀️";
            TxtSubGreeting.Text = "Ready to conquer the day? Don't forget your habits!";
        }
        else if (now.Hour >= 12 && now.Hour < 18)
        {
            TxtGreeting.Text = "Hey Abhishek 🐾";
            TxtSubGreeting.Text = "Hope your afternoon is going great. Stay hydrated!";
        }
        else if (now.Hour >= 18 && now.Hour < 21)
        {
            TxtGreeting.Text = "Good evening, Abhishek 🌆";
            TxtSubGreeting.Text = "Time to wind down or catch up on sports!";
        }
        else
        {
            TxtGreeting.Text = "Still up, Abhishek? 🌙";
            TxtSubGreeting.Text = "Remember to get some good rest soon!";
        }
    }

    // Tapping the dog triggers screen walk
    private void DogSprite_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        TriggerScreenWalk();
    }

    private void BtnWalkDog_Click(object sender, RoutedEventArgs e)
    {
        TriggerScreenWalk();
    }

    private void TriggerScreenWalk()
    {
        TxtDogReaction.Text = "Walking across your screen! 🐾💨";
        _walkInService.CheckAndTriggerWalkIn(force: true);

        ResetReactionTextAfter(3);
    }

    private void BtnFeedDog_Click(object sender, RoutedEventArgs e)
    {
        _isCustomAnimation = true;
        TxtDogReaction.Text = "Crunch crunch! Yum, thanks Abhishek! 🍖😋❤️";

        int frame = 0;
        var feedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(130) };
        feedTimer.Tick += (s, ev) =>
        {
            ImgHomeDog.Source = _foodFrames[frame % 5];
            frame++;
            if (frame >= 12) // ~1.5s animation
            {
                feedTimer.Stop();
                _isCustomAnimation = false;
            }
        };
        feedTimer.Start();

        ResetReactionTextAfter(3.5);
    }

    private void BtnPetDog_Click(object sender, RoutedEventArgs e)
    {
        _isCustomAnimation = true;
        TxtDogReaction.Text = "*happy tail wags & barks* Woof! 🐶❤️🐾";

        int frame = 0;
        var petTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        petTimer.Tick += (s, ev) =>
        {
            ImgHomeDog.Source = _restFrames[frame % 5];
            frame++;
            if (frame >= 14) // ~1.4s animation
            {
                petTimer.Stop();
                _isCustomAnimation = false;
            }
        };
        petTimer.Start();

        ResetReactionTextAfter(3.5);
    }

    private void ResetReactionTextAfter(double seconds)
    {
        _resetReactionTimer?.Stop();
        _resetReactionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        _resetReactionTimer.Tick += (s, e) =>
        {
            _resetReactionTimer.Stop();
            TxtDogReaction.Text = "Tap me to walk across your screen! 🐾";
        };
        _resetReactionTimer.Start();
    }

    private void BtnTestPopup_Click(object sender, RoutedEventArgs e)
    {
        var variants = new[] { SpriteVariant.Idle, SpriteVariant.Water, SpriteVariant.Food, SpriteVariant.Sleep, SpriteVariant.Rest, SpriteVariant.Barca, SpriteVariant.F1 };
        var random = new Random();
        var selected = variants[random.Next(variants.Length)];

        string testMsg = selected switch
        {
            SpriteVariant.Water => "paani pi le",
            SpriteVariant.Food => "kuch khaya?",
            SpriteVariant.Sleep => "abe soja ab",
            SpriteVariant.Barca => "Barca match day today!",
            SpriteVariant.F1 => "Lights out and away we go!",
            SpriteVariant.Rest => "take a quick stretch break!",
            _ => "Woof! Hanging out on your desktop :)"
        };

        _popupService.ShowPopup("Companion Alert", testMsg, selected);
    }
}
