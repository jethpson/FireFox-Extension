using Microsoft.EntityFrameworkCore;
using schedule_sync_function.Models;

namespace schedule_sync_function.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AnimeCatalog> AnimeCatalog { get; set; }
    public DbSet<DailySchedule> DailySchedule { get; set; }
}