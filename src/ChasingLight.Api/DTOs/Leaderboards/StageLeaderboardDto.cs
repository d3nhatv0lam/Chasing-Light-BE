namespace ChasingLight.Api.DTOs.Leaderboards;

public record StageLeaderboardDto(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    int BestTimeSeconds,
    int BestStepCount,
    short StarsEarned,
    int Rank);