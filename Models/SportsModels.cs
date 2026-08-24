namespace PixelDogReminders.Models;

public class MatchFixture
{
    public string Id { get; set; } = string.Empty;
    public string Competition { get; set; } = string.Empty;
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime KickoffUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class F1SessionEvent
{
    public string RaceName { get; set; } = string.Empty;
    public string CircuitName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty; // Practice, Qualifying, Sprint, Race
    public DateTime StartUtc { get; set; }
}

public class ScheduleItem
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "FC Barcelona" or "Formula 1"
    public string CategoryColor { get; set; } = "#A50044"; // Blaugrana or F1 red
    public DateTime DateTimeUtc { get; set; }
    public DateTime LocalDateTime => DateTimeUtc.ToLocalTime();
    public string FormattedDate => LocalDateTime.ToString("ddd, dd MMM yyyy");
    public string FormattedTime => LocalDateTime.ToString("hh:mm tt");
    public bool IsF1 { get; set; }
}
