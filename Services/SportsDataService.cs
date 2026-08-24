using System.Net.Http;
using System.Text.Json;
using PixelDogReminders.Models;

namespace PixelDogReminders.Services;

public class SportsDataService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private readonly PersistenceService _persistence;

    public SportsDataService(PersistenceService persistence)
    {
        _persistence = persistence;
    }

    public class SportsCacheData
    {
        public DateTime CachedAtUtc { get; set; }
        public List<ScheduleItem> Items { get; set; } = new();
    }

    public async Task<List<ScheduleItem>> GetUpcomingScheduleAsync(string? footballApiKey, bool forceRefresh = false)
    {
        // Check cache first (valid for 12 hours)
        var cachedJson = _persistence.LoadSportsCache();
        if (!forceRefresh && !string.IsNullOrEmpty(cachedJson))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<SportsCacheData>(cachedJson);
                if (cached != null && (DateTime.UtcNow - cached.CachedAtUtc).TotalHours < 12)
                {
                    return cached.Items;
                }
            }
            catch
            {
                // Fall through to fetch
            }
        }

        var results = new List<ScheduleItem>();

        // 1. Fetch F1 Race Sessions
        try
        {
            var f1Items = await FetchF1ScheduleAsync();
            results.AddRange(f1Items);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"F1 fetch error: {ex.Message}");
        }

        // 2. Fetch FC Barcelona Fixtures (if API key provided)
        if (!string.IsNullOrWhiteSpace(footballApiKey))
        {
            try
            {
                var barcaItems = await FetchBarcaScheduleAsync(footballApiKey);
                results.AddRange(barcaItems);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Barca fetch error: {ex.Message}");
            }
        }

        // Sort chronologically
        var sorted = results
            .Where(x => x.DateTimeUtc >= DateTime.UtcNow && x.DateTimeUtc <= DateTime.UtcNow.AddDays(30))
            .OrderBy(x => x.DateTimeUtc)
            .ToList();

        // Save cache
        try
        {
            var cacheObj = new SportsCacheData
            {
                CachedAtUtc = DateTime.UtcNow,
                Items = sorted
            };
            var cacheJson = JsonSerializer.Serialize(cacheObj);
            _persistence.SaveSportsCache(cacheJson);
        }
        catch
        {
            // Ignore cache write error
        }

        return sorted;
    }

    private async Task<List<ScheduleItem>> FetchF1ScheduleAsync()
    {
        var items = new List<ScheduleItem>();
        var url = "https://api.jolpi.ca/ergast/f1/current.json";
        
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "PixelDogReminders/1.0");

        var response = await HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return items;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("MRData", out var mrData) ||
            !mrData.TryGetProperty("RaceTable", out var raceTable) ||
            !raceTable.TryGetProperty("Races", out var races))
        {
            return items;
        }

        var now = DateTime.UtcNow;
        var limit = now.AddDays(30);

        foreach (var race in races.EnumerateArray())
        {
            var raceName = race.GetProperty("raceName").GetString() ?? "Grand Prix";
            var circuitName = race.TryGetProperty("Circuit", out var circuit) && circuit.TryGetProperty("circuitName", out var cName)
                ? cName.GetString() ?? ""
                : "";

            // Helper to parse session
            void CheckSession(string sessionName, JsonElement parent, string propName)
            {
                if (parent.TryGetProperty(propName, out var sObj))
                {
                    var dStr = sObj.TryGetProperty("date", out var d) ? d.GetString() : null;
                    var tStr = sObj.TryGetProperty("time", out var t) ? t.GetString() : "00:00:00Z";
                    if (DateTime.TryParse($"{dStr}T{tStr}", out var dtUtc))
                    {
                        var utc = dtUtc.ToUniversalTime();
                        if (utc >= now && utc <= limit)
                        {
                            items.Add(new ScheduleItem
                            {
                                Title = $"{raceName} — {sessionName}",
                                Subtitle = circuitName,
                                Category = "Formula 1",
                                CategoryColor = "#E10600",
                                DateTimeUtc = utc,
                                IsF1 = true
                            });
                        }
                    }
                }
            }

            // Sprint
            CheckSession("Sprint", race, "Sprint");
            // Qualifying
            CheckSession("Qualifying", race, "Qualifying");
            // Race
            var raceDate = race.TryGetProperty("date", out var rd) ? rd.GetString() : null;
            var raceTime = race.TryGetProperty("time", out var rt) ? rt.GetString() : "00:00:00Z";
            if (DateTime.TryParse($"{raceDate}T{raceTime}", out var raceDtUtc))
            {
                var utc = raceDtUtc.ToUniversalTime();
                if (utc >= now && utc <= limit)
                {
                    items.Add(new ScheduleItem
                    {
                        Title = $"{raceName} — Grand Prix (Race)",
                        Subtitle = circuitName,
                        Category = "Formula 1",
                        CategoryColor = "#E10600",
                        DateTimeUtc = utc,
                        IsF1 = true
                    });
                }
            }
        }

        return items;
    }

    private async Task<List<ScheduleItem>> FetchBarcaScheduleAsync(string apiKey)
    {
        var items = new List<ScheduleItem>();
        // Team ID 81 is FC Barcelona in Football-Data.org
        var url = "https://api.football-data.org/v4/teams/81/matches?status=SCHEDULED";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Auth-Token", apiKey);
        request.Headers.Add("User-Agent", "PixelDogReminders/1.0");

        var response = await HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return items;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("matches", out var matches))
        {
            return items;
        }

        var now = DateTime.UtcNow;
        var limit = now.AddDays(30);

        foreach (var match in matches.EnumerateArray())
        {
            var utcDateStr = match.TryGetProperty("utcDate", out var ud) ? ud.GetString() : null;
            if (!DateTime.TryParse(utcDateStr, out var utcDate)) continue;

            utcDate = utcDate.ToUniversalTime();
            if (utcDate < now || utcDate > limit) continue;

            var competition = match.TryGetProperty("competition", out var comp) && comp.TryGetProperty("name", out var cName)
                ? cName.GetString() ?? "Football"
                : "Football";

            var homeTeam = match.TryGetProperty("homeTeam", out var ht) && ht.TryGetProperty("name", out var htName)
                ? htName.GetString() ?? "Home"
                : "Home";

            var awayTeam = match.TryGetProperty("awayTeam", out var at) && at.TryGetProperty("name", out var atName)
                ? atName.GetString() ?? "Away"
                : "Away";

            items.Add(new ScheduleItem
            {
                Title = $"{homeTeam} vs {awayTeam}",
                Subtitle = competition,
                Category = "FC Barcelona",
                CategoryColor = "#004D98",
                DateTimeUtc = utcDate,
                IsF1 = false
            });
        }

        return items;
    }
}
