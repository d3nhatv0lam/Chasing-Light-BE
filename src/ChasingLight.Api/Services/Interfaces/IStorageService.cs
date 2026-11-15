using Microsoft.AspNetCore.Http;

namespace ChasingLight.Api.Services.Interfaces;

public interface IStorageService
{
    Task<string> UploadAvatarAsync(string fileName, IFormFile file, CancellationToken cancellationToken = default);
    Task DeleteAvatarAsync(string fileName, CancellationToken cancellationToken = default);
}
