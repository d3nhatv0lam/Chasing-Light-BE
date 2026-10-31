using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChasingLight.Api.DTOs.Auth;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ChasingLight.Api.Infrastructures.Identity;

public class SupabaseAuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseAuthService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SupabaseAuthService(
        HttpClient httpClient,
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseAuthService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.Url))
        {
            throw new InvalidOperationException("Supabase:Url is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.PublishableKey))
        {
            throw new InvalidOperationException("Supabase:PublishableKey is not configured.");
        }

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(_options.Url.TrimEnd('/') + "/");
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            email = request.Email.Trim(),
            password = request.Password
        };

        return await SendAuthRequestAsync("auth/v1/signup", payload, isRegistration: true, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            email = request.Email.Trim(),
            password = request.Password
        };

        return await SendAuthRequestAsync("auth/v1/token?grant_type=password", payload, isRegistration: false, cancellationToken);
    }

    public async Task<AuthResponse> LoginWithOAuthAsync(OAuthLoginRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            provider = request.Provider.Trim().ToLower(),
            id_token = request.IdToken.Trim(),
            nonce = request.Nonce
        };

        return await SendAuthRequestAsync("auth/v1/token?grant_type=id_token", payload, isRegistration: false, cancellationToken);
    }

    private async Task<AuthResponse> SendAuthRequestAsync(
        string endpoint, 
        object payload, 
        bool isRegistration, 
        CancellationToken cancellationToken)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
        requestMessage.Headers.Add("apikey", _options.PublishableKey);
        requestMessage.Content = JsonContent.Create(payload);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Supabase Auth service.");
            throw new AppException(
                "Unable to reach authentication service. Please try again later.", 
                "AUTH_SERVICE_UNAVAILABLE", 
                StatusCodes.Status503ServiceUnavailable);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorInfo = ParseSupabaseError(responseBody);
            _logger.LogWarning("Supabase Auth rejected request ({StatusCode}): {Message}", response.StatusCode, errorInfo.Message);

            throw new AppException(
                errorInfo.Message,
                errorInfo.ErrorCode,
                (int)response.StatusCode
            );
        }

        var authData = JsonSerializer.Deserialize<SupabaseAuthResponse>(responseBody, JsonOptions);
        var userId = authData?.User?.Id ?? authData?.RootId ?? Guid.Empty;
        var email = authData?.User?.Email ?? authData?.RootEmail ?? string.Empty;

        // Trường hợp đăng ký khi Supabase BẬT tính năng Confirm Email:
        // Supabase trả về 200 OK với thông tin User, nhưng session chưa được cấp (access_token = null).
        if (isRegistration && string.IsNullOrEmpty(authData?.AccessToken))
        {
            return new AuthResponse(
                AccessToken: null,
                RefreshToken: null,
                ExpiresIn: 0,
                UserId: userId,
                Email: email,
                RequiresEmailConfirmation: true
            );
        }

        if (authData?.AccessToken is null)
        {
            throw new AppException(
                "Authentication failed: No access token returned.",
                "AUTH_FAILED",
                StatusCodes.Status401Unauthorized
            );
        }

        return new AuthResponse(
            AccessToken: authData.AccessToken,
            RefreshToken: authData.RefreshToken ?? string.Empty,
            ExpiresIn: authData.ExpiresIn,
            UserId: userId,
            Email: email,
            RequiresEmailConfirmation: false
        );
    }

    private static (string Message, string ErrorCode) ParseSupabaseError(string responseBody)
    {
        try
        {
            var error = JsonSerializer.Deserialize<SupabaseErrorPayload>(responseBody, JsonOptions);
            var message = error?.ErrorDescription ?? error?.Msg ?? "Authentication failed.";
            var code = (error?.ErrorCode ?? error?.Error ?? "AUTH_ERROR").ToUpper().Replace(' ', '_');

            return (message, code);
        }
        catch
        {
            return ("Authentication failed.", "AUTH_ERROR");
        }
    }

    private record SupabaseAuthResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("user")] SupabaseUserData? User,
        [property: JsonPropertyName("id")] Guid? RootId,
        [property: JsonPropertyName("email")] string? RootEmail
    );

    private record SupabaseUserData(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("email")] string? Email
    );

    private record SupabaseErrorPayload(
        [property: JsonPropertyName("msg")] string? Msg,
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("error_description")] string? ErrorDescription,
        [property: JsonPropertyName("error_code")] string? ErrorCode
    );
}
