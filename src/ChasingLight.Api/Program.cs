using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.Infrastructures.Extensions;
using ChasingLight.Api.Infrastructures.Middlewares;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(msg => !string.IsNullOrEmpty(msg))
                .ToList();

            var errorResponse = new ErrorResponse(
                statusCode: StatusCodes.Status400BadRequest,
                errorCode: "VALIDATION_ERROR",
                message: string.Join("; ", errors)
            );

            return new BadRequestObjectResult(errorResponse);
        };
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDataBaseConfiguration(builder.Configuration)
    .AddSupabaseJwtAuthentication(builder.Configuration);

builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapNotFoundFallback();

app.Run();
