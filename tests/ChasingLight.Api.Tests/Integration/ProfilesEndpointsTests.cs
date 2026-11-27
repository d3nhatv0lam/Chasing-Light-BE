using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.DTOs.Profiles;
using ChasingLight.Api.Tests.Helpers;

namespace ChasingLight.Api.Tests.Integration;

public class ProfilesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProfilesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyProfile_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/profiles/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_WithUnsupportedExtension_Returns400InvalidFileFormat()
    {
        var user = TestDbContextFactory.CreateProfile();
        await _factory.SeedAsync(async db =>
        {
            db.Profiles.Add(user);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateAuthenticatedClient(user.Id);
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not-an-image"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        form.Add(fileContent, "file", "notes.txt");

        var response = await client.PostAsync("/api/v1/profiles/me/avatar", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("INVALID_FILE_FORMAT", error!.ErrorCode);
    }

    [Fact]
    public async Task UploadAvatar_WithValidPng_Returns200WithAvatarUrl()
    {
        var user = TestDbContextFactory.CreateProfile();
        await _factory.SeedAsync(async db =>
        {
            db.Profiles.Add(user);
            await db.SaveChangesAsync();
        });

        _factory.StorageService.PublicUrlToReturn = "https://cdn.example.test/avatars/test.png";
        var client = _factory.CreateAuthenticatedClient(user.Id);

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        form.Add(fileContent, "file", "avatar.png");

        var response = await client.PostAsync("/api/v1/profiles/me/avatar", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal("https://cdn.example.test/avatars/test.png", profile!.AvatarUrl);
    }

    [Fact]
    public async Task UpdateDisplayName_WithTooShortName_Returns400ValidationError()
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid());
        var response = await client.PatchAsJsonAsync("/api/v1/profiles/me", new UpdateProfileRequest("A"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", error!.ErrorCode);
    }
}
