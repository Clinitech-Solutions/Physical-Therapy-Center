using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Authentication;
using PTCenter.Application.Contracts.Users;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Errors;
using PTCenter.Infrastructure.Persistence;

namespace PTCenter.Infrastructure.Services;

public class UserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext context) : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
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
        var users = await _userManager.Users.ToListAsync();

        var userResponses = new List<UserResponse>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var userResponse = user.Adapt<UserResponse>();
            userResponse.Roles = roles;

            userResponses.Add(userResponse);
        }

        return Result.Success(userResponses);
    }

    public async Task<Result> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request)
    {
        if (await _userManager.FindByIdAsync(userId) is not { } user)
            return Result.Failure(UserErrors.UserNotFound);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var result = await _userManager.ResetPasswordAsync(
            user,
            token,
            request.NewPassword);

        if (result.Succeeded)
            return Result.Success();

        var error = result.Errors.First();

        return Result.Failure(
            new Error(
                error.Code,
                error.Description,
                StatusCodes.Status400BadRequest));
    }

    public async Task<Result<UserResponse>> AddAsync(CreateUserRequest request)
    {
        // Check duplicate username
        if (await _context.Users.AnyAsync(x => x.UserName == request.UserName))
        {
            return Result.Failure<UserResponse>(
                UserErrors.DuplicatedUserName);
        }

        // Validate roles
        var allowedRoles = await _roleManager.Roles
            .Select(role => role.Name)
            .Where(role => role != null)
            .ToListAsync();

        if (request.Roles.Except(allowedRoles).Any())
        {
            return Result.Failure<UserResponse>(
                UserErrors.InvalidRoles);
        }

        // Create user explicitly
        // This fixes the FullName NULL problem
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            FullName = request.FullName,
            Gender = request.Gender,
            BirthDate = request.BirthDate

        };
        user.EmailConfirmed = true;
        // Create Identity user
        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            var error = result.Errors.First();

            return Result.Failure<UserResponse>(
                new Error(
                    error.Code,
                    error.Description,
                    StatusCodes.Status400BadRequest));
        }

        // Add roles
        var roleResult = await _userManager.AddToRolesAsync(
            user,
            request.Roles);

        if (!roleResult.Succeeded)
        {
            var error = roleResult.Errors.First();

            return Result.Failure<UserResponse>(
                new Error(
                    error.Code,
                    error.Description,
                    StatusCodes.Status400BadRequest));
        }
       

        // Map response
        var response = user.Adapt<UserResponse>();

        response.Roles = request.Roles;

        return Result.Success(response);
    }
    public async Task<Result<UserResponse>> UpdateAsync(string userId, UpdateUserRequest request)
    { // Find user
        if (await _userManager.FindByIdAsync(userId) is not { } user)
        { return Result.Failure<UserResponse>(UserErrors.UserNotFound); }
        // Check duplicate username
        var duplicateUserName = await _context.Users.AnyAsync(x => x.UserName == request.UserName && x.Id != userId);
        if (duplicateUserName) { return Result.Failure<UserResponse>(UserErrors.DuplicatedUserName); }
        // Validate roles
        var allowedRoles = await _roleManager.Roles.Select(role => role.Name).Where(role => role != null)
            .ToListAsync();
        if (request.Roles.Except(allowedRoles).Any())
        { return Result.Failure<UserResponse>(UserErrors.InvalidRoles); }
        // Update user properties
        user.UserName = request.UserName;
        user.FullName = request.FullName;
        user.Gender = request.Gender;
        user.BirthDate = request.BirthDate;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var error = updateResult.Errors.First();
            return Result.Failure<UserResponse>(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }
        // Get current roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        // Remove old roles
        var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeRolesResult.Succeeded)
        {
            var error = removeRolesResult.Errors.First();
            return Result.Failure<UserResponse>(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }
        var addRolesResult = await _userManager.AddToRolesAsync(user, request.Roles);
        if (!addRolesResult.Succeeded)
        {
            var error = addRolesResult.Errors.First();
            return Result.Failure<UserResponse>(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }
        var response = user.Adapt<UserResponse>();
        response.Roles = request.Roles; return Result.Success(response);
    }
    public async Task<Result> DeleteAsync(string userId)
    {
        if (await _userManager.FindByIdAsync(userId) is not { } user)
        { return Result.Failure(UserErrors.UserNotFound); }
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var error = result.Errors.First();
            return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }
        return Result.Success();
    }
}

