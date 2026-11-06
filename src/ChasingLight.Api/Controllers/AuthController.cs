using ChasingLight.Api.DTOs.Auth;
using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChasingLight.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }
    
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }
    
    [HttpPost("oauth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginWithOAuth([FromBody] OAuthLoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginWithOAuthAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Yêu cầu gửi mã OTP 6 số về hòm thư để đặt lại mật khẩu.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(new { message = "If the email is registered, a 6-digit verification code has been sent." });
    }

    /// <summary>
    /// Xác thực mã OTP 6 số và nhận Recovery Token tạm thời.
    /// </summary>
    [HttpPost("verify-recovery-otp")]
    [ProducesResponseType(typeof(VerifyRecoveryOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyRecoveryOtp([FromBody] VerifyRecoveryOtpRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.VerifyRecoveryOtpAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Đặt mật khẩu mới bằng Recovery Token và nhận Game JWT để vào thẳng game.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(response);
    }
}
