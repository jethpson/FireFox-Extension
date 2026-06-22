using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using user_service.Models;

namespace user_service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{

    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {

        _db = db;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login()
    {

        var entraId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue("preferred_username") ?? User.FindFirstValue(ClaimTypes.Email);

        if (entraId == null) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.EntraId == entraId);

        if (user == null)
        {

            user = new User
            {

                EntraId = entraId,
                Email = email ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        return Ok(new { user.Id, user.Email, user.CreatedAt });
    }
}