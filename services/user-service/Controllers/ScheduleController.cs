
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Text.Json;

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

    [HttpPost("seed-catalog")]
    public async Task<IActionResult> SeedCatalog()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer PZ5V2e7A4o49aM77apjz5JJ6MHrqee");
        
        int page = 1;
        int total = 0;
        string cdnBase = "https://img.animeschedule.net/production/assets/public/img/anime/jpg/default/";

        while (true)
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
                var imageUrl = $"{cdnBase}{slug}.jpg";

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
            page++;
            await Task.Delay(500);
        }

        return Ok(new { total });
    }
}