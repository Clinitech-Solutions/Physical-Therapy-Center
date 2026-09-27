

using PTCenter.Api.Extensions;
using PTCenter.Application.Contracts.Users;

namespace PTCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize(Roles = DefaultRoles.Admin.Name + "," + DefaultRoles.Owner.Name+"," + DefaultRoles.Ceo.Name)]
public class UsersController(IUserService userService) : ControllerBase 
{
    private readonly IUserService _userService = userService;
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    
    public async Task<IActionResult> Add(CreateUserRequest request)
    {
        var result = await _userService.AddAsync(request);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
    [HttpPost]

    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string userId, UpdateUserRequest request)
    {
        var result = await _userService.UpdateAsync(userId, request);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(string userId)
    {
        var result = await _userService.DeleteAsync(userId);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _userService.GetAllAsync();
        return Ok(result);
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _userService.GetProfileAsync(User.GetUserId());
        return Ok(result);
    }
    
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(string? userId)
    {
        var result = await _userService.GetProfileAsync(userId ?? User.GetUserId());
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}