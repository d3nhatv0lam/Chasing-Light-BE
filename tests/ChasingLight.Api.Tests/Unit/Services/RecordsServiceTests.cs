using ChasingLight.Api.DTOs.Records;
using ChasingLight.Api.Entities;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Services;
using ChasingLight.Api.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChasingLight.Api.Tests.Unit.Services;

public class RecordsServiceTests
{
    private static RecordsService CreateSut(Infrastructures.Data.ChasingLightDbContext db) =>
        new(db, NullLogger<RecordsService>.Instance);

    [Fact]
    public async Task CompleteStageAsync_WhenNoExistingRecord_CreatesRecordAndUpdatesTotalStars()
    {
        using var db = TestDbContextFactory.CreateContext();
        var user = TestDbContextFactory.CreateProfile(totalStars: 0);
        var stage = TestDbContextFactory.CreateStage(stageNumber: 1, difficulty: Difficulty.Easy);
        db.Profiles.Add(user);
        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var request = new CompleteStageRequest
        {
            StageId = stage.Id,
            StarsEarned = 3,
            TimeDoneSeconds = 45,
            StepCount = 12
        };

        var result = await sut.CompleteStageAsync(user.Id, request);

        Assert.True(result.Success);
        Assert.True(result.IsNewBest);
        Assert.Equal(3, result.TotalStars);
        Assert.Equal((short)3, result.StarsEarned);
        Assert.Equal(45, result.BestTimeSeconds);
        Assert.Equal(12, result.BestStepCount);

        var savedRecord = await db.UserRecords.SingleAsync(r => r.UserId == user.Id && r.StageId == stage.Id);
        Assert.Equal((short)3, savedRecord.StarsEarned);
        Assert.Equal(45, savedRecord.BestTimeSeconds);
        Assert.Equal(12, savedRecord.BestStepCount);
        Assert.Equal(3, (await db.Profiles.FindAsync(user.Id))!.TotalStars);
    }

    [Theory]
    [InlineData((short)3, 120, 50, true)]   // Nhiều sao hơn -> thắng dù chậm và nhiều bước hơn
    [InlineData((short)2, 50, 30, true)]    // Bằng sao, thời gian nhanh hơn -> thắng dù nhiều bước hơn
    [InlineData((short)2, 60, 15, true)]    // Bằng sao, bằng thời gian, ít bước hơn -> thắng
    [InlineData((short)1, 10, 5, false)]    // Ít sao hơn -> thua dù nhanh hơn
    [InlineData((short)2, 70, 10, false)]   // Bằng sao, thời gian chậm hơn -> thua dù ít bước hơn
    [InlineData((short)2, 60, 20, false)]   // Bằng hệt kỷ lục cũ -> không cập nhật
    public async Task CompleteStageAsync_ComparesAgainstExistingRecord_AccordingToPriorityRules(
        short newStars,
        int newTime,
        int newSteps,
        bool expectedIsNewBest)
    {
        using var db = TestDbContextFactory.CreateContext();
        var user = TestDbContextFactory.CreateProfile(totalStars: 2);
        var stage = TestDbContextFactory.CreateStage();
        var existing = TestDbContextFactory.CreateRecord(user.Id, stage.Id, starsEarned: 2, bestTimeSeconds: 60, bestStepCount: 20);
        db.Profiles.Add(user);
        db.Stages.Add(stage);
        db.UserRecords.Add(existing);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.CompleteStageAsync(user.Id, new CompleteStageRequest
        {
            StageId = stage.Id,
            StarsEarned = newStars,
            TimeDoneSeconds = newTime,
            StepCount = newSteps
        });

        Assert.Equal(expectedIsNewBest, result.IsNewBest);
        if (expectedIsNewBest)
        {
            Assert.Equal(newStars, result.StarsEarned);
            Assert.Equal(newTime, result.BestTimeSeconds);
            Assert.Equal(newSteps, result.BestStepCount);
            Assert.Equal(newStars, result.TotalStars);
        }
        else
        {
            Assert.Equal((short)2, result.StarsEarned);
            Assert.Equal(60, result.BestTimeSeconds);
            Assert.Equal(20, result.BestStepCount);
            Assert.Equal(2, result.TotalStars);
        }
    }

    [Fact]
    public async Task CompleteStageAsync_SumsTotalStarsAcrossMultipleStages()
    {
        using var db = TestDbContextFactory.CreateContext();
        var user = TestDbContextFactory.CreateProfile(totalStars: 2);
        var stage1 = TestDbContextFactory.CreateStage(stageNumber: 1);
        var stage2 = TestDbContextFactory.CreateStage(stageNumber: 2);
        db.Profiles.Add(user);
        db.Stages.AddRange(stage1, stage2);
        db.UserRecords.Add(TestDbContextFactory.CreateRecord(user.Id, stage1.Id, starsEarned: 2));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.CompleteStageAsync(user.Id, new CompleteStageRequest
        {
            StageId = stage2.Id,
            StarsEarned = 3,
            TimeDoneSeconds = 30,
            StepCount = 10
        });

        Assert.Equal(5, result.TotalStars);
        Assert.Equal(5, (await db.Profiles.FindAsync(user.Id))!.TotalStars);
    }

