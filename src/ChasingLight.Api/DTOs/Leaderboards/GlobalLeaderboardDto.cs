namespace ChasingLight.Api.DTOs.Leaderboards;

public record GlobalLeaderboardDto(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    int TotalStars,
    int Rank);