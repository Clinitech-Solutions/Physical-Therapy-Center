using Microsoft.AspNetCore.Mvc;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Authentication;

namespace PTCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.GetTokenAsync(request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        return result.IsSuccess
            ? Ok(new { message = "User registered successfully. OTP has been sent to your email." })
            : result.ToProblem();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyRegister(VerifyRegisterRequest request)
    {
        var result = await _authService.VerifyRegisterAsync(request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendOtp(ResendOtpRequest request)
    {
        var result = await _authService.ResendOtpAsync(request);

        return result.IsSuccess
            ? Ok(new { message = "A new OTP has been sent to your email." })
            : result.ToProblem();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
    {
        var result = await _authService.GoogleLoginAsync(request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);

        return result.IsSuccess
            ? Ok(new { message = "A new OTP has been sent to your email." })
            : result.ToProblem();
    }
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyForgotPasswordOtp(VerifyForgotPasswordRequest request)
    {
        var result = await _authService.VerifyForgotPasswordOtpAsync(request);

        return result.IsSuccess
            ? Ok()
            : result.ToProblem();
    } 
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        return result.IsSuccess
            ? Ok(new { message = "Password changed successfully." })
            : result.ToProblem();
    }
}