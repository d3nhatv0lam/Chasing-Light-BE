using System.ComponentModel.DataAnnotations;

namespace ChasingLight.Api.DTOs.Auth;

public record ForgotPasswordRequest(
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    string Email
);
