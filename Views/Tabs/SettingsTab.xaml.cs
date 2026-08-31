using System;
using System.Windows;
using System.Windows.Threading;
using PixelDogReminders.Models;
using PixelDogReminders.Services;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using WpfControl = System.Windows.Controls.UserControl;
using WpfMessageBox = System.Windows.MessageBox;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace PixelDogReminders.Views.Tabs;

public partial class SettingsTab : WpfControl
{
    private readonly PersistenceService _persistence;
    private readonly PopupService _popupService;
    private readonly FlagReminderService? _flagService;
    private bool _isInitializing = true;

    public SettingsTab(PersistenceService persistence, PopupService popupService, FlagReminderService? flagService = null)
    {
        _persistence = persistence;
        _popupService = popupService;
        _flagService = flagService;
        _isInitializing = true;

        InitializeComponent();

        LoadSettings();
        _isInitializing = false;
    }

    private void LoadSettings()
    {
        _isInitializing = true;
        var (settings, _) = _persistence.LoadData();

        // 1. Position
        switch (settings.Position)
        {
            case PopupPosition.TopLeft: RbTopLeft.IsChecked = true; break;
            case PopupPosition.TopCenter: RbTopCenter.IsChecked = true; break;
            case PopupPosition.TopRight: RbTopRight.IsChecked = true; break;
            case PopupPosition.BottomLeft: RbBottomLeft.IsChecked = true; break;
            case PopupPosition.BottomCenter: RbBottomCenter.IsChecked = true; break;
            case PopupPosition.BottomRight:
            default:
                RbBottomRight.IsChecked = true;
                break;
        }

        // 2. Snooze
        foreach (WpfComboBoxItem item in CmbSnooze.Items)
        {
            if (item.Tag is string tag && int.TryParse(tag, out var mins) && mins == settings.SnoozeDurationMinutes)
            {
                CmbSnooze.SelectedItem = item;
                break;
            }
        }

        // 3. Launch on Startup & Startup Greeting
        ChkLaunchOnStartup.IsChecked = settings.LaunchOnStartup;
        ChkStartupGreeting.IsChecked = settings.StartupGreetingEnabled;

        // 4. API Key
        TxtApiKey.Text = settings.FootballDataApiKey;

        // 5. Timetable Settings
        ChkTimetableReminders.IsChecked = settings.TimetableRemindersEnabled;

        foreach (WpfComboBoxItem item in CmbDefaultDuration.Items)
        {
            if (item.Tag is string tag && int.TryParse(tag, out var dur) && dur == settings.DefaultClassDurationMinutes)
            {
                CmbDefaultDuration.SelectedItem = item;
                break;
            }
        }

        foreach (WpfComboBoxItem item in CmbLeadTime.Items)
        {
            if (item.Tag is string tag && int.TryParse(tag, out var lead) && lead == settings.LeadTimeMinutes)
            {
                CmbLeadTime.SelectedItem = item;
                break;
            }
        }

        foreach (WpfComboBoxItem item in CmbFlagPosition.Items)
        {
            if (item.Tag is string tag && Enum.TryParse<FlagPosition>(tag, true, out var pos) && pos == settings.ClassFlagPosition)
            {
                CmbFlagPosition.SelectedItem = item;
                break;
            }
        }
    }

    private void Position_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        PopupPosition pos = PopupPosition.BottomRight;
        if (RbTopLeft.IsChecked == true) pos = PopupPosition.TopLeft;
        else if (RbTopCenter.IsChecked == true) pos = PopupPosition.TopCenter;
        else if (RbTopRight.IsChecked == true) pos = PopupPosition.TopRight;
        else if (RbBottomLeft.IsChecked == true) pos = PopupPosition.BottomLeft;
        else if (RbBottomCenter.IsChecked == true) pos = PopupPosition.BottomCenter;
        else if (RbBottomRight.IsChecked == true) pos = PopupPosition.BottomRight;

