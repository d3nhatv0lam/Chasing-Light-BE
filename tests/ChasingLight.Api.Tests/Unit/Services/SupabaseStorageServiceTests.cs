using System.Net;
using System.Text;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Infrastructures.Identity;
using ChasingLight.Api.Infrastructures.Storage;
using ChasingLight.Api.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ChasingLight.Api.Tests.Unit.Services;

public class SupabaseStorageServiceTests
{
    private static readonly IOptions<SupabaseOptions> ValidOptions = Options.Create(new SupabaseOptions
    {
        Url = "https://example.supabase.co/",
        PublishableKey = "sb_key"
    });

    private static IFormFile CreateFormFile(string contentType = "")
    {
        var bytes = Encoding.UTF8.GetBytes("png-data");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task UploadAvatarAsync_WhenSuccess_UsesFallbackContentTypeAndReturnsPublicUrl()
    {
        var handler = MockHttpMessageHandler.FromResponses(
            new HttpResponseMessage(HttpStatusCode.OK));
        var sut = new SupabaseStorageService(new HttpClient(handler), ValidOptions, NullLogger<SupabaseStorageService>.Instance);

        var url = await sut.UploadAvatarAsync("u1_123.png", CreateFormFile(contentType: ""));

        Assert.Equal("https://example.supabase.co/storage/v1/object/public/avatars/u1_123.png", url);
        Assert.Equal("storage/v1/object/avatars/u1_123.png", handler.LastRequest!.RequestUri!.PathAndQuery.TrimStart('/'));
        Assert.Equal("image/png", handler.LastRequest.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task UploadAvatarAsync_WhenNetworkFails_Throws503AppException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Timeout"));
        var sut = new SupabaseStorageService(new HttpClient(handler), ValidOptions, NullLogger<SupabaseStorageService>.Instance);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            sut.UploadAvatarAsync("u1_123.png", CreateFormFile("image/png")));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, ex.StatusCode);
        Assert.Equal("STORAGE_UNAVAILABLE", ex.ErrorCode);
    }

    [Fact]
    public async Task UploadAvatarAsync_WhenStorageRejects_Throws502AppException()
    {
        var handler = MockHttpMessageHandler.FromResponses(
            MockHttpMessageHandler.JsonResponse(HttpStatusCode.BadRequest, """{"error":"Bucket not found"}"""));
        var sut = new SupabaseStorageService(new HttpClient(handler), ValidOptions, NullLogger<SupabaseStorageService>.Instance);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            sut.UploadAvatarAsync("u1_123.png", CreateFormFile("image/png")));

        Assert.Equal(StatusCodes.Status502BadGateway, ex.StatusCode);
        Assert.Equal("STORAGE_UPLOAD_FAILED", ex.ErrorCode);
    }
}
