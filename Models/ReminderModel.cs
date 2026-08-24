using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace PixelDogReminders.Models;

public class ReminderModel : INotifyPropertyChanged
{
    private Guid _id = Guid.NewGuid();
    private string _name = string.Empty;
    private string _message = string.Empty;
    private SpriteVariant _variant = SpriteVariant.Idle;
    private List<string> _timeSlots = new(); // "HH:mm" formatted
    private int? _intervalMinutes;
    private bool _isEnabled;
    private bool _isIntervalBased;
    private DateTime? _lastFiredTime;

    public Guid Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string Message
    {
        get => _message;
        set => SetField(ref _message, value);
    }

    public SpriteVariant Variant
    {
        get => _variant;
        set => SetField(ref _variant, value);
    }

    public List<string> TimeSlots
    {
        get => _timeSlots;
        set => SetField(ref _timeSlots, value);
    }

    public int? IntervalMinutes
    {
        get => _intervalMinutes;
        set => SetField(ref _intervalMinutes, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    public bool IsIntervalBased
    {
        get => _isIntervalBased;
        set => SetField(ref _isIntervalBased, value);
    }

    public DateTime? LastFiredTime
    {
        get => _lastFiredTime;
        set => SetField(ref _lastFiredTime, value);
    }

    [JsonIgnore]
    public string SummaryText
    {
        get
        {
            if (IsIntervalBased && IntervalMinutes.HasValue)
            {
                return $"Every {IntervalMinutes} minutes";
            }
            if (TimeSlots.Count > 0)
            {
                return string.Join(", ", TimeSlots);
            }
            return "No time set";
        }
    }

    [JsonIgnore]
    public string VariantPreviewPath => $"pack://application:,,,/Assets/Sprites/{Variant.ToKey()}_0.png";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        if (propertyName is nameof(TimeSlots) or nameof(IntervalMinutes) or nameof(IsIntervalBased))
        {
            OnPropertyChanged(nameof(SummaryText));
        }
        if (propertyName == nameof(Variant))
        {
            OnPropertyChanged(nameof(VariantPreviewPath));
        }
        return true;
    }
}
