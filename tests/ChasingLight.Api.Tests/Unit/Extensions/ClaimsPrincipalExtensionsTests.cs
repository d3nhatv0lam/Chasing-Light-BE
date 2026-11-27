using System.Security.Claims;
using ChasingLight.Api.Infrastructures.Extensions;

namespace ChasingLight.Api.Tests.Unit.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_WhenSubIsValidGuid_ReturnsParsedGuid()
    {
        var expected = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", expected.ToString())],
            "TestAuth"));

        var actual = principal.GetUserId();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void GetUserId_WhenSubMissingOrInvalid_ThrowsUnauthorizedAccessException(string? subValue)
    {
        var claims = subValue is null ? Array.Empty<Claim>() : [new Claim("sub", subValue)];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        Assert.Throws<UnauthorizedAccessException>(() => principal.GetUserId());
    }
}
