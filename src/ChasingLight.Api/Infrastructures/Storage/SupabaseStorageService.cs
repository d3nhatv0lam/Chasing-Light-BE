using System.Net.Http.Headers;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Infrastructures.Identity;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ChasingLight.Api.Infrastructures.Storage;

public class SupabaseStorageService : IStorageService
{
    private const string AvatarBucket = "avatars";

    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseStorageService> _logger;

    public SupabaseStorageService(
        HttpClient httpClient,
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseStorageService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.Url) || string.IsNullOrWhiteSpace(_options.PublishableKey))
        {
            throw new InvalidOperationException("Supabase credentials are not configured.");
        }

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(_options.Url.TrimEnd('/') + "/");
        }
    }

    public async Task<string> UploadAvatarAsync(
        string fileName, 
        IFormFile file, 
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"storage/v1/object/{AvatarBucket}/{fileName}";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);

        requestMessage.Headers.Add("apikey", _options.PublishableKey);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.PublishableKey);

        await using var stream = file.OpenReadStream();
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "image/png" : file.ContentType);
        requestMessage.Content = content;

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Supabase Storage during avatar upload.");
            throw new AppException(
                "Storage service is unavailable. Please try again later.",
                "STORAGE_UNAVAILABLE",
                StatusCodes.Status503ServiceUnavailable);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Supabase Storage rejected avatar upload ({StatusCode}): {Body}", response.StatusCode, errorBody);
            throw new AppException(
                "Failed to upload avatar image.",
                "STORAGE_UPLOAD_FAILED",
                StatusCodes.Status502BadGateway);
        }

        return $"{_options.Url.TrimEnd('/')}/storage/v1/object/public/{AvatarBucket}/{fileName}";
    }

    public async Task DeleteAvatarAsync(
        string fileName, 
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"storage/v1/object/{AvatarBucket}/{fileName}";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, endpoint);

        requestMessage.Headers.Add("apikey", _options.PublishableKey);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.PublishableKey);

        try
        {
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Supabase Storage delete avatar returned status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete avatar from Supabase Storage: {FileName}", fileName);
        }
    }
}
