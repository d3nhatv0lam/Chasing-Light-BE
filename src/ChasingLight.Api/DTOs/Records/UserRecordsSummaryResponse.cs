namespace ChasingLight.Api.DTOs.Records;

public record UserRecordsSummaryResponse
{
    public int TotalStars { get; init; }
    public int CompletedStagesCount { get; init; }
    public IReadOnlyList<UserRecordDto> Records { get; init; } = Array.Empty<UserRecordDto>();
}
