using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PTCenter.Api.Extensions;

public static class UserExtensions
{
    public static string GetUserId(this ClaimsPrincipal claims) =>
        claims.FindFirstValue(ClaimTypes.NameIdentifier)!;


    public static string? GetUserIdFromToken(this string token)
    {
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var jwtToken = handler.ReadJwtToken(token);
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            return userId;
        }
        catch
        {
            return null;
        }
    }
}
