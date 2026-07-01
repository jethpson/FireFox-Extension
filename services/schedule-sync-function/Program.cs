using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using schedule_sync_function.Data;
using schedule_sync_function.Models;
using System.Text.Json;

var services = new ServiceCollection();
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(Environment.GetEnvironmentVariable("DefaultConnection")));
services.AddHttpClient();

var provider = services.BuildServiceProvider();
var db = provider.GetRequiredService<AppDbContext>();
var httpFactory = provider.GetRequiredService<IHttpClientFactory>();
var http = httpFactory.CreateClient();

const string API_TOKEN = "PZ5V2e7A4o49aM77apjz5JJ6MHrqee";
const string CDN_BASE = "https://img.animeschedule.net/production/assets/public/img/";

http.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_TOKEN}");
http.DefaultRequestHeaders.Add("Accept", "application/json");

var today = DateOnly.FromDateTime(DateTime.UtcNow);
var todayStr = today.ToString("yyyy-MM-dd");

Console.WriteLine($"Schedule sync started for {todayStr}");

var response = await http.GetAsync("https://animeschedule.net/api/v3/timetables");
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
    var imageVersionRoute = item.TryGetProperty("imageVersionRoute", out var ivr) 
        ? ivr.GetString() ?? "" 
        : "";
    var imageUrl = string.IsNullOrEmpty(imageVersionRoute)
        ? $"{CDN_BASE}anime/jpg/default/{slug}.jpg"
        : $"{CDN_BASE}{imageVersionRoute}";

    var key = (slug, episode);
    if (seen.Contains(key)) continue;
    seen.Add(key);

    var existing = await db.AnimeCatalog.FirstOrDefaultAsync(a => a.Slug == slug);
    if (existing == null)
    {

        db.AnimeCatalog.Add(new AnimeCatalog
        {

            Slug = slug,
            Title = title,
            ImageUrl = imageUrl,
            Status = "airing",
            UpdatedAt = DateTime.UtcNow
        });
    }
    else
    {

        existing.ImageUrl = imageUrl;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    var scheduleExists = await db.DailySchedule.AnyAsync(
        s => s.Slug == slug && s.EpisodeNumber == episode && s.AirDate == today);

    if (!scheduleExists)
    {

        db.DailySchedule.Add(new DailySchedule
        {
            
            Slug = slug,
            EpisodeNumber = episode,
            AirDate = today
        });
    }
}

await db.SaveChangesAsync();
Console.WriteLine($"Schedule sync complete for {todayStr}");