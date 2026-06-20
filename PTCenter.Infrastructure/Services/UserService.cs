using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Authentication;
using PTCenter.Application.Contracts.Users;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Errors;
using PTCenter.Infrastructure.Persistence;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PTCenter.Infrastructure.Services;

public class UserService(UserManager<ApplicationUser> userManager,RoleManager<IdentityRole> _roleManager, ApplicationDbContext context) : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = _roleManager;
    private readonly ApplicationDbContext _context = context;

    
    public async Task<Result<UserProfileResponse>> GetProfileAsync(string userId)
    {
        if (await _userManager.FindByIdAsync(userId) is not { } user)
            return Result.Failure<UserProfileResponse>(UserErrors.UserNotFound);

        var roles = await _userManager.GetRolesAsync(user);

        var userMapped = user.Adapt<UserProfileResponse>();
        userMapped.Roles = roles;

        return Result.Success(userMapped);
    }

    public async Task<Result<List<UserResponse>>> GetAllAsync()
    {
        var users = await _userManager.Users
            .ToListAsync();

        var userResponses = new List<UserResponse>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var userResponse = user.Adapt<UserResponse>();
            userResponse.Roles = await _userManager.GetRolesAsync(user);

            userResponses.Add(userResponse);
        }

        return Result.Success(userResponses);
    }
    
    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        if( await _userManager.FindByIdAsync(userId) is not { } user)
            return Result.Failure(UserErrors.UserNotFound);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        return result.Succeeded ? Result.Success() : Result.Failure(new Error(result.Errors.First().Code, result.Errors.First().Description, StatusCodes.Status400BadRequest));
    }

    public async Task<Result<UserResponse>> AddAsync(CreateUserRequest request)
    {
        if (await _context.Users.AnyAsync(x => x.UserName == request.UserName))
            return Result.Failure<UserResponse>(UserErrors.DuplicatedUserName);
        
        var allowedRoles = _roleManager.Roles.Select(role => role.Name);
        if (request.Roles.Except(allowedRoles).Any())
            return Result.Failure<UserResponse>(UserErrors.InvalidRoles);
        
        var user = request.Adapt<ApplicationUser>();
        
        var result = await _userManager.CreateAsync(user, request.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRolesAsync(user, request.Roles);

            var response = user.Adapt<UserResponse>();

            return Result.Success(response);
        }
        
        var error = result.Errors.First();

        return Result.Failure<UserResponse>(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
    }
    
}