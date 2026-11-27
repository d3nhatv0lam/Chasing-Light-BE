using System.Text.Json;
using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.Exceptions;
using ChasingLight.Api.Infrastructures.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChasingLight.Api.Tests.Unit.Middlewares;

public class ExceptionMiddlewareTests
{
    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "ChasingLight.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static async Task<(int StatusCode, ErrorResponse Body)> InvokeWithExceptionAsync(
        Exception exceptionToThrow,
        string environmentName)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionMiddleware(
            _ => Task.FromException(exceptionToThrow),
            NullLogger<ExceptionMiddleware>.Instance,
            new StubHostEnvironment(environmentName));

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await JsonSerializer.DeserializeAsync<ErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return (context.Response.StatusCode, body!);
    }

    [Fact]
    public async Task InvokeAsync_MapsAppExceptionCorrectly()
    {
        var (status, body) = await InvokeWithExceptionAsync(
            new NotFoundException("Stage not found", "STAGE_MISSING"),
            Environments.Production);

        Assert.Equal(StatusCodes.Status404NotFound, status);
        Assert.Equal("STAGE_MISSING", body.ErrorCode);
        Assert.Equal("Stage not found", body.Message);
        Assert.Null(body.Detail);
    }

    [Fact]
    public async Task InvokeAsync_MapsStandardExceptionsToExpectedStatusCodes()
    {
        var keyNotFound = await InvokeWithExceptionAsync(new KeyNotFoundException("Missing key"), Environments.Production);
        var badArg = await InvokeWithExceptionAsync(new ArgumentException("Bad input"), Environments.Production);
        var unauth = await InvokeWithExceptionAsync(new UnauthorizedAccessException("Denied"), Environments.Production);
        var unknown = await InvokeWithExceptionAsync(new InvalidOperationException("Boom"), Environments.Production);

        Assert.Equal((404, "RESOURCE_NOT_FOUND"), (keyNotFound.StatusCode, keyNotFound.Body.ErrorCode));
        Assert.Equal((400, "INVALID_ARGUMENT"), (badArg.StatusCode, badArg.Body.ErrorCode));
        Assert.Equal((401, "UNAUTHORIZED"), (unauth.StatusCode, unauth.Body.ErrorCode));
        Assert.Equal((500, "INTERNAL_SERVER_ERROR"), (unknown.StatusCode, unknown.Body.ErrorCode));
    }

    [Fact]
    public async Task InvokeAsync_IncludesStackDetailOnlyInDevelopment()
    {
        var dev = await InvokeWithExceptionAsync(new Exception("Dev details"), Environments.Development);
        var prod = await InvokeWithExceptionAsync(new Exception("Prod hidden"), Environments.Production);

        Assert.NotNull(dev.Body.Detail);
        Assert.Contains("Dev details", dev.Body.Detail);
        Assert.Null(prod.Body.Detail);
    }
}
