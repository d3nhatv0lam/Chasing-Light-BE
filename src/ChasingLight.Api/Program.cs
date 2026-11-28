using ChasingLight.Api.Infrastructures.Extensions;
using ChasingLight.Api.Infrastructures.Middlewares;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.ConfigureValidationErrorResponse();
builder.Services.AddDataBaseConfiguration(builder.Configuration);
builder.Services.AddSupabaseJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Chỉ chuyển hướng HTTPS khi chạy trực tiếp ngoài container;
// trong Docker/Cloud PaaS, TLS thường do reverse proxy phía trước đảm nhận.
var isRunningInContainer = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (!isRunningInContainer)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Endpoint kiểm tra sức khỏe cho Docker HEALTHCHECK và Cloud PaaS;
// bắt buộc đặt trước MapNotFoundFallback() để không bị bắt nhầm thành 404.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous();

app.MapControllers();
app.MapNotFoundFallback();

app.Run();

public partial class Program { }