namespace ChasingLight.Api.DTOs.Records;

public record SyncRecordsResponse
{
    public bool Success { get; init; }
    public int TotalStars { get; init; }
    public int UpdatedCount { get; init; }
    public IReadOnlyList<UserRecordDto> Records { get; init; } = Array.Empty<UserRecordDto>();
    public string Message { get; init; } = string.Empty;
}
