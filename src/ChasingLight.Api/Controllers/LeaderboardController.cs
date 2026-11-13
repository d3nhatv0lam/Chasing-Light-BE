using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.DTOs.Leaderboards;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChasingLight.Api.Controllers;

[ApiController]
[Route("api/v1/leaderboards")]
[AllowAnonymous]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    /// <summary>
    /// Lấy bảng xếp hạng toàn cầu theo tổng số sao đạt được.
    /// </summary>
    /// <param name="limit">Số lượng người chơi hiển thị tối đa (mặc định 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("global")]
    [ProducesResponseType(typeof(IReadOnlyList<GlobalLeaderboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGlobalLeaderboard(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _leaderboardService.GetGlobalLeaderboardAsync(limit, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy bảng xếp hạng của một màn chơi cụ thể theo số sao, thời gian và số bước đi.
    /// </summary>
    /// <param name="stageId">ID của màn chơi.</param>
    /// <param name="limit">Số lượng người chơi hiển thị tối đa (mặc định 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("stage/{stageId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<StageLeaderboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStageLeaderboard(
        [FromRoute] Guid stageId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _leaderboardService.GetStageLeaderboardAsync(stageId, limit, cancellationToken);
        return Ok(result);
    }
}
