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
                var workArea = SystemParameters.WorkArea;

                var flagWindow = new ClassFlagWindow();
                flagWindow.SetContent(label, countdown, accentColor);

                // Compute vertical position based on settings
                double targetTop = settings.ClassFlagPosition switch
                {
                    FlagPosition.Top => workArea.Top + (workArea.Height * 0.20),
                    FlagPosition.Middle => workArea.Top + (workArea.Height * 0.50) - 35,
                    FlagPosition.Bottom => workArea.Bottom - 110,
                    _ => workArea.Top + (workArea.Height * 0.20)
                };

                flagWindow.Top = targetTop;
                flagWindow.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing flag reminder: {ex.Message}");
            }
        });
    }
}
