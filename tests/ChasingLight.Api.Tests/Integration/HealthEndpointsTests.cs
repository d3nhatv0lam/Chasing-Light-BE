using System.Net;
using System.Net.Http.Json;

namespace ChasingLight.Api.Tests.Integration;

public class HealthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_WithoutAuth_Returns200Healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        Assert.Equal("healthy", payload.Status);
    }

    private sealed record HealthResponse(string Status);
}
