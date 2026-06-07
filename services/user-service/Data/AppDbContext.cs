using Microsoft.EntityFrameworkCore;
using user_service.Models;

public class AppDbContext : DbContext
{

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<TrackedSeries> TrackedSeries { get; set; }
    public DbSet<WatchHistory> WatchHistory { get; set; }
    public DbSet<DailySchedule> DailySchedule { get; set; }
    public DbSet<AnimeCatalog> AnimeCatalog { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        modelBuilder.Entity<TrackedSeries>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId);

        modelBuilder.Entity<WatchHistory>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId);
    }
}