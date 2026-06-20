
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
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
    //[HttpPost]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //public async Task<IActionResult> Register(RegisterRequest request)
    //{
    //    var result = await _authService.RegisterAsync(request);
    //    return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    //}
    //[HttpPost]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    //{
    //    var result = await _authService.RefreshTokenAsync(request);
    //    return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    //}
    //[HttpPost]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //public async Task<IActionResult> ForgetPassword(ForgetPasswordRequest request)
    //{
    //    var result = await _authService.ForgetPasswordAsync(request);
    //    return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    //}

}