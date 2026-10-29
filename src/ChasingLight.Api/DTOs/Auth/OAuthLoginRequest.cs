using System.ComponentModel.DataAnnotations;

namespace ChasingLight.Api.DTOs.Auth;

public record OAuthLoginRequest(
    [Required(ErrorMessage = "OAuth provider is required.")]
    string Provider,

    [Required(ErrorMessage = "OAuth IdToken is required.")]
    string IdToken,

    string? Nonce = null
);
