using ChasingLight.Api.DTOs.Profiles;

namespace ChasingLight.Api.Services.Interfaces;

public interface IProfilesService
{
    Task<UserProfileResponse?> GetProfileByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}