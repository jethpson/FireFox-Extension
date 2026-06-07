namespace user_service.Models;

public class TrackedSeries
{
    
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
}