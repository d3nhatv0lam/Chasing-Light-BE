namespace ChasingLight.Api.Entities;

public class Stage
{
    public Guid Id { get; set; } // GUID từ Unity StageAsset
    public int StageNumber { get; set; }
    public Difficulty Difficulty { get; set; } = Difficulty.Easy;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
 
    public ICollection<UserRecord> UserRecords { get; set; } = new List<UserRecord>();
}