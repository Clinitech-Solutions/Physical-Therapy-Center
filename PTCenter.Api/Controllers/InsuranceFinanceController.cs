using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Contracts.Billing;
using PTCenter.Infrastructure.Persistence;
using PTCenter.Infrastructure.Services;

namespace PTCenter.Api.Controllers;

[ApiController]
[Route("api/Finance/insurance")]
[Authorize(Roles=PTCenter.Api.Authorization.ReceptionPermissions.Administration)]
public class InsuranceFinanceController(ApplicationDbContext db,TimeProvider clock) : ControllerBase
{
    private readonly InsuranceFinanceService finance=new(db,clock);
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try{return Ok(await action());}
        catch(BookingRuleException e){return Problem(statusCode:e.Status,title:e.Code,detail:e.Message,extensions:new Dictionary<string,object?>{{"details",e.Details}});}
        catch(DbUpdateException){return Problem(statusCode:409,title:"Insurance.ConcurrentChange",detail:"Financial records changed. Reconcile the original cheque/submission before retrying the same key.");}
        catch(Microsoft.Data.SqlClient.SqlException e) when(e.Number is 1205 or 1222 or 51001){return Problem(statusCode:409,title:"Insurance.ConcurrentChange",detail:"Financial records changed concurrently. Reconcile the original operation before retrying.");}
    }
    [HttpGet("companies")]
    public Task<IActionResult> Companies([FromQuery] InsuranceFinanceQuery query,CancellationToken ct)=>Execute(()=>finance.CompaniesAsync(query,ct));
    [HttpGet("companies/{companyId:int}/account")]
    public Task<IActionResult> Account(int companyId,[FromQuery] InsuranceFinanceQuery query,CancellationToken ct)=>Execute(()=>finance.AccountAsync(companyId,query,ct));
    [HttpGet("companies/{companyId:int}/patients/{patientId:int}/sessions")]
    public Task<IActionResult> Sessions(int companyId,int patientId,[FromQuery] InsuranceFinanceQuery query,CancellationToken ct)=>Execute(()=>finance.SessionsAsync(companyId,patientId,query,ct));
    [HttpGet("companies/{companyId:int}/statement")]
    public async Task<IActionResult> Statement(int companyId,[FromQuery] InsuranceFinanceQuery query,CancellationToken ct)
    {
        try{return File(await finance.StatementAsync(companyId,query,ct),"text/csv; charset=utf-8",$"insurance-{companyId}-{query.ServiceMonth}.csv");}
        catch(BookingRuleException e){return Problem(statusCode:e.Status,title:e.Code,detail:e.Message);}
    }
    [HttpGet("companies/{companyId:int}/selection")]
    public Task<IActionResult> Selection(int companyId,[FromQuery] InsuranceFinanceQuery query,CancellationToken ct)=>Execute(async()=>{
        query.CompanyId=companyId;var rows=await finance.SelectedSessionsAsync(query,ct);
        if(rows.Count>1000)throw new BookingRuleException("Insurance.ScopeTooLarge","Select up to 1000 services at a time by refining the filters.",400);
        return rows;
    });
    [HttpGet("companies/{companyId:int}/statement-data")]
    public Task<IActionResult> StatementData(int companyId,[FromQuery] InsuranceFinanceQuery query,CancellationToken ct)=>Execute(()=>{
        query.CompanyId=companyId;return finance.SelectedSessionsAsync(query,ct);
    });
    [HttpGet("receipts")]
    public Task<IActionResult> History([FromQuery] InsuranceFinanceQuery query,CancellationToken ct)=>Execute(()=>finance.HistoryAsync(query,ct));
    [HttpGet("cheques/{id:int}")]
    public Task<IActionResult> Receipt(int id,CancellationToken ct)=>Execute(()=>finance.ReceiptAsync(id,ct));
    [HttpGet("cheques/operations/{key}")]
    public Task<IActionResult> ReconcileCheque(string key,CancellationToken ct)=>Execute(()=>finance.ReconcileChequeAsync(key,ct));
    [HttpPost("cheques/preview")]
    public Task<IActionResult> Preview(ChequeRequest request,CancellationToken ct)=>Execute(()=>finance.PreviewAsync(request,ct));
    [HttpPost("cheques")]
    public Task<IActionResult> Record(ChequeRequest request,CancellationToken ct)=>Execute(()=>finance.RecordChequeAsync(request,Actor,ct));
    [HttpPost("monthly-claims")]
    public Task<IActionResult> Submit(MonthlyClaimRequest request,CancellationToken ct)=>Execute(()=>finance.SubmitMonthlyAsync(request,Actor,ct));
    [HttpGet("monthly-claims/operations/{key}")]
    public Task<IActionResult> ReconcileSubmission(string key,CancellationToken ct)=>Execute(()=>finance.ReconcileSubmissionAsync(key,ct));
}
