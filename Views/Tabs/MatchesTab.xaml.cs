using System.Collections.ObjectModel;
using System.Windows;
using PixelDogReminders.Models;
using PixelDogReminders.Services;
using WpfControl = System.Windows.Controls.UserControl;
using WpfMessageBox = System.Windows.MessageBox;

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
        _isInitializing = true;

        InitializeComponent();

        ItemsSchedule.ItemsSource = _scheduleItems;

        var (settings, _) = _persistence.LoadData();
        ChkMasterToggle.IsChecked = settings.MatchRemindersEnabled;
        UpdateApiKeyPromptVisibility(settings.FootballDataApiKey);

        _isInitializing = false;

        Loaded += async (s, e) => await LoadScheduleAsync(force: false);
    }

    private void UpdateApiKeyPromptVisibility(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            PnlApiKeyPrompt.Visibility = Visibility.Visible;
        }
        else
        {
            PnlApiKeyPrompt.Visibility = Visibility.Collapsed;
        }
    }

    public async Task LoadScheduleAsync(bool force = false)
    {
        TxtStatus.Visibility = Visibility.Visible;
        TxtStatus.Text = "Updating fixtures & race sessions...";
        BtnRefresh.IsEnabled = false;

        try
        {
            var (settings, _) = _persistence.LoadData();
            UpdateApiKeyPromptVisibility(settings.FootballDataApiKey);

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

    private async void BtnSaveMatchesApiKey_Click(object sender, RoutedEventArgs e)
    {
        var key = TxtMatchesApiKey.Text.Trim();
        if (string.IsNullOrEmpty(key))
        {
            WpfMessageBox.Show("Please enter a valid Football-Data.org API key.", "Key Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var (settings, reminders) = _persistence.LoadData();
        settings.FootballDataApiKey = key;
        _persistence.SaveData(settings, reminders);

        PnlApiKeyPrompt.Visibility = Visibility.Collapsed;
        WpfMessageBox.Show("API Key saved! Loading FC Barcelona fixtures...", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);

        await LoadScheduleAsync(force: true);
        await _scheduler.RefreshSportsDataAsync(force: true);
    }
}
