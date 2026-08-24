using System.Collections.ObjectModel;
using System.Windows;
using PixelDogReminders.Models;
using PixelDogReminders.Services;
using WpfControl = System.Windows.Controls.UserControl;

namespace PixelDogReminders.Views.Tabs;

public partial class MatchesTab : WpfControl
{
    private readonly PersistenceService _persistence;
    private readonly SportsDataService _sportsService;
    private readonly ReminderScheduler _scheduler;
    private readonly ObservableCollection<ScheduleItem> _scheduleItems = new();
    private bool _isInitializing = true;

    public MatchesTab(PersistenceService persistence, SportsDataService sportsService, ReminderScheduler scheduler)
    {
        _persistence = persistence;
        _sportsService = sportsService;
        _scheduler = scheduler;

        InitializeComponent(); // safe: _persistence already set before this

        ItemsSchedule.ItemsSource = _scheduleItems;

        var (settings, _) = _persistence.LoadData();

        // Set checkbox without triggering handler
        _isInitializing = true;
        ChkMasterToggle.IsChecked = settings.MatchRemindersEnabled;
        _isInitializing = false;

        Loaded += async (s, e) => await LoadScheduleAsync(force: false);
    }

    public async Task LoadScheduleAsync(bool force = false)
    {
        TxtStatus.Visibility = Visibility.Visible;
        TxtStatus.Text = "Updating fixtures & race sessions...";
        BtnRefresh.IsEnabled = false;

        try
        {
            var (settings, _) = _persistence.LoadData();
            var items = await _sportsService.GetUpcomingScheduleAsync(settings.FootballDataApiKey, force);

            _scheduleItems.Clear();
            foreach (var item in items)
            {
                _scheduleItems.Add(item);
            }

            PnlEmptyState.Visibility = _scheduleItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            TxtStatus.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Failed to update: {ex.Message}";
        }
        finally
        {
            BtnRefresh.IsEnabled = true;
        }
    }

    private void ChkMasterToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var (settings, reminders) = _persistence.LoadData();
        settings.MatchRemindersEnabled = ChkMasterToggle.IsChecked == true;
        _persistence.SaveData(settings, reminders);
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadScheduleAsync(force: true);
        await _scheduler.RefreshSportsDataAsync(force: true);
    }
}
