using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using PixelDogReminders.Models;
using PixelDogReminders.Services;
using PixelDogReminders.Views.Tabs;
using WpfApp = System.Windows.Application;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace PixelDogReminders;

public enum NavTarget
{
    Home,
    Reminders,
    Matches,
    Timetable,
    Settings
}

public partial class MainWindow : Window
{
    private readonly PersistenceService _persistence;
    private readonly SportsDataService _sportsService;
    private readonly PopupService _popupService;
    private readonly FlagReminderService _flagService;
    private readonly WalkInService _walkInService;
    private readonly ReminderScheduler _scheduler;

    private readonly HomeTab _homeTab;
    private readonly RemindersTab _remindersTab;
    private readonly MatchesTab _matchesTab;
    private readonly TimetableTab _timetableTab;
    private readonly SettingsTab _settingsTab;

    private Forms.NotifyIcon? _notifyIcon;
    private Forms.ToolStripMenuItem? _pauseMenuItem;
    private bool _isExiting = false;

    private NavTarget _activeNav = NavTarget.Home;

    private readonly DispatcherTimer _sidebarSpriteTimer;
    private readonly DispatcherTimer _sidebarClockTimer;
    private readonly BitmapImage[] _sidebarDogFrames = new BitmapImage[5];
    private int _sidebarFrameIndex = 0;

    public MainWindow(bool isStartupLaunch = false)
    {
        InitializeComponent();

        // 1. Initialize Services
        _persistence = new PersistenceService();
        _sportsService = new SportsDataService(_persistence);
        _popupService = new PopupService(_persistence);
        _flagService = new FlagReminderService(_persistence);
        _walkInService = new WalkInService(_persistence);
        _scheduler = new ReminderScheduler(_persistence, _popupService, _sportsService, _flagService);

        // 2. Initialize Views
        _homeTab = new HomeTab(_persistence, _popupService, _walkInService);
        _remindersTab = new RemindersTab(_persistence);
        _matchesTab = new MatchesTab(_persistence, _sportsService, _scheduler);
        _timetableTab = new TimetableTab(_persistence);
        _settingsTab = new SettingsTab(_persistence, _popupService, _flagService);

        // Navigation from promo card to matches
        _remindersTab.NavigateToMatchesRequested += (s, e) =>
        {
            NavigateTo(NavTarget.Matches);
        };

        // 3. Preload sidebar dog sprite frames
        for (int i = 0; i < 5; i++)
        {
            try
            {
                _sidebarDogFrames[i] = new BitmapImage(new Uri($"pack://application:,,,/Assets/Sprites/idle_{i}.png", UriKind.Absolute));
            }
            catch
            {
                // Fallback
            }
        }

        // 4. Setup Sidebar Timers
        _sidebarSpriteTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(125) // 8 FPS
        };
        _sidebarSpriteTimer.Tick += (s, e) =>
        {
            if (_sidebarDogFrames.Length > 0 && _sidebarDogFrames[0] != null)
            {
                _sidebarFrameIndex = (_sidebarFrameIndex + 1) % 5;
                if (_sidebarDogFrames[_sidebarFrameIndex] != null)
                {
                    SidebarDogSprite.Source = _sidebarDogFrames[_sidebarFrameIndex];
                }
            }
        };
        _sidebarSpriteTimer.Start();

        _sidebarClockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _sidebarClockTimer.Tick += (s, e) =>
        {
            SidebarClock.Text = DateTime.Now.ToString("hh:mm tt");
        };
        _sidebarClockTimer.Start();
        SidebarClock.Text = DateTime.Now.ToString("hh:mm tt");

        // 5. Setup Tray NotifyIcon
        SetupSystemTray();

        // 6. Ensure registered as Windows Startup App by default
        ApplyWindowsStartupSetting();

        // 7. Start Scheduler
        _scheduler.Start();

        // 8. Restore Sidebar State & Navigate to Home
        var (settings, _) = _persistence.LoadData();
        ApplySidebarState(settings.SidebarCollapsed, animate: false);
        NavigateTo(NavTarget.Home);

        // 9. Handle startup launch mode vs normal launch mode
        if (isStartupLaunch)
        {
            // Start hidden in tray
            WindowState = WindowState.Minimized;
            Hide();
        }