    [Fact]
    public async Task CompleteStageAsync_WhenStageDoesNotExist_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.CreateContext();
        var user = TestDbContextFactory.CreateProfile();
        db.Profiles.Add(user);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.CompleteStageAsync(user.Id, new CompleteStageRequest
        {
            StageId = Guid.NewGuid(),
            StarsEarned = 1,
            TimeDoneSeconds = 10,
            StepCount = 5
        }));
    }

    [Fact]
    public async Task CompleteStageAsync_WhenProfileDoesNotExist_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.CreateContext();
        var stage = TestDbContextFactory.CreateStage();
        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.CompleteStageAsync(Guid.NewGuid(), new CompleteStageRequest
        {
            StageId = stage.Id,
            StarsEarned = 1,
            TimeDoneSeconds = 10,
            StepCount = 5
        }));
    }

    [Fact]
    public async Task GetUserRecordsAsync_ReturnsSummaryOrderedByDifficultyThenStageNumber()
    {
        using var db = TestDbContextFactory.CreateContext();
        var user = TestDbContextFactory.CreateProfile(totalStars: 6);
        var hardStage = TestDbContextFactory.CreateStage(stageNumber: 1, difficulty: Difficulty.Hard);
        var easyStage2 = TestDbContextFactory.CreateStage(stageNumber: 2, difficulty: Difficulty.Easy);
        var easyStage1 = TestDbContextFactory.CreateStage(stageNumber: 1, difficulty: Difficulty.Easy);
        db.Profiles.Add(user);
        db.Stages.AddRange(hardStage, easyStage2, easyStage1);
        db.UserRecords.AddRange(
            TestDbContextFactory.CreateRecord(user.Id, hardStage.Id, starsEarned: 1),
            TestDbContextFactory.CreateRecord(user.Id, easyStage2.Id, starsEarned: 2),
            TestDbContextFactory.CreateRecord(user.Id, easyStage1.Id, starsEarned: 3));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var summary = await sut.GetUserRecordsAsync(user.Id);

        Assert.Equal(6, summary.TotalStars);
        Assert.Equal(3, summary.CompletedStagesCount);
        Assert.Equal(
            [easyStage1.Id, easyStage2.Id, hardStage.Id],
            summary.Records.Select(r => r.StageId).ToArray());
    }

    [Fact]
    public async Task SyncRecordsAsync_WhenRecordsEmpty_ReturnsZeroUpdatedWithoutModifyingTotalStars()
    {
        using var db = TestDbContextFactory.CreateContext();
        var user = TestDbContextFactory.CreateProfile(totalStars: 3);
        var stage = TestDbContextFactory.CreateStage();
        db.Profiles.Add(user);
        db.Stages.Add(stage);
        db.UserRecords.Add(TestDbContextFactory.CreateRecord(user.Id, stage.Id, starsEarned: 3));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.SyncRecordsAsync(user.Id, new SyncRecordsRequest { Records = [] });

        Assert.True(result.Success);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(3, result.TotalStars);
        Assert.Single(result.Records);
    }

    [Fact]
    public async Task SyncRecordsAsync_SkipsUnknownStages_DoesNotOverwriteBetterCloudRecord_AndUpdatesBetterOrNew()
    {
        using var db = TestDbContextFactory.CreateContext();
        var user = TestDbContextFactory.CreateProfile(totalStars: 5);
        var stageKeepBetter = TestDbContextFactory.CreateStage(stageNumber: 1, difficulty: Difficulty.Easy);
        var stageUpgrade = TestDbContextFactory.CreateStage(stageNumber: 2, difficulty: Difficulty.Easy);
        var stageNew = TestDbContextFactory.CreateStage(stageNumber: 3, difficulty: Difficulty.Easy);
        var unknownStageId = Guid.NewGuid();

        db.Profiles.Add(user);
        db.Stages.AddRange(stageKeepBetter, stageUpgrade, stageNew);
        db.UserRecords.AddRange(
            TestDbContextFactory.CreateRecord(user.Id, stageKeepBetter.Id, starsEarned: 3, bestTimeSeconds: 20, bestStepCount: 8),
            TestDbContextFactory.CreateRecord(user.Id, stageUpgrade.Id, starsEarned: 2, bestTimeSeconds: 50, bestStepCount: 15));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.SyncRecordsAsync(user.Id, new SyncRecordsRequest
        {
            Records =
            [
                new SyncRecordItem { StageId = unknownStageId, StarsEarned = 3, TimeDoneSeconds = 10, StepCount = 5 },
                new SyncRecordItem { StageId = stageKeepBetter.Id, StarsEarned = 2, TimeDoneSeconds = 15, StepCount = 5 },
                new SyncRecordItem { StageId = stageUpgrade.Id, StarsEarned = 3, TimeDoneSeconds = 40, StepCount = 14 },
                new SyncRecordItem { StageId = stageNew.Id, StarsEarned = 2, TimeDoneSeconds = 35, StepCount = 12 }
            ]
        });

        Assert.True(result.Success);
        Assert.Equal(2, result.UpdatedCount);
        Assert.Equal(8, result.TotalStars);
        Assert.Equal(3, result.Records.Count);

        var kept = await db.UserRecords.FindAsync(user.Id, stageKeepBetter.Id);
        Assert.Equal((short)3, kept!.StarsEarned);
        Assert.Equal(20, kept.BestTimeSeconds);
    }
}
