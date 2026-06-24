
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
}