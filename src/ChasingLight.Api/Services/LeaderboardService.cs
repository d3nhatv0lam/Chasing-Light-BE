using ChasingLight.Api.DTOs.Leaderboards;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Infrastructures.Data;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChasingLight.Api.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly ChasingLightDbContext _db;
    private readonly ILogger<LeaderboardService> _logger;

    public LeaderboardService(ChasingLightDbContext db, ILogger<LeaderboardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GlobalLeaderboardDto>> GetGlobalLeaderboardAsync(
        int limit = 50, 
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            limit = 50;
        }

        var profiles = await _db.Profiles
            .AsNoTracking()
            .OrderByDescending(p => p.TotalStars)
            .ThenBy(p => p.UpdatedAt)
            .Take(limit)
            .Select(p => new
            {
                p.Id,
                p.DisplayName,
                p.AvatarUrl,
                p.TotalStars
            })
            .ToListAsync(cancellationToken);

        var result = profiles
            .Select((p, index) => new GlobalLeaderboardDto(
                UserId: p.Id,
                DisplayName: p.DisplayName,
                AvatarUrl: p.AvatarUrl,
                TotalStars: p.TotalStars,
                Rank: index + 1
            ))
            .ToList();

        return result;
    }

    public async Task<IReadOnlyList<StageLeaderboardDto>> GetStageLeaderboardAsync(
        Guid stageId, 
        int limit = 50, 
        CancellationToken cancellationToken = default)
    {
        var stageExists = await _db.Stages
            .AnyAsync(s => s.Id == stageId, cancellationToken);

        if (!stageExists)
        {
            throw new NotFoundException("Stage", stageId);
        }

        if (limit <= 0)
        {
            limit = 50;
        }

        var records = await _db.UserRecords
            .AsNoTracking()
            .Where(r => r.StageId == stageId)
            .OrderByDescending(r => r.StarsEarned)
            .ThenBy(r => r.BestTimeSeconds)
            .ThenBy(r => r.BestStepCount)
            .ThenBy(r => r.UpdatedAt)
            .Take(limit)
            .Select(r => new
            {
                r.UserId,
                DisplayName = r.Profile.DisplayName,
                AvatarUrl = r.Profile.AvatarUrl,
                r.BestTimeSeconds,
                r.BestStepCount,
                r.StarsEarned
            })
            .ToListAsync(cancellationToken);

        var result = records
            .Select((r, index) => new StageLeaderboardDto(
                UserId: r.UserId,
                DisplayName: r.DisplayName,
                AvatarUrl: r.AvatarUrl,
                BestTimeSeconds: r.BestTimeSeconds,
                BestStepCount: r.BestStepCount,
                StarsEarned: r.StarsEarned,
                Rank: index + 1
            ))
            .ToList();

        return result;
    }
}
