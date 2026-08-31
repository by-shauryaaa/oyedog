using System.IO;
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
        Assert.True(settings.LaunchOnStartup);
        Assert.Null(settings.LastWalkInDate);
    }

    [Fact]
    public void PersistenceService_SaveAndLoad_RoundTrip()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");
        try
        {
            var service = new PersistenceService(tempFile);
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
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
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
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");
        try
        {
            var service = new PersistenceService(tempFile);
            var (settings, reminders) = service.LoadData();
            var morningTime = new DateTime(2026, 8, 25, 8, 30, 0); // 8:30 AM
            settings.StartupGreetingEnabled = true;
            settings.LastWalkInDate = morningTime.Date.AddDays(-1); // Yesterday relative to test time
            service.SaveData(settings, reminders);

            Assert.True(service.ShouldShowWalkIn(morningTime));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ShouldShowWalkIn_AlreadyShownToday_ReturnsFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");
        try
        {
            var service = new PersistenceService(tempFile);
            var (settings, reminders) = service.LoadData();
            var morningTime = new DateTime(2026, 8, 25, 9, 30, 0);
            settings.StartupGreetingEnabled = true;
            settings.LastWalkInDate = new DateTime(2026, 8, 25, 7, 0, 0); // Already ran on same date
            service.SaveData(settings, reminders);

            Assert.False(service.ShouldShowWalkIn(morningTime));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ShouldShowWalkIn_EveningHour_ReturnsFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");
        try
        {
            var service = new PersistenceService(tempFile);
            var (settings, reminders) = service.LoadData();
            var eveningTime = new DateTime(2026, 8, 25, 15, 0, 0); // 3:00 PM (afternoon/evening)
            settings.StartupGreetingEnabled = true;
            settings.LastWalkInDate = eveningTime.Date.AddDays(-1);
            service.SaveData(settings, reminders);

            Assert.False(service.ShouldShowWalkIn(eveningTime));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ShouldShowWalkIn_DisabledSetting_ReturnsFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");
        try
        {
            var service = new PersistenceService(tempFile);
            var (settings, reminders) = service.LoadData();
            settings.StartupGreetingEnabled = false;
            service.SaveData(settings, reminders);

            var morningTime = new DateTime(2026, 8, 25, 8, 30, 0);
            Assert.False(service.ShouldShowWalkIn(morningTime));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SubjectColorPalette_ReturnsUniqueColors()
    {
        var used = new List<string>();
        for (int i = 0; i < SubjectColorPalette.Palette.Length; i++)
        {
            var nextColor = SubjectColorPalette.Next(used);
            Assert.False(used.Contains(nextColor));
            used.Add(nextColor);
        }
    }

    [Fact]
    public void Slot_EndTime_DerivedCorrectly()
    {
        var slot = new Slot
        {
            StartTime = new TimeSpan(9, 0, 0)
        };
        var end90 = slot.GetEndTime(90);
        Assert.Equal(new TimeSpan(10, 30, 0), end90);

        var end45 = slot.GetEndTime(45);
        Assert.Equal(new TimeSpan(9, 45, 0), end45);
    }

    [Fact]
    public void SlotOverlap_DirectConflict_Detected()
    {
        var slot1 = new Slot { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(9, 0, 0) };
        int dur1 = 60; // 09:00 - 10:00

        var slot2 = new Slot { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(9, 30, 0) };
        int dur2 = 60; // 09:30 - 10:30

        var s1Start = slot1.StartTime;
        var s1End = slot1.GetEndTime(dur1);
        var s2Start = slot2.StartTime;
        var s2End = slot2.GetEndTime(dur2);

        // Overlap test
        bool overlaps = s1Start < s2End && s2Start < s1End;
        Assert.True(overlaps);
    }

    [Fact]
    public void SlotOverlap_AdjacentSlots_DoNotConflict()
    {
        var slot1 = new Slot { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(9, 0, 0) };
        int dur1 = 60; // 09:00 - 10:00

        var slot2 = new Slot { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(10, 0, 0) };
        int dur2 = 60; // 10:00 - 11:00

        var s1Start = slot1.StartTime;
        var s1End = slot1.GetEndTime(dur1);
        var s2Start = slot2.StartTime;
        var s2End = slot2.GetEndTime(dur2);

        // Overlap test (end-exclusive)
        bool overlaps = s1Start < s2End && s2Start < s1End;
        Assert.False(overlaps);
    }

    [Fact]
    public void AppSettings_TimetableDefaults()
    {
        var settings = new AppSettings();
        Assert.True(settings.TimetableRemindersEnabled);
        Assert.Equal(60, settings.DefaultClassDurationMinutes);
        Assert.Equal(10, settings.LeadTimeMinutes);
        Assert.Equal(FlagPosition.Top, settings.ClassFlagPosition);
    }

    [Fact]
    public void PersistenceService_SavesAndLoadsSubjects()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");
        try
        {
            var service = new PersistenceService(tempFile);
            var testSubject = new Subject
            {
                Name = "Operating Systems",
                DurationMinutes = 90,
                Room = "Lab 4",
                Color = "#BA68C8",
                Slots = new List<Slot>
                {
                    new() { DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeSpan(11, 0, 0) },
                    new() { DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeSpan(14, 0, 0) }
                }
            };

            service.SaveSubjects(new List<Subject> { testSubject });

            var loaded = service.LoadSubjects();
            Assert.Single(loaded);
            Assert.Equal("Operating Systems", loaded[0].Name);
            Assert.Equal(90, loaded[0].DurationMinutes);
            Assert.Equal(2, loaded[0].Slots.Count);
            Assert.Equal(DayOfWeek.Tuesday, loaded[0].Slots[0].DayOfWeek);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
