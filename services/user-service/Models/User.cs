namespace user_service.Models;

public class User
{
    
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string EntraId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}