using System.Windows.Threading;
using PixelDogReminders.Models;

namespace PixelDogReminders.Services;

public class ReminderScheduler
{
    private readonly PersistenceService _persistence;
    private readonly PopupService _popupService;
    private readonly SportsDataService _sportsService;
    private readonly DispatcherTimer _timer;
    private readonly HashSet<string> _notifiedSportsEvents = new();
    private readonly List<(DateTime FireAtUtc, ReminderModel Reminder)> _snoozedReminders = new();
    private bool _isPaused = false;
    private DateTime _lastSportsRefreshUtc = DateTime.MinValue;
    private List<ScheduleItem> _cachedSchedule = new();

    public event EventHandler<bool>? PauseStateChanged;

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (_isPaused != value)
            {
                _isPaused = value;
                PauseStateChanged?.Invoke(this, _isPaused);
            }
        }
    }

    public ReminderScheduler(PersistenceService persistence, PopupService popupService, SportsDataService sportsService)
    {
        _persistence = persistence;
        _popupService = popupService;
        _sportsService = sportsService;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _timer.Tick += Timer_Tick;
    }

    public void Start()
    {
        _timer.Start();
        // Initial sports check in background
        _ = RefreshSportsDataAsync();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public async Task RefreshSportsDataAsync(bool force = false)
    {
        var (settings, _) = _persistence.LoadData();
        _cachedSchedule = await _sportsService.GetUpcomingScheduleAsync(settings.FootballDataApiKey, force);
        _lastSportsRefreshUtc = DateTime.UtcNow;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (IsPaused) return;

        var now = DateTime.Now;
        var nowUtc = DateTime.UtcNow;
        var (settings, reminders) = _persistence.LoadData();

        // 1. Process Snoozed Reminders
        for (int i = _snoozedReminders.Count - 1; i >= 0; i--)
        {
            var item = _snoozedReminders[i];
            if (nowUtc >= item.FireAtUtc)
            {
                _snoozedReminders.RemoveAt(i);
                FireReminder(item.Reminder, settings, isSnoozed: true);
            }
        }

        // 2. Process Standard Reminders
        bool stateChanged = false;
        foreach (var reminder in reminders)
        {
            if (!reminder.IsEnabled) continue;

            if (reminder.IsIntervalBased && reminder.IntervalMinutes.HasValue && reminder.IntervalMinutes.Value > 0)
            {
                var last = reminder.LastFiredTime ?? now.AddMinutes(-reminder.IntervalMinutes.Value);
                if ((now - last).TotalMinutes >= reminder.IntervalMinutes.Value)
                {
                    reminder.LastFiredTime = now;
                    stateChanged = true;
                    FireReminder(reminder, settings);
                }
            }
            else
            {
                // Fixed time slots ("HH:mm")
                var currentTimeStr = now.ToString("HH:mm");
                if (reminder.TimeSlots.Contains(currentTimeStr))
                {
                    // Check if already fired this minute
                    var alreadyFired = reminder.LastFiredTime.HasValue &&
                                       reminder.LastFiredTime.Value.Date == now.Date &&
                                       reminder.LastFiredTime.Value.Hour == now.Hour &&
                                       reminder.LastFiredTime.Value.Minute == now.Minute;

                    if (!alreadyFired)
                    {
                        reminder.LastFiredTime = now;
                        stateChanged = true;
                        FireReminder(reminder, settings);
                    }
                }
            }
        }

        if (stateChanged)
        {
            _persistence.SaveData(settings, reminders);
        }

        // 3. Process Sports Reminders
        if (settings.MatchRemindersEnabled)
        {
            CheckSportsEvents(nowUtc, settings);
        }

        // Refresh sports schedule once daily
        if ((nowUtc - _lastSportsRefreshUtc).TotalHours >= 24)
        {
            _ = RefreshSportsDataAsync();
        }
    }

    private void CheckSportsEvents(DateTime nowUtc, AppSettings settings)
    {
        foreach (var item in _cachedSchedule)
        {
            var diff = (nowUtc - item.DateTimeUtc).TotalMinutes;
            // Fire if event is starting now or started within last 5 minutes
            if (diff >= 0 && diff <= 5)
            {
                var eventKey = $"{item.Category}_{item.Title}_{item.DateTimeUtc:yyyyMMddHHmm}";
                if (!_notifiedSportsEvents.Contains(eventKey))
                {
                    _notifiedSportsEvents.Add(eventKey);
                    var variant = item.IsF1 ? SpriteVariant.F1 : SpriteVariant.Barca;
                    var title = item.Category;
                    var msg = item.IsF1 ? $"Lights out! {item.Title}" : $"Matchday! {item.Title}";

                    _popupService.ShowPopup(title, msg, variant);
                }
            }
        }
    }

    private void FireReminder(ReminderModel reminder, AppSettings settings, bool isSnoozed = false)
    {
        var title = isSnoozed ? $"{reminder.Name} (Snoozed)" : reminder.Name;
        _popupService.ShowPopup(
            title,
            reminder.Message,
            reminder.Variant,
            onSnooze: () =>
            {
                var snoozeDuration = Math.Max(1, settings.SnoozeDurationMinutes);
                _snoozedReminders.Add((DateTime.UtcNow.AddMinutes(snoozeDuration), reminder));
            },
            onOkii: () =>
            {
                // Completed/dismissed
            }
        );
    }
}
