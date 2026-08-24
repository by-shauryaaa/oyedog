using System.IO;
using System.Text.Json;
using PixelDogReminders.Models;

namespace PixelDogReminders.Services;

public class PersistenceService
{
    private static readonly string DefaultAppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PixelDogReminders"
    );

    private readonly string _configFilePath;
    private readonly string _sportsCacheFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public PersistenceService(string? customConfigPath = null, string? customCachePath = null)
    {
        if (customConfigPath != null)
        {
            _configFilePath = customConfigPath;
            _sportsCacheFilePath = customCachePath ?? Path.Combine(Path.GetDirectoryName(customConfigPath) ?? ".", "sports_cache.json");
        }
        else
        {
            if (!Directory.Exists(DefaultAppDataFolder))
            {
                Directory.CreateDirectory(DefaultAppDataFolder);
            }
            _configFilePath = Path.Combine(DefaultAppDataFolder, "config.json");
            _sportsCacheFilePath = Path.Combine(DefaultAppDataFolder, "sports_cache.json");
        }
    }

    public class AppDataContainer
    {
        public AppSettings Settings { get; set; } = new();
        public List<ReminderModel> Reminders { get; set; } = new();
    }

    public (AppSettings Settings, List<ReminderModel> Reminders) LoadData()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var container = JsonSerializer.Deserialize<AppDataContainer>(json, JsonOptions);
                if (container != null && container.Reminders != null && container.Reminders.Count > 0)
                {
                    return (container.Settings, container.Reminders);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
        }

        // Default initial data
        var defaultSettings = new AppSettings
        {
            Position = PopupPosition.BottomRight,
            MatchRemindersEnabled = true,
            StartupGreetingEnabled = true,
            LaunchOnStartup = true,
            SnoozeDurationMinutes = 5
        };
        var defaultReminders = CreateDefaultReminders();
        SaveData(defaultSettings, defaultReminders);
        return (defaultSettings, defaultReminders);
    }

    public void SaveData(AppSettings settings, List<ReminderModel> reminders)
    {
        try
        {
            var dir = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var container = new AppDataContainer
            {
                Settings = settings,
                Reminders = reminders
            };
            var json = JsonSerializer.Serialize(container, JsonOptions);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
        }
    }

    public bool ShouldShowWalkIn(DateTime now)
    {
        var (settings, _) = LoadData();
        if (!settings.StartupGreetingEnabled) return false;

        // Morning window: 6:00 AM to 11:59 AM
        if (now.Hour < 6 || now.Hour >= 12) return false;

        // Has already shown today
        if (settings.LastWalkInDate.HasValue && settings.LastWalkInDate.Value.Date == now.Date)
        {
            return false;
        }

        return true;
    }

    public void RecordWalkInShown(DateTime now)
    {
        var (settings, reminders) = LoadData();
        settings.LastWalkInDate = now;
        SaveData(settings, reminders);
    }

    public static List<ReminderModel> CreateDefaultReminders()
    {
        return new List<ReminderModel>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Food",
                Message = "kuch khaya?",
                Variant = SpriteVariant.Food,
                IsIntervalBased = false,
                TimeSlots = new List<string> { "13:00", "20:00" },
                IsEnabled = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Water",
                Message = "paani pi le",
                Variant = SpriteVariant.Water,
                IsIntervalBased = true,
                IntervalMinutes = 120,
                TimeSlots = new List<string>(),
                IsEnabled = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Sleep",
                Message = "abe soja ab",
                Variant = SpriteVariant.Sleep,
                IsIntervalBased = false,
                TimeSlots = new List<string> { "23:00" },
                IsEnabled = true
            }
        };
    }

    public string? LoadSportsCache()
    {
        try
        {
            if (File.Exists(_sportsCacheFilePath))
            {
                return File.ReadAllText(_sportsCacheFilePath);
            }
        }
        catch
        {
            // Ignore cache read failures
        }
        return null;
    }

    public void SaveSportsCache(string json)
    {
        try
        {
            var dir = Path.GetDirectoryName(_sportsCacheFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(_sportsCacheFilePath, json);
        }
        catch
        {
            // Ignore cache write failures
        }
    }
}
