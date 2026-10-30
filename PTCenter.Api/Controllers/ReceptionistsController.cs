using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTCenter.Application.Contracts.Receptionist;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Receptionist,Senior,Doctor,Ceo,Admin,Owner")]
// [Authorize(Roles = "Receptionist,Admin")]
public class ReceptionistController(IReceptionistService receptionistService) : ControllerBase
{
    private readonly IReceptionistService _receptionistService = receptionistService;

    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var result = await _receptionistService.GetDashboardStatsAsync();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("today-sessions")]
    public async Task<IActionResult> GetTodaySessions([FromQuery] TodaySessionsQueryRequest request)
    {
        var result = await _receptionistService.GetTodaySessionsAsync(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    [HttpPut("appointments/{appointmentId}/status")]
    public async Task<IActionResult> UpdateAppointmentStatus(int appointmentId, [FromBody] AppointmentStatus newStatus)
    {
        var result = await _receptionistService.UpdateAppointmentStatusAsync(appointmentId, newStatus);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
    [HttpPut("appointments/{appointmentId}/cancel")]
    public async Task<IActionResult> CancelAppointmentAsAbsent(int appointmentId)
    {
        var result = await _receptionistService.CancelAppointmentAsAbsentAsync(appointmentId);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
