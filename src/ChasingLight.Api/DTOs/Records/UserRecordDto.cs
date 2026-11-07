using ChasingLight.Api.Entities;

namespace ChasingLight.Api.DTOs.Records;

public record UserRecordDto
{
    public Guid StageId { get; init; }
    public int StageNumber { get; init; }
    public Difficulty Difficulty { get; init; }
    public short StarsEarned { get; init; }
    public int BestTimeSeconds { get; init; }
    public int BestStepCount { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
