using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Authentication;

namespace PTCenter.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> GetTokenAsync(LoginRequest request);
    Task<Result> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponse>> VerifyRegisterAsync(VerifyRegisterRequest request);
    Task<Result> ResendOtpAsync(ResendOtpRequest request);
    Task<Result<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request);
}