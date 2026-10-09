using ChasingLight.Api.DTOs.Common;

namespace ChasingLight.Api.Infrastructures.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapNotFoundFallback(this IEndpointRouteBuilder app)
    {
        app.MapFallback(async (HttpContext context) =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
    
            var errorResponse = new ErrorResponse(
                statusCode: StatusCodes.Status404NotFound,
                errorCode: "ENDPOINT_NOT_FOUND",
                message: $"Đường dẫn '{context.Request.Path}' không tồn tại trên hệ thống!"
            );

            await context.Response.WriteAsJsonAsync(errorResponse);
        });
        return app;
    }
}