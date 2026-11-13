using System.ComponentModel.DataAnnotations;

namespace ChasingLight.Api.DTOs.Profiles;

public record UpdateProfileRequest(
    [MaxLength(150, ErrorMessage = "DisplayName cannot exceed 150 characters.")]
    string? DisplayName,

    [MaxLength(1000, ErrorMessage = "AvatarUrl cannot exceed 1000 characters.")]
    string? AvatarUrl
);
