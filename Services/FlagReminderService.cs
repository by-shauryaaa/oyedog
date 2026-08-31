using System;
using System.Windows;
using PixelDogReminders.Models;
using PixelDogReminders.Views;
using WpfApp = System.Windows.Application;

namespace PixelDogReminders.Services;

public class FlagReminderService
{
    private readonly PersistenceService _persistence;

    public FlagReminderService(PersistenceService persistence)
    {
        _persistence = persistence;
    }

    public void Show(string label, string countdown, string accentColor)
    {
        WpfApp.Current?.Dispatcher?.Invoke(() =>
        {
            try
            {
                var (settings, _) = _persistence.LoadData();

                var flagWindow = new ClassFlagWindow();
                flagWindow.SetContent(label, countdown, accentColor, settings.ReminderStyle, settings.ClassFlagPosition);
                flagWindow.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing flag reminder: {ex.Message}");
            }
        });
    }
}
