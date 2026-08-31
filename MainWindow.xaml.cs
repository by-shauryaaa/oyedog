using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
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
    private Forms.ContextMenuStrip? _trayMenu;
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

        // Wire DisplayName changes
        _settingsTab.DisplayNameChanged += (s, newName) =>
        {
            ApplyDisplayName();
            _homeTab.UpdateGreeting();
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

        // 5. Setup Tray NotifyIcon & Unified Schedule Menu
        SetupSystemTray();

        // 6. Ensure registered as Windows Startup App by default
        ApplyWindowsStartupSetting();

        // 7. Start Scheduler
        _scheduler.Start();

        // 8. Restore Sidebar State & Set Display Name
        var (settings, _) = _persistence.LoadData();
        ApplySidebarState(settings.SidebarCollapsed, animate: false);
        ApplyDisplayName();
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

    public void ApplyDisplayName()
    {
        var (settings, _) = _persistence.LoadData();
        string name = string.IsNullOrWhiteSpace(settings.DisplayName) ? "Abhishek" : settings.DisplayName.Trim();

        Title = $"Oye Dog — {name}'s Companion";
        if (_notifyIcon != null)
        {
            _notifyIcon.Text = $"Oye Dog — {name}'s Companion";
        }
    }

    public void NavigateTo(NavTarget target)
    {
        bool isFreshHomeNav = (_activeNav != NavTarget.Home && target == NavTarget.Home);
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

        if (isFreshHomeNav)
        {
            _homeTab.TriggerEntranceWalk();
        }
    }

    private void UpdateNavHighlights()
    {
        BtnNavHomeCollapsed.Tag = _activeNav == NavTarget.Home ? "Active" : null;
        BtnNavReminders.Tag = _activeNav == NavTarget.Reminders ? "Active" : null;
        BtnNavMatches.Tag = _activeNav == NavTarget.Matches ? "Active" : null;
        BtnNavTimetable.Tag = _activeNav == NavTarget.Timetable ? "Active" : null;
        BtnNavSettings.Tag = _activeNav == NavTarget.Settings ? "Active" : null;

        // Companion habitat highlight for Home
        if (_activeNav == NavTarget.Home)
        {
            PnlDogBadge.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(255, 215, 0));  // #FFD700
            PnlDogBadge.Background = new WpfSolidColorBrush(WpfColor.FromRgb(61, 39, 29));    // #3D271D
            DogFloorShadow.Fill = new WpfSolidColorBrush(WpfColor.FromRgb(110, 65, 30));      // Warm golden floor glow
        }
        else
        {
            PnlDogBadge.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(90, 62, 43));   // #5A3E2B
            PnlDogBadge.Background = new WpfSolidColorBrush(WpfColor.FromRgb(42, 27, 20));    // #2A1B14
            DogFloorShadow.Fill = new WpfSolidColorBrush(WpfColor.FromRgb(30, 18, 10));       // Subtle soft shadow
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
        var (settings, _) = _persistence.LoadData();
        string name = string.IsNullOrWhiteSpace(settings.DisplayName) ? "Abhishek" : settings.DisplayName.Trim();

        _notifyIcon = new Forms.NotifyIcon
        {
            Visible = true,
            Text = $"Oye Dog — {name}'s Companion"
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

        _trayMenu = new Forms.ContextMenuStrip();
        _trayMenu.Opening += (s, e) => RebuildTrayScheduleMenu();

        // Left-Click & Right-Click both trigger the unified schedule menu
        _notifyIcon.MouseUp += (s, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                RebuildTrayScheduleMenu();
                _trayMenu.Show(Forms.Cursor.Position);
            }
        };

        _notifyIcon.ContextMenuStrip = _trayMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowAndRestore();
    }

    private class TrayScheduleItem
    {
        public TimeSpan StartTime { get; set; }
        public string Text { get; set; } = "";
        public bool IsInProgress { get; set; }
    }

    private void RebuildTrayScheduleMenu()
    {
        if (_trayMenu == null) return;

        _trayMenu.Items.Clear();

        var today = DateTime.Today;
        var nowTime = DateTime.Now.TimeOfDay;

        // 1. Header Item
        var headerItem = new Forms.ToolStripMenuItem($"📅 Today's Schedule — {today:ddd, MMM d}")
        {
            Enabled = false,
            Font = new Font(_trayMenu.Font, System.Drawing.FontStyle.Bold)
        };
        _trayMenu.Items.Add(headerItem);
        _trayMenu.Items.Add(new Forms.ToolStripSeparator());

        var scheduleItems = new List<TrayScheduleItem>();

        // 2. Load today's classes
        try
        {
            var subjects = _persistence.LoadSubjects();
            foreach (var subject in subjects)
            {
                foreach (var slot in subject.Slots.Where(s => s.DayOfWeek == today.DayOfWeek))
                {
                    var endTime = slot.GetEndTime(subject.DurationMinutes);
                    bool inProgress = (nowTime >= slot.StartTime && nowTime < endTime);

                    string startStr = DateTime.Today.Add(slot.StartTime).ToString("hh:mm tt");
                    string endStr = DateTime.Today.Add(endTime).ToString("hh:mm tt");
                    string roomStr = string.IsNullOrWhiteSpace(subject.Room) ? "" : $" ({subject.Room})";

                    string prefix = inProgress ? "▶ " : "  ";
                    string suffix = inProgress ? " [NOW]" : "";
                    string itemText = $"{prefix}{startStr} - {endStr} • {subject.Name}{roomStr}{suffix}";

                    scheduleItems.Add(new TrayScheduleItem
                    {
                        StartTime = slot.StartTime,
                        Text = itemText,
                        IsInProgress = inProgress
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed loading timetable for tray menu: {ex.Message}");
        }

        // 3. Load today's sports matches
        try
        {
            var sportsItems = _sportsService.GetCachedSchedule();
            foreach (var sItem in sportsItems)
            {
                if (sItem.LocalDateTime.Date == today)
                {
                    var startTime = sItem.LocalDateTime.TimeOfDay;
                    var endTime = startTime.Add(TimeSpan.FromHours(sItem.IsF1 ? 1.5 : 2.0));
                    bool inProgress = (nowTime >= startTime && nowTime < endTime);

                    string prefix = inProgress ? "▶ " : "  ";
                    string suffix = inProgress ? " [NOW]" : "";
                    string icon = sItem.IsF1 ? "🏎️" : "⚽";

                    scheduleItems.Add(new TrayScheduleItem
                    {
                        StartTime = startTime,
                        Text = $"{prefix}{sItem.FormattedTime} • {icon} {sItem.Title} ({sItem.Subtitle}){suffix}",
                        IsInProgress = inProgress
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed loading sports for tray menu: {ex.Message}");
        }

        // 4. Sort and populate schedule items
        if (scheduleItems.Count > 0)
        {
            foreach (var item in scheduleItems.OrderBy(i => i.StartTime))
            {
                var menuItem = new Forms.ToolStripMenuItem(item.Text)
                {
                    Enabled = false
                };
                if (item.IsInProgress)
                {
                    menuItem.Font = new Font(_trayMenu.Font, System.Drawing.FontStyle.Bold);
                }
                _trayMenu.Items.Add(menuItem);
            }
        }
        else
        {
            var emptyItem = new Forms.ToolStripMenuItem("  ✨ Nothing scheduled today")
            {
                Enabled = false
            };
            _trayMenu.Items.Add(emptyItem);
        }

        // 5. Standard Action Items
        _trayMenu.Items.Add(new Forms.ToolStripSeparator());

        var openItem = new Forms.ToolStripMenuItem("Open Oye Dog");
        openItem.Font = new Font(_trayMenu.Font, System.Drawing.FontStyle.Bold);
        openItem.Click += (s, e) => ShowAndRestore();

        var triggerWalkInItem = new Forms.ToolStripMenuItem("Dogu, Walk In! 🐾");
        triggerWalkInItem.Click += (s, e) =>
        {
            _walkInService.CheckAndTriggerWalkIn(force: true);
        };

        _pauseMenuItem = new Forms.ToolStripMenuItem(_scheduler.IsPaused ? "Resume Reminders" : "Pause Reminders");
        _pauseMenuItem.Click += (s, e) =>
        {
            _scheduler.IsPaused = !_scheduler.IsPaused;
            _pauseMenuItem.Text = _scheduler.IsPaused ? "Resume Reminders" : "Pause Reminders";
        };

        var exitItem = new Forms.ToolStripMenuItem("Exit Oye Dog");
        exitItem.Click += (s, e) =>
        {
            _isExiting = true;
            _notifyIcon!.Visible = false;
            _notifyIcon.Dispose();
            WpfApp.Current.Shutdown();
        };

        _trayMenu.Items.Add(openItem);
        _trayMenu.Items.Add(triggerWalkInItem);
        _trayMenu.Items.Add(new Forms.ToolStripSeparator());
        _trayMenu.Items.Add(_pauseMenuItem);
        _trayMenu.Items.Add(new Forms.ToolStripSeparator());
        _trayMenu.Items.Add(exitItem);
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
