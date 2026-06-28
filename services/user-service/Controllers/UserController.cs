using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using user_service.Models;

namespace user_service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly AppDbContext _db;

    public UserController(AppDbContext db)
    {
        _db = db;
    }

    private string? GetEntraId() =>
        User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<user_service.Models.User?> GetUser()
    {
        var entraId = GetEntraId();
        if (entraId == null) return null;
        return await _db.Users.FirstOrDefaultAsync(u => u.EntraId == entraId);
    }

    // GET /api/user/tracked
    [HttpGet("tracked")]
    public async Task<IActionResult> GetTracked()
    {
        var user = await GetUser();
        if (user == null) return Unauthorized();

        var tracked = await _db.TrackedSeries
            .Where(t => t.UserId == user.Id)
            .Join(_db.AnimeCatalog,
                t => t.Slug,
                a => a.Slug,
                (t, a) => new
                {
                    a.Title,
                    a.Slug,
                    a.ImageUrl,
                    a.Status,
                    t.AddedAt
                })
            .ToListAsync();

        return Ok(tracked);
    }

    // POST /api/user/tracked
    [HttpPost("tracked")]
    public async Task<IActionResult> AddTracked([FromBody] TrackRequest req)
    {
        var user = await GetUser();
        if (user == null) return Unauthorized();

        var exists = await _db.TrackedSeries
            .AnyAsync(t => t.UserId == user.Id && t.Slug == req.Slug);

        if (exists) return Conflict("Already tracking this series.");

        _db.TrackedSeries.Add(new TrackedSeries
        {
            UserId = user.Id,
            Slug = req.Slug,
            AddedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok();
    }

    // DELETE /api/user/tracked/{slug}
    [HttpDelete("tracked/{slug}")]
    public async Task<IActionResult> RemoveTracked(string slug)
    {
        var user = await GetUser();
        if (user == null) return Unauthorized();

        var tracked = await _db.TrackedSeries
            .FirstOrDefaultAsync(t => t.UserId == user.Id && t.Slug == slug);

        if (tracked == null) return NotFound();

        _db.TrackedSeries.Remove(tracked);
        await _db.SaveChangesAsync();
        return Ok();
    }

    // POST /api/user/watched
    [HttpPost("watched")]
    public async Task<IActionResult> LogWatched([FromBody] WatchRequest req)
    {
        var user = await GetUser();
        if (user == null) return Unauthorized();

        var existing = await _db.WatchHistory.FirstOrDefaultAsync(w =>
            w.UserId == user.Id &&
            w.Slug == req.Slug &&
            w.EpisodeNumber == req.EpisodeNumber);

        if (existing != null)
        {
            existing.MinutesWatched = req.MinutesWatched;
            existing.WatchedAt = DateTime.UtcNow;
        }
        else
        {
            _db.WatchHistory.Add(new WatchHistory
            {
                UserId = user.Id,
                Slug = req.Slug,
                EpisodeNumber = req.EpisodeNumber,
                MinutesWatched = req.MinutesWatched,
                WatchedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    // GET /api/user/watched
    [HttpGet("watched")]
    public async Task<IActionResult> GetWatched()
    {
        var user = await GetUser();
        if (user == null) return Unauthorized();

        var history = await _db.WatchHistory
            .Where(w => w.UserId == user.Id)
            .Select(w => new
            {
                w.Slug,
                w.EpisodeNumber,
                w.MinutesWatched,
                w.WatchedAt,
                Watched = w.MinutesWatched >= 5
            })
            .ToListAsync();

        return Ok(history);
    }
}

public record TrackRequest(string Slug);
public record WatchRequest(string Slug, int EpisodeNumber, int MinutesWatched);