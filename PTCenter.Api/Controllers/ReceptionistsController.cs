using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTCenter.Application.Contracts.Receptionist;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Api.Extensions;
using PTCenter.Infrastructure.Services;

namespace PTCenter.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Receptionist,Senior,Doctor,Ceo,Admin,Owner")]
// [Authorize(Roles = "Receptionist,Admin")]
public class ReceptionistController(IReceptionistService receptionistService, PTCenter.Infrastructure.Persistence.ApplicationDbContext db, BookingService bookings) : ClinicalController
{
    private readonly IReceptionistService _receptionistService = receptionistService;

    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var result = await _receptionistService.GetDashboardStatsAsync();
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.Error.StatusCode ?? 400, result.Error);
    }

    [HttpGet("today-sessions")]
    public async Task<IActionResult> GetTodaySessions([FromQuery] TodaySessionsQueryRequest request)
    {
        var result = await _receptionistService.GetTodaySessionsAsync(request);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.Error.StatusCode ?? 400, result.Error);
    }
    [HttpPut("appointments/{appointmentId}/status")]
    public async Task<IActionResult> UpdateAppointmentStatus(int appointmentId, [FromBody] AppointmentStatus newStatus)
    {
        if (!await PTCenter.Api.Authorization.AttendanceAuthorization.AllowedAsync(db, User, appointmentId)) return Forbid();
        var result = await _receptionistService.UpdateAppointmentStatusAsync(appointmentId, newStatus);
        return result.IsSuccess ? Ok() : StatusCode(result.Error.StatusCode ?? 400, result.Error);
    }
    [HttpPut("appointments/{appointmentId}/cancel")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public Task<IActionResult> CancelAppointment(int appointmentId, UnattendedAppointmentRequest request) =>
        Execute(async () => { await bookings.CloseUnattendedAsync(appointmentId, false, request, Actor); return new { appointmentId }; });

    [HttpPut("appointments/{appointmentId}/no-show")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public Task<IActionResult> NoShow(int appointmentId, UnattendedAppointmentRequest request) =>
        Execute(async () => { await bookings.CloseUnattendedAsync(appointmentId, true, request, Actor); return new { appointmentId }; });
}
