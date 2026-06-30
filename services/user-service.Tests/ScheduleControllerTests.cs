using Microsoft.EntityFrameworkCore;
using user_service.Controllers;
using user_service.Models;
using Xunit;

namespace user_service.Tests;

public class ScheduleControllerTests
{
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetToday_ReturnsOnlyTodaysSchedule()
    {
        var db = GetDbContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        db.AnimeCatalog.Add(new AnimeCatalog { Slug = "one-piece", Title = "One Piece", ImageUrl = "img.jpg", Status = "airing" });
        db.DailySchedule.Add(new DailySchedule { Slug = "one-piece", EpisodeNumber = 1, AirDate = today });
        db.DailySchedule.Add(new DailySchedule { Slug = "one-piece", EpisodeNumber = 0, AirDate = yesterday });
        await db.SaveChangesAsync();

        var controller = new ScheduleController(db);
        var result = await controller.GetToday() as Microsoft.AspNetCore.Mvc.OkObjectResult;

        Assert.NotNull(result);
        var schedule = result.Value as System.Collections.IEnumerable;
        var count = schedule!.Cast<object>().Count();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetCatalog_FiltersBySearchTerm()
    {
        var db = GetDbContext();
        db.AnimeCatalog.Add(new AnimeCatalog { Slug = "one-piece", Title = "One Piece", ImageUrl = "img.jpg", Status = "airing" });
        db.AnimeCatalog.Add(new AnimeCatalog { Slug = "naruto", Title = "Naruto", ImageUrl = "img.jpg", Status = "finished" });
        await db.SaveChangesAsync();

        var controller = new ScheduleController(db);
        var result = await controller.GetCatalog("Piece") as Microsoft.AspNetCore.Mvc.OkObjectResult;

        Assert.NotNull(result);
        var results = result.Value as System.Collections.IEnumerable;
        var count = results!.Cast<object>().Count();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetCatalog_ReturnsAllWhenNoSearch()
    {
        var db = GetDbContext();
        db.AnimeCatalog.Add(new AnimeCatalog { Slug = "one-piece", Title = "One Piece", ImageUrl = "img.jpg", Status = "airing" });
        db.AnimeCatalog.Add(new AnimeCatalog { Slug = "naruto", Title = "Naruto", ImageUrl = "img.jpg", Status = "finished" });
        await db.SaveChangesAsync();

        var controller = new ScheduleController(db);
        var result = await controller.GetCatalog(null) as Microsoft.AspNetCore.Mvc.OkObjectResult;

        Assert.NotNull(result);
        var results = result.Value as System.Collections.IEnumerable;
        var count = results!.Cast<object>().Count();
        Assert.Equal(2, count);
    }
}