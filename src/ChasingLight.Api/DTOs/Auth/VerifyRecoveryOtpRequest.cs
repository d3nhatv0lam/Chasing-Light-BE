using System.ComponentModel.DataAnnotations;

namespace ChasingLight.Api.DTOs.Auth;

public record VerifyRecoveryOtpRequest(
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    string Email,

    [Required(ErrorMessage = "Verification OTP is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits.")]
    string Otp
);
