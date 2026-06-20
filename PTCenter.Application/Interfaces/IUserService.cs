using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Authentication;
using PTCenter.Application.Contracts.Users;

namespace PTCenter.Application.Interfaces;

public interface IUserService
{
    Task<Result<UserProfileResponse>> GetProfileAsync(string userId);
    
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    
    Task<Result<List<UserResponse>>> GetAllAsync();
    
    Task<Result<UserResponse>> AddAsync(CreateUserRequest request);
}