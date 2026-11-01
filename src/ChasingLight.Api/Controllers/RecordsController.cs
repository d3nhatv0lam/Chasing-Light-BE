using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.DTOs.Records;
using ChasingLight.Api.Infrastructures.Extensions;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChasingLight.Api.Controllers;

[ApiController]
[Route("api/v1/records")]
[Authorize]
public class RecordsController : ControllerBase
{
    private readonly IRecordsService _recordsService;

    public RecordsController(IRecordsService recordsService)
    {
        _recordsService = recordsService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserRecordsSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyRecords(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var response = await _recordsService.GetUserRecordsAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("complete")]
    [ProducesResponseType(typeof(CompleteStageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteStage(
        [FromBody] CompleteStageRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var response = await _recordsService.CompleteStageAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("sync")]
    [ProducesResponseType(typeof(SyncRecordsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncRecords(
        [FromBody] SyncRecordsRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var response = await _recordsService.SyncRecordsAsync(userId, request, cancellationToken);
        return Ok(response);
    }
}
