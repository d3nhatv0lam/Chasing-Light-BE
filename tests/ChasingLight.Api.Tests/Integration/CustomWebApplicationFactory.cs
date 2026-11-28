using System.Security.Claims;
using System.Text.Encodings.Web;
using ChasingLight.Api.DTOs.Auth;
using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.Infrastructures.Data;
using ChasingLight.Api.Services.Interfaces;
using ChasingLight.Api.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChasingLight.Api.Tests.Integration;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"IntegrationDb_{Guid.NewGuid():N}";

    public FakeStorageService StorageService { get; } = new();
    public FakeAuthService AuthService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SupabaseDatabase", "Host=localhost;Database=test_db;Username=postgres;Password=postgres");
        builder.UseSetting("Supabase:Url", "https://example.supabase.co");
        builder.UseSetting("Supabase:PublishableKey", "test_publishable_key");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ChasingLightDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ChasingLightDbContext>>();

            services.AddDbContext<ChasingLightDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IStorageService>();
            services.AddSingleton<IStorageService>(StorageService);

            services.RemoveAll<IAuthService>();
            services.AddSingleton<IAuthService>(AuthService);

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        return client;
    }

    public async Task SeedAsync(Func<ChasingLightDbContext, Task> seedAction)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChasingLightDbContext>();
        await seedAction(db);
    }
}

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestAuth";
    public const string UserIdHeader = "X-Test-UserId";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing test user id"));
        }

        var claims = new[] { new Claim("sub", userId) };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        await Response.WriteAsJsonAsync(new ErrorResponse(
            StatusCodes.Status401Unauthorized,
            "UNAUTHORIZED",
            "Token is invalid or user sub is missing"));
    }
}

public sealed class FakeAuthService : IAuthService
{
    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AuthResponse("acc", "ref", 3600, Guid.NewGuid(), request.Email));

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AuthResponse("acc", "ref", 3600, Guid.NewGuid(), request.Email));

    public Task<AuthResponse> LoginWithOAuthAsync(OAuthLoginRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AuthResponse("acc", "ref", 3600, Guid.NewGuid(), "oauth@example.com"));

    public Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AuthResponse("new-acc", "new-ref", 3600, Guid.NewGuid(), "user@example.com"));

    public Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<VerifyRecoveryOtpResponse> VerifyRecoveryOtpAsync(VerifyRecoveryOtpRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new VerifyRecoveryOtpResponse("rec-token", 300, request.Email));

    public Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AuthResponse("acc", "ref", 3600, Guid.NewGuid(), "user@example.com"));

    public Task<AuthResponse> ChangePasswordAsync(string userEmail, ChangePasswordRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AuthResponse("acc", "ref", 3600, Guid.NewGuid(), userEmail));

    public Task<AuthResponse> SetPasswordAsync(string userAccessToken, string userEmail, SetPasswordRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AuthResponse("acc", "ref", 3600, Guid.NewGuid(), userEmail));
}
