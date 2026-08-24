using PixelDogReminders.Models;
using PixelDogReminders.Services;
using Xunit;

namespace PixelDogReminders.Tests;

public class AppTests
{
    [Fact]
    public void DefaultReminders_ShouldMatchUserSpecification()
    {
        var defaults = PersistenceService.CreateDefaultReminders();

        Assert.Equal(3, defaults.Count);

        // 1. Food
        var food = defaults.FirstOrDefault(r => r.Name == "Food");
        Assert.NotNull(food);
        Assert.Equal("kuch khaya?", food.Message);
        Assert.Equal(SpriteVariant.Food, food.Variant);
        Assert.False(food.IsEnabled); // Disabled by default
        Assert.Contains("13:00", food.TimeSlots);
        Assert.Contains("20:00", food.TimeSlots);

        // 2. Water
        var water = defaults.FirstOrDefault(r => r.Name == "Water");
        Assert.NotNull(water);
        Assert.Equal("paani pi le", water.Message);
        Assert.Equal(SpriteVariant.Water, water.Variant);
        Assert.True(water.IsIntervalBased);
        Assert.Equal(120, water.IntervalMinutes); // Every 2 hours
        Assert.False(water.IsEnabled); // Disabled by default

        // 3. Sleep
        var sleep = defaults.FirstOrDefault(r => r.Name == "Sleep");
        Assert.NotNull(sleep);
        Assert.Equal("abe soja ab", sleep.Message);
        Assert.Equal(SpriteVariant.Sleep, sleep.Variant);
        Assert.Contains("23:00", sleep.TimeSlots);
        Assert.True(sleep.IsEnabled); // Enabled by default
    }

    [Fact]
    public void SpriteVariants_ShouldHaveValidKeysAndNames()
    {
        var variants = Enum.GetValues<SpriteVariant>();
        Assert.Equal(9, variants.Length); // 7 original + Walking + BirthdayWalk

        foreach (var v in variants)
        {
            Assert.False(string.IsNullOrEmpty(v.ToKey()));
            Assert.False(string.IsNullOrEmpty(v.ToDisplayName()));
        }
    }

    [Fact]
    public void ReminderModel_SummaryText_IntervalMode()
    {
        var model = new ReminderModel
        {
            Name = "Water",
            IsIntervalBased = true,
            IntervalMinutes = 90
        };

        Assert.Equal("Every 90 minutes", model.SummaryText);
    }

    [Fact]
    public void ReminderModel_SummaryText_FixedTimeMode()
    {
        var model = new ReminderModel
        {
            Name = "Meds",
            IsIntervalBased = false,
            TimeSlots = new List<string> { "09:00", "15:00", "21:00" }
        };

        Assert.Equal("09:00, 15:00, 21:00", model.SummaryText);
    }

    [Fact]
    public void AppSettings_Defaults()
    {
        var settings = new AppSettings();
        Assert.Equal(PopupPosition.BottomRight, settings.Position);
        Assert.Equal(5, settings.SnoozeDurationMinutes);
        Assert.True(settings.MatchRemindersEnabled);
        Assert.True(settings.StartupGreetingEnabled);
        Assert.Null(settings.LastWalkInDate);
    }

    [Fact]
    public void PersistenceService_SaveAndLoad_RoundTrip()
    {
        var service = new PersistenceService();
        var settings = new AppSettings
        {
            Position = PopupPosition.TopLeft,
            SnoozeDurationMinutes = 10,
            MatchRemindersEnabled = false,
            StartupGreetingEnabled = true
        };

        var reminders = new List<ReminderModel>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Custom Test",
                Message = "test message",
                Variant = SpriteVariant.Rest,
                TimeSlots = new List<string> { "12:34" },
                IsEnabled = true
            }
        };

        service.SaveData(settings, reminders);

        var (loadedSettings, loadedReminders) = service.LoadData();

        Assert.Equal(PopupPosition.TopLeft, loadedSettings.Position);
        Assert.Equal(10, loadedSettings.SnoozeDurationMinutes);
        Assert.False(loadedSettings.MatchRemindersEnabled);
        Assert.True(loadedSettings.StartupGreetingEnabled);

        Assert.Contains(loadedReminders, r => r.Name == "Custom Test" && r.Message == "test message");
    }

    [Fact]
    public void ScheduleItem_DateAndTimeFormatting()
    {
        var dtUtc = new DateTime(2026, 8, 25, 14, 30, 0, DateTimeKind.Utc);
        var item = new ScheduleItem
        {
            Title = "Spanish Grand Prix",
            Subtitle = "Circuit de Barcelona-Catalunya",
            Category = "Formula 1",
            CategoryColor = "#E10600",
            DateTimeUtc = dtUtc,
            IsF1 = true
        };

        var local = dtUtc.ToLocalTime();
        Assert.Equal(local.ToString("ddd, dd MMM yyyy"), item.FormattedDate);
        Assert.Equal(local.ToString("hh:mm tt"), item.FormattedTime);
        Assert.True(item.IsF1);
    }

    [Fact]
    public void ShouldShowWalkIn_MorningAndNewDay_ReturnsTrue()
    {
        var service = new PersistenceService();
        var (settings, reminders) = service.LoadData();
        settings.StartupGreetingEnabled = true;
        settings.LastWalkInDate = DateTime.Today.AddDays(-1); // Yesterday
        service.SaveData(settings, reminders);

        var morningTime = new DateTime(2026, 8, 25, 8, 30, 0); // 8:30 AM
        Assert.True(service.ShouldShowWalkIn(morningTime));
    }

    [Fact]
    public void ShouldShowWalkIn_AlreadyShownToday_ReturnsFalse()
    {
        var service = new PersistenceService();
        var (settings, reminders) = service.LoadData();
        settings.StartupGreetingEnabled = true;
        settings.LastWalkInDate = new DateTime(2026, 8, 25, 7, 0, 0); // Already ran today
        service.SaveData(settings, reminders);

        var morningTime = new DateTime(2026, 8, 25, 9, 30, 0);
        Assert.False(service.ShouldShowWalkIn(morningTime));
    }

    [Fact]
    public void ShouldShowWalkIn_EveningHour_ReturnsFalse()
    {
        var service = new PersistenceService();
        var (settings, reminders) = service.LoadData();
        settings.StartupGreetingEnabled = true;
        settings.LastWalkInDate = DateTime.Today.AddDays(-1);
        service.SaveData(settings, reminders);

        var eveningTime = new DateTime(2026, 8, 25, 15, 0, 0); // 3:00 PM (afternoon/evening)
        Assert.False(service.ShouldShowWalkIn(eveningTime));
    }

    [Fact]
    public void ShouldShowWalkIn_DisabledSetting_ReturnsFalse()
    {
        var service = new PersistenceService();
        var (settings, reminders) = service.LoadData();
        settings.StartupGreetingEnabled = false;
        service.SaveData(settings, reminders);

        var morningTime = new DateTime(2026, 8, 25, 8, 30, 0);
        Assert.False(service.ShouldShowWalkIn(morningTime));
    }
}
