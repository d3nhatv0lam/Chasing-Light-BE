using System.ComponentModel.DataAnnotations;

namespace ChasingLight.Api.DTOs.Auth;

public record ResetPasswordRequest(
    [Required(ErrorMessage = "Recovery token is required.")]
    string RecoveryToken,

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters.")]
    string NewPassword
);
