using ChasingLight.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ChasingLight.Api.Tests.Helpers;

/// <summary>
/// Fake IStorageService phục vụ unit test và integration test cho avatar upload.
/// </summary>
public sealed class FakeStorageService : IStorageService
{
    public string PublicUrlToReturn { get; set; } = "https://cdn.example.test/avatars/uploaded.png";
    public Exception? ExceptionToThrow { get; set; }
    public string? LastUploadedFileName { get; private set; }
    public IFormFile? LastUploadedFile { get; private set; }
    public string? LastDeletedFileName { get; private set; }

    public Task<string> UploadAvatarAsync(
        string fileName,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        LastUploadedFileName = fileName;
        LastUploadedFile = file;
        return Task.FromResult(PublicUrlToReturn);
    }

    public Task DeleteAvatarAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        LastDeletedFileName = fileName;
        return Task.CompletedTask;
    }
}
