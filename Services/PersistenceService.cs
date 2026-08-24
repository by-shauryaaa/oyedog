using System.IO;
using System.Text.Json;
using PixelDogReminders.Models;

namespace PixelDogReminders.Services;

public class PersistenceService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PixelDogReminders"
    );

    private static readonly string ConfigFilePath = Path.Combine(AppDataFolder, "config.json");
    private static readonly string SportsCacheFilePath = Path.Combine(AppDataFolder, "sports_cache.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public PersistenceService()
    {
        if (!Directory.Exists(AppDataFolder))
        {
            Directory.CreateDirectory(AppDataFolder);
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
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                var container = JsonSerializer.Deserialize<AppDataContainer>(json, JsonOptions);
                if (container != null)
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
        var defaultSettings = new AppSettings();
        var defaultReminders = CreateDefaultReminders();
        SaveData(defaultSettings, defaultReminders);
        return (defaultSettings, defaultReminders);
    }

    public void SaveData(AppSettings settings, List<ReminderModel> reminders)
    {
        try
        {
            var container = new AppDataContainer
            {
                Settings = settings,
                Reminders = reminders
            };
            var json = JsonSerializer.Serialize(container, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
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
            if (File.Exists(SportsCacheFilePath))
            {
                return File.ReadAllText(SportsCacheFilePath);
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
            File.WriteAllText(SportsCacheFilePath, json);
        }
        catch
        {
            // Ignore cache write failures
        }
    }
}
