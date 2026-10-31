using System.ComponentModel.DataAnnotations;

namespace ChasingLight.Api.DTOs.Auth;

public record RegisterRequest(
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    string Email,

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    string Password
);
