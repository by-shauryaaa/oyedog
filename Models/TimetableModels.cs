using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace PixelDogReminders.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FlagPosition
{
    Top,
    Middle,
    Bottom
}

public class Slot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubjectId { get; set; }
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Monday;
    public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

    public TimeSpan GetEndTime(int durationMinutes)
    {
        return StartTime.Add(TimeSpan.FromMinutes(durationMinutes));
    }
}

public class Subject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int DurationMinutes { get; set; } = 60;
    public string? Room { get; set; } = "";
    public string Color { get; set; } = "#64B5F6";
    public List<Slot> Slots { get; set; } = new();
}

public static class SubjectColorPalette
{
    public static readonly string[] Palette = new[]
    {
        "#E57373", // Coral Red
        "#81C784", // Soft Green
        "#64B5F6", // Sky Blue
        "#FFB74D", // Warm Amber
        "#BA68C8", // Lavender Purple
        "#4DD0E1", // Turquoise
        "#FFD54F", // Golden Yellow
        "#A1887F", // Warm Mocha
        "#F06292", // Rose Pink
        "#AED581", // Lime Sage
        "#7986CB", // Indigo Blue
        "#4DB6AC"  // Mint Teal
    };

    public static string Next(IEnumerable<string>? existingColors)
    {
        var used = existingColors != null ? new HashSet<string>(existingColors, StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
        foreach (var color in Palette)
        {
            if (!used.Contains(color))
            {
                return color;
            }
        }
        // If all 12 are used, choose the one with least use or modulo
        return Palette[used.Count % Palette.Length];
    }
}
