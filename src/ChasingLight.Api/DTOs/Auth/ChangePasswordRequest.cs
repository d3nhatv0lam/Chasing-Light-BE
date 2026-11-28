using System.ComponentModel.DataAnnotations;

namespace ChasingLight.Api.DTOs.Auth;

public record ChangePasswordRequest(
    [Required(ErrorMessage = "Current password is required.")]
    string CurrentPassword,

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters.")]
    string NewPassword
);
