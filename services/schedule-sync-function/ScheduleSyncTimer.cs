using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using schedule_sync_function.Data;
using schedule_sync_function.Models;
using System.Text.Json;

namespace schedule_sync_function;

public class ScheduleSyncTimer
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private const string API_TOKEN = "PZ5V2e7A4o49aM77apjz5JJ6MHrqee";
    private const string CDN_BASE = "https://img.animeschedule.net/production/assets/public/img/anime/jpg/default/";

    public ScheduleSyncTimer(AppDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _http = httpClientFactory.CreateClient();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_TOKEN}");
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    [Function("ScheduleSyncTimer")]
    public async Task Run([TimerTrigger("0 0 * * *")] TimerInfo timerInfo)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStr = today.ToString("yyyy-MM-dd");

        Console.WriteLine($"Schedule sync started for {todayStr}");

        var response = await _http.GetAsync("https://animeschedule.net/api/v3/timetables");
        response.EnsureSuccessStatusCode();

        var data = await JsonSerializer.DeserializeAsync<JsonElement>(
            await response.Content.ReadAsStreamAsync());

        var seen = new HashSet<(string, int)>();

        foreach (var item in data.EnumerateArray())
        {
            var episodeDateRaw = item.GetProperty("episodeDate").GetString() ?? "";
            if (!episodeDateRaw.StartsWith(todayStr)) continue;

            var title = item.GetProperty("title").GetString() ?? "Unknown";
            var episode = item.GetProperty("episodeNumber").GetInt32();
            var slug = item.GetProperty("route").GetString() ?? "";

            var key = (slug, episode);
            if (seen.Contains(key)) continue;
            seen.Add(key);

            // Add to catalog if not exists
            var exists = await _db.AnimeCatalog.AnyAsync(a => a.Slug == slug);
            if (!exists)
            {
                _db.AnimeCatalog.Add(new AnimeCatalog
                {
                    Slug = slug,
                    Title = title,
                    ImageUrl = $"{CDN_BASE}{slug}.jpg",
                    Status = "airing",
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Add to daily schedule if not exists
            var scheduleExists = await _db.DailySchedule.AnyAsync(
                s => s.Slug == slug && s.EpisodeNumber == episode && s.AirDate == today);

            if (!scheduleExists)
            {
                _db.DailySchedule.Add(new DailySchedule
                {
                    Slug = slug,
                    EpisodeNumber = episode,
                    AirDate = today
                });
            }
        }

        await _db.SaveChangesAsync();
        Console.WriteLine($"Schedule sync complete for {todayStr}");
    }
}