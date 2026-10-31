namespace ChasingLight.Api.Entities;

public class UserRecord
{
    public Guid UserId { get; set; }
    public Profile Profile { get; set; } = null!;
    public Guid StageId { get; set; }
    public Stage Stage { get; set; } = null!;
    public int BestTimeSeconds { get; set; }
    public int BestStepCount { get; set; }
    public short StarsEarned { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}