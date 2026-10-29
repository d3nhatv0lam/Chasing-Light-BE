namespace ChasingLight.Api.Entities;

public class Profile
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public int TotalStars { get; set; } = 0;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public ICollection<UserRecord>? UserRecords { get; set; }
}