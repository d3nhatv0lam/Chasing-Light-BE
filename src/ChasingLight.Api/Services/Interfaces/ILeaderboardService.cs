using ChasingLight.Api.DTOs.Leaderboards;
using ChasingLight.Api.DTOs.Records;

namespace ChasingLight.Api.Services.Interfaces;

public interface ILeaderboardService
{
    Task<IReadOnlyList<GlobalLeaderboardDto>> GetGlobalLeaderboardAsync(int limit = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StageLeaderboardDto>> GetStageLeaderboardAsync(Guid stageId, int limit = 50,
        CancellationToken cancellationToken = default);
}