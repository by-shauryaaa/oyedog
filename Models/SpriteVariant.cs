using System.Text.Json.Serialization;

namespace PixelDogReminders.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpriteVariant
{
    Idle,
    Water,
    Food,
    Sleep,
    Rest,
    Barca,
    F1,
    Walking,
    BirthdayWalk
}

public static class SpriteVariantExtensions
{
    public static string ToKey(this SpriteVariant variant) => variant switch
    {
        SpriteVariant.Idle => "idle",
        SpriteVariant.Water => "water",
        SpriteVariant.Food => "food",
        SpriteVariant.Sleep => "sleep",
        SpriteVariant.Rest => "rest",
        SpriteVariant.Barca => "barca",
        SpriteVariant.F1 => "f1",
        SpriteVariant.Walking => "walking",
        SpriteVariant.BirthdayWalk => "birthday_walk",
        _ => "idle"
    };

    public static string ToDisplayName(this SpriteVariant variant) => variant switch
    {
        SpriteVariant.Idle => "Idle / Companion",
        SpriteVariant.Water => "Water (Hydrate)",
        SpriteVariant.Food => "Food (Snack/Meal)",
        SpriteVariant.Sleep => "Sleep (Rest)",
        SpriteVariant.Rest => "Break / Stretch",
        SpriteVariant.Barca => "FC Barcelona",
        SpriteVariant.F1 => "Formula 1 Racing",
        SpriteVariant.Walking => "Walking (Locomotion)",
        SpriteVariant.BirthdayWalk => "Birthday Celebration Walk",
        _ => "Idle"
    };
}
