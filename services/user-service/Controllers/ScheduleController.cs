using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace user_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly AppDbContext _db;

    public ScheduleController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var schedule = await _db.DailySchedule
            .Where(s => s.AirDate == today)
            .Join(_db.AnimeCatalog,
                s => s.Slug,
                a => a.Slug,
                (s, a) => new
                {
                    a.Title,
                    a.Slug,
                    a.ImageUrl,
                    s.EpisodeNumber,
                    s.AirDate
                })
            .ToListAsync();

        return Ok(schedule);
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog([FromQuery] string? search)
    {
        var query = _db.AnimeCatalog.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(a => a.Title.Contains(search));

        var results = await query
            .Select(a => new { a.Title, a.Slug, a.ImageUrl, a.Status })
            .Take(50)
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("anilist")]
    public async Task<IActionResult> GetAnilistId([FromQuery] string slug)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer PZ5V2e7A4o49aM77apjz5JJ6MHrqee");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        var response = await client.GetFromJsonAsync<JsonElement>(
            $"https://animeschedule.net/api/v3/anime/{slug}");

        if (!response.TryGetProperty("websites", out var websites))
            return NotFound();

        var anilistUrl = websites.TryGetProperty("aniList", out var al) ? al.GetString() : null;
        if (anilistUrl == null) return NotFound();

        var anilistId = anilistUrl.Split("/anime/").ElementAtOrDefault(1)?.Split("/").FirstOrDefault();

        return Ok(new { anilistId, slug });
    }

    [HttpGet("my-today")]
    public async Task<IActionResult> GetMyToday()
    {
        var entraId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (entraId == null) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.EntraId == entraId);
        if (user == null) return Unauthorized();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get today's schedule for only the user's tracked series
        var myShows = await _db.DailySchedule
            .Where(s => s.AirDate == today)
            .Join(_db.AnimeCatalog,
                s => s.Slug,
                a => a.Slug,
                (s, a) => new { s, a })
            .Join(_db.TrackedSeries.Where(t => t.UserId == user.Id),
                sa => sa.s.Slug,
                t => t.Slug,
                (sa, t) => new { sa.s, sa.a })
            .Select(x => new
            {
                x.a.Title,
                x.a.Slug,
                x.a.ImageUrl,
                x.s.EpisodeNumber,
                x.s.AirDate
            })
            .ToListAsync();

        // Filter out watched episodes (5+ minutes)
        var watchedEpisodes = await _db.WatchHistory
            .Where(w => w.UserId == user.Id && w.MinutesWatched >= 5)
            .Select(w => new { w.Slug, w.EpisodeNumber })
            .ToListAsync();

        var unwatched = myShows
            .Where(s => !watchedEpisodes.Any(w => w.Slug == s.Slug && w.EpisodeNumber == s.EpisodeNumber))
            .ToList();

        return Ok(unwatched);
    }

    [HttpPost("seed-catalog")]
    public async Task<IActionResult> SeedCatalog([FromQuery] int startPage = 1)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer PZ5V2e7A4o49aM77apjz5JJ6MHrqee");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        int page = startPage;
        int total = 0;
        const string CDN_BASE = "https://img.animeschedule.net/production/assets/public/img/";

        while (true)
        {
            try
            {
                var response = await client.GetFromJsonAsync<JsonElement>(
                    $"https://animeschedule.net/api/v3/anime?page={page}");

                var animeList = response.GetProperty("anime").EnumerateArray().ToList();
                if (!animeList.Any()) break;

                foreach (var show in animeList)
                {
                    var slug = show.GetProperty("route").GetString() ?? "";
                    var title = show.GetProperty("title").GetString() ?? "";
                    var status = show.GetProperty("status").GetString() ?? "";
                    var imageVersionRoute = show.TryGetProperty("imageVersionRoute", out var ivr)
                        ? ivr.GetString() ?? ""
                        : "";
                    var imageUrl = string.IsNullOrEmpty(imageVersionRoute)
                        ? $"{CDN_BASE}anime/jpg/default/{slug}.jpg"
                        : $"{CDN_BASE}{imageVersionRoute}";

                    var exists = await _db.AnimeCatalog.AnyAsync(a => a.Slug == slug);
                    if (!exists)
                    {
                        _db.AnimeCatalog.Add(new user_service.Models.AnimeCatalog
                        {
                            Slug = slug,
                            Title = title,
                            ImageUrl = imageUrl,
                            Status = status,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _db.SaveChangesAsync();
                total += animeList.Count;
                Console.WriteLine($"Page {page} done — {total} processed this run");
                page++;
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on page {page}: {ex.Message}");
                return Ok(new { total, stoppedAtPage = page, error = ex.Message });
            }
        }

        return Ok(new { total, completed = true });
    }
}