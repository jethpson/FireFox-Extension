namespace user_service.Models;

public class WatchHistory
{
    
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int EpisodeNumber { get; set; }
    public int MinutesWatched { get; set; }
    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
}