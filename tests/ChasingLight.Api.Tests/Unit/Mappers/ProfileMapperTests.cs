using ChasingLight.Api.Entities;
using ChasingLight.Api.Mappers;

namespace ChasingLight.Api.Tests.Unit.Mappers;

public class ProfileMapperTests
{
    [Fact]
    public void ToResponse_MapsAllProfileProperties()
    {
        var id = Guid.NewGuid();
        var updatedAt = new DateTimeOffset(2026, 5, 1, 10, 30, 0, TimeSpan.Zero);
        var profile = new Profile
        {
            Id = id,
            DisplayName = "Bao Nghi",
            AvatarUrl = "https://cdn.example/avatar.webp",
            TotalStars = 42,
            UpdatedAt = updatedAt
        };

        var dto = profile.ToResponse();

        Assert.Equal(id, dto.Id);
        Assert.Equal("Bao Nghi", dto.DisplayName);
        Assert.Equal("https://cdn.example/avatar.webp", dto.AvatarUrl);
        Assert.Equal(42, dto.TotalStars);
        Assert.Equal(updatedAt, dto.UpdatedAt);
    }
}
