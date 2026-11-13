using ChasingLight.Api.DTOs.Profiles;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Infrastructures.Data;
using ChasingLight.Api.Mappers;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChasingLight.Api.Services;

public class ProfilesService : IProfilesService
{
    private readonly ChasingLightDbContext _db;
    private readonly ILogger<ProfilesService> _logger;

    public ProfilesService(ChasingLightDbContext db, ILogger<ProfilesService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UserProfileResponse?> GetProfileByIdAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.Profiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);

        if (profile is null) return null;

        return profile.ToResponse();
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasDisplayName = !string.IsNullOrWhiteSpace(request.DisplayName);
        var hasAvatarUrl = !string.IsNullOrWhiteSpace(request.AvatarUrl);

        if (!hasDisplayName && !hasAvatarUrl)
        {
            throw new AppException(
                "At least DisplayName or AvatarUrl must be provided.",
                "INVALID_PROFILE_UPDATE",
                StatusCodes.Status400BadRequest);
        }

        var profile = await _db.Profiles
            .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);

        if (profile is null)
        {
            throw new NotFoundException("Profile", userId);
        }

        if (hasDisplayName)
        {
            var trimmedName = request.DisplayName!.Trim();
            if (trimmedName.Length > 150)
            {
                throw new AppException(
                    "DisplayName cannot exceed 150 characters.",
                    "INVALID_DISPLAY_NAME",
                    StatusCodes.Status400BadRequest);
            }
            profile.DisplayName = trimmedName;
        }

        if (hasAvatarUrl)
        {
            var trimmedUrl = request.AvatarUrl!.Trim();
            if (trimmedUrl.Length > 1000)
            {
                throw new AppException(
                    "AvatarUrl cannot exceed 1000 characters.",
                    "INVALID_AVATAR_URL",
                    StatusCodes.Status400BadRequest);
            }
            profile.AvatarUrl = trimmedUrl;
        }

        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Profile updated for user {UserId}. DisplayName: {DisplayName}, AvatarUrl: {AvatarUrl}",
            userId, profile.DisplayName, profile.AvatarUrl);

        return profile.ToResponse();
    }
}
