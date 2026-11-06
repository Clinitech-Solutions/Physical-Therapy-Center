using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Billing;
using PTCenter.Application.Interfaces;
using PTCenter.Infrastructure.Persistence;
using PTCenter.Infrastructure.Services;

namespace PTCenter.Api.Controllers;

[ApiController]
[Authorize(Roles = "Receptionist,Senior,Doctor,Ceo,Admin,Owner")]
[Route("api/[controller]")]
public class BillingController(IBillingService billing, ApplicationDbContext db) : ControllerBase
{
    private bool Finance => User.IsInRole("Ceo") || User.IsInRole("Admin") || User.IsInRole("Owner");
    private readonly BillingQueryService query = new(db);
    private async Task<IActionResult> Query<T>(Func<Task<T>> action) {
        try { return Ok(await action()); }
        catch(BookingRuleException e) { return Problem(statusCode:e.Status,title:e.Code,detail:e.Message); }
    }
    [HttpGet("patients")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public Task<IActionResult> Patients([FromQuery] BillingQuery filters,CancellationToken ct) => Query(()=>query.PatientsAsync(filters,Finance,ct));
    [HttpGet("patients/{patientId:int}/invoices")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public Task<IActionResult> PatientInvoices(int patientId,[FromQuery] BillingQuery filters,CancellationToken ct) => Query(()=>query.InvoicesAsync(patientId,filters,Finance,ct));
    [HttpGet("filter-options")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public Task<IActionResult> Options(CancellationToken ct) => Query(()=>query.OptionsAsync(ct));
    [HttpGet("accounts/{key}/allocation")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public Task<IActionResult> Allocation(string key,[FromQuery] decimal amount,CancellationToken ct) => Query(()=>new BillingAllocationService(db).PreviewAsync(key,amount,ct));
    [HttpGet("export")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public async Task<IActionResult> Export([FromQuery] BillingQuery filters,[FromQuery] string kind="patients",CancellationToken ct=default) {
        try { return File(await query.ExportAsync(filters,Finance,kind,ct),"text/csv; charset=utf-8",$"billing-{kind}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv"); }
        catch(BookingRuleException e) { return Problem(statusCode:e.Status,title:e.Code,detail:e.Message); }
    }
    private async Task<IActionResult> Execute<T>(Func<Task<Result<T>>> action)
    {
        try {
            var result=await action();
            return result.IsFailure ? StatusCode(result.Error.StatusCode ?? 400,result.Error) : Ok(result.Value);
        }
        catch(DbUpdateException) { return Conflict(new {code="Billing.ConcurrentChange",description="Financial records changed. Check the original payment history before retrying with the same key."}); }
        catch(Microsoft.Data.SqlClient.SqlException error) when(error.Number is 1205 or 1222)
        { return Conflict(new {code="Billing.ConcurrentChange",description="Financial records changed concurrently. Check the original payment history before retrying with the same key."}); }
    }
    [HttpGet("invoices")]
    public Task<IActionResult> GetAllInvoices() => Execute(billing.GetAllInvoicesAsync);
    [HttpGet("invoices/{id:int}")]
    public Task<IActionResult> GetInvoiceById(int id) => Execute(()=>billing.GetInvoiceByIdAsync(id));
    [HttpPost("invoices")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public Task<IActionResult> CreateInvoice(CreateInvoiceRequest request) => Execute(()=>billing.CreateInvoiceAsync(request));
    [HttpPatch("invoices/{id:int}/status")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public Task<IActionResult> UpdateInvoiceStatus(int id,UpdateInvoiceStatusRequest request) => Execute(()=>billing.UpdateInvoiceStatusAsync(id,request));
    [HttpDelete("invoices/{id:int}")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public Task<IActionResult> DeleteInvoice(int id) => Execute(async()=>{
        var result=await billing.DeleteInvoiceAsync(id);
        return result.IsFailure ? Result.Failure<object>(result.Error) : Result.Success<object>(new{message="Invoice deleted successfully."});
    });
    [HttpPost("payments")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public Task<IActionResult> AddPayment(CreatePaymentRequest request) => Execute(()=>billing.AddPaymentAsync(request));
    [HttpGet("invoices/{invoiceId:int}/payments")]
    public Task<IActionResult> GetPaymentsByInvoiceId(int invoiceId) => Execute(()=>billing.GetPaymentsByInvoiceIdAsync(invoiceId));
}
