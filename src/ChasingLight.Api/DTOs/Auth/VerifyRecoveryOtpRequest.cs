using System.ComponentModel.DataAnnotations;

namespace ChasingLight.Api.DTOs.Auth;

public record VerifyRecoveryOtpRequest(
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    string Email,

    [Required(ErrorMessage = "Verification OTP is required.")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "OTP must be exactly 8 digits.")]
    string Otp
);
