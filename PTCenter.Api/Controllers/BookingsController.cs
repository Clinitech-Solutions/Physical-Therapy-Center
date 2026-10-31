using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Services;

namespace PTCenter.Api.Controllers;

[ApiController]
[Route("api/Bookings")]
[Authorize(Roles = "Receptionist,Senior,Doctor,Ceo,Admin,Owner")]
public class BookingsController(BookingService service) : ControllerBase
{
    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (BookingRuleException error) { return Problem(statusCode: error.Status, title: error.Code, detail: error.Message); }
        catch (DbUpdateException) { return Problem(statusCode: 409, title: "Booking.ConcurrentChange", detail: "Booking data changed. Reconcile the retry key before trying again."); }
        catch (Microsoft.Data.SqlClient.SqlException error) when (error.Number is 1205 or 1222) { return Problem(statusCode: 409, title: "Booking.ConcurrentChange", detail: "Booking data changed concurrently. Reconcile the same operation key before retrying."); }
    }
    [HttpGet("patients")]
    public Task<IActionResult> Directory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100) => Execute(() => service.DirectoryAsync(pageNumber, pageSize));
    [HttpGet("patients/{id:int}")]
    public Task<IActionResult> Context(int id) => Execute(() => service.ContextAsync(id));
    [HttpGet("clinicians/workload")]
    public Task<IActionResult> Workload() => Execute(() => service.WorkloadAsync());
    [HttpGet("patients/{id:int}/clinicians")]
    public Task<IActionResult> Clinicians(int id, [FromQuery] int? planId, [FromQuery] bool treatment = false) => Execute(() => service.CliniciansAsync(id, planId, treatment));
    [HttpPost("preview")]
    public Task<IActionResult> Preview(BookingRequest request) => Execute(() => service.PreviewAsync(request));
    [HttpPost]
    public Task<IActionResult> Create(BookingRequest request) => Execute(() => service.CreateAsync(request));
    [HttpGet("operations/{key}")]
    public Task<IActionResult> Reconcile(string key) => Execute(() => service.ReceiptAsync(key));
    [HttpGet("accounts")]
    public Task<IActionResult> Accounts() => Execute(() => service.AccountsAsync());
    [HttpPost("accounts/{key}/payments")]
    public Task<IActionResult> Collect(string key, CollectBookingPaymentRequest request) => Execute(() => service.CollectAsync(key, request));
    [HttpGet("accounts/{key}/payments/{operationKey}")]
    public Task<IActionResult> ReconcileCollection(string key, string operationKey) => Execute(() => service.ReconcileCollectionAsync(key, operationKey));
    [HttpPut("appointments/{id:int}/schedule")]
    public Task<IActionResult> Reschedule(int id, RescheduleBookingRequest request) => Execute(async () => { await service.RescheduleAsync(id, request); return new { id }; });
    [HttpPut("appointments/{id:int}/status")]
    public Task<IActionResult> Status(int id, [FromBody] AppointmentStatus status) => Execute(async () => { await service.TransitionAsync(id, status); return new { id }; });
    [HttpPost("assessments/complete")]
    [Authorize(Roles = "Senior,Ceo,Admin,Owner")]
    public Task<IActionResult> CompleteAssessment(CompleteAssessmentRequest request) => Execute(() => service.CompleteAssessmentAsync(request,
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", User.IsInRole("Ceo") || User.IsInRole("Admin") || User.IsInRole("Owner")));
    [HttpPost("assessments/{id:int}/complete")]
    [Authorize(Roles = "Senior,Ceo,Admin,Owner")]
    public Task<IActionResult> CompleteExisting(int id) => Execute(async () => { await service.CompleteExistingAssessmentAsync(id,
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", User.IsInRole("Ceo") || User.IsInRole("Admin") || User.IsInRole("Owner")); return new { id }; });
}
