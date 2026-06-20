using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Authentication;

namespace PTCenter.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> GetTokenAsync(LoginRequest request);
}