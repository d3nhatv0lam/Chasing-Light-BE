using System.IO;
using ChasingLight.Api.DTOs.Profiles;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Infrastructures.Data;
using ChasingLight.Api.Mappers;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ChasingLight.Api.Services;

public class ProfilesService : IProfilesService
{
    private readonly ChasingLightDbContext _db;
    private readonly IStorageService _storageService;
    private readonly ILogger<ProfilesService> _logger;

    public ProfilesService(
        ChasingLightDbContext db, 
        IStorageService storageService,
        ILogger<ProfilesService> logger)
    {
        _db = db;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<UserProfileResponse?> GetProfileByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.Profiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);

        if (profile is null) return null;

        return profile.ToResponse();
    }

    public async Task<UserProfileResponse> UpdateDisplayNameAsync(
        Guid userId, 
        string displayName, 
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.Profiles
            .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Profile", userId);

        profile.DisplayName = displayName.Trim();
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DisplayName updated for user {UserId} to {DisplayName}", userId, profile.DisplayName);
        return profile.ToResponse();
    }

    public async Task<UserProfileResponse> UploadAvatarAsync(
        Guid userId, 
        IFormFile file, 
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.Profiles
            .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Profile", userId);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{userId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
        var publicUrl = await _storageService.UploadAvatarAsync(fileName, file, cancellationToken);

        profile.AvatarUrl = publicUrl;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Avatar updated for user {UserId} to {AvatarUrl}", userId, profile.AvatarUrl);
        return profile.ToResponse();
    }

    public async Task<UserProfileResponse> DeleteAvatarAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.Profiles
            .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Profile", userId);

        profile.AvatarUrl = null;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Avatar removed for user {UserId}", userId);
        return profile.ToResponse();
    }
}
