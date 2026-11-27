using System.Net;
using System.Net.Http.Json;
using ChasingLight.Api.DTOs.Auth;
using ChasingLight.Api.DTOs.Common;

namespace ChasingLight.Api.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_Returns400ValidationError()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("not-an-email", "123456"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", error!.ErrorCode);
    }

    [Fact]
    public async Task Login_WithValidPayload_Returns200AuthResponse()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("player@example.com", "123456"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("acc", auth!.AccessToken);
        Assert.Equal("player@example.com", auth.Email);
    }

    [Fact]
    public async Task VerifyRecoveryOtp_WithNonEightDigitOtp_Returns400ValidationError()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/verify-recovery-otp",
            new VerifyRecoveryOtpRequest("player@example.com", "123456"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", error!.ErrorCode);
    }
}
