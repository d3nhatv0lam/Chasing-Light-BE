using ChasingLight.Api.DTOs.Leaderboards;
using ChasingLight.Api.Infrastructures.Data;
using ChasingLight.Api.Services.Interfaces;

namespace ChasingLight.Api.Services;

public class LeaderboardService: ILeaderboardService
{
    private readonly ChasingLightDbContext _db;
    private readonly ILogger<LeaderboardService> _logger;

    public LeaderboardService(ChasingLightDbContext db, ILogger<LeaderboardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<IReadOnlyList<GlobalLeaderboardDto>> GetGlobalLeaderboardAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<StageLeaderboardDto>> GetStageLeaderboardAsync(Guid stageId, int limit = 50, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}