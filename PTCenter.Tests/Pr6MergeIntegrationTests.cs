using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Application.Contracts.Patients;
using PTCenter.Domain.Entities;
using PTCenter.Infrastructure.Persistence;
using PTCenter.Infrastructure.Services;
using Xunit;

namespace PTCenter.Tests;

public sealed partial class BookingDatabaseTests
{
    private async Task<(BookingReceipt Booking, RecoveryOptions Options)> MergeRecoveryBooking()
    {
        var plan = await PlanAsync(6);
        var booking = await service.CreateAsync(Treatment(plan, 3));
        var third = booking.Appointments[2];
        db.DoctorAbsences.Add(new DoctorAbsence { DoctorId = "doctor", AbsentDate = new(2030, 2, 11) });
        await db.SaveChangesAsync();
        return (booking, await service.RecoveryOptionsAsync(third.Id, new(2030, 2, 6)));
    }

    [Fact]
    public async Task Merge_absence_recovery_does_not_offer_a_move_into_a_full_prescribed_week()
    {
        var (_, options) = await MergeRecoveryBooking();
        Assert.NotEmpty(options.Matching); // Keep-time replacement in the original week remains possible.
        Assert.DoesNotContain(options.NearestMatching, candidate =>
            ClinicCalendar.Week(new DateTimeOffset(candidate.Start, TimeSpan.Zero)) == ClinicCalendar.Week(start));
    }

    [Fact]
    public async Task Merge_absence_and_retained_Senior_reschedules_enforce_weekly_limit_without_financial_mutation()
    {
        var (booking, options) = await MergeRecoveryBooking();
        var candidate = options.Matching.First(c => c.ClinicianId == "senior");
        var request = new RecoveryRequest("merge-recovery", options.Appointment.Version, options.Appointment.AbsenceIds[0],
            candidate.ClinicianId, start.AddDays(2), candidate.RoomId, "Reschedule", "Move to another clinic day");
        var invoices = await db.Invoices.Select(i => new { i.Id, i.TotalAmount, i.PatientAmount, i.InsuranceAmount }).ToListAsync();
        var error = await Assert.ThrowsAsync<BookingRuleException>(() => service.ResolveAbsenceAsync(options.Appointment.AppointmentId, request, "senior"));
        Assert.Equal("Booking.RecoveryConflict", error.Code);
        Assert.Contains("WeeklyFrequencyLimit", error.Message);
        Assert.Equal(invoices, await db.Invoices.Select(i => new { i.Id, i.TotalAmount, i.PatientAmount, i.InsuranceAmount }).ToListAsync());
        Assert.Empty(await db.AppointmentScheduleChanges.ToListAsync());
        var replacement = request with { OperationKey = "merge-keep-time", StartTime = booking.Appointments[2].StartTime, Action = "Replace" };
        await service.ResolveAbsenceAsync(options.Appointment.AppointmentId, replacement, "senior");
        var retained = await Assert.ThrowsAsync<BookingRuleException>(() => service.RescheduleAsync(options.Appointment.AppointmentId,
            new RescheduleBookingRequest { ClinicianId = "senior", RoomId = candidate.RoomId, StartTime = start.AddDays(2) }));
        Assert.Contains("WeeklyFrequencyLimit", retained.Message);
    }

    [Fact]
    public async Task Merge_enriched_booking_receipt_identifies_covering_Senior_and_retains_original_invoice_links()
    {
        var (booking, options) = await MergeRecoveryBooking();
        var candidate = options.Matching.First(c => c.ClinicianId == "senior");
        await service.ResolveAbsenceAsync(options.Appointment.AppointmentId, new("merge-cover-receipt", options.Appointment.Version,
            options.Appointment.AbsenceIds[0], candidate.ClinicianId, new DateTimeOffset(candidate.Start, TimeSpan.Zero), candidate.RoomId,
            "Replace", "Keep the existing treatment under Senior coverage"), "senior");
        var receipt = await service.ReceiptAsync(booking.IdempotencyKey);
        var row = receipt.Appointments.Single(a => a.Id == options.Appointment.AppointmentId);
        Assert.Equal("senior", row.ClinicianId);
        Assert.Equal("Senior", row.ClinicianName);
        Assert.Equal(candidate.RoomId, row.RoomId);
        Assert.Single(row.InvoiceIds);
        Assert.Equal(booking.InvoiceIds, receipt.InvoiceIds);
        Assert.Equal(booking.TotalAmount, receipt.TotalAmount);
        Assert.Equal(booking.PatientAmount, receipt.PatientAmount);
    }
}

