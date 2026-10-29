using ChasingLight.Api.DTOs.Common;
using ChasingLight.Api.Infrastructures.Data;
using ChasingLight.Api.Services;
using ChasingLight.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ChasingLight.Api.Infrastructures.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataBaseConfiguration(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SupabaseDatabase")
                               ?? throw new InvalidOperationException(
                                   "ConnectionStrings:SupabaseDatabase is not configured");

        services.AddDbContext<ChasingLightDbContext>(options =>
        {
            options.UseNpgsql(connectionString,
                npgsqlOptions => { npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3); });
        });
        return services;
    }
    
    public static IServiceCollection AddSupabaseJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"] ??
                          throw new InvalidOperationException("Supabase:Url is not configured");

        // đăng ký hệ thống Authentication của ASP.NET Core.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // giữ nguyên tên claim (giữ nguyên "sub", "email")
                options.MapInboundClaims = false;

                options.Authority = $"{supabaseUrl}/auth/v1";

                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    // Bắt buộc xác minh nguồn phát hành token
                    ValidateIssuer = true,
                    //Chỉ chấp nhận issuer khớp với URL Auth của dự án
                    ValidIssuer = $"{supabaseUrl}/auth/v1",

                    // Bắt buộc xác minh đối tượng nhận token
                    ValidateAudience = true,
                    // Chỉ chấp nhận token có audience phù hợp
                    ValidAudience = "authenticated",

                    // Kiểm tra token còn hạn sử dụng
                    ValidateLifetime = true,
                    // cho phép sai lệch
                    ClockSkew = TimeSpan.FromMinutes(1),

                    // Giới hạn thuật toán chữ ký được chấp nhận
                    // Chỉ chấp nhận thuật toán ký bất đối xứng chuẩn 
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256]
                    
                };
                
                options.Events = new JwtBearerEvents()
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        
                        var errorResponse = new ErrorResponse(
                            statusCode: StatusCodes.Status401Unauthorized,
                            errorCode: "UNAUTHORIZED",
                            message: "Token is invalid or user sub is missing");

                        await context.Response.WriteAsJsonAsync(errorResponse);
                    }
                };
            });

        services.AddAuthorization();
        
        return services;
    }
    
    /// <summary>
    /// Đăng ký các Business Services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRecordsService, RecordsService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IProfilesService, ProfilesService>();
        return services;
    }
}