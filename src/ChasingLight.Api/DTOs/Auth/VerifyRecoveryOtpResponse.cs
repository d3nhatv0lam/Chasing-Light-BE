namespace ChasingLight.Api.DTOs.Auth;

public record VerifyRecoveryOtpResponse(
    string RecoveryToken,
    int ExpiresIn,
    string Email
);