        Loaded += (s, e) =>
        {
            // Check walk-in greeting on launch (will trigger if morning and hasn't run today)
            _walkInService.CheckAndTriggerWalkIn(force: false);
        };
    }

    public void NavigateTo(NavTarget target)
    {
        _activeNav = target;

        MainContent.Content = target switch
        {
            NavTarget.Home => _homeTab,
            NavTarget.Reminders => _remindersTab,
            NavTarget.Matches => _matchesTab,
            NavTarget.Timetable => _timetableTab,
            NavTarget.Settings => _settingsTab,
            _ => _homeTab
        };

        UpdateNavHighlights();
    }

    private void UpdateNavHighlights()
    {
        BtnNavHomeCollapsed.Tag = _activeNav == NavTarget.Home ? "Active" : null;
        BtnNavReminders.Tag = _activeNav == NavTarget.Reminders ? "Active" : null;
        BtnNavMatches.Tag = _activeNav == NavTarget.Matches ? "Active" : null;
        BtnNavTimetable.Tag = _activeNav == NavTarget.Timetable ? "Active" : null;
        BtnNavSettings.Tag = _activeNav == NavTarget.Settings ? "Active" : null;

        // Dog mini widget border highlight for Home
        if (_activeNav == NavTarget.Home)
        {
            PnlSidebarDogWidget.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(255, 215, 0)); // #FFD700
            PnlSidebarDogWidget.Background = new WpfSolidColorBrush(WpfColor.FromRgb(61, 39, 29));   // #3D271D
        }
        else
        {
            PnlSidebarDogWidget.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(90, 62, 43));  // #5A3E2B
            PnlSidebarDogWidget.Background = new WpfSolidColorBrush(WpfColor.FromRgb(36, 22, 14));   // #24160E
        }
    }

    private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        var (settings, reminders) = _persistence.LoadData();
        settings.SidebarCollapsed = !settings.SidebarCollapsed;
        _persistence.SaveData(settings, reminders);

        ApplySidebarState(settings.SidebarCollapsed, animate: true);
    }

    private void ApplySidebarState(bool collapsed, bool animate)
    {
        double targetWidth = collapsed ? 54 : 200;

        if (animate)
        {
            var anim = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            SidebarBorder.BeginAnimation(WidthProperty, anim);
        }
        else
        {
            SidebarBorder.Width = targetWidth;
        }

        BtnToggleSidebar.Content = collapsed ? "▶" : "◀";
        BtnToggleSidebar.ToolTip = collapsed ? "Expand sidebar" : "Collapse sidebar";

        PnlBranding.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        TxtLabelReminders.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        TxtLabelMatches.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        TxtLabelTimetable.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        TxtLabelSettings.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;

        BtnNavHomeCollapsed.Visibility = collapsed ? Visibility.Visible : Visibility.Collapsed;
        PnlSidebarDogWidget.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        PnlSidebarTrayChip.Visibility = collapsed ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnNavHome_Click(object sender, RoutedEventArgs e) => NavigateTo(NavTarget.Home);
    private void BtnNavReminders_Click(object sender, RoutedEventArgs e) => NavigateTo(NavTarget.Reminders);
    private void BtnNavMatches_Click(object sender, RoutedEventArgs e) => NavigateTo(NavTarget.Matches);
    private void BtnNavTimetable_Click(object sender, RoutedEventArgs e) => NavigateTo(NavTarget.Timetable);
    private void BtnNavSettings_Click(object sender, RoutedEventArgs e) => NavigateTo(NavTarget.Settings);
    private void PnlSidebarDogWidget_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) => NavigateTo(NavTarget.Home);

    public void ApplyWindowsStartupSetting()
    {
        try
        {
            var (settings, _) = _persistence.LoadData();
            var exePath = Environment.ProcessPath;

            if (!string.IsNullOrEmpty(exePath) && exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
                if (key != null)
                {
                    if (settings.LaunchOnStartup)
                    {
                        key.SetValue("OyeDog", $"\"{exePath}\" --startup");
                    }
                    else
                    {
                        key.DeleteValue("OyeDog", false);
                    }

                    // Clean up legacy key if present
                    key.DeleteValue("PixelDogReminders", false);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update registry startup run key: {ex.Message}");
        }
    }

    private void SetupSystemTray()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Visible = true,
            Text = "Oye Dog — Abhishek's Companion"
        };

        // Try load app icon
        try
        {
            var iconStream = WpfApp.GetResourceStream(new Uri("pack://application:,,,/Assets/Sprites/idle_0.png"))?.Stream;
            if (iconStream != null)
            {
                using var bmp = new Bitmap(iconStream);
                var hIcon = bmp.GetHicon();
                _notifyIcon.Icon = System.Drawing.Icon.FromHandle(hIcon);
            }
            else
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
        }
        catch
        {
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        var contextMenu = new Forms.ContextMenuStrip();

        var openItem = new Forms.ToolStripMenuItem("Open Oye Dog");
        openItem.Font = new Font(openItem.Font, System.Drawing.FontStyle.Bold);
        openItem.Click += (s, e) => ShowAndRestore();

        var triggerWalkInItem = new Forms.ToolStripMenuItem("Dogu, Walk In! 🐾");
        triggerWalkInItem.Click += (s, e) =>
        {
            _walkInService.CheckAndTriggerWalkIn(force: true);
        };

        _pauseMenuItem = new Forms.ToolStripMenuItem("Pause Reminders");
        _pauseMenuItem.Click += (s, e) =>
        {
            _scheduler.IsPaused = !_scheduler.IsPaused;
            _pauseMenuItem.Text = _scheduler.IsPaused ? "Resume Reminders" : "Pause Reminders";
        };

        var exitItem = new Forms.ToolStripMenuItem("Exit Oye Dog");
        exitItem.Click += (s, e) =>
        {
            _isExiting = true;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            WpfApp.Current.Shutdown();
        };

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(triggerWalkInItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(_pauseMenuItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowAndRestore();
    }

    public void ShowAndRestore()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
