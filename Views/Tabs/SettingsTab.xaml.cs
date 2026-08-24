using System.Windows;
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
    private bool _isInitializing = true;

    public SettingsTab(PersistenceService persistence, PopupService popupService)
    {
        _persistence = persistence;
        _popupService = popupService;
        _isInitializing = true;

        InitializeComponent();

        LoadSettings();
        _isInitializing = false;
    }

    private void LoadSettings()
    {
        _isInitializing = true;
        var (settings, _) = _persistence.LoadData();

        // Position
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

        // Snooze
        foreach (WpfComboBoxItem item in CmbSnooze.Items)
        {
            if (item.Tag is string tag && int.TryParse(tag, out var mins) && mins == settings.SnoozeDurationMinutes)
            {
                CmbSnooze.SelectedItem = item;
                break;
            }
        }

        // Launch on Startup & Startup Greeting
        ChkLaunchOnStartup.IsChecked = settings.LaunchOnStartup;
        ChkStartupGreeting.IsChecked = settings.StartupGreetingEnabled;

        // API Key
        TxtApiKey.Text = settings.FootballDataApiKey;
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

    private void BtnSaveApiKey_Click(object sender, RoutedEventArgs e)
    {
        var (settings, reminders) = _persistence.LoadData();
        settings.FootballDataApiKey = TxtApiKey.Text.Trim();
        _persistence.SaveData(settings, reminders);
        WpfMessageBox.Show("Football API key saved! Matches tab will refresh fixtures automatically.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
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
}
