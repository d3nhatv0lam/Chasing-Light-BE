using ChasingLight.Api.DTOs.Profiles;
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
}
