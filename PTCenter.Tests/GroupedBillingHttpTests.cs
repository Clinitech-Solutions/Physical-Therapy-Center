using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Application.Contracts.Billing;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;
using PTCenter.Infrastructure.Services;
using Xunit;

namespace PTCenter.Tests;

public static class GroupedBillingFixture
{
    public static async Task SeedAsync(BookingTestHost host)
    {
        await ReceptionAccessFixture.SeedAsync(host);
        using var scope=host.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        foreach(var user in await db.Users.Where(u=>u.Id=="access-selfpay" || u.Id=="access-insured").ToListAsync()) user.FullName="Same Patient";
        db.InsuranceCompanies.Add(new(){Id=2,Name="Historical Company Two",IsActive=true});
        var plan=new TreatmentPlan {Assessment=new(){PatientId=1,SeniorId="access-senior",Diagnosis="Billing fixture",ConditionType="Physio",Priority="Routine",CompletedAt=DateTime.UtcNow},DoctorId="access-doctor",NumberOfSessions=12,FrequencyPerWeek=2,TreatmentType="Physio"};
        db.TreatmentPlans.Add(plan);await db.SaveChangesAsync();
        var a=new TreatmentCourseAgreement{PatientId=1,TreatmentPlanId=1,FundingType="SelfPay",PricingMode="Package",CoveredSessions=12,PrescribedSessions=12,TotalAmount=1200,PatientAmount=1200,InstallmentsJson="[{\"dueDate\":\"2030-01-01\",\"amount\":1130}]"};
        var b=new TreatmentCourseAgreement{PatientId=1,TreatmentPlanId=plan.Id,FundingType="Insurance",InsuranceCompanyId=1,PricingMode="Package",CoveredSessions=12,PrescribedSessions=12,TotalAmount=1200,PatientAmount=240,InsuranceAmount=960,ApprovalReference="saved-approval"};
        var c=new TreatmentCourseAgreement{PatientId=2,TreatmentPlanId=2,FundingType="Insurance",InsuranceCompanyId=2,PricingMode="PerSession",CoveredSessions=1,PrescribedSessions=1,TotalAmount=100,PatientAmount=0,InsuranceAmount=100,ApprovalReference="approval-two"};
        db.TreatmentCourseAgreements.AddRange(a,b,c);await db.SaveChangesAsync();
        var start=ClinicCalendar.DayStart(new(2026,11,6));
        for(var number=1;number<=12;number++) {
            var invoice=new Invoice {PatientId=1,InvoiceNumber=$"SELF-{number}",Type=InvoiceType.Session,CourseAgreementId=a.Id,TotalAmount=100,PatientAmount=100,CreatedAt=start.AddHours(-2)};
            if(number<=2) invoice.Payments.Add(new(){Amount=number==1?30:40,Status=PaymentStatus.Paid,PaymentMethod=PaymentMethod.Cash,PayerType="Patient",PaymentDate=start.AddHours(1),IdempotencyKey=$"course:one-receipt:{number}"});
            db.Invoices.Add(invoice);
            var session=new TreatmentPlanSession{TreatmentPlanId=plan.Id,SessionNumber=number,Status=number<=6?SessionStatus.Completed:SessionStatus.Pending};db.TreatmentPlanSessions.Add(session);await db.SaveChangesAsync();
            var appointment=new Appointment{PatientId=1,DoctorId="access-doctor",SeniorId="access-senior",TreatmentStartedBy=number<=6?"access-senior":null,TreatmentPlanSessionId=session.Id,AppointmentType="Session",StartTime=start.AddHours(2),EndTime=start.AddHours(3),Status=number<=6?AppointmentStatus.Completed:AppointmentStatus.Pending,ActualCheckOut=number<=6?start.AddHours(3):null};db.Appointments.Add(appointment);await db.SaveChangesAsync();
            var insured=new Invoice{PatientId=1,InvoiceNumber=$"INS-{number}",Type=InvoiceType.Session,CourseAgreementId=b.Id,TreatmentPlanSessionId=session.Id,AppointmentId=appointment.Id,TotalAmount=100,PatientAmount=20,InsuranceAmount=80,CreatedAt=start.AddHours(-2),DeliveredAt=number<=6?start.AddHours(3):null};
            insured.Payments.Add(new(){Amount=20,PaymentMethod=PaymentMethod.Visa,PayerType="Patient",Status=PaymentStatus.Paid,PaymentDate=start.AddHours(-1),IdempotencyKey=$"invoice:insured-{number}"});db.Invoices.Add(insured);
        }
        db.Invoices.AddRange(new Invoice{PatientId=1,InvoiceNumber="WAIVED",TotalAmount=100,PatientAmount=100,Status=InvoiceStatus.Waived,CreatedAt=start.AddHours(-2)},
            new Invoice{PatientId=2,InvoiceNumber="ZERO-PATIENT",CourseAgreementId=c.Id,TotalAmount=100,PatientAmount=0,InsuranceAmount=100,CreatedAt=start.AddHours(-2)},
            new Invoice{PatientId=1,InvoiceNumber="LEGACY-INS",TotalAmount=50,PatientAmount=10,InsuranceAmount=40,CreatedAt=start.AddHours(-2)});
        // Changing current membership must never relabel saved company 1 or unknown legacy history.
        var patient=await db.Patients.FindAsync(1);patient!.InsuranceCompanyId=2;patient.PaymentType="Insurance";
        await db.SaveChangesAsync();
        foreach(var invoice in await db.Invoices.ToListAsync()) invoice.CreatedAt=start.AddHours(-2);
        await db.SaveChangesAsync();
    }
}

