using System.Net;
using System.Net.Http.Json;
using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.DTOs.Leaderboards;
using ChasingLight.Api.Tests.Helpers;

namespace ChasingLight.Api.Tests.Integration;

public class LeaderboardEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LeaderboardEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetGlobalLeaderboard_WithoutAuth_Returns200()
    {
        var profile = TestDbContextFactory.CreateProfile(displayName: "Leader", totalStars: 9);
        await _factory.SeedAsync(async db =>
        {
            db.Profiles.Add(profile);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/leaderboards/global");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<GlobalLeaderboardDto>>();
        Assert.NotNull(list);
        Assert.Contains(list, item => item.UserId == profile.Id && item.TotalStars == 9);
    }

    [Fact]
    public async Task GetStageLeaderboard_WhenStageNotFound_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/leaderboards/stage/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("RESOURCE_NOT_FOUND", error!.ErrorCode);
    }

    [Fact]
    public async Task UnknownRoute_Returns404EndpointNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ENDPOINT_NOT_FOUND", error!.ErrorCode);
    }
}
