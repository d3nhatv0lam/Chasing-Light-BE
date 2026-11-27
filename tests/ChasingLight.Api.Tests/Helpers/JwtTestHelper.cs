using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ChasingLight.Api.Tests.Helpers;

/// <summary>
/// Tạo JWT token phục vụ test đọc claim OAuth và xác thực người dùng.
/// </summary>
public static class JwtTestHelper
{
    public static string CreateUnsignedToken(params Claim[] claims)
    {
        var token = new JwtSecurityToken(claims: claims);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