public class Pr6MergeHttpTests
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web){Converters={new JsonStringEnumConverter()}};
    [Fact]
    public async Task Merge_legacy_username_is_ignored_and_registration_replay_retains_unverified_real_email()
    {
        using var host = new BookingTestHost();
        using var client = host.CreateClient(new() { BaseAddress = new("https://localhost") });
        await ReceptionAccessFixture.SeedAsync(host);
        await ReceptionAccessFixture.LoginAsync(client);
        var request = new { userName = "access.doctor", fullName = "Merged registration", phoneNumber = "01012345678",
            gender = 0, birthDate = "1990-01-01", email = "merge.patient@example.invalid", paymentType = "Cash", idempotencyKey = "merge-registration" };
        var first = await client.PostAsJsonAsync("/api/Patients/Add", request);
        var retry = await client.PostAsJsonAsync("/api/Patients/Add", request with { userName = "ignored-other-legacy-name" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var patient = (await first.Content.ReadFromJsonAsync<PatientResponse>(Json))!;
        Assert.Equal(patient.Id, (await retry.Content.ReadFromJsonAsync<PatientResponse>(Json))!.Id);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == patient.ApplicationUserId);
        Assert.Matches("^patient\\.[a-f0-9]{32}$", user.UserName!);
        Assert.Equal(request.email, user.Email);
        Assert.False(user.EmailConfirmed);
        Assert.Single(await db.Patients.Where(p => p.RegistrationOperationKey == request.idempotencyKey).ToListAsync());
        Assert.Equal("access.doctor", (await db.Users.SingleAsync(u => u.UserName == "access.doctor")).UserName);
    }

    [Fact]
    public async Task Merge_legacy_insurer_settlement_can_recover_a_committed_receipt_but_cannot_create_new_cash()
    {
        using var host = new BookingTestHost();
        using var client = host.CreateClient(new() { BaseAddress = new("https://localhost") });
        await GroupedBillingFixture.SeedAsync(host);
        await ReceptionAccessFixture.LoginAsync(client, "Ceo");
        int claimId, paymentId;
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var invoice = await db.Invoices.SingleAsync(i => i.InvoiceNumber == "INS-1");
            var finance = new CourseFinanceService(db);
            var claim = await finance.SubmitClaimAsync(new("merge-old-claim", invoice.Id, "OLD-CLAIM"), "access-ceo");
            claimId = claim.Id;
            // Represents a committed historical operation made before cheque entry existed.
            paymentId = await finance.SettleClaimAsync(claimId, new("merge-old-receipt", 30,
                PTCenter.Domain.Entities.Enums.PaymentMethod.Cash, "OLD-RECEIPT"));
        }
        var path = $"/api/Finance/insurance/claims/{claimId}/settlements";
        var request = new SettleInsuranceClaimRequest("merge-old-receipt", 30,
            PTCenter.Domain.Entities.Enums.PaymentMethod.Cash, "OLD-RECEIPT");
        var recovered = await client.PostAsJsonAsync(path, request);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        Assert.Equal(paymentId, (await recovered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("paymentId").GetInt32());
        var fresh = await client.PostAsJsonAsync(path, request with { IdempotencyKey = "merge-new-cash" });
        Assert.Equal(HttpStatusCode.Conflict, fresh.StatusCode);
        Assert.Equal("Insurance.ChequeRequired", (await fresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path, request with { Amount = 31 })).StatusCode);
        await ReceptionAccessFixture.LoginAsync(client);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(path, request)).StatusCode);
        using var verify = host.Services.CreateScope();
        var verificationDb = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await verificationDb.Payments.Where(p => p.IdempotencyKey == "insurer:merge-old-receipt").ToListAsync());
        Assert.False(await verificationDb.Payments.AnyAsync(p => p.IdempotencyKey == "insurer:merge-new-cash"));
    }
}
