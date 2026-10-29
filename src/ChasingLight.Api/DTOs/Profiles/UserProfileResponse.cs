namespace ChasingLight.Api.DTOs.Profiles;

public record UserProfileResponse(
    Guid Id,
    string? DisplayName,
    string? AvatarUrl,
    int TotalStars,
    DateTimeOffset UpdatedAt);