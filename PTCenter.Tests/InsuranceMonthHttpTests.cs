using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Application.Contracts.Billing;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;
using PTCenter.Infrastructure.Services;
using Xunit;

namespace PTCenter.Tests;

public static class InsuranceMonthFixture
{
    public const string ServiceMonth="2026-10";
    public sealed class DecemberClock : TimeProvider {public override DateTimeOffset GetUtcNow()=>new(2026,12,15,10,0,0,TimeSpan.Zero);}
    public static async Task SeedAsync(BookingTestHost host)
    {
        await ReceptionAccessFixture.SeedAsync(host);
        using var scope=host.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.InsuranceCompanies.FindAsync(1))!.Name="AXA";
        db.InsuranceCompanies.Add(new(){Id=2,Name="Historical Bupa",IsActive=true});
        var historic=new TreatmentPlan{Assessment=new(){PatientId=1,SeniorId="access-senior",Diagnosis="Saved insurance course",ConditionType="Physio",Priority="Routine",CompletedAt=DateTime.UtcNow},DoctorId="access-doctor",NumberOfSessions=4,FrequencyPerWeek=2,TreatmentType="Physio"};
        db.TreatmentPlans.Add(historic);await db.SaveChangesAsync();
        var agreements=new[]{new TreatmentCourseAgreement{PatientId=1,TreatmentPlanId=1,InsuranceCompanyId=1},new TreatmentCourseAgreement{PatientId=2,TreatmentPlanId=2,InsuranceCompanyId=1},new TreatmentCourseAgreement{PatientId=1,TreatmentPlanId=historic.Id,InsuranceCompanyId=2}};
        foreach(var a in agreements){a.FundingType="Insurance";a.PricingMode="PerSession";a.ApprovalReference="saved-approval-"+a.TreatmentPlanId;a.CoveredSessions=20;a.PrescribedSessions=20;a.TotalAmount=2000;a.PatientAmount=400;a.InsuranceAmount=1600;}
        db.TreatmentCourseAgreements.AddRange(agreements);await db.SaveChangesAsync();
        var first=ClinicCalendar.DayStart(new(2026,10,1));var end=ClinicCalendar.DayStart(new(2026,11,1));var number=0;
        async Task Add(int patientId,TreatmentCourseAgreement? agreement,string reference,DateTime? delivered)
        {
            var session=new TreatmentPlanSession{TreatmentPlanId=agreement?.TreatmentPlanId ?? 1,SessionNumber=++number,Status=delivered!=null?SessionStatus.Completed:SessionStatus.Pending};db.TreatmentPlanSessions.Add(session);await db.SaveChangesAsync();
            var appointment=new Appointment{PatientId=patientId,DoctorId="access-doctor",TreatmentStartedBy=delivered!=null?"access-senior":null,AppointmentType="Session",TreatmentPlanSessionId=session.Id,Status=delivered!=null?AppointmentStatus.Completed:AppointmentStatus.Pending,
                StartTime=delivered?.AddHours(-1) ?? end.AddMonths(1),EndTime=delivered ?? end.AddMonths(1).AddHours(1),ActualCheckOut=delivered};db.Appointments.Add(appointment);await db.SaveChangesAsync();
            db.Invoices.Add(new(){PatientId=patientId,InvoiceNumber=reference,CourseAgreementId=agreement?.Id,TreatmentPlanSessionId=session.Id,AppointmentId=appointment.Id,DeliveredAt=delivered,TotalAmount=100,PatientAmount=20,InsuranceAmount=80,Type=InvoiceType.Session,
                Payments=[new(){Amount=20,PaymentMethod=PaymentMethod.Cash,PayerType="Patient",Status=PaymentStatus.Paid,PaymentDate=first.AddHours(1),IdempotencyKey="patient:"+reference}]});
            await db.SaveChangesAsync();
        }
        await Add(1,agreements[0],"AXA-OCT-A",first);await Add(1,agreements[0],"AXA-OCT-B",first.AddDays(10));await Add(1,agreements[0],"AXA-OCT-END",end.AddTicks(-1));
        await Add(2,agreements[1],"AXA-OCT-C",first.AddDays(15));await Add(2,agreements[1],"AXA-OCT-D",first.AddDays(16));
        await Add(1,agreements[0],"AXA-SEP",first.AddTicks(-1));await Add(1,agreements[0],"AXA-NOV",end);await Add(1,agreements[0],"FUTURE",null);
        await Add(1,agreements[2],"BUPA-OCT",first.AddDays(12));await Add(1,null,"UNKNOWN-COMPANY",first.AddDays(13));
        (await db.Patients.FindAsync(1))!.InsuranceCompanyId=2;await db.SaveChangesAsync();
    }
}

