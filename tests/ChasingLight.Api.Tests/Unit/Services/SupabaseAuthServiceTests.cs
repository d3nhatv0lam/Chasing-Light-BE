using System.Net;
using System.Security.Claims;
using ChasingLight.Api.DTOs.Auth;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Infrastructures.Identity;
using ChasingLight.Api.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ChasingLight.Api.Tests.Unit.Services;

public class SupabaseAuthServiceTests
{
    private static readonly IOptions<SupabaseOptions> ValidOptions = Options.Create(new SupabaseOptions
    {
        Url = "https://example.supabase.co",
        PublishableKey = "sb_publishable_test_key"
    });

    private static SupabaseAuthService CreateSut(
        MockHttpMessageHandler handler,
        Infrastructures.Data.ChasingLightDbContext db,
        IOptions<SupabaseOptions>? options = null)
    {
        var httpClient = new HttpClient(handler);
        return new SupabaseAuthService(
            httpClient,
            options ?? ValidOptions,
            db,
            NullLogger<SupabaseAuthService>.Instance);
    }

    [Theory]
    [InlineData("", "key")]
    [InlineData("https://example.supabase.co", "")]
    public void Constructor_WhenSupabaseOptionsInvalid_ThrowsInvalidOperationException(string url, string key)
    {
        using var db = TestDbContextFactory.CreateContext();
        var handler = MockHttpMessageHandler.FromResponses();
        var badOptions = Options.Create(new SupabaseOptions { Url = url, PublishableKey = key });

        Assert.Throws<InvalidOperationException>(() => CreateSut(handler, db, badOptions));
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailConfirmationRequired_ReturnsRequiresEmailConfirmationTrue()
    {
        using var db = TestDbContextFactory.CreateContext();
        var userId = Guid.NewGuid();
        var handler = MockHttpMessageHandler.FromResponses(
            MockHttpMessageHandler.JsonResponse(HttpStatusCode.OK, $$"""
                {
                  "id": "{{userId}}",
                  "email": "user@example.com",
                  "access_token": null
                }
                """));

        var sut = CreateSut(handler, db);
        var result = await sut.RegisterAsync(new RegisterRequest("  user@example.com ", "123456"));

        Assert.True(result.RequiresEmailConfirmation);
        Assert.Null(result.AccessToken);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("auth/v1/signup", handler.LastRequest!.RequestUri!.PathAndQuery.TrimStart('/'));
        Assert.Contains("sb_publishable_test_key", handler.LastRequest.Headers.GetValues("apikey"));
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsValid_ReturnsTokens()
    {
        using var db = TestDbContextFactory.CreateContext();
        var userId = Guid.NewGuid();
        var handler = MockHttpMessageHandler.FromResponses(
            MockHttpMessageHandler.JsonResponse(HttpStatusCode.OK, $$"""
                {
                  "access_token": "jwt-access",
                  "refresh_token": "jwt-refresh",
                  "expires_in": 3600,
                  "user": { "id": "{{userId}}", "email": "player@example.com" }
                }
                """));

        var sut = CreateSut(handler, db);
        var result = await sut.LoginAsync(new LoginRequest("player@example.com", "secret123"));

        Assert.False(result.RequiresEmailConfirmation);
        Assert.Equal("jwt-access", result.AccessToken);
        Assert.Equal("jwt-refresh", result.RefreshToken);
        Assert.Equal(3600, result.ExpiresIn);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task LoginAsync_WhenSupabaseRejects_ThrowsAppExceptionWithNormalizedErrorCode()
    {
        using var db = TestDbContextFactory.CreateContext();
        var handler = MockHttpMessageHandler.FromResponses(
            MockHttpMessageHandler.JsonResponse(HttpStatusCode.BadRequest, """
                {
                  "error": "invalid grant",
                  "error_description": "Invalid login credentials",
                  "error_code": "invalid_credentials"
                }
                """));

        var sut = CreateSut(handler, db);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            sut.LoginAsync(new LoginRequest("player@example.com", "wrong")));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", ex.ErrorCode);
        Assert.Equal("Invalid login credentials", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WhenNetworkFails_Throws503AppException()
    {
        using var db = TestDbContextFactory.CreateContext();
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network down"));
        var sut = CreateSut(handler, db);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            sut.LoginAsync(new LoginRequest("player@example.com", "secret")));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, ex.StatusCode);
        Assert.Equal("AUTH_SERVICE_UNAVAILABLE", ex.ErrorCode);
    }

    [Fact]
    public async Task LoginWithOAuthAsync_FillsNullProfileFields_AndDoesNotOverwriteExistingCustomValues()
    {
        using var db = TestDbContextFactory.CreateContext();
        var emptyUserId = Guid.NewGuid();
        var customUserId = Guid.NewGuid();

        db.Profiles.AddRange(
            TestDbContextFactory.CreateProfile(id: emptyUserId, displayName: "", avatarUrl: null),
            TestDbContextFactory.CreateProfile(id: customUserId, displayName: "Custom Name", avatarUrl: "https://custom.cdn/me.png"));
        await db.SaveChangesAsync();

        var emptyToken = JwtTestHelper.CreateUnsignedToken(
            new Claim("name", "Google User"),
            new Claim("picture", "https://lh3.googleusercontent.com/pic.png"));
        var customToken = JwtTestHelper.CreateUnsignedToken(
            new Claim("name", "Should Not Overwrite"),
            new Claim("picture", "https://lh3.googleusercontent.com/other.png"));

        var handler = MockHttpMessageHandler.FromResponses(
            MockHttpMessageHandler.JsonResponse(HttpStatusCode.OK, $$"""
                {
                  "access_token": "acc-1",
                  "refresh_token": "ref-1",
                  "expires_in": 3600,
                  "user": { "id": "{{emptyUserId}}", "email": "empty@example.com" }
                }
                """),
            MockHttpMessageHandler.JsonResponse(HttpStatusCode.OK, $$"""
                {
                  "access_token": "acc-2",
                  "refresh_token": "ref-2",
                  "expires_in": 3600,
                  "user": { "id": "{{customUserId}}", "email": "custom@example.com" }
                }
                """));

        var sut = CreateSut(handler, db);

        await sut.LoginWithOAuthAsync(new OAuthLoginRequest("Google", emptyToken, "nonce-1"));
        await sut.LoginWithOAuthAsync(new OAuthLoginRequest("Google", customToken, "nonce-2"));

        var updatedEmpty = await db.Profiles.FindAsync(emptyUserId);
        Assert.Equal("Google User", updatedEmpty!.DisplayName);
        Assert.Equal("https://lh3.googleusercontent.com/pic.png", updatedEmpty.AvatarUrl);

        var untouchedCustom = await db.Profiles.FindAsync(customUserId);
        Assert.Equal("Custom Name", untouchedCustom!.DisplayName);
        Assert.Equal("https://custom.cdn/me.png", untouchedCustom.AvatarUrl);
    }

    [Fact]
    public async Task ForgotPasswordAsync_SwallowsNon429Errors_AndThrowsOn429()
    {
        using var db = TestDbContextFactory.CreateContext();
        var handler = MockHttpMessageHandler.FromResponses(
            MockHttpMessageHandler.JsonResponse(HttpStatusCode.BadRequest, """{"msg":"User not found"}"""),
            MockHttpMessageHandler.JsonResponse(HttpStatusCode.TooManyRequests, """{"msg":"Rate limit exceeded"}"""));

        var sut = CreateSut(handler, db);

        // Lần 1: 400 bị nuốt để tránh dò email
        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("missing@example.com"));

        // Lần 2: 429 phải ném lỗi
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            sut.ForgotPasswordAsync(new ForgotPasswordRequest("user@example.com")));
        Assert.Equal(StatusCodes.Status429TooManyRequests, ex.StatusCode);
        Assert.Equal("TOO_MANY_REQUESTS", ex.ErrorCode);
    }

