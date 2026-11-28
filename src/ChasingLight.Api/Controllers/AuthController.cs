using System.Security.Claims;
using ChasingLight.Api.DTOs.Auth;
using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChasingLight.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }
    
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }
    
    [HttpPost("oauth")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginWithOAuth([FromBody] OAuthLoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginWithOAuthAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Làm mới Access Token bằng Refresh Token hợp lệ.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Yêu cầu gửi mã OTP 6 số về hòm thư để đặt lại mật khẩu.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Đổi mật khẩu chủ động (yêu cầu xác minh mật khẩu hiện tại).
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirst("email")?.Value
            ?? throw new AppException("Cannot determine user email from token.", "INVALID_TOKEN", StatusCodes.Status401Unauthorized);

        var response = await _authService.ChangePasswordAsync(userEmail, request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Thiết lập mật khẩu lần đầu cho tài khoản OAuth (chưa có mật khẩu).
    /// </summary>
    [HttpPost("set-password")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request, CancellationToken cancellationToken)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirst("email")?.Value
            ?? throw new AppException("Cannot determine user email from token.", "INVALID_TOKEN", StatusCodes.Status401Unauthorized);

        var authHeader = HttpContext.Request.Headers.Authorization.ToString();
        var accessToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..].Trim()
            : authHeader.Trim();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new AppException("Access token is missing in Authorization header.", "UNAUTHORIZED", StatusCodes.Status401Unauthorized);
        }

        var response = await _authService.SetPasswordAsync(accessToken, userEmail, request, cancellationToken);
        return Ok(response);
    }
}