public class InsuranceMonthHttpTests
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web){Converters={new JsonStringEnumConverter()}};
    private static HttpClient Client(BookingTestHost host)=>host.WithWebHostBuilder(b=>b.ConfigureServices(s=>s.AddSingleton<TimeProvider>(new InsuranceMonthFixture.DecemberClock()))).CreateClient(new(){BaseAddress=new("https://localhost")});
    private static async Task<T> Read<T>(HttpResponseMessage response){Assert.True(response.IsSuccessStatusCode,await response.Content.ReadAsStringAsync());return (await response.Content.ReadFromJsonAsync<T>(Json))!;}
    private static async Task<InsuranceSessionPage> Sessions(HttpClient client,int patient=1,string month="2026-10")=>await Read<InsuranceSessionPage>(await client.GetAsync($"/api/Finance/insurance/companies/1/patients/{patient}/sessions?serviceMonth={month}&pageSize=100"));
    private static async Task<int[]> Submit(HttpClient client,int[]? ids=null,string key="monthly",string month="2026-10",string reference="AXA-OCT-CLAIM"){
        ids ??=(await Sessions(client,1,month)).Items.Concat((await Sessions(client,2,month)).Items).Select(r=>r.Invoice.Id).ToArray();
        var result=await Read<MonthlyClaimReceipt>(await client.PostAsJsonAsync("/api/Finance/insurance/monthly-claims",new MonthlyClaimRequest(key,1,month,ids,reference)));return result.ClaimIds.ToArray();
    }
    private static ChequeRequest Cheque(string key="cheque-one",decimal amount=100,int[]? claims=null,string[]? months=null)=>new(){IdempotencyKey=key,CompanyId=1,ServiceMonths=months ?? ["2026-10"],ClaimIds=claims ?? [],ChequeNumber=key,BankName="National Bank",Amount=amount,ReceivedOn=new(2026,12,5),Notes="Received immediately as collection"};
    private static async Task<ChequePreview> Preview(HttpClient client,ChequeRequest request)=>await Read<ChequePreview>(await client.PostAsJsonAsync("/api/Finance/insurance/cheques/preview",request));
    private static async Task<ChequeReceipt> Record(HttpClient client,ChequeRequest request){request.ExpectedAllocationHash=(await Preview(client,request)).Hash;return await Read<ChequeReceipt>(await client.PostAsJsonAsync("/api/Finance/insurance/cheques",request));}

    [Fact] public async Task Company_month_groups_before_pagination_and_preserves_historical_company_delivery_and_patient_scope()
    {
        using var host=new BookingTestHost();await InsuranceMonthFixture.SeedAsync(host);using var client=Client(host);await ReceptionAccessFixture.LoginAsync(client,"Ceo");
        var account=await Read<InsuranceMonthAccount>(await client.GetAsync("/api/Finance/insurance/companies/1/account?serviceMonth=2026-10&pageSize=1"));
        Assert.Equal(2,account.MatchingPatients);Assert.Equal(5,account.MatchingInvoices);Assert.Single(account.Patients);Assert.Equal(400,account.Totals.Earned);Assert.Equal(400,account.Totals.Remaining);Assert.Equal(0,account.Totals.Receipts);
        Assert.Contains(account.Months,m=>m.Month=="2026-09");Assert.Contains(account.Months,m=>m.Month=="2026-11");Assert.DoesNotContain(account.Months,m=>m.Month=="2026-12");
        var next=await Read<InsuranceMonthAccount>(await client.GetAsync("/api/Finance/insurance/companies/1/account?serviceMonth=2026-10&pageSize=1&page=2"));Assert.Equal(account.Totals,next.Totals);Assert.NotEqual(account.Patients[0].PatientId,next.Patients[0].PatientId);
        var sessions=await Sessions(client);Assert.Equal(3,sessions.MatchingInvoices);Assert.Equal(240,sessions.Totals.Earned);Assert.All(sessions.Items,r=>Assert.Equal(1,r.Invoice.CompanyId));Assert.Contains(sessions.Items,r=>r.Invoice.Reference=="AXA-OCT-A");Assert.Contains(sessions.Items,r=>r.Invoice.Reference=="AXA-OCT-END");
        var other=await Read<InsuranceMonthAccount>(await client.GetAsync("/api/Finance/insurance/companies/2/account?serviceMonth=2026-10"));Assert.Equal(80,other.Totals.Earned);
        var companies=await Read<InsuranceCompanyPage>(await client.GetAsync("/api/Finance/insurance/companies"));Assert.Equal(1,companies.UnavailableCompanyInvoices);Assert.Contains(companies.Items,c=>c.CompanyId==0 && c.Amounts.Earned==80);
    }
    [Fact] public async Task Monthly_submissions_retain_individual_claims_and_multiple_batch_references_and_retry_once()
    {
        using var host=new BookingTestHost();await InsuranceMonthFixture.SeedAsync(host);using var client=Client(host);await ReceptionAccessFixture.LoginAsync(client,"Ceo");var ids=(await Sessions(client)).Items.Select(i=>i.Invoice.Id).ToArray();
        var first=await Submit(client,ids[..2],"batch-a",reference:"STATEMENT-A");var second=await Submit(client,[ids[2]],"batch-b",reference:"STATEMENT-B");Assert.Equal(first,await Submit(client,ids[..2],"batch-a",reference:"STATEMENT-A"));
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/Finance/insurance/monthly-claims",new MonthlyClaimRequest("batch-a",1,"2026-10",ids[..2],"CHANGED"))).StatusCode);
        var rows=await Sessions(client);Assert.Equal(2,rows.Items.Select(r=>r.SubmissionBatchId).Distinct().Count());Assert.Equal(3,rows.Items.Select(r=>r.ClaimId).Distinct().Count());Assert.Equal(240,rows.Totals.Submitted);
        var found=await Read<MonthlyClaimReceipt>(await client.GetAsync("/api/Finance/insurance/monthly-claims/operations/batch-a"));Assert.Equal(160,found.Amount);Assert.Equal("STATEMENT-A",found.Reference);
        using var scope=host.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();Assert.Equal(10,await db.Invoices.CountAsync());Assert.Equal(2,await db.InsuranceClaimSubmissions.CountAsync());Assert.Equal(0,await db.InsuranceChequeReceipts.CountAsync());
    }
    [Fact] public async Task December_cheque_settles_October_across_patients_once_and_preserves_balance_and_audit_allocations()
    {
        using var host=new BookingTestHost();await InsuranceMonthFixture.SeedAsync(host);using var client=Client(host);await ReceptionAccessFixture.LoginAsync(client,"Ceo");await Submit(client);
        var a=(await Sessions(client,1)).Items[0].ClaimId!.Value;var b=(await Sessions(client,2)).Items[0].ClaimId!.Value;var request=Cheque(claims:[a,b]);var preview=await Preview(client,request);
        Assert.Equal(160,preview.Before);Assert.Equal(60,preview.After);Assert.Equal(new decimal[]{80,20},preview.Allocations.Select(i=>i.Amount));request.ExpectedAllocationHash=preview.Hash;
        var receipt=await Read<ChequeReceipt>(await client.PostAsJsonAsync("/api/Finance/insurance/cheques",request));Assert.Equal(100,receipt.Amount);Assert.Equal(new DateOnly(2026,12,5),receipt.ReceivedOn);Assert.Equal(2,receipt.Allocations.Count);Assert.All(receipt.Allocations,r=>Assert.Equal("2026-10",r.ServiceMonth));Assert.False(string.IsNullOrEmpty(receipt.RecordedBy));Assert.Equal(new InsuranceMonthFixture.DecemberClock().GetUtcNow().UtcDateTime,receipt.RecordedAt);
        var retry=await Read<ChequeReceipt>(await client.PostAsJsonAsync("/api/Finance/insurance/cheques",request));Assert.Equal(receipt.Id,retry.Id);Assert.Equal(receipt.Id,(await Read<ChequeReceipt>(await client.GetAsync("/api/Finance/insurance/cheques/operations/cheque-one"))).Id);
        var account=await Read<InsuranceMonthAccount>(await client.GetAsync("/api/Finance/insurance/companies/1/account?serviceMonth=2026-10"));Assert.Equal(400,account.Totals.Earned);Assert.Equal(100,account.Totals.Receipts);Assert.Equal(300,account.Totals.Remaining);Assert.Equal(1,account.Totals.ReceiptCount);
        var history=await Read<InsuranceReceiptHistory>(await client.GetAsync("/api/Finance/insurance/receipts?companyId=1&serviceMonth=2026-10&from=2026-12-01&to=2026-12-31"));Assert.Single(history.Items);Assert.Equal(100,history.ActualReceiptTotal);Assert.Equal(100,history.AllocatedToScope);
        Assert.Empty((await Read<InsuranceReceiptHistory>(await client.GetAsync("/api/Finance/insurance/receipts?companyId=1&serviceMonth=2026-10&from=2026-10-01&to=2026-10-31"))).Items);
        var collection=await Read<BillingPage<BillingPatient>>(await client.GetAsync("/api/Billing/patients?dateBasis=Collection&from=2026-12-01&to=2026-12-31"));Assert.Equal(100,collection.Totals.CollectedInPeriod);Assert.Equal(1,collection.Totals.ReceiptCount);
        await ReceptionAccessFixture.LoginAsync(client);var reception=await Read<BillingPage<BillingPatient>>(await client.GetAsync("/api/Billing/patients?dateBasis=Collection&from=2026-12-01&to=2026-12-31"));Assert.Equal(0,reception.Totals.CollectedInPeriod);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/Finance/insurance/cheques/{receipt.Id}")).StatusCode);
        using var scope=host.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();Assert.Single(await db.InsuranceChequeReceipts.ToListAsync());Assert.Equal(2,await db.Payments.CountAsync(p=>p.InsuranceChequeReceiptId==receipt.Id));Assert.Equal(200,(await db.Payments.ToListAsync()).Where(CourseFinanceService.PatientReceipt).Sum(p=>p.Amount));
    }
    [Fact] public async Task Cheques_are_confined_to_selected_months_and_explicit_multi_month_receipt_is_counted_once()
    {
        using var host=new BookingTestHost();await InsuranceMonthFixture.SeedAsync(host);using var client=Client(host);await ReceptionAccessFixture.LoginAsync(client,"Ceo");await Submit(client);var september=(await Submit(client,month:"2026-09",key:"september"))[0];
        var invalid=Cheque(claims:[september]);Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/Finance/insurance/cheques/preview",invalid)).StatusCode);
        var receipt=await Record(client,Cheque(amount:90,months:["2026-09","2026-10"]));Assert.Equal(new[]{"2026-09","2026-10"},receipt.Allocations.Select(i=>i.ServiceMonth));
        var history=await Read<InsuranceReceiptHistory>(await client.GetAsync("/api/Finance/insurance/receipts?companyId=1&serviceMonth=2026-10"));Assert.Equal(90,history.ActualReceiptTotal);Assert.Equal(10,history.AllocatedToScope);Assert.Equal(2,history.Items[0].Allocations.Count);
        var company=await Read<InsuranceCompanyPage>(await client.GetAsync("/api/Finance/insurance/companies?companyId=1"));Assert.Equal(90,company.Totals.Receipts);Assert.Equal(1,company.Totals.ReceiptCount);
    }
    [Fact] public async Task Duplicate_cheques_double_clicks_and_lost_response_reconciliation_never_create_another_receipt()
    {
        using var host=new BookingTestHost();await InsuranceMonthFixture.SeedAsync(host);using var client=Client(host);await ReceptionAccessFixture.LoginAsync(client,"Ceo");await Submit(client);var request=Cheque();request.ExpectedAllocationHash=(await Preview(client,request)).Hash;
        var responses=await Task.WhenAll(client.PostAsJsonAsync("/api/Finance/insurance/cheques",request),client.PostAsJsonAsync("/api/Finance/insurance/cheques",request));var receipts=new List<ChequeReceipt>();foreach(var response in responses)receipts.Add(await Read<ChequeReceipt>(response));Assert.Equal(receipts[0].Id,receipts[1].Id);
        request.IdempotencyKey="different-operation";request.BankName="  national   BANK ";request.ChequeNumber=" CHEQUE-ONE ";var duplicate=await client.PostAsJsonAsync("/api/Finance/insurance/cheques/preview",request);Assert.Equal(HttpStatusCode.Conflict,duplicate.StatusCode);Assert.Equal(receipts[0].Id,(await duplicate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("details").GetProperty("receiptId").GetInt32());
        request.IdempotencyKey="cheque-one";request.Amount=99;Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/Finance/insurance/cheques",request)).StatusCode);
        using var scope=host.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();Assert.Single(await db.InsuranceChequeReceipts.ToListAsync());Assert.Equal(100,(await db.Payments.Where(p=>p.InsuranceChequeReceiptId!=null).ToListAsync()).Sum(p=>p.Amount));
    }
    [Fact] public async Task Allocation_changes_overpayment_unsubmitted_and_invalid_cheques_are_explicitly_rejected()
    {
        using var host=new BookingTestHost();await InsuranceMonthFixture.SeedAsync(host);using var client=Client(host);await ReceptionAccessFixture.LoginAsync(client,"Ceo");
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/Finance/insurance/cheques/preview",Cheque())).StatusCode);await Submit(client);
        var request=Cheque(amount:30);request.ExpectedAllocationHash=(await Preview(client,request)).Hash;await Record(client,Cheque("intervening",20));Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/Finance/insurance/cheques",request)).StatusCode);
        request.ExpectedAllocationHash=(await Preview(client,request)).Hash;Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/Finance/insurance/cheques",request)).StatusCode);
        var over=await client.PostAsJsonAsync("/api/Finance/insurance/cheques/preview",Cheque("over",351));Assert.Equal(HttpStatusCode.BadRequest,over.StatusCode);Assert.Equal(1,(await over.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("details").GetProperty("excess").GetDecimal());
        foreach(var bad in new[]{Cheque("zero",0),Cheque("negative",-1),Cheque("fraction",1.001m)})Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/Finance/insurance/cheques/preview",bad)).StatusCode);
        var future=Cheque("future");future.ReceivedOn=new(2027,1,1);Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/Finance/insurance/cheques/preview",future)).StatusCode);
    }
    [Theory][InlineData("Receptionist")][InlineData("Doctor")][InlineData("Patient")]
    public async Task Non_finance_roles_cannot_read_or_mutate_company_month_cheque_finance(string role)
    {
        using var host=new BookingTestHost();await InsuranceMonthFixture.SeedAsync(host);using var client=Client(host);await ReceptionAccessFixture.LoginAsync(client,role);
        foreach(var url in new[]{"companies","companies/1/account?serviceMonth=2026-10","receipts","cheques/1","cheques/operations/test","companies/1/statement?serviceMonth=2026-10"})Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/Finance/insurance/"+url)).StatusCode);
        foreach(var url in new[]{"cheques","cheques/preview","monthly-claims"})Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/Finance/insurance/"+url,new{})).StatusCode);
    }
    [Fact] public async Task Filtered_account_expansion_history_and_statement_reconcile_across_pages_and_keep_claim_references()
    {
        using var host=new BookingTestHost();await InsuranceMonthFixture.SeedAsync(host);using var client=Client(host);await ReceptionAccessFixture.LoginAsync(client,"Ceo");await Submit(client);await Record(client,Cheque());
        var q="serviceMonth=2026-10&search=01000000002&claimReference=AXA-OCT&paymentState=Unpaid&pageSize=1";
        var account=await Read<InsuranceMonthAccount>(await client.GetAsync("/api/Finance/insurance/companies/1/account?"+q));Assert.Single(account.Patients);Assert.Equal(2,account.MatchingInvoices);Assert.Equal(160,account.Totals.Earned);
        var detail=await Read<InsuranceSessionPage>(await client.GetAsync("/api/Finance/insurance/companies/1/patients/2/sessions?"+q));Assert.Single(detail.Items);Assert.Equal(160,detail.Totals.Earned);
        var csv=await client.GetStringAsync("/api/Finance/insurance/companies/1/statement?"+q);Assert.Equal(2,csv.Split('\n').Count(line=>line.Contains("\"AXA-OCT-C\"")||line.Contains("\"AXA-OCT-D\"")));Assert.Contains("Africa/Cairo",csv);Assert.Contains("AXA-OCT-CLAIM",csv);Assert.Contains("2026-10",csv);
    }
}
