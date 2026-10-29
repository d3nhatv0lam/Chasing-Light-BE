namespace ChasingLight.Api.DTOs.Auth;

public record AuthResponse(
    string? AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    Guid UserId,
    string Email,
    bool RequiresEmailConfirmation = false
);
