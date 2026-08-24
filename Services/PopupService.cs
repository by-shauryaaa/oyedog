using PixelDogReminders.Models;
using PixelDogReminders.Views;
using WpfApp = System.Windows.Application;

namespace PixelDogReminders.Services;

public class PopupService
{
    private readonly PersistenceService _persistence;
    private ReminderPopupWindow? _activePopup;
    private readonly Queue<(string Title, string Message, SpriteVariant Variant, Action? OnSnooze, Action? OnOkii)> _queue = new();
    private readonly object _lock = new();

    public PopupService(PersistenceService persistence)
    {
        _persistence = persistence;
    }

    public void ShowPopup(string title, string message, SpriteVariant variant, Action? onSnooze = null, Action? onOkii = null)
    {
        WpfApp.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                if (_activePopup != null)
                {
                    _queue.Enqueue((title, message, variant, onSnooze, onOkii));
                    return;
                }

                var (settings, _) = _persistence.LoadData();
                var popup = new ReminderPopupWindow(title, message, variant, settings.Position);
                _activePopup = popup;

                popup.SnoozeClicked += (s, e) =>
                {
                    onSnooze?.Invoke();
                    OnPopupDismissed();
                };

                popup.OkiiClicked += (s, e) =>
                {
                    onOkii?.Invoke();
                    OnPopupDismissed();
                };

                popup.Closed += (s, e) =>
                {
                    OnPopupDismissed();
                };

                popup.Show();
            }
        });
    }

    private void OnPopupDismissed()
    {
        lock (_lock)
        {
            _activePopup = null;
            if (_queue.Count > 0)
            {
                var next = _queue.Dequeue();
                ShowPopup(next.Title, next.Message, next.Variant, next.OnSnooze, next.OnOkii);
            }
        }
    }

    public void DismissActivePopup()
    {
        WpfApp.Current?.Dispatcher.Invoke(() =>
        {
            _activePopup?.PlaySlideOutAndClose();
        });
    }
}
