using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using PixelDogReminders.Models;
using PixelDogReminders.Services;
using PixelDogReminders.Views.Dialogs;
using WpfButton = System.Windows.Controls.Button;
using WpfControl = System.Windows.Controls.UserControl;
using WpfMessageBox = System.Windows.MessageBox;

namespace PixelDogReminders.Views.Tabs;

public partial class RemindersTab : WpfControl
{
    private readonly PersistenceService _persistence;
    private readonly ObservableCollection<ReminderModel> _reminders = new();

    public event EventHandler? NavigateToMatchesRequested;

    public RemindersTab(PersistenceService persistence)
    {
        InitializeComponent();
        _persistence = persistence;

        ItemsReminders.ItemsSource = _reminders;
        LoadReminders();
    }

    public void LoadReminders()
    {
        var (_, list) = _persistence.LoadData();
        _reminders.Clear();
        foreach (var item in list)
        {
            _reminders.Add(item);
        }
    }

    private void SaveReminders()
    {
        var (settings, _) = _persistence.LoadData();
        _persistence.SaveData(settings, _reminders.ToList());
    }

    private void BtnAddReminder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ReminderEditDialog
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            _reminders.Add(dialog.ResultReminder);
            SaveReminders();
        }
    }

    private void BtnEditReminder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton btn && btn.Tag is ReminderModel reminder)
        {
            var dialog = new ReminderEditDialog(reminder)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                var index = _reminders.IndexOf(reminder);
                if (index >= 0)
                {
                    _reminders[index] = dialog.ResultReminder;
                    SaveReminders();
                }
            }
        }
    }

    private void BtnDeleteReminder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton btn && btn.Tag is ReminderModel reminder)
        {
            var result = WpfMessageBox.Show($"Delete reminder '{reminder.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _reminders.Remove(reminder);
                SaveReminders();
            }
        }
    }

    private void ChkEnabled_Changed(object sender, RoutedEventArgs e)
    {
        SaveReminders();
    }

    private void PromoCard_Click(object sender, MouseButtonEventArgs e)
    {
        NavigateToMatchesRequested?.Invoke(this, EventArgs.Empty);
    }
}