    [Fact]
    public async Task VerifyRecoveryOtpAsync_WhenTokenMissing_Throws400AppException()
    {
        using var db = TestDbContextFactory.CreateContext();
        var handler = MockHttpMessageHandler.FromResponses(
            MockHttpMessageHandler.JsonResponse(HttpStatusCode.OK, """{"access_token": ""}"""));

        var sut = CreateSut(handler, db);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            sut.VerifyRecoveryOtpAsync(new VerifyRecoveryOtpRequest("user@example.com", "12345678")));

        Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
        Assert.Equal("OTP_VERIFICATION_FAILED", ex.ErrorCode);
    }

    [Fact]
    public async Task ResetPasswordAsync_UpdatesPasswordAndLogsInWithNewPassword()
    {
        using var db = TestDbContextFactory.CreateContext();
        var userId = Guid.NewGuid();
        var handler = MockHttpMessageHandler.FromResponses(
            MockHttpMessageHandler.JsonResponse(HttpStatusCode.OK, $$"""{"id": "{{userId}}", "email": "user@example.com"}"""),
            MockHttpMessageHandler.JsonResponse(HttpStatusCode.OK, $$"""
                {
                  "access_token": "new-jwt",
                  "refresh_token": "new-ref",
                  "expires_in": 3600,
                  "user": { "id": "{{userId}}", "email": "user@example.com" }
                }
                """));

        var sut = CreateSut(handler, db);
        var result = await sut.ResetPasswordAsync(new ResetPasswordRequest("recovery-tok", "newpass123"));

        Assert.Equal("new-jwt", result.AccessToken);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        Assert.Equal("recovery-tok", handler.Requests[0].Headers.Authorization!.Parameter);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
    }
}
