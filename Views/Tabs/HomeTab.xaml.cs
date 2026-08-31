using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PixelDogReminders.Models;
using PixelDogReminders.Services;
using WpfControl = System.Windows.Controls.UserControl;

namespace PixelDogReminders.Views.Tabs;

public enum TimeOfDayPeriod
{
    Morning,
    Day,
    Evening,
    Night
}

public partial class HomeTab : WpfControl
{
    private readonly PersistenceService _persistence;
    private readonly PopupService _popupService;
    private readonly WalkInService _walkInService;

    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _spriteTimer;
    private readonly DispatcherTimer _greetingTimer;
    private readonly DispatcherTimer _ambientTimer;

    private int _currentFrame = 0;
    private readonly BitmapImage[] _idleFrames = new BitmapImage[5];
    private readonly BitmapImage[] _foodFrames = new BitmapImage[5];
    private readonly BitmapImage[] _restFrames = new BitmapImage[5];
    private readonly BitmapImage[] _walkFrames = new BitmapImage[8];

    // Ambient sprites
    private readonly BitmapImage[] _birdFrames = new BitmapImage[2];
    private readonly BitmapImage[] _fireflyFrames = new BitmapImage[2];
    private readonly BitmapImage[] _starFrames = new BitmapImage[2];

    private bool _isCustomAnimation = false;
    private bool _isEntranceWalking = false;
    private DispatcherTimer? _resetReactionTimer;

    private TimeOfDayPeriod _currentPeriod = TimeOfDayPeriod.Day;
    private double _cloud1X = 40;
    private double _cloud2X = 350;
    private double _bird1X = -50;
    private double _bird2X = -90;
    private double _fireflyTick = 0;
    private int _ambientFrameCount = 0;

    public HomeTab(PersistenceService persistence, PopupService popupService, WalkInService walkInService)
    {
        InitializeComponent();
        _persistence = persistence;
        _popupService = popupService;
        _walkInService = walkInService;

        var now = DateTime.Now;
        bool isBirthday = (now.Month == 8 && now.Day == 25);

        // 1. Preload Dog Sprites
        var walkPrefix = isBirthday ? "birthday_walk" : "walking";
        for (int i = 0; i < 8; i++)
        {
            try
            {
                _walkFrames[i] = new BitmapImage(new Uri($"pack://application:,,,/Assets/Sprites/{walkPrefix}_{i}.png", UriKind.Absolute));
            }
            catch { }
        }

        for (int i = 0; i < 5; i++)
        {
            try
            {
                _idleFrames[i] = new BitmapImage(new Uri($"pack://application:,,,/Assets/Sprites/idle_{i}.png", UriKind.Absolute));
                _foodFrames[i] = new BitmapImage(new Uri($"pack://application:,,,/Assets/Sprites/food_{i}.png", UriKind.Absolute));
                _restFrames[i] = new BitmapImage(new Uri($"pack://application:,,,/Assets/Sprites/rest_{i}.png", UriKind.Absolute));
            }
            catch { }
        }

        // 2. Preload Ambient Sprites
        for (int i = 0; i < 2; i++)
        {
            try
            {
                _birdFrames[i] = new BitmapImage(new Uri($"pack://application:,,,/Assets/Sprites/bird_{i}.png", UriKind.Absolute));
                _fireflyFrames[i] = new BitmapImage(new Uri($"pack://application:,,,/Assets/Sprites/firefly_{i}.png", UriKind.Absolute));
                _starFrames[i] = new BitmapImage(new Uri($"pack://application:,,,/Assets/Sprites/star_{i}.png", UriKind.Absolute));
            }
            catch { }
        }

        if (_idleFrames[0] != null)
        {
            ImgHomeDog.Source = _idleFrames[0];
        }

        // 3. Live Clock (1-second tick)
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => UpdateClock();

        // 4. Companion Sprite Animation (125ms / 8 FPS)
        _spriteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(125) };
        _spriteTimer.Tick += (s, e) =>
        {
            if (_isEntranceWalking)
            {
                _currentFrame = (_currentFrame + 1) % 8;
                if (_walkFrames[_currentFrame] != null)
                {
                    ImgHomeDog.Source = _walkFrames[_currentFrame];
                }
            }
            else if (!_isCustomAnimation)
            {
                _currentFrame = (_currentFrame + 1) % 5;
                if (_idleFrames[_currentFrame] != null)
                {
                    ImgHomeDog.Source = _idleFrames[_currentFrame];
                }
            }
        };

