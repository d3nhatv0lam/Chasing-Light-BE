namespace ChasingLight.Api.DTOs.Records;

public record CompleteStageResponse
{
    public bool Success { get; init; }
    public bool IsNewBest { get; init; }
    public int TotalStars { get; init; }
    public int BestTimeSeconds { get; init; }
    public int BestStepCount { get; init; }
    public short StarsEarned { get; init; }
    public string Message { get; init; } = string.Empty;
}