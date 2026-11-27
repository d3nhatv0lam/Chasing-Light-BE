using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Services;
using ChasingLight.Api.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChasingLight.Api.Tests.Unit.Services;

public class LeaderboardServiceTests
{
    private static LeaderboardService CreateSut(Infrastructures.Data.ChasingLightDbContext db) =>
        new(db, NullLogger<LeaderboardService>.Instance);

    [Fact]
    public async Task GetGlobalLeaderboardAsync_ExcludesZeroStars_AndOrdersByTotalStarsDescThenUpdatedAtAsc()
    {
        using var db = TestDbContextFactory.CreateContext();
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var zeroStars = TestDbContextFactory.CreateProfile(displayName: "Zero", totalStars: 0, updatedAt: baseTime);
        var topPlayer = TestDbContextFactory.CreateProfile(displayName: "Top", totalStars: 12, updatedAt: baseTime.AddMinutes(10));
        var tiedEarlier = TestDbContextFactory.CreateProfile(displayName: "TieEarly", totalStars: 8, updatedAt: baseTime.AddMinutes(1));
        var tiedLater = TestDbContextFactory.CreateProfile(displayName: "TieLate", totalStars: 8, updatedAt: baseTime.AddMinutes(5));

        db.Profiles.AddRange(zeroStars, topPlayer, tiedLater, tiedEarlier);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var leaderboard = await sut.GetGlobalLeaderboardAsync();

        Assert.Equal(3, leaderboard.Count);
        Assert.Equal([topPlayer.Id, tiedEarlier.Id, tiedLater.Id], leaderboard.Select(x => x.UserId).ToArray());
        Assert.Equal([1, 2, 3], leaderboard.Select(x => x.Rank).ToArray());
    }

    [Theory]
    [InlineData(-5, 50)]
    [InlineData(0, 50)]
    [InlineData(10, 10)]
    [InlineData(150, 100)]
    public async Task GetGlobalLeaderboardAsync_NormalizesAndCapsLimit(int requestedLimit, int expectedCount)
    {
        using var db = TestDbContextFactory.CreateContext();
        for (var i = 1; i <= 110; i++)
        {
            db.Profiles.Add(TestDbContextFactory.CreateProfile(displayName: $"P{i}", totalStars: i));
        }
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var leaderboard = await sut.GetGlobalLeaderboardAsync(requestedLimit);

        Assert.Equal(expectedCount, leaderboard.Count);
    }

    [Fact]
    public async Task GetStageLeaderboardAsync_WhenStageDoesNotExist_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.CreateContext();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetStageLeaderboardAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetStageLeaderboardAsync_OrdersByStarsDescTimeAscStepsAscUpdatedAtAsc_AndFiltersByStage()
    {
        using var db = TestDbContextFactory.CreateContext();
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var targetStage = TestDbContextFactory.CreateStage(stageNumber: 1);
        var otherStage = TestDbContextFactory.CreateStage(stageNumber: 2);
        db.Stages.AddRange(targetStage, otherStage);

        var p1 = TestDbContextFactory.CreateProfile(displayName: "3StarsFast");
        var p2 = TestDbContextFactory.CreateProfile(displayName: "3StarsSlowLessSteps");
        var p3 = TestDbContextFactory.CreateProfile(displayName: "3StarsSlowMoreStepsEarly");
        var p4 = TestDbContextFactory.CreateProfile(displayName: "3StarsSlowMoreStepsLate");
        var p5 = TestDbContextFactory.CreateProfile(displayName: "2Stars");
        var pOther = TestDbContextFactory.CreateProfile(displayName: "OtherStageOnly");
        db.Profiles.AddRange(p1, p2, p3, p4, p5, pOther);

        db.UserRecords.AddRange(
            TestDbContextFactory.CreateRecord(p5.Id, targetStage.Id, starsEarned: 2, bestTimeSeconds: 10, bestStepCount: 5, updatedAt: baseTime),
            TestDbContextFactory.CreateRecord(p4.Id, targetStage.Id, starsEarned: 3, bestTimeSeconds: 40, bestStepCount: 15, updatedAt: baseTime.AddMinutes(5)),
            TestDbContextFactory.CreateRecord(p3.Id, targetStage.Id, starsEarned: 3, bestTimeSeconds: 40, bestStepCount: 15, updatedAt: baseTime.AddMinutes(1)),
            TestDbContextFactory.CreateRecord(p2.Id, targetStage.Id, starsEarned: 3, bestTimeSeconds: 40, bestStepCount: 12, updatedAt: baseTime.AddMinutes(10)),
            TestDbContextFactory.CreateRecord(p1.Id, targetStage.Id, starsEarned: 3, bestTimeSeconds: 30, bestStepCount: 25, updatedAt: baseTime.AddMinutes(20)),
            TestDbContextFactory.CreateRecord(pOther.Id, otherStage.Id, starsEarned: 3, bestTimeSeconds: 1, bestStepCount: 1, updatedAt: baseTime));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.GetStageLeaderboardAsync(targetStage.Id);

        Assert.Equal(5, result.Count);
        Assert.Equal([p1.Id, p2.Id, p3.Id, p4.Id, p5.Id], result.Select(x => x.UserId).ToArray());
        Assert.Equal([1, 2, 3, 4, 5], result.Select(x => x.Rank).ToArray());
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(120, 100)]
    public async Task GetStageLeaderboardAsync_NormalizesAndCapsLimit(int requestedLimit, int expectedCount)
    {
        using var db = TestDbContextFactory.CreateContext();
        var stage = TestDbContextFactory.CreateStage();
        db.Stages.Add(stage);

        for (var i = 1; i <= 105; i++)
        {
            var profile = TestDbContextFactory.CreateProfile(displayName: $"U{i}");
            db.Profiles.Add(profile);
            db.UserRecords.Add(TestDbContextFactory.CreateRecord(profile.Id, stage.Id, starsEarned: 3, bestTimeSeconds: i, bestStepCount: i));
        }
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.GetStageLeaderboardAsync(stage.Id, requestedLimit);

        Assert.Equal(expectedCount, result.Count);
    }
}
