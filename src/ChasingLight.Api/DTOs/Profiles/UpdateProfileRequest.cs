using System.ComponentModel.DataAnnotations;

namespace ChasingLight.Api.DTOs.Profiles;

public record UpdateProfileRequest(
    [Required(ErrorMessage = "DisplayName is required.")]
    [MinLength(2, ErrorMessage = "DisplayName must be at least 2 characters long.")]
    [MaxLength(150, ErrorMessage = "DisplayName cannot exceed 150 characters.")]
    string DisplayName
);
