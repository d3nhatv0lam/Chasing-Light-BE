using System.Net;
using System.Net.Http.Json;
using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.DTOs.Records;
using ChasingLight.Api.Tests.Helpers;

namespace ChasingLight.Api.Tests.Integration;

public class RecordsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RecordsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyRecords_WithoutAuthHeader_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/records/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("UNAUTHORIZED", error!.ErrorCode);
    }

    [Fact]
    public async Task CompleteStage_WithInvalidStars_Returns400ValidationError()
    {
        var userId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/api/v1/records/complete", new CompleteStageRequest
        {
            StageId = Guid.NewGuid(),
            TimeDoneSeconds = 10,
            StepCount = 5,
            StarsEarned = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", error!.ErrorCode);
        Assert.Contains("Stars earned must be between 1 and 3", error.Message);
    }

    [Fact]
    public async Task CompleteStage_WithValidData_PersistsAndReturns200()
    {
        var user = TestDbContextFactory.CreateProfile();
        var stage = TestDbContextFactory.CreateStage();
        await _factory.SeedAsync(async db =>
        {
            db.Profiles.Add(user);
            db.Stages.Add(stage);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateAuthenticatedClient(user.Id);
        var response = await client.PostAsJsonAsync("/api/v1/records/complete", new CompleteStageRequest
        {
            StageId = stage.Id,
            TimeDoneSeconds = 42,
            StepCount = 16,
            StarsEarned = 3
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CompleteStageResponse>();
        Assert.True(payload!.Success);
        Assert.True(payload.IsNewBest);
        Assert.Equal(3, payload.TotalStars);
    }
}