        var (settings, reminders) = _persistence.LoadData();
        settings.Position = pos;
        _persistence.SaveData(settings, reminders);
    }

    private void CmbSnooze_SelectionChanged(object sender, WpfSelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (CmbSnooze.SelectedItem is WpfComboBoxItem selected && selected.Tag is string tag && int.TryParse(tag, out var mins))
        {
            var (settings, reminders) = _persistence.LoadData();
            settings.SnoozeDurationMinutes = mins;
            _persistence.SaveData(settings, reminders);
        }
    }

    private void ChkLaunchOnStartup_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var (settings, reminders) = _persistence.LoadData();
        settings.LaunchOnStartup = ChkLaunchOnStartup.IsChecked == true;
        _persistence.SaveData(settings, reminders);

        if (System.Windows.Application.Current.MainWindow is MainWindow mainWin)
        {
            mainWin.ApplyWindowsStartupSetting();
        }
    }

    private void ChkStartupGreeting_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var (settings, reminders) = _persistence.LoadData();
        settings.StartupGreetingEnabled = ChkStartupGreeting.IsChecked == true;
        _persistence.SaveData(settings, reminders);
    }

    private void ChkTimetableReminders_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var (settings, reminders) = _persistence.LoadData();
        settings.TimetableRemindersEnabled = ChkTimetableReminders.IsChecked == true;
        _persistence.SaveData(settings, reminders);
    }

    private void CmbDefaultDuration_SelectionChanged(object sender, WpfSelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (CmbDefaultDuration.SelectedItem is WpfComboBoxItem selected && selected.Tag is string tag && int.TryParse(tag, out var mins))
        {
            var (settings, reminders) = _persistence.LoadData();
            settings.DefaultClassDurationMinutes = mins;
            _persistence.SaveData(settings, reminders);
        }
    }

    private void CmbLeadTime_SelectionChanged(object sender, WpfSelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (CmbLeadTime.SelectedItem is WpfComboBoxItem selected && selected.Tag is string tag && int.TryParse(tag, out var mins))
        {
            var (settings, reminders) = _persistence.LoadData();
            settings.LeadTimeMinutes = mins;
            _persistence.SaveData(settings, reminders);
        }
    }

    private void CmbFlagPosition_SelectionChanged(object sender, WpfSelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (CmbFlagPosition.SelectedItem is WpfComboBoxItem selected && selected.Tag is string tag && Enum.TryParse<FlagPosition>(tag, true, out var pos))
        {
            var (settings, reminders) = _persistence.LoadData();
            settings.ClassFlagPosition = pos;
            _persistence.SaveData(settings, reminders);
        }
    }

    private void BtnSaveApiKey_Click(object sender, RoutedEventArgs e)
    {
        var (settings, reminders) = _persistence.LoadData();
        settings.FootballDataApiKey = TxtApiKey.Text.Trim();
        _persistence.SaveData(settings, reminders);
        WpfMessageBox.Show("Football API key saved! Matches tab will refresh fixtures automatically.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnTestFlag_Click(object sender, RoutedEventArgs e)
    {
        var subjects = _persistence.LoadSubjects();
        string testName = subjects.Count > 0 ? subjects[0].Name : "Data Structures";
        string testColor = subjects.Count > 0 ? subjects[0].Color : "#64B5F6";

        var (settings, _) = _persistence.LoadData();
        string countdown = $"in {settings.LeadTimeMinutes} min";

        _flagService?.Show(testName, countdown, testColor);
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
            SpriteVariant.Barca => "Barca match today!",
            SpriteVariant.F1 => "Lights out and away we go!",
            SpriteVariant.Rest => "take a quick stretch!",
            _ => "Woof! Just checking in on you :)"
        };

        _popupService.ShowPopup("Test Companion", testMsg, selected);
    }

    public event EventHandler<string>? DisplayNameChanged;
    private int _madeForAbhishekClickCount = 0;
    private DispatcherTimer? _clickResetTimer;

    private void PnlMadeForAbhishek_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        TriggerConfettiBurst(25);

        _madeForAbhishekClickCount++;

        _clickResetTimer?.Stop();
        _clickResetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _clickResetTimer.Tick += (s, ev) =>
        {
            _clickResetTimer.Stop();
            _madeForAbhishekClickCount = 0;
        };
        _clickResetTimer.Start();

        if (_madeForAbhishekClickCount >= 4)
        {
            _clickResetTimer.Stop();
            _madeForAbhishekClickCount = 0;

            var (settings, reminders) = _persistence.LoadData();
            var dlg = new Dialogs.NameChangeDialog(settings.DisplayName)
            {
                Owner = Window.GetWindow(this)
            };

            if (dlg.ShowDialog() == true)
            {
                settings.DisplayName = dlg.ResultDisplayName;
                _persistence.SaveData(settings, reminders);

                DisplayNameChanged?.Invoke(this, settings.DisplayName);
                TriggerConfettiBurst(50); // Big celebration!
            }
        }
    }

    private void TriggerConfettiBurst(int particleCount = 25)
    {
        double width = ConfettiCanvas.ActualWidth > 0 ? ConfettiCanvas.ActualWidth : 600;
        double height = ConfettiCanvas.ActualHeight > 0 ? ConfettiCanvas.ActualHeight : 500;

        var colors = new[]
        {
            System.Windows.Media.Color.FromRgb(255, 75, 110),
            System.Windows.Media.Color.FromRgb(255, 215, 0),
            System.Windows.Media.Color.FromRgb(50, 200, 255),
            System.Windows.Media.Color.FromRgb(150, 240, 60),
            System.Windows.Media.Color.FromRgb(210, 90, 255),
            System.Windows.Media.Color.FromRgb(255, 140, 40)
        };

        var rng = new Random();

        for (int i = 0; i < particleCount; i++)
        {
            var p = new System.Windows.Shapes.Rectangle
            {
                Width = rng.Next(6, 12),
                Height = rng.Next(6, 12),
                Fill = new System.Windows.Media.SolidColorBrush(colors[rng.Next(colors.Length)]),
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
            };

            double startX = (width / 2.0) + rng.Next(-180, 180);
            double startY = height - 50 + rng.Next(-20, 20);
            double endX = startX + rng.Next(-80, 80);
            double endY = rng.Next(30, 200);

            System.Windows.Controls.Canvas.SetLeft(p, startX);
            System.Windows.Controls.Canvas.SetTop(p, startY);

            ConfettiCanvas.Children.Add(p);

            // Animate upward arc then fall
            var animY = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = startY,
                To = -20,
                Duration = TimeSpan.FromMilliseconds(rng.Next(1200, 2000)),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };

            var animFade = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = animY.Duration
            };

            animFade.Completed += (s, ev) =>
            {
                ConfettiCanvas.Children.Remove(p);
            };

            p.BeginAnimation(OpacityProperty, animFade);
            p.BeginAnimation(System.Windows.Controls.Canvas.TopProperty, animY);
        }
    }
}