public class GroupedBillingHttpTests
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = new(System.Text.Json.JsonSerializerDefaults.Web) { Converters={new System.Text.Json.Serialization.JsonStringEnumConverter()} };
    private static async Task<BillingPage<T>> Read<T>(HttpClient client,string path) {
        var response=await client.GetAsync(path);Assert.True(response.IsSuccessStatusCode,await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<BillingPage<T>>(Json))!;
    }
    [Fact] public async Task Groups_before_pagination_and_reconciles_filter_expansion_and_exports()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await GroupedBillingFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        var first=await Read<BillingPatient>(client,"/api/Billing/patients?pageSize=1");var second=await Read<BillingPatient>(client,"/api/Billing/patients?pageSize=1&page=2");
        Assert.Equal(2,first.MatchingPatients);Assert.Equal(27,first.MatchingInvoices);Assert.Single(first.Items);Assert.NotEqual(first.Items[0].PatientId,second.Items[0].PatientId);Assert.Equal(first.Items[0].Name,second.Items[0].Name);
        Assert.Equal(2650,first.Totals.Agreed);Assert.Equal(1450,first.Totals.PatientLiability);Assert.Equal(310,first.Totals.PatientReceipts);Assert.Equal(1140,first.Totals.PatientRemaining);Assert.Equal(first.Totals,second.Totals);Assert.Equal(13,first.Totals.ReceiptCount);
        var patient=first.Items.Single();Assert.Equal("Mixed",patient.Funding);Assert.Equal(26,patient.InvoiceCount);Assert.Contains("Historical company unavailable",patient.Companies);
        var detail=await Read<BillingInvoice>(client,"/api/Billing/patients/1/invoices?pageSize=10");Assert.Equal(26,detail.MatchingInvoices);Assert.Equal(10,detail.Items.Count);Assert.Equal(patient.Amounts,detail.Totals);
        var filtered=await Read<BillingPatient>(client,"/api/Billing/patients?companyIds=1");Assert.Single(filtered.Items);Assert.Equal(12,filtered.MatchingInvoices);Assert.Equal(1200,filtered.Totals.Agreed);Assert.Equal(240,filtered.Totals.PatientReceipts);Assert.Equal(0,filtered.Totals.PatientRemaining);Assert.Equal("Settled",filtered.Items[0].PatientStatus);Assert.False(filtered.Items[0].FullySettled);
        var expansion=await Read<BillingInvoice>(client,"/api/Billing/patients/1/invoices?companyIds=1&pageSize=100");Assert.All(expansion.Items,i=>{Assert.Equal(1,i.CompanyId);Assert.Equal("Patient share fully paid",i.CollectionBlockedReason);});Assert.Equal(filtered.Items[0].Amounts,expansion.Totals);
        var csv=await client.GetStringAsync("/api/Billing/export?kind=invoices&companyIds=1&pageSize=1");Assert.Equal(12,csv.Split('\n').Count(line=>line.Contains("\"INS-")));Assert.Contains("Africa/Cairo",csv);Assert.Equal(1200,decimal.Parse(System.Text.RegularExpressions.Regex.Match(csv,"\"Agreed\",\"([0-9.]+)\"").Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture));
        var summary=await client.GetStringAsync("/api/Billing/export?kind=patients&pageSize=1");Assert.Equal(2,summary.Split('\n').Count(line=>line.Contains("\"Same Patient\"")));
    }
    [Fact] public async Task Receipt_dates_methods_and_service_delivery_have_distinct_meanings()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await GroupedBillingFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        Assert.Equal(0,(await Read<BillingPatient>(client,"/api/Billing/patients?from=2026-11-06&to=2026-11-06")).MatchingInvoices);
        var collected=await Read<BillingPatient>(client,"/api/Billing/patients?dateBasis=Collection&from=2026-11-06&to=2026-11-06&method=Cash");Assert.Equal(2,collected.MatchingInvoices);Assert.Equal(70,collected.Totals.CollectedInPeriod);Assert.Equal(70,collected.Totals.PatientReceipts);Assert.Equal(1,collected.Totals.ReceiptCount);Assert.Equal(130,collected.Totals.PatientRemaining);
        var delivered=await Read<BillingPatient>(client,"/api/Billing/patients?dateBasis=Service&from=2026-11-06&to=2026-11-06&clinicianId=access-senior");Assert.Equal(6,delivered.MatchingInvoices);Assert.Equal(120,delivered.Totals.PatientReceipts);
        await ReceptionAccessFixture.LoginAsync(client,"Ceo");var finance=await Read<BillingPatient>(client,"/api/Billing/patients?companyIds=1");Assert.Equal(960,finance.Totals.InsurerPlanned);Assert.Equal(480,finance.Totals.InsurerEarned);Assert.Equal(480,finance.Totals.InsurerEarnedOutstanding);
    }
    [Theory] [InlineData("2026-04-24",23)] [InlineData("2026-10-29",25)] [InlineData("2026-11-06",24)]
    public async Task Cairo_date_filters_are_half_open_on_normal_and_DST_days(string date,int hours)
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await ReceptionAccessFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        var bounds=ClinicCalendar.UtcDay(DateOnly.Parse(date));Assert.Equal(hours,(bounds.End-bounds.Start).TotalHours);
        using(var scope=host.Services.CreateScope()) {var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();foreach(var time in new[]{bounds.Start.AddTicks(-1),bounds.Start,bounds.End.AddTicks(-1),bounds.End})db.Invoices.Add(new(){PatientId=1,InvoiceNumber=time.Ticks.ToString(),CreatedAt=time,DeliveredAt=time,TotalAmount=10,PatientAmount=10,Payments=[new(){Amount=1,PaymentMethod=PaymentMethod.Cash,Status=PaymentStatus.Paid,PaymentDate=time}]});await db.SaveChangesAsync();foreach(var invoice in await db.Invoices.ToListAsync())invoice.CreatedAt=new DateTime(long.Parse(invoice.InvoiceNumber),DateTimeKind.Utc);await db.SaveChangesAsync();}
        foreach(var basis in new[]{"Invoice","Service","Collection"}) {var result=await Read<BillingPatient>(client,$"/api/Billing/patients?dateBasis={basis}&from={date}&to={date}");Assert.Equal(2,result.MatchingInvoices);Assert.Equal(20,result.Totals.Agreed);}
    }
    [Fact] public async Task Reception_cannot_manage_claims_and_invalid_filters_are_rejected()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await ReceptionAccessFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/Finance/insurance/accounts")).StatusCode);
        foreach(var path in new[]{"/api/Finance/insurance/claims","/api/Finance/insurance/claims/1/settlements"})Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(path,new{})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/Billing/patients?claimStatus=Submitted")).StatusCode);
        foreach(var query in new[]{"page=0","pageSize=101","dateBasis=Other","from=2026-11-07&to=2026-11-06","type=77","method=77","companyIds=-1"})Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/Billing/patients?"+query)).StatusCode);
        await ReceptionAccessFixture.LoginAsync(client,"Ceo");Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/Finance/insurance/accounts")).StatusCode);
    }
    [Fact] public async Task Collection_rejects_waivers_insurer_money_overpayment_and_stale_balances_and_retries_once()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await GroupedBillingFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        var invoices=await client.GetFromJsonAsync<List<InvoiceResponse>>("/api/Billing/invoices",Json);
        foreach(var number in new[]{"WAIVED","ZERO-PATIENT","INS-1"})Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/Billing/payments",new CreatePaymentRequest{InvoiceId=invoices!.Single(i=>i.InvoiceNumber==number).Id,Amount=1,Method=PaymentMethod.Cash,IdempotencyKey=Guid.NewGuid().ToString()})).StatusCode);
        var id=invoices!.Single(i=>i.InvoiceNumber=="SELF-3").Id;
        var request=new CreatePaymentRequest{InvoiceId=id,Amount=30,ExpectedRemaining=100,Method=PaymentMethod.Cash,IdempotencyKey="grouped-partial"};
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/Billing/payments",request)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/Billing/payments",request)).StatusCode);
        request.IdempotencyKey="stale-other";Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/Billing/payments",request)).StatusCode);
        request.ExpectedRemaining=70;request.Amount=71;Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/Billing/payments",request)).StatusCode);
        request.Amount=1;request.Method=PaymentMethod.Insurance;Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/Billing/payments",request)).StatusCode);
        var result=await Read<BillingPatient>(client,"/api/Billing/patients");Assert.Equal(340,result.Totals.PatientReceipts);Assert.Equal(1110,result.Totals.PatientRemaining);
    }
    [Fact] public async Task CSV_escapes_formula_like_text_and_keeps_all_pages()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await GroupedBillingFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        using(var scope=host.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();(await db.Users.FindAsync("access-selfpay"))!.FullName="  =HYPERLINK(\"unsafe\")";await db.SaveChangesAsync();}
        var csv=await client.GetStringAsync("/api/Billing/export?kind=patients&pageSize=1");Assert.Contains("'  =HYPERLINK",csv);Assert.Contains("Same Patient",csv);
    }
    [Fact] public async Task Saved_account_preview_rejects_changed_allocations_and_preserves_a_single_receipt_on_retry()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await ReceptionAccessFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        var saved=await client.PostAsJsonAsync("/api/Bookings",new PTCenter.Application.Contracts.Bookings.BookingRequest{IdempotencyKey="allocation-booking",PatientId=1,TreatmentPlanId=1,ClinicianId="access-doctor",RoomId=1,PricingMode="Package",UnitPrice=100,StartsAt=[new DateTimeOffset(2030,2,4,10,0,0,TimeSpan.Zero)],Installments=[new(new(2030,1,1),300)]});Assert.True(saved.IsSuccessStatusCode,await saved.Content.ReadAsStringAsync());
        var account=(await client.GetFromJsonAsync<List<PTCenter.Application.Contracts.Bookings.BookingAccount>>("/api/Bookings/accounts"))!.Single();
        var preview=(await client.GetFromJsonAsync<BillingAllocation>($"/api/Billing/accounts/{account.Key}/allocation?amount=150"))!;Assert.Equal(new decimal[]{100,50},preview.Items.Select(i=>i.Amount));
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/Billing/payments",new CreatePaymentRequest{InvoiceId=preview.Items[0].InvoiceId,IdempotencyKey="intervening",Amount=50,Method=PaymentMethod.Cash})).StatusCode);
        var request=new PTCenter.Application.Contracts.Bookings.CollectBookingPaymentRequest("preview-collection",150,PaymentMethod.Visa,preview.Hash);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/Bookings/accounts/{account.Key}/payments",request)).StatusCode);
        preview=(await client.GetFromJsonAsync<BillingAllocation>($"/api/Billing/accounts/{account.Key}/allocation?amount=150"))!;Assert.Equal(new decimal[]{50,100},preview.Items.Select(i=>i.Amount));
        request=request with {ExpectedAllocationHash=preview.Hash};
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync($"/api/Bookings/accounts/{account.Key}/payments",request)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync($"/api/Bookings/accounts/{account.Key}/payments",request)).StatusCode);
        var after=await Read<BillingPatient>(client,"/api/Billing/patients");Assert.Equal(300,after.Totals.Agreed);Assert.Equal(200,after.Totals.PatientReceipts);Assert.Equal(100,after.Totals.PatientRemaining);Assert.Equal(2,after.Totals.ReceiptCount);
        account=(await client.GetFromJsonAsync<List<PTCenter.Application.Contracts.Bookings.BookingAccount>>("/api/Bookings/accounts"))!.Single();Assert.Equal(100,account.RemainingAmount);Assert.Single(account.Installments);
    }
    [Fact] public async Task CEO_can_submit_and_settle_earned_claims_while_patient_and_insurer_cash_remain_separate()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await GroupedBillingFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client,"Ceo");
        var invoices=await Read<BillingInvoice>(client,"/api/Billing/patients/1/invoices?companyIds=1&pageSize=100");var id=invoices.Items.Single(i=>i.Reference=="INS-1").Id;
        var claim=await client.PostAsJsonAsync("/api/Finance/insurance/claims",new {invoiceId=id,idempotencyKey="ceo-claim",submissionReference="CLAIM-001"});Assert.Equal(HttpStatusCode.OK,claim.StatusCode);
        var claimId=(await claim.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetInt32();
        var receipt=new{idempotencyKey="ceo-settlement",amount=30,method="Online",settlementReference="INSURER-RECEIPT-001"};
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/Finance/insurance/claims/{claimId}/settlements",receipt)).StatusCode);
        var cheque=new ChequeRequest{IdempotencyKey="ceo-cheque",CompanyId=1,ServiceMonths=["2026-11"],ClaimIds=[claimId],ChequeNumber="CEO-001",BankName="Bank",Amount=30,ReceivedOn=new(2026,11,6)};
        var review=await client.PostAsJsonAsync("/api/Finance/insurance/cheques/preview",cheque);Assert.Equal(HttpStatusCode.OK,review.StatusCode);cheque.ExpectedAllocationHash=(await review.Content.ReadFromJsonAsync<ChequePreview>())!.Hash;
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/Finance/insurance/cheques",cheque)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/Finance/insurance/cheques",cheque)).StatusCode);
        var summary=await Read<BillingPatient>(client,"/api/Billing/patients?companyIds=1&claimStatus=Partial");Assert.Equal(80,summary.Totals.ClaimsSubmitted);Assert.Equal(30,summary.Totals.InsurerReceipts);Assert.Equal(50,summary.Totals.InsurerEarnedOutstanding);Assert.Equal(20,summary.Totals.PatientReceipts);
        await ReceptionAccessFixture.LoginAsync(client);Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync($"/api/Finance/insurance/claims/{claimId}/settlements",receipt)).StatusCode);
        var reception=await Read<BillingPatient>(client,"/api/Billing/patients?dateBasis=Collection&from=2026-11-06&to=2026-11-06");Assert.Equal(70,reception.Totals.CollectedInPeriod);
    }
    [Fact] public async Task One_patient_preserves_multiple_historical_companies_and_pricing_agreements()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await GroupedBillingFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        using(var scope=host.Services.CreateScope()) { var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var plan=new TreatmentPlan{Assessment=new(){PatientId=1,SeniorId="access-senior",Diagnosis="Another course",ConditionType="Physio",Priority="Routine",CompletedAt=DateTime.UtcNow},DoctorId="access-doctor",NumberOfSessions=1,TreatmentType="Physio"};db.TreatmentPlans.Add(plan);await db.SaveChangesAsync();
            var agreement=new TreatmentCourseAgreement{PatientId=1,TreatmentPlanId=plan.Id,FundingType="Insurance",PricingMode="PerSession",InsuranceCompanyId=2,CoveredSessions=1,TotalAmount=50,PatientAmount=10,InsuranceAmount=40};db.TreatmentCourseAgreements.Add(agreement);await db.SaveChangesAsync();
            db.Invoices.Add(new(){PatientId=1,InvoiceNumber="OTHER-COMPANY",CourseAgreementId=agreement.Id,TotalAmount=50,PatientAmount=10,InsuranceAmount=40});await db.SaveChangesAsync();}
        var result=await Read<BillingPatient>(client,"/api/Billing/patients?companyIds=1&companyIds=2");var patient=result.Items.Single(p=>p.PatientId==1);Assert.Equal(13,patient.InvoiceCount);Assert.Equal(2,patient.Companies.Length);Assert.Equal(1250,patient.Amounts.Agreed);
        var detail=await Read<BillingInvoice>(client,"/api/Billing/patients/1/invoices?companyIds=2&pricingMode=PerSession");Assert.Single(detail.Items);Assert.Equal("OTHER-COMPANY",detail.Items[0].Reference);Assert.Equal(50,detail.Totals.Agreed);
    }
    [Fact] public async Task Concurrent_invoice_collections_cannot_spend_the_same_balance_twice()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await GroupedBillingFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        var invoices=await client.GetFromJsonAsync<List<InvoiceResponse>>("/api/Billing/invoices",Json);var id=invoices!.Single(i=>i.InvoiceNumber=="SELF-3").Id;
        var results=await Task.WhenAll(Enumerable.Range(1,2).Select(n=>client.PostAsJsonAsync("/api/Billing/payments",new CreatePaymentRequest{InvoiceId=id,IdempotencyKey=$"race-{n}",Amount=60,ExpectedRemaining=100,Method=PaymentMethod.Cash})));
        Assert.Single(results,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(results,r=>r.StatusCode==HttpStatusCode.Conflict);
        var summary=await Read<BillingPatient>(client,"/api/Billing/patients");Assert.Equal(370,summary.Totals.PatientReceipts);Assert.Equal(1080,summary.Totals.PatientRemaining);
    }
    [Fact] public async Task Numeric_invoice_retry_keys_remain_distinct_receipts()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await GroupedBillingFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        var invoices=await client.GetFromJsonAsync<List<InvoiceResponse>>("/api/Billing/invoices",Json);var id=invoices!.Single(i=>i.InvoiceNumber=="SELF-4").Id;
        foreach(var key in new[]{"1","2"}) Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/Billing/payments",new CreatePaymentRequest{InvoiceId=id,IdempotencyKey=key,Amount=1,Method=PaymentMethod.Cash})).StatusCode);
        var summary=await Read<BillingPatient>(client,"/api/Billing/patients");Assert.Equal(312,summary.Totals.PatientReceipts);Assert.Equal(15,summary.Totals.ReceiptCount);
    }
}
