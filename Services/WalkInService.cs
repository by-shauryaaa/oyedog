using System.Windows;
using PixelDogReminders.Views;
using WpfApp = System.Windows.Application;

namespace PixelDogReminders.Services;

public class WalkInService
{
    private readonly PersistenceService _persistence;
    private WalkInGreetingWindow? _activeWalkInWindow;

    public WalkInService(PersistenceService persistence)
    {
        _persistence = persistence;
    }

    public void CheckAndTriggerWalkIn(bool force = false)
    {
        var now = DateTime.Now;
        if (!force && !_persistence.ShouldShowWalkIn(now))
        {
            return;
        }

        _persistence.RecordWalkInShown(now);

        WpfApp.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                if (_activeWalkInWindow != null && _activeWalkInWindow.IsLoaded)
                {
                    _activeWalkInWindow.Close();
                }

                _activeWalkInWindow = new WalkInGreetingWindow();
                _activeWalkInWindow.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show walk-in greeting: {ex.Message}");
            }
        });
    }
}
