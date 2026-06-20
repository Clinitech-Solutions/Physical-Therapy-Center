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
    public static readonly Error UserAlreadyExists = new("User.UserAlreadyExists", "User with the same username or email already exists", StatusCodes.Status409Conflict);
    public static readonly Error RegistrationFailed = new("User.RegistrationFailed", "User registration failed", StatusCodes.Status400BadRequest);
    public static readonly Error UserNotVerified = new("User.UserNotVerified", "User is not verified", StatusCodes.Status403Forbidden);
    public static readonly Error EmailSendingFailed = new("User.EmailSendingFailed", "Failed to send email", StatusCodes.Status500InternalServerError);
    public static readonly Error NoOtpFound = new("User.NoOtpFound", " No OTP found for the user", StatusCodes.Status404NotFound);
    public static readonly Error OtpExpired = new("User.OtpExpired", "OTP expired. Please request a new one.", StatusCodes.Status400BadRequest);
    public static readonly Error OtpNotVerified = new("User.OtpNotVerified", "The OTP is Not Verified", StatusCodes.Status400BadRequest);
    public static readonly Error FailedToUpdateUser =
        new(
            "User.FailedToUpdateUser",
            "Failed to update user.",
            StatusCodes.Status500InternalServerError
        );
    public static readonly Error FailedToAssignRole =
    new("User.FailedToAssignRole", "Failed to assign user role.", StatusCodes.Status500InternalServerError);
    public static readonly Error EmailAlreadyConfirmed =
    new(
        "User.EmailAlreadyConfirmed",
        "Email is already confirmed.",
        StatusCodes.Status400BadRequest
    );
    public static readonly Error EmailNotConfirmed =
    new(
        "User.EmailNotConfirmed",
        "Please verify your email before logging in.",
        StatusCodes.Status401Unauthorized
    );
    public static readonly Error InvalidGoogleToken =
    new(
        "User.InvalidGoogleToken",
        "Invalid Google token.",
        StatusCodes.Status401Unauthorized);
}

 

    