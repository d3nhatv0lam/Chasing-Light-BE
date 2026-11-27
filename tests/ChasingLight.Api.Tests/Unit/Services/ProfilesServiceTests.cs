using System.Text;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Services;
using ChasingLight.Api.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChasingLight.Api.Tests.Unit.Services;

public class ProfilesServiceTests
{
    private static ProfilesService CreateSut(
        Infrastructures.Data.ChasingLightDbContext db,
        FakeStorageService? storage = null) =>
        new(db, storage ?? new FakeStorageService(), NullLogger<ProfilesService>.Instance);

    [Fact]
    public async Task GetProfileByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        using var db = TestDbContextFactory.CreateContext();
        var profile = TestDbContextFactory.CreateProfile(
            displayName: "Duc",
            avatarUrl: "https://cdn.example/a.png",
            totalStars: 15);
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var response = await sut.GetProfileByIdAsync(profile.Id);

        Assert.NotNull(response);
        Assert.Equal(profile.Id, response.Id);
        Assert.Equal("Duc", response.DisplayName);
        Assert.Equal("https://cdn.example/a.png", response.AvatarUrl);
        Assert.Equal(15, response.TotalStars);
    }

    [Fact]
    public async Task GetProfileByIdAsync_WhenMissing_ReturnsNull()
    {
        using var db = TestDbContextFactory.CreateContext();
        var sut = CreateSut(db);

        var response = await sut.GetProfileByIdAsync(Guid.NewGuid());

        Assert.Null(response);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_TrimsWhitespaceAndUpdatesTimestamp()
    {
        using var db = TestDbContextFactory.CreateContext();
        var oldUpdatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var profile = TestDbContextFactory.CreateProfile(displayName: "Old", updatedAt: oldUpdatedAt);
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var response = await sut.UpdateDisplayNameAsync(profile.Id, "   Minh Duc   ");

        Assert.Equal("Minh Duc", response.DisplayName);
        Assert.True(response.UpdatedAt > oldUpdatedAt);
        Assert.Equal("Minh Duc", (await db.Profiles.FindAsync(profile.Id))!.DisplayName);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_WhenProfileMissing_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.CreateContext();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.UpdateDisplayNameAsync(Guid.NewGuid(), "Test"));
    }

    [Fact]
    public async Task UploadAvatarAsync_GeneratesLowercaseExtensionFileNameAndSavesAvatarUrl()
    {
        using var db = TestDbContextFactory.CreateContext();
        var profile = TestDbContextFactory.CreateProfile(avatarUrl: null);
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();

        var fakeStorage = new FakeStorageService
        {
            PublicUrlToReturn = "https://supabase.test/storage/v1/object/public/avatars/new.png"
        };
        var sut = CreateSut(db, fakeStorage);

        var content = Encoding.UTF8.GetBytes("fake-png-bytes");
        await using var stream = new MemoryStream(content);
        var formFile = new FormFile(stream, 0, content.Length, "file", "Avatar.PNG")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var response = await sut.UploadAvatarAsync(profile.Id, formFile);

        Assert.NotNull(fakeStorage.LastUploadedFileName);
        Assert.StartsWith($"{profile.Id}_", fakeStorage.LastUploadedFileName);
        Assert.EndsWith(".png", fakeStorage.LastUploadedFileName);
        Assert.Equal(fakeStorage.PublicUrlToReturn, response.AvatarUrl);
        Assert.Equal(fakeStorage.PublicUrlToReturn, (await db.Profiles.FindAsync(profile.Id))!.AvatarUrl);
    }

    [Fact]
    public async Task DeleteAvatarAsync_ClearsAvatarUrl()
    {
        using var db = TestDbContextFactory.CreateContext();
        var profile = TestDbContextFactory.CreateProfile(avatarUrl: "https://cdn.example/old.png");
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var response = await sut.DeleteAvatarAsync(profile.Id);

        Assert.Null(response.AvatarUrl);
        Assert.Null((await db.Profiles.FindAsync(profile.Id))!.AvatarUrl);
    }
}
