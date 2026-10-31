using Microsoft.AspNetCore.Mvc;
using PTCenter.Application.Contracts.Rooms;
using PTCenter.Application.Interfaces;

namespace PTCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.StaffRead)]
public class RoomsController(IRoomService roomService) : ControllerBase
{
    private readonly IRoomService _roomService = roomService;

    [HttpPost("availability")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Receptionist,Senior,Doctor,Ceo,Admin,Owner")]
    public async Task<IActionResult> Availability(RoomAvailabilityRequest request)
    {
        var result = await _roomService.GetAvailableAsync(request);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.Error.StatusCode ?? 400, result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _roomService.GetAllAsync();

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(
                result.Error.StatusCode ?? StatusCodes.Status400BadRequest,
                result.Error);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _roomService.GetByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(
                result.Error.StatusCode ?? StatusCodes.Status400BadRequest,
                result.Error);
    }

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoomRequest request)
    {
        var result = await _roomService.CreateAsync(request);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(
                result.Error.StatusCode ?? StatusCodes.Status400BadRequest,
                result.Error);
    }

    [HttpPut("{id:int}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateRoomRequest request)
    {
        var result = await _roomService.UpdateAsync(id, request);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(
                result.Error.StatusCode ?? StatusCodes.Status400BadRequest,
                result.Error);
    }

    public class MaintenanceRequest
    {
        public bool Maintenance { get; set; }
    }

    [HttpPatch("{id:int}/maintenance")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public async Task<IActionResult> UpdateMaintenance(
        int id,
        [FromBody] MaintenanceRequest request)
    {
        var result = await _roomService.UpdateMaintenanceAsync(
            id, request.Maintenance);

        return result.IsSuccess
            ? NoContent()
            : StatusCode(
                result.Error.StatusCode ?? StatusCodes.Status400BadRequest,
                result.Error);
    }

    [HttpDelete("{id:int}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _roomService.DeleteAsync(id);

        return result.IsSuccess
            ? NoContent()
            : StatusCode(
                result.Error.StatusCode ?? StatusCodes.Status400BadRequest,
                result.Error);
    }
}
