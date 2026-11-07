using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;
using PTCenter.Infrastructure.Services;
using Xunit;

namespace PTCenter.Tests;

public class AttendancePolicyTests
{
    private sealed class Clock(DateTimeOffset now) : TimeProvider { public DateTimeOffset Now = now; public override DateTimeOffset GetUtcNow() => Now; }

    [Fact]
    public async Task Arrival_uses_Cairo_calendar_date_allows_same_day_early_arrival_and_rejects_other_days()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host);
        var id = await DoctorWorkspaceFixture.BookAsync(host);
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var appointment = await db.Appointments.AsNoTracking().SingleAsync(a => a.Id == id);
        var clock = new Clock(new DateTimeOffset(appointment.StartTime, TimeSpan.Zero).AddDays(-1));
        var service = new BookingService(db, clock);
        foreach (var offset in new[] { -1, 1 })
        {
            clock.Now = new DateTimeOffset(appointment.StartTime, TimeSpan.Zero).AddDays(offset);
            var error = await Assert.ThrowsAsync<BookingRuleException>(() => service.TransitionAsync(id, AppointmentStatus.InProgress));
            Assert.Equal("Booking.ArrivalDate", error.Code);
        }
        clock.Now = new DateTimeOffset(appointment.StartTime, TimeSpan.Zero).AddHours(-2);
        await service.TransitionAsync(id, AppointmentStatus.InProgress);
        var first = (await db.Appointments.AsNoTracking().SingleAsync(a => a.Id == id)).ActualCheckIn;
        Assert.Equal(clock.Now.UtcDateTime, first);
        Assert.Null((await db.Appointments.AsNoTracking().SingleAsync(a => a.Id == id)).TreatmentStartedAt);
        clock.Now = clock.Now.AddMinutes(2); await service.TransitionAsync(id, AppointmentStatus.InProgress);
        Assert.Equal(first, (await db.Appointments.AsNoTracking().SingleAsync(a => a.Id == id)).ActualCheckIn);
    }

    [Fact]
    public async Task No_show_waits_for_end_is_audited_once_and_retains_money_and_course_allocation()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host);
        var id = await DoctorWorkspaceFixture.BookAsync(host);
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var appointment = await db.Appointments.AsNoTracking().SingleAsync(a => a.Id == id);
        var clock = new Clock(new DateTimeOffset(appointment.EndTime, TimeSpan.Zero).AddTicks(-1)); var service = new BookingService(db, clock);
        var actor = await db.Users.Where(u => u.UserName == "reception.access.local").Select(u => u.Id).SingleAsync();
        var request = new UnattendedAppointmentRequest(Guid.NewGuid().ToString(), "Patient did not attend; phone unanswered");
        var before = await service.ContextAsync(appointment.PatientId);
        var planId=before.Plans.Single().Id;
        var plan=await db.TreatmentPlans.SingleAsync(p=>p.Id==planId);
        plan.NumberOfSessions=1; await db.SaveChangesAsync();
        before=await service.ContextAsync(appointment.PatientId);
        Assert.DoesNotContain((await service.DirectoryAsync()).Items,p=>p.Id==appointment.PatientId);
        var money = await db.Payments.SumAsync(p => p.Amount); var invoiceCount = await db.Invoices.CountAsync();
        Assert.Equal("Booking.NoShowTooEarly", (await Assert.ThrowsAsync<BookingRuleException>(() => service.CloseUnattendedAsync(id, true, request, actor))).Code);
        clock.Now = new DateTimeOffset(appointment.EndTime, TimeSpan.Zero);
        Assert.Equal("Booking.NoShowTooEarly", (await Assert.ThrowsAsync<BookingRuleException>(() => service.CloseUnattendedAsync(id, true, request, actor))).Code);
        clock.Now = clock.Now.AddMilliseconds(1);
        await service.CloseUnattendedAsync(id, true, request, actor);
        clock.Now = clock.Now.AddMinutes(1); await service.CloseUnattendedAsync(id, true, request, actor);
        var after = await service.ContextAsync(appointment.PatientId);
        Assert.Equal(before.RemainingApprovedSessions, after.RemainingApprovedSessions);
        Assert.Equal(before.Plans.Single().RemainingSessions, after.Plans.Single().RemainingSessions);
        Assert.True(after.Plans.Single().StaffReviewRequired);
        Assert.DoesNotContain((await service.DirectoryAsync()).Items,p=>p.Id==appointment.PatientId);
        Assert.Equal(0, after.Plans.Single().CompletedSessions);
        Assert.Equal(money, await db.Payments.SumAsync(p => p.Amount)); Assert.Equal(invoiceCount, await db.Invoices.CountAsync());
        var saved = await db.Appointments.AsNoTracking().SingleAsync(a => a.Id == id);
        Assert.Equal(AppointmentStatus.NoShow, saved.Status); Assert.Null(saved.ActualCheckIn); Assert.Null(saved.TreatmentStartedAt);
        var audit = Assert.Single(await db.AppointmentScheduleChanges.Where(a => a.AppointmentId == id).ToListAsync());
        Assert.Equal("NoShow", audit.Action); Assert.Equal(actor, audit.ActorId); Assert.Equal(request.Reason, audit.Reason);
        Assert.Equal(appointment.EndTime.AddMilliseconds(1), audit.ChangedAt);
    }

    [Fact]
    public async Task Cancellation_requires_reason_and_employee_and_cannot_bypass_protected_status_transitions()
    {
        using var host = new BookingTestHost(); await DoctorWorkspaceFixture.SeedAsync(host);
        var id = await DoctorWorkspaceFixture.BookAsync(host);
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var a = await db.Appointments.AsNoTracking().SingleAsync(a => a.Id == id);
        var clock = new Clock(new DateTimeOffset(a.StartTime, TimeSpan.Zero).AddHours(-1)); var service = new BookingService(db, clock);
        var actor = await db.Users.Where(u => u.UserName == "reception.access.local").Select(u => u.Id).SingleAsync();
        await Assert.ThrowsAsync<BookingRuleException>(() => service.CloseUnattendedAsync(id, false, new(Guid.NewGuid().ToString(), " "), actor));
        Assert.Equal("Booking.ReasonedActionRequired", (await Assert.ThrowsAsync<BookingRuleException>(() => service.TransitionAsync(id, AppointmentStatus.Cancelled))).Code);
        await service.CloseUnattendedAsync(id, false, new(Guid.NewGuid().ToString(), "Patient requested cancellation"), actor);
        Assert.Equal(AppointmentStatus.Cancelled, (await db.Appointments.AsNoTracking().SingleAsync(a => a.Id == id)).Status);
        Assert.Equal("Cancel", Assert.Single(await db.AppointmentScheduleChanges.ToListAsync()).Action);
        Assert.NotEmpty(await db.Payments.ToListAsync());
    }
}
