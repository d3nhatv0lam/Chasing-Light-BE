using ChasingLight.Api.DTOs.Auth;

namespace ChasingLight.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginWithOAuthAsync(OAuthLoginRequest request, CancellationToken cancellationToken = default);
}
