using System.ComponentModel.DataAnnotations;

namespace ChasingLight.Api.DTOs.Auth;

public record RefreshTokenRequest(
    [Required(ErrorMessage = "Refresh token is required.")]
    string RefreshToken
);
