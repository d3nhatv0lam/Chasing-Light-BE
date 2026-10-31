
using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.DTOs.Profiles;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Infrastructures.Extensions;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChasingLight.Api.Controllers;

[ApiController]
[Route("api/v1/profiles")]
// [Authorize] // comment to test API
public class ProfilesController : ControllerBase
{
    private readonly IProfilesService _profilesService;

    public ProfilesController(IProfilesService profilesService)
    {
        _profilesService = profilesService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var profile = await _profilesService.GetProfileByIdAsync(userId, cancellationToken);

        if (profile is null)
            throw new NotFoundException("Profile not found");

        return Ok(profile);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileById(Guid id, CancellationToken cancellationToken)
    {
        var profile = await _profilesService.GetProfileByIdAsync(id, cancellationToken);
        if (profile is null)
        {
            throw new NotFoundException("Profile", id);
        }

        return Ok(profile);
    }
}