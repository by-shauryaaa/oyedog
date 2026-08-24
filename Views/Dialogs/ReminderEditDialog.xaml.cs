using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using PixelDogReminders.Models;
using WpfMessageBox = System.Windows.MessageBox;

namespace PixelDogReminders.Views.Dialogs;

public partial class ReminderEditDialog : Window
{
    private readonly ReminderModel _reminder;
    private readonly ObservableCollection<string> _timeSlots = new();

    public ReminderModel ResultReminder => _reminder;

    public ReminderEditDialog(ReminderModel? existing = null)
    {
        InitializeComponent();

        _reminder = existing != null ? CloneReminder(existing) : new ReminderModel
        {
            Name = "Water",
            Message = "paani pi le",
            Variant = SpriteVariant.Water,
            IsIntervalBased = false,
            TimeSlots = new List<string> { "10:00", "14:00", "18:00" },
            IsEnabled = true
        };

        // Populate Variants
        foreach (SpriteVariant v in Enum.GetValues(typeof(SpriteVariant)))
        {
            CmbVariant.Items.Add(v);
        }

        // Initialize UI values
        TxtName.Text = _reminder.Name;
        TxtMessage.Text = _reminder.Message;
        CmbVariant.SelectedItem = _reminder.Variant;

        foreach (var t in _reminder.TimeSlots)
        {
            _timeSlots.Add(t);
        }
        LstTimeSlots.ItemsSource = _timeSlots;

        if (_reminder.IsIntervalBased)
        {
            RbInterval.IsChecked = true;
            TxtIntervalMinutes.Text = (_reminder.IntervalMinutes ?? 60).ToString();
        }
        else
        {
            RbFixedTimes.IsChecked = true;
        }

        ChkEnabled.IsChecked = _reminder.IsEnabled;
        UpdateVariantPreview();
    }

    private static ReminderModel CloneReminder(ReminderModel r)
    {
        return new ReminderModel
        {
            Id = r.Id,
            Name = r.Name,
            Message = r.Message,
            Variant = r.Variant,
            IsIntervalBased = r.IsIntervalBased,
            IntervalMinutes = r.IntervalMinutes,
            TimeSlots = new List<string>(r.TimeSlots),
            IsEnabled = r.IsEnabled,
            LastFiredTime = r.LastFiredTime
        };
    }

    private void UpdateVariantPreview()
    {
        if (CmbVariant.SelectedItem is SpriteVariant variant)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Assets/Sprites/{variant.ToKey()}_0.png", UriKind.Absolute);
                ImgVariantPreview.Source = new BitmapImage(uri);
            }
            catch
            {
                // Fallback
            }
        }
    }

    private void CmbVariant_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateVariantPreview();
    }

    private void TriggerType_Changed(object sender, RoutedEventArgs e)
    {
        if (PnlFixedTimes == null || PnlInterval == null) return;

        if (RbFixedTimes.IsChecked == true)
        {
            PnlFixedTimes.Visibility = Visibility.Visible;
            PnlInterval.Visibility = Visibility.Collapsed;
        }
        else
        {
            PnlFixedTimes.Visibility = Visibility.Collapsed;
            PnlInterval.Visibility = Visibility.Visible;
        }
    }

    private void BtnAddTime_Click(object sender, RoutedEventArgs e)
    {
        var val = TxtNewTime.Text.Trim();
        if (TimeOnly.TryParse(val, out var time))
        {
            var formatted = time.ToString("HH:mm");
            if (!_timeSlots.Contains(formatted))
            {
                _timeSlots.Add(formatted);
            }
        }
        else
        {
            WpfMessageBox.Show("Please enter a valid time in HH:mm format (e.g. 14:30 or 2:30 PM).", "Invalid Time", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnRemoveTime_Click(object sender, RoutedEventArgs e)
    {
        if (LstTimeSlots.SelectedItem is string item)
        {
            _timeSlots.Remove(item);
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            WpfMessageBox.Show("Please enter a name for the reminder.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _reminder.Name = TxtName.Text.Trim();
        _reminder.Message = TxtMessage.Text.Trim();
        _reminder.Variant = (SpriteVariant)(CmbVariant.SelectedItem ?? SpriteVariant.Idle);
        _reminder.IsIntervalBased = RbInterval.IsChecked == true;
        _reminder.IsEnabled = ChkEnabled.IsChecked == true;

        if (_reminder.IsIntervalBased)
        {
            if (int.TryParse(TxtIntervalMinutes.Text, out var mins) && mins > 0)
            {
                _reminder.IntervalMinutes = mins;
            }
            else
            {
                WpfMessageBox.Show("Please enter a valid positive interval in minutes.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        else
        {
            _reminder.TimeSlots = _timeSlots.OrderBy(x => x).ToList();
        }

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
