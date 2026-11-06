using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTCenter.Api.Authorization;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Infrastructure.Services;
namespace PTCenter.Api.Controllers;

[ApiController, Route("api/Absences"), Authorize(Roles = ReceptionPermissions.Operations)]
public class AbsencesController(BookingService bookings) : ClinicalController
{
    [HttpGet] public Task<IActionResult> List([FromQuery] DateOnly? date) => Execute(() => bookings.AbsencesAsync(date));
    [HttpPost("preview")] public Task<IActionResult> Preview(AbsenceRequest request) => Execute(() => bookings.AbsenceImpactAsync(request));
    [HttpPost] public Task<IActionResult> Create(AbsenceRequest request) => Execute(() => bookings.SaveAbsenceAsync(null, request, Actor));
    [HttpPut("{id:int}")] public Task<IActionResult> Update(int id, AbsenceRequest request) => Execute(() => bookings.SaveAbsenceAsync(id, request, Actor));
    [HttpPost("{id:int}/withdraw")] public Task<IActionResult> Withdraw(int id, AbsenceRequest request) => Execute(() => bookings.SaveAbsenceAsync(id, request, Actor, true));
    [HttpGet("affected")] public Task<IActionResult> Affected([FromQuery] DateOnly? date) => Execute(() => bookings.AffectedBookingsAsync(date));
    [HttpGet("appointments/{id:int}/options")] public Task<IActionResult> Options(int id, [FromQuery] DateOnly? date) => Execute(() => bookings.RecoveryOptionsAsync(id, date));
    [HttpPost("appointments/{id:int}/resolve")] public Task<IActionResult> Resolve(int id, RecoveryRequest request) => Execute(() => bookings.ResolveAbsenceAsync(id, request, Actor));
    [HttpPost("appointments/{id:int}/contact")] public Task<IActionResult> Contact(int id, RecoveryContactRequest request) => Execute(() => bookings.RecordRecoveryContactAsync(id, request, Actor));
    [HttpGet("appointments/{id:int}/history")] public Task<IActionResult> History(int id) => Execute(() => bookings.RecoveryHistoryAsync(id));
}
