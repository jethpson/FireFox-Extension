namespace schedule_sync_function.Models;

public class DailySchedule
{
    
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int EpisodeNumber { get; set; }
    public DateOnly AirDate { get; set; }
}