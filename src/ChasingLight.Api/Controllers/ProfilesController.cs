using System.IO;
using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.DTOs.Profiles;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Infrastructures.Extensions;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChasingLight.Api.Controllers;

[ApiController]
[Route("api/v1/profiles")]
[Authorize]
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

    /// <summary>
    /// Cập nhật tên hiển thị của người chơi.
    /// </summary>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDisplayName(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var response = await _profilesService.UpdateDisplayNameAsync(userId, request.DisplayName, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Tải file ảnh đại diện lên (tối đa 2MB, định dạng .png, .jpg, .webp).
    /// </summary>
    [HttpPost("me/avatar")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 2 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ErrorResponse(
                StatusCodes.Status400BadRequest, 
                "INVALID_FILE", 
                "File cannot be empty."));
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp"))
        {
            return BadRequest(new ErrorResponse(
                StatusCodes.Status400BadRequest, 
                "INVALID_FILE_FORMAT", 
                "Only .png, .jpg, .jpeg, and .webp images are allowed."));
        }

        var userId = User.GetUserId();
        var response = await _profilesService.UploadAvatarAsync(userId, file, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Gỡ ảnh đại diện về mặc định.
    /// </summary>
    [HttpDelete("me/avatar")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var response = await _profilesService.DeleteAvatarAsync(userId, cancellationToken);
        return Ok(response);
    }
}
