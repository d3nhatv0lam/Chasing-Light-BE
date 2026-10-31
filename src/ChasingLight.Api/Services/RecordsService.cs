using ChasingLight.Api.DTOs.Records;
using ChasingLight.Api.Entities;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Infrastructures.Data;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChasingLight.Api.Services;

public class RecordsService : IRecordsService
{
    private readonly ChasingLightDbContext _db;
    private readonly ILogger<RecordsService> _logger;

    public RecordsService(ChasingLightDbContext db, ILogger<RecordsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CompleteStageResponse> CompleteStageAsync(
        Guid userId, 
        CompleteStageRequest request, 
        CancellationToken cancellationToken = default)
    {
        var stageExists = await _db.Stages.AnyAsync(s => s.Id == request.StageId, cancellationToken);
        if (!stageExists)
        {
            throw new NotFoundException("Stage", request.StageId);
        }

        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);
        if (profile is null)
        {
            throw new NotFoundException("Profile", userId);
        }

        var existingRecord = await _db.UserRecords
            .FirstOrDefaultAsync(r => r.UserId == userId && r.StageId == request.StageId, cancellationToken);

        bool isNewBest = false;

        if (existingRecord is null)
        {
            isNewBest = true;
            var newRecord = new UserRecord
            {
                UserId = userId,
                StageId = request.StageId,
                StarsEarned = request.StarsEarned,
                BestTimeSeconds = request.TimeDoneSeconds,
                BestStepCount = request.StepCount,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.UserRecords.Add(newRecord);
        }
        else if (IsBetterRecord(request.StarsEarned, request.TimeDoneSeconds, request.StepCount, existingRecord))
        {
            isNewBest = true;
            existingRecord.StarsEarned = request.StarsEarned;
            existingRecord.BestTimeSeconds = request.TimeDoneSeconds;
            existingRecord.BestStepCount = request.StepCount;
            existingRecord.UpdatedAt = DateTimeOffset.UtcNow;
        }

        if (isNewBest)
        {
            await _db.SaveChangesAsync(cancellationToken);

            var totalStars = await _db.UserRecords
                .Where(r => r.UserId == userId)
                .SumAsync(r => (int)r.StarsEarned, cancellationToken);

            profile.TotalStars = totalStars;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            var currentRecord = existingRecord ?? await _db.UserRecords
                .FirstAsync(r => r.UserId == userId && r.StageId == request.StageId, cancellationToken);

            return new CompleteStageResponse
            {
                Success = true,
                IsNewBest = true,
                TotalStars = totalStars,
                BestTimeSeconds = currentRecord.BestTimeSeconds,
                BestStepCount = currentRecord.BestStepCount,
                StarsEarned = currentRecord.StarsEarned,
                Message = "New best record saved successfully"
            };
        }

        return new CompleteStageResponse
        {
            Success = true,
            IsNewBest = false,
            TotalStars = profile.TotalStars,
            BestTimeSeconds = existingRecord!.BestTimeSeconds,
            BestStepCount = existingRecord.BestStepCount,
            StarsEarned = existingRecord.StarsEarned,
            Message = "Current record is better or equal. No update performed."
        };
    }

    public async Task<UserRecordsSummaryResponse> GetUserRecordsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);
        if (profile is null)
        {
            throw new NotFoundException("Profile", userId);
        }

        var records = await _db.UserRecords
            .Where(r => r.UserId == userId)
            .Include(r => r.Stage)
            .OrderBy(r => r.Stage.Difficulty)
            .ThenBy(r => r.Stage.StageNumber)
            .Select(r => new UserRecordDto
            {
                StageId = r.StageId,
                StageNumber = r.Stage.StageNumber,
                Difficulty = r.Stage.Difficulty,
                StarsEarned = r.StarsEarned,
                BestTimeSeconds = r.BestTimeSeconds,
                BestStepCount = r.BestStepCount,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new UserRecordsSummaryResponse
        {
            TotalStars = profile.TotalStars,
            CompletedStagesCount = records.Count,
            Records = records
        };
    }

    public async Task<SyncRecordsResponse> SyncRecordsAsync(
        Guid userId, 
        SyncRecordsRequest request, 
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);
        if (profile is null)
        {
            throw new NotFoundException("Profile", userId);
        }

        if (request.Records == null || request.Records.Count == 0)
        {
            var currentRecords = await GetUserRecordDtosAsync(userId, cancellationToken);
            return new SyncRecordsResponse
            {
                Success = true,
                TotalStars = profile.TotalStars,
                UpdatedCount = 0,
                Records = currentRecords,
                Message = "No records provided to sync"
            };
        }

        var requestStageIds = request.Records.Select(r => r.StageId).Distinct().ToList();

        var existingStages = await _db.Stages
            .Where(s => requestStageIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var existingRecords = await _db.UserRecords
            .Where(r => r.UserId == userId && requestStageIds.Contains(r.StageId))
            .ToDictionaryAsync(r => r.StageId, cancellationToken);

        int updatedCount = 0;

        foreach (var item in request.Records)
        {
            if (!existingStages.ContainsKey(item.StageId))
            {
                _logger.LogWarning("Skipping sync for stage {StageId} as it does not exist in database.", item.StageId);
                continue;
            }

            if (existingRecords.TryGetValue(item.StageId, out var existingRecord))
            {
                if (IsBetterRecord(item.StarsEarned, item.TimeDoneSeconds, item.StepCount, existingRecord))
                {
                    existingRecord.StarsEarned = item.StarsEarned;
                    existingRecord.BestTimeSeconds = item.TimeDoneSeconds;
                    existingRecord.BestStepCount = item.StepCount;
                    existingRecord.UpdatedAt = DateTimeOffset.UtcNow;
                    updatedCount++;
                }
            }
            else
            {
                var newRecord = new UserRecord
                {
                    UserId = userId,
                    StageId = item.StageId,
                    StarsEarned = item.StarsEarned,
                    BestTimeSeconds = item.TimeDoneSeconds,
                    BestStepCount = item.StepCount,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                _db.UserRecords.Add(newRecord);
                existingRecords[item.StageId] = newRecord;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);

            var totalStars = await _db.UserRecords
                .Where(r => r.UserId == userId)
                .SumAsync(r => (int)r.StarsEarned, cancellationToken);

            profile.TotalStars = totalStars;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var allRecords = await GetUserRecordDtosAsync(userId, cancellationToken);

        return new SyncRecordsResponse
        {
            Success = true,
            TotalStars = profile.TotalStars,
            UpdatedCount = updatedCount,
            Records = allRecords,
            Message = $"Synced successfully. {updatedCount} records updated."
        };
    }

    private async Task<IReadOnlyList<UserRecordDto>> GetUserRecordDtosAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.UserRecords
            .Where(r => r.UserId == userId)
            .Include(r => r.Stage)
            .OrderBy(r => r.Stage.Difficulty)
            .ThenBy(r => r.Stage.StageNumber)
            .Select(r => new UserRecordDto
            {
                StageId = r.StageId,
                StageNumber = r.Stage.StageNumber,
                Difficulty = r.Stage.Difficulty,
                StarsEarned = r.StarsEarned,
                BestTimeSeconds = r.BestTimeSeconds,
                BestStepCount = r.BestStepCount,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private static bool IsBetterRecord(short newStars, int newTime, int newSteps, UserRecord existing)
    {
        if (newStars > existing.StarsEarned)
            return true;

        if (newStars < existing.StarsEarned)
            return false;

        if (newTime < existing.BestTimeSeconds)
            return true;

        if (newTime > existing.BestTimeSeconds)
            return false;

        return newSteps < existing.BestStepCount;
    }
}
