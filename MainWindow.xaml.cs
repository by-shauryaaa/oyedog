using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using PixelDogReminders.Services;
using PixelDogReminders.Views.Tabs;
using WpfApp = System.Windows.Application;

namespace PixelDogReminders;

public partial class MainWindow : Window
{
    private readonly PersistenceService _persistence;
    private readonly SportsDataService _sportsService;
    private readonly PopupService _popupService;
    private readonly WalkInService _walkInService;
    private readonly ReminderScheduler _scheduler;

    private readonly HomeTab _homeTab;
    private readonly RemindersTab _remindersTab;
    private readonly MatchesTab _matchesTab;
    private readonly SettingsTab _settingsTab;

    private Forms.NotifyIcon? _notifyIcon;
    private Forms.ToolStripMenuItem? _pauseMenuItem;
    private bool _isExiting = false;

    public MainWindow(bool isStartupLaunch = false)
    {
        InitializeComponent();

        // 1. Initialize Services
        _persistence = new PersistenceService();
        _sportsService = new SportsDataService(_persistence);
        _popupService = new PopupService(_persistence);
        _walkInService = new WalkInService(_persistence);
        _scheduler = new ReminderScheduler(_persistence, _popupService, _sportsService);

        // 2. Initialize Tabs
        _homeTab = new HomeTab(_persistence, _popupService, _walkInService);
        _remindersTab = new RemindersTab(_persistence);
        _matchesTab = new MatchesTab(_persistence, _sportsService, _scheduler);
        _settingsTab = new SettingsTab(_persistence, _popupService);

        HomeContent.Content = _homeTab;
        RemindersContent.Content = _remindersTab;
        MatchesContent.Content = _matchesTab;
        SettingsContent.Content = _settingsTab;

        // Navigation from promo card to matches tab (index 2)
        _remindersTab.NavigateToMatchesRequested += (s, e) =>
        {
            MainTabControl.SelectedIndex = 2;
        };

        // 3. Setup Tray NotifyIcon
        SetupSystemTray();

        // 4. Ensure registered as Windows Startup App by default
        ApplyWindowsStartupSetting();

        // 5. Start Scheduler
        _scheduler.Start();

        // 6. Handle startup launch mode vs normal launch mode
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
                        key.SetValue("PixelDogReminders", $"\"{exePath}\" --startup");
                    }
                    else
                    {
                        key.DeleteValue("PixelDogReminders", false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Startup registration notice: {ex.Message}");
        }
    }

    private void SetupSystemTray()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Oye Dog",
            Visible = true
        };

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

        // Context Menu
        var contextMenu = new Forms.ContextMenuStrip();

        var openItem = new Forms.ToolStripMenuItem("Open Oye Dog", null, (s, e) => ShowAndRestore());
        openItem.Font = new Font(openItem.Font, System.Drawing.FontStyle.Bold);

        _pauseMenuItem = new Forms.ToolStripMenuItem("Pause Reminders", null, (s, e) =>
        {
            _scheduler.IsPaused = !_scheduler.IsPaused;
            if (_pauseMenuItem != null)
            {
                _pauseMenuItem.Checked = _scheduler.IsPaused;
            }
        });

        var testPopupItem = new Forms.ToolStripMenuItem("Test Dog Popup", null, (s, e) =>
        {
            _popupService.ShowPopup("Pixel Companion", "Woof! Running from the system tray :)", Models.SpriteVariant.Idle);
        });

        var testWalkInItem = new Forms.ToolStripMenuItem("Walk Across Screen 🐾", null, (s, e) =>
        {
            _walkInService.CheckAndTriggerWalkIn(force: true);
        });

        var quitItem = new Forms.ToolStripMenuItem("Quit", null, (s, e) => ExitApplication());

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(_pauseMenuItem);
        contextMenu.Items.Add(testPopupItem);
        contextMenu.Items.Add(testWalkInItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(quitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowAndRestore();
    }

    public void ShowAndRestore()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        _homeTab.UpdateGreeting();
        _remindersTab.LoadReminders();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            Hide();
            _notifyIcon?.ShowBalloonTip(2000, "Oye Dog", "Still running in your system tray! Reminders & morning greetings will continue.", Forms.ToolTipIcon.Info);
        }
        else
        {
            base.OnClosing(e);
        }
    }

    public void ExitApplication()
    {
        _isExiting = true;
        _scheduler.Stop();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        WpfApp.Current.Shutdown();
    }
}
