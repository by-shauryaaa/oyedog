using System.Text.Json.Serialization;

namespace PixelDogReminders.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PopupPosition
{
    BottomRight,
    BottomCenter,
    BottomLeft,
    TopRight,
    TopCenter,
    TopLeft
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ClassReminderStyle
{
    Simple,
    Cloud,
    Banner
}

public class AppSettings
{
    public PopupPosition Position { get; set; } = PopupPosition.BottomRight;
    public int SnoozeDurationMinutes { get; set; } = 5;
    public bool MatchRemindersEnabled { get; set; } = true;
    public string FootballDataApiKey { get; set; } = "";
    public bool StartupGreetingEnabled { get; set; } = true;
    public bool LaunchOnStartup { get; set; } = true;
    public DateTime? LastWalkInDate { get; set; } = null;

    // Timetable Settings
    public bool TimetableRemindersEnabled { get; set; } = true;
    public int DefaultClassDurationMinutes { get; set; } = 60;
    public int LeadTimeMinutes { get; set; } = 10;
    public FlagPosition ClassFlagPosition { get; set; } = FlagPosition.Top;
    public ClassReminderStyle ReminderStyle { get; set; } = ClassReminderStyle.Simple;

    // Navigation / UI
    public bool SidebarCollapsed { get; set; } = false;
    public string DisplayName { get; set; } = "Abhishek";
}