        // 5. Greeting & Scenery update timer (1-minute interval)
        _greetingTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _greetingTimer.Tick += (s, e) =>
        {
            UpdateGreeting();
            UpdateTimeOfDay(animate: true);
        };

        // 6. Ambient motion loop (50ms interval ~ 20 FPS)
        _ambientTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _ambientTimer.Tick += AmbientTimer_Tick;

        Loaded += (s, e) =>
        {
            UpdateClock();
            UpdateGreeting();
            UpdateTimeOfDay(animate: false);

            _clockTimer.Start();
            _spriteTimer.Start();
            _greetingTimer.Start();
            _ambientTimer.Start();
        };

        Unloaded += (s, e) =>
        {
            _clockTimer.Stop();
            _spriteTimer.Stop();
            _greetingTimer.Stop();
            _ambientTimer.Stop();
        };
    }

    public void TriggerEntranceWalk()
    {
        _isEntranceWalking = true;
        _currentFrame = 0;

        // 1. Position dog at left edge
        TransDogEntrance.X = -340;

        // 2. Hide surrounding contextual action buttons and speech bubble
        PnlBtnFeed.Opacity = 0;
        PnlBtnPet.Opacity = 0;
        PnlBtnWalk.Opacity = 0;
        PnlDogSpeech.Opacity = 0;

        ScaleBtnFeed.ScaleX = 0.4;
        ScaleBtnFeed.ScaleY = 0.4;
        ScaleBtnPet.ScaleX = 0.4;
        ScaleBtnPet.ScaleY = 0.4;
        ScaleBtnWalk.ScaleX = 0.4;
        ScaleBtnWalk.ScaleY = 0.4;

        if (_walkFrames[0] != null)
        {
            ImgHomeDog.Source = _walkFrames[0];
        }

        // 3. Animate walk across the lawn to center (1.6 seconds)
        var walkAnim = new DoubleAnimation
        {
            From = -340,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(1600),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        walkAnim.Completed += (s, e) =>
        {
            _isEntranceWalking = false;
            _currentFrame = 0;
            if (_idleFrames[0] != null)
            {
                ImgHomeDog.Source = _idleFrames[0];
            }

            // 4. Pop-in contextual buttons & speech bubble
            PopInActionButtons();
        };

        TransDogEntrance.BeginAnimation(TranslateTransform.XProperty, walkAnim);
    }

    private void PopInActionButtons()
    {
        var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 };

        // Animate Feed Button
        var fadeFeed = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350));
        var scaleFeed = new DoubleAnimation(0.4, 1.0, TimeSpan.FromMilliseconds(450)) { EasingFunction = ease };
        PnlBtnFeed.BeginAnimation(OpacityProperty, fadeFeed);
        ScaleBtnFeed.BeginAnimation(ScaleTransform.ScaleXProperty, scaleFeed);
        ScaleBtnFeed.BeginAnimation(ScaleTransform.ScaleYProperty, scaleFeed);

        // Animate Pet Button (slight delay)
        var fadePet = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400)) { BeginTime = TimeSpan.FromMilliseconds(80) };
        var scalePet = new DoubleAnimation(0.4, 1.0, TimeSpan.FromMilliseconds(500)) { BeginTime = TimeSpan.FromMilliseconds(80), EasingFunction = ease };
        PnlBtnPet.BeginAnimation(OpacityProperty, fadePet);
        ScaleBtnPet.BeginAnimation(ScaleTransform.ScaleXProperty, scalePet);
        ScaleBtnPet.BeginAnimation(ScaleTransform.ScaleYProperty, scalePet);

        // Animate Walk Button
        var fadeWalk = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(450)) { BeginTime = TimeSpan.FromMilliseconds(140) };
        var scaleWalk = new DoubleAnimation(0.4, 1.0, TimeSpan.FromMilliseconds(550)) { BeginTime = TimeSpan.FromMilliseconds(140), EasingFunction = ease };
        PnlBtnWalk.BeginAnimation(OpacityProperty, fadeWalk);
        ScaleBtnWalk.BeginAnimation(ScaleTransform.ScaleXProperty, scaleWalk);
        ScaleBtnWalk.BeginAnimation(ScaleTransform.ScaleYProperty, scaleWalk);

        // Fade in speech bubble
        var fadeSpeech = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400)) { BeginTime = TimeSpan.FromMilliseconds(200) };
        PnlDogSpeech.BeginAnimation(OpacityProperty, fadeSpeech);
    }

    private void UpdateClock()
    {
        TxtLiveClock.Text = DateTime.Now.ToString("hh:mm:ss tt").ToUpperInvariant();
    }

    public void UpdateGreeting()
    {
        var now = DateTime.Now;
        var (settings, _) = _persistence.LoadData();
        string name = string.IsNullOrWhiteSpace(settings.DisplayName) ? "Abhishek" : settings.DisplayName.Trim();

        // Birthday Easter Egg on August 25
        if (now.Month == 8 && now.Day == 25)
        {
            TxtGreeting.Text = $"🎂 Happy Birthday {name} ♥ 🐶";
            TxtSubGreeting.Text = "Wishing you a legendary year ahead filled with joy, goals, and fast laps!";
            return;
        }

        if (now.Hour >= 5 && now.Hour < 12)
        {
            TxtGreeting.Text = $"Good morning, {name} ☀️";
            TxtSubGreeting.Text = "Ready to conquer the day? Don't forget your habits!";
        }
        else if (now.Hour >= 12 && now.Hour < 18)
        {
            TxtGreeting.Text = $"Hey {name} 🐾";
            TxtSubGreeting.Text = "Hope your afternoon is going great. Stay hydrated!";
        }
        else if (now.Hour >= 18 && now.Hour < 21)
        {
            TxtGreeting.Text = $"Good evening, {name} 🌆";
            TxtSubGreeting.Text = "Time to wind down or catch up on sports!";
        }
        else
        {
            TxtGreeting.Text = $"Still up, {name}? 🌙";
            TxtSubGreeting.Text = "Remember to get some good rest soon!";
        }
    }

    private void UpdateTimeOfDay(bool animate)
    {
        var hour = DateTime.Now.Hour;
        TimeOfDayPeriod newPeriod;

        if (hour >= 5 && hour < 12)
            newPeriod = TimeOfDayPeriod.Morning;
        else if (hour >= 12 && hour < 18)
            newPeriod = TimeOfDayPeriod.Day;
        else if (hour >= 18 && hour < 21)
            newPeriod = TimeOfDayPeriod.Evening;
        else
            newPeriod = TimeOfDayPeriod.Night;

        _currentPeriod = newPeriod;

        double targetMorning = (newPeriod == TimeOfDayPeriod.Morning) ? 1.0 : 0.0;
        double targetDay = (newPeriod == TimeOfDayPeriod.Day) ? 1.0 : 0.0;
        double targetEvening = (newPeriod == TimeOfDayPeriod.Evening) ? 1.0 : 0.0;
        double targetNight = (newPeriod == TimeOfDayPeriod.Night) ? 1.0 : 0.0;

        if (animate)
        {
            var duration = TimeSpan.FromMilliseconds(800);
            ImgSceneryMorning.BeginAnimation(OpacityProperty, new DoubleAnimation(targetMorning, duration));
            ImgSceneryDay.BeginAnimation(OpacityProperty, new DoubleAnimation(targetDay, duration));
            ImgSceneryEvening.BeginAnimation(OpacityProperty, new DoubleAnimation(targetEvening, duration));
            ImgSceneryNight.BeginAnimation(OpacityProperty, new DoubleAnimation(targetNight, duration));
        }
        else
        {
            ImgSceneryMorning.Opacity = targetMorning;
            ImgSceneryDay.Opacity = targetDay;
            ImgSceneryEvening.Opacity = targetEvening;
            ImgSceneryNight.Opacity = targetNight;
        }

        // Ambient elements visibility
        ImgBird1.Opacity = (newPeriod == TimeOfDayPeriod.Morning) ? 0.9 : 0.0;
        ImgBird2.Opacity = (newPeriod == TimeOfDayPeriod.Morning) ? 0.8 : 0.0;

        ImgCloud1.Opacity = (newPeriod == TimeOfDayPeriod.Day) ? 0.9 : 0.0;
        ImgCloud2.Opacity = (newPeriod == TimeOfDayPeriod.Day) ? 0.8 : 0.0;

        PnlLanternGlow.Opacity = (newPeriod == TimeOfDayPeriod.Evening || newPeriod == TimeOfDayPeriod.Night) ? 0.75 : 0.0;

        ImgStar1.Opacity = (newPeriod == TimeOfDayPeriod.Night) ? 0.9 : 0.0;
        ImgStar2.Opacity = (newPeriod == TimeOfDayPeriod.Night) ? 0.85 : 0.0;
        ImgFirefly1.Opacity = (newPeriod == TimeOfDayPeriod.Night) ? 0.9 : 0.0;
        ImgFirefly2.Opacity = (newPeriod == TimeOfDayPeriod.Night) ? 0.85 : 0.0;
    }

    private void AmbientTimer_Tick(object? sender, EventArgs e)
    {
        _ambientFrameCount++;
        _fireflyTick += 0.05;

        double canvasWidth = ActualWidth > 0 ? ActualWidth : 680;

        // 1. Morning Birds Animation
        if (_currentPeriod == TimeOfDayPeriod.Morning)
        {
            _bird1X += 2.2;
            _bird2X += 2.0;

            if (_bird1X > canvasWidth + 50) _bird1X = -60;
            if (_bird2X > canvasWidth + 50) _bird2X = -100;

            System.Windows.Controls.Canvas.SetLeft(ImgBird1, _bird1X);
            System.Windows.Controls.Canvas.SetLeft(ImgBird2, _bird2X);

            // Flap wings every 4 ticks
            int flapFrame = (_ambientFrameCount / 4) % 2;
            if (_birdFrames[flapFrame] != null)
            {
                ImgBird1.Source = _birdFrames[flapFrame];
                ImgBird2.Source = _birdFrames[(flapFrame + 1) % 2];
            }
        }

        // 2. Day Clouds Drift
        if (_currentPeriod == TimeOfDayPeriod.Day)
        {
            _cloud1X += 0.35;
            _cloud2X += 0.25;

            if (_cloud1X > canvasWidth + 80) _cloud1X = -100;
            if (_cloud2X > canvasWidth + 80) _cloud2X = -120;

            System.Windows.Controls.Canvas.SetLeft(ImgCloud1, _cloud1X);
            System.Windows.Controls.Canvas.SetLeft(ImgCloud2, _cloud2X);
        }

        // 3. Evening / Night Lantern Flicker
        if (_currentPeriod == TimeOfDayPeriod.Evening || _currentPeriod == TimeOfDayPeriod.Night)
        {
            double pulse = 0.65 + Math.Sin(_ambientFrameCount * 0.15) * 0.15 + (new Random().NextDouble() * 0.08);
            PnlLanternGlow.Opacity = Math.Clamp(pulse, 0.4, 0.95);
        }

        // 4. Night Fireflies & Stars Twinkle
        if (_currentPeriod == TimeOfDayPeriod.Night)
        {
            // Fireflies floating in gentle sine waves
            double ff1X = 140 + Math.Sin(_fireflyTick * 1.2) * 35;
            double ff1Y = 320 + Math.Cos(_fireflyTick * 0.8) * 18;
            double ff2X = 380 + Math.Cos(_fireflyTick * 1.0) * 40;
            double ff2Y = 300 + Math.Sin(_fireflyTick * 1.3) * 22;

            System.Windows.Controls.Canvas.SetLeft(ImgFirefly1, ff1X);
            System.Windows.Controls.Canvas.SetTop(ImgFirefly1, ff1Y);
            System.Windows.Controls.Canvas.SetLeft(ImgFirefly2, ff2X);
            System.Windows.Controls.Canvas.SetTop(ImgFirefly2, ff2Y);

            // Twinkle stars every 8 ticks
            int starFrame = (_ambientFrameCount / 8) % 2;
            if (_starFrames[starFrame] != null)
            {
                ImgStar1.Source = _starFrames[starFrame];
                ImgStar2.Source = _starFrames[(starFrame + 1) % 2];
            }
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
