using Microsoft.AspNetCore.Http;

namespace PTCenter.Domain.Errors;

public class UserErrors
{
    public static readonly Error InvalidCredentials = new("User.InvalidCredentials", "Invalid email/password", StatusCodes.Status401Unauthorized);

    public static readonly Error DisabledUser = new("User.DisabledUser", "Disabled user, please contact your administrator", StatusCodes.Status401Unauthorized);

    public static readonly Error LockedUser = new("User.LockedUser", "Locked user, please contact your administrator", StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidJwtToken = new("User.InvalidJwtToken", "Invalid Jwt token", StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidRefreshToken = new("User.InvalidRefreshToken", "Invalid refresh token", StatusCodes.Status401Unauthorized);

    public static readonly Error DuplicatedUserName = new("User.DuplicatedUserName", "Another user with the same UserName is already exists", StatusCodes.Status409Conflict);

    public static readonly Error UserNotFound = new("User.NotFound", "User is not found ", StatusCodes.Status404NotFound);

    public static readonly Error InvalidRoles = new("User.InvalidRoles", "Invalid Roles", StatusCodes.Status400BadRequest);
}