using ChasingLight.Api.Entities;
using ChasingLight.Api.Infrastructures.Data;
using Microsoft.EntityFrameworkCore;

namespace ChasingLight.Api.Tests.Helpers;

/// <summary>
/// Tạo DbContext cô lập bằng EF Core InMemory cho từng unit test và hỗ trợ seed dữ liệu mẫu.
/// </summary>
public static class TestDbContextFactory
{
    public static ChasingLightDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ChasingLightDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options;

        return new ChasingLightDbContext(options);
    }

    public static Profile CreateProfile(
        Guid? id = null,
        string? displayName = "Player",
        string? avatarUrl = null,
        int totalStars = 0,
        DateTimeOffset? updatedAt = null)
    {
        return new Profile
        {
            Id = id ?? Guid.NewGuid(),
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            TotalStars = totalStars,
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow
        };
    }

    public static Stage CreateStage(
        Guid? id = null,
        int stageNumber = 1,
        Difficulty difficulty = Difficulty.Easy)
    {
        return new Stage
        {
            Id = id ?? Guid.NewGuid(),
            StageNumber = stageNumber,
            Difficulty = difficulty,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static UserRecord CreateRecord(
        Guid userId,
        Guid stageId,
        short starsEarned = 1,
        int bestTimeSeconds = 60,
        int bestStepCount = 20,
        DateTimeOffset? updatedAt = null)
    {
        return new UserRecord
        {
            UserId = userId,
            StageId = stageId,
            StarsEarned = starsEarned,
            BestTimeSeconds = bestTimeSeconds,
            BestStepCount = bestStepCount,
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow
        };
    }
}
