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

public class AppSettings
{
    public PopupPosition Position { get; set; } = PopupPosition.BottomRight;
    public int SnoozeDurationMinutes { get; set; } = 5;
    public bool MatchRemindersEnabled { get; set; } = true;
    public string FootballDataApiKey { get; set; } = "";
    public bool StartupGreetingEnabled { get; set; } = true;
    public bool LaunchOnStartup { get; set; } = true;
    public DateTime? LastWalkInDate { get; set; } = null;
}
