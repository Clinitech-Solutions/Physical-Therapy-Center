using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Authentication;
using Microsoft.AspNetCore.Identity;

namespace PTCenter.Infrastructure.Services;

public class AuthService(UserManager<ApplicationUser> userManager,SignInManager<ApplicationUser> signInManager,IJwtProvider jwtProvider) : IAuthService 
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager  = signInManager;
    private readonly IJwtProvider _jwtProvider = jwtProvider;

    public async Task<Result<AuthResponse>> GetTokenAsync(LoginRequest request)
    {

        if (await _userManager.FindByNameAsync(request.UserName) is not { } user)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, true);
        if (result.Succeeded)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var jwtResult = _jwtProvider.GenerateToken(user, userRoles);

            var response = new AuthResponse(user.Id, user.UserName!, jwtResult.token, jwtResult.expiresIn);

            return Result.Success(response);
        }

        var error = result.IsLockedOut ? UserErrors.LockedUser : UserErrors.InvalidCredentials;

        return Result.Failure<AuthResponse>(error);

    }
}