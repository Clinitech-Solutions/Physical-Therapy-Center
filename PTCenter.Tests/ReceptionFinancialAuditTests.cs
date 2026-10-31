using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Contracts.Receptionist;
using PTCenter.Application.Contracts.Billing;
using PTCenter.Application.Services;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Services;
using Xunit;

namespace PTCenter.Tests;

public sealed partial class BookingDatabaseTests
{
    private sealed class AuditClock(DateTimeOffset instant) : TimeProvider { public override DateTimeOffset GetUtcNow()=>instant; }

    [Theory]
    [InlineData("2026-10-31T12:00:00Z","2026-10-30T22:00:00Z","2026-10-31T22:00:00Z")]
    [InlineData("2026-04-24T12:00:00Z","2026-04-23T22:00:00Z","2026-04-24T21:00:00Z")]
    [InlineData("2026-10-29T12:00:00Z","2026-10-28T21:00:00Z","2026-10-29T22:00:00Z")]
    public void Daily_reporting_uses_Cairo_boundaries_including_23_and_25_hour_days(string instant,string start,string end)
    {
        var range=ClinicCalendar.UtcDay(DateTimeOffset.Parse(instant));
        Assert.Equal(DateTimeOffset.Parse(start).UtcDateTime,range.Start);Assert.Equal(DateTimeOffset.Parse(end).UtcDateTime,range.End);
    }

    [Fact] public async Task Dashboard_and_today_sessions_use_Cairo_day_and_actual_patient_and_insurer_receipts()
    {
        var clock=new AuditClock(new(2026,10,31,21,50,0,TimeSpan.Zero));
        var begins=new DateTime(2026,10,30,22,0,0,DateTimeKind.Utc);var ends=begins.AddDays(1);
        foreach(var at in new[]{begins.AddMinutes(-1),begins,ends.AddMinutes(-1),ends})
            db.Appointments.Add(new(){PatientId=patientId,DoctorId="doctor",RoomId=1,AppointmentType="Session",StartTime=at,EndTime=at.AddHours(1),Status=AppointmentStatus.Pending});
        var invoice=new Invoice{PatientId=patientId,InvoiceNumber="AUDIT-DAY",TotalAmount=200,PatientAmount=100,InsuranceAmount=100,Status=InvoiceStatus.Pending};db.Invoices.Add(invoice);await db.SaveChangesAsync();
        invoice.CreatedAt=begins;
        invoice.Payments.Add(new(){Amount=15,PaymentMethod=PaymentMethod.Cash,Status=PaymentStatus.Paid,PaymentDate=begins});
        invoice.Payments.Add(new(){Amount=25,PaymentMethod=PaymentMethod.Online,PayerType="Insurer",Status=PaymentStatus.Paid,PaymentDate=ends.AddMinutes(-1)});
        invoice.Payments.Add(new(){Amount=9,PaymentMethod=PaymentMethod.Insurance,Status=PaymentStatus.Paid,PaymentDate=ends});
        invoice.Payments.Add(new(){Amount=8,PaymentMethod=PaymentMethod.Cash,Status=PaymentStatus.Paid,PaymentDate=begins.AddMinutes(-1)});
        invoice.Payments.Add(new(){Amount=100,PaymentMethod=PaymentMethod.Cash,Status=PaymentStatus.Pending,PaymentDate=begins});await db.SaveChangesAsync();
        var reporting=new ReceptionistService(db,service,clock);var stats=(await reporting.GetDashboardStatsAsync()).Value;
        Assert.Equal(2,stats.TodaysSessions);Assert.Equal(200,stats.TotalExpectedToday);Assert.Equal(40,stats.TotalCollectedToday);
        Assert.Equal(15,stats.PatientCollectedToday);Assert.Equal(25,stats.InsurerCollectedToday);
        var sessions=(await reporting.GetTodaySessionsAsync(new TodaySessionsQueryRequest{PageNumber=1,PageSize=1000})).Value;
        Assert.Equal(2,sessions.TotalCount);Assert.Equal(100,sessions.PageSize);Assert.All(sessions.Items,row=>Assert.InRange(row.StartTime,begins,ends.AddTicks(-1)));
    }

    [Theory]
    [InlineData("Insurer")]
    [InlineData("Insurance")]
    public async Task Explicit_insurer_receipts_never_pay_a_patients_balance_or_check_in_gate(string payer)
    {
        var request=Treatment(await PlanAsync(2),1);request.PricingMode="Package";
        var receipt=await service.CreateAsync(request);var appointment=receipt.Appointments.Single();
        var invoice=await db.Invoices.Include(i=>i.Payments).SingleAsync(i=>i.AppointmentId==appointment.Id);
        invoice.Payments.Add(new(){Amount=50,PaymentMethod=PaymentMethod.Online,PayerType=payer,Status=PaymentStatus.Paid,PaymentDate=DateTime.UtcNow});await db.SaveChangesAsync();
        var profile=(await service.ProfileAsync(patientId)).Courses.Single();Assert.Equal(0,profile.PatientPaid);Assert.Equal(50,profile.InsurerPaid);
        var bill=(await new BillingService(db).GetInvoiceByIdAsync(invoice.Id)).Value;Assert.Equal(0,bill.PaidAmount);Assert.Equal(100,bill.RemainingAmount);
        var gate=await new CourseFinanceService(db).GateAsync(appointment.Id);Assert.False(gate.CanStart);Assert.Equal(100,gate.AdditionalPaymentRequired);
    }

    [Fact] public void Cent_allocation_conserves_totals_and_nonnegative_patient_insurer_shares()
    {
        foreach(var count in new[]{1,2,3,7,10,37,200})foreach(var cents in new[]{0,1,2,99,100,101,9999,1234567})
        {
            var total=cents/100m;var patient=decimal.Round(total*0.175m,2,MidpointRounding.AwayFromZero);
            var rows=Enumerable.Range(1,count).Select(n=>(Total:CourseFinanceService.Allocate(total,count,n),Patient:CourseFinanceService.Allocate(patient,count,n))).ToList();
            Assert.Equal(total,rows.Sum(r=>r.Total));Assert.Equal(patient,rows.Sum(r=>r.Patient));Assert.All(rows,r=>Assert.InRange(r.Patient,0,r.Total));
            Assert.Equal(total-patient,rows.Sum(r=>r.Total-r.Patient));
        }
    }

    [Fact] public async Task Billing_rejects_changed_retry_details_without_another_receipt()
    {
        var request=Treatment(await PlanAsync(1),1);var receipt=await service.CreateAsync(request);var billing=new BillingService(db);
        var payment=new CreatePaymentRequest{InvoiceId=receipt.InvoiceIds.Single(),Amount=30.01m,Method=PaymentMethod.Online,IdempotencyKey="audit-receipt"};
        var first=(await billing.AddPaymentAsync(payment)).Value;Assert.Equal(first.Id,(await billing.AddPaymentAsync(payment)).Value.Id);
        payment.Amount=30.02m;var changed=await billing.AddPaymentAsync(payment);Assert.True(changed.IsFailure);Assert.Equal(409,changed.Error.StatusCode);
        Assert.Equal(30.01m,await db.Payments.SumAsync(p=>p.Amount));Assert.Single(await db.Payments.ToListAsync());
    }
}
