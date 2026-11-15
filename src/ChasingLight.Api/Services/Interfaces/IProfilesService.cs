using ChasingLight.Api.DTOs.Profiles;
using Microsoft.AspNetCore.Http;

namespace ChasingLight.Api.Services.Interfaces;

public interface IProfilesService
{
    Task<UserProfileResponse?> GetProfileByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UploadAvatarAsync(Guid userId, IFormFile file, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> DeleteAvatarAsync(Guid userId, CancellationToken cancellationToken = default);
}
