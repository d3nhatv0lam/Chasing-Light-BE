using System.Security.Claims;

namespace ChasingLight.Api.Infrastructures.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Get user id from claims principal
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub");
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("Token is invalid or user sub is missing");
        }

        return userId;
    }
}