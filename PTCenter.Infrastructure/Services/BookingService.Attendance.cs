using System.Data;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Infrastructure.Services;

public partial class BookingService
{
    public async Task CloseUnattendedAsync(int id, bool noShow, UnattendedAppointmentRequest request, string actor)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 100 ||
            string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
            throw Rule("AttendanceReason", "Enter a reason of up to 500 characters and a stable operation key.");
        var action = noShow ? "NoShow" : "Cancel";
        var fingerprint = Hash(new { id, action, reason = request.Reason.Trim(), actor });
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ClinicMutationLock.AcquireAsync(db);
        var previous = await db.AppointmentScheduleChanges.SingleOrDefaultAsync(c => c.OperationKey == request.IdempotencyKey);
        if (previous != null)
        {
            if (previous.RequestHash != fingerprint || previous.ActorId != actor)
                throw Rule("RetryKeyConflict", "This operation key belongs to a different appointment action.", 409);
            return;
        }
        var appointment = await db.Appointments.Include(a => a.Room).Include(a => a.TreatmentPlanSession).SingleOrDefaultAsync(a => a.Id == id)
            ?? throw Rule("AppointmentNotFound", "Appointment was not found.", 404);
        if (appointment.Status != AppointmentStatus.Pending || appointment.ActualCheckIn != null || appointment.ActualCheckOut != null ||
            appointment.TreatmentStartedAt != null || appointment.TreatmentEndedAt != null)
            throw Rule("AttendanceLocked", "Only an unarrived, unstarted pending appointment can be closed through this action.", 409);
        if (noShow && appointment.EndTime >= CurrentUtc)
            throw Rule("NoShowTooEarly", "No-show can be recorded only after the scheduled appointment end.", 409);
        if (!noShow && appointment.StartTime <= CurrentUtc)
            throw Rule("CancellationWindow", "Only an upcoming unarrived appointment can be cancelled.", 409);
        var actorName = await db.Users.Where(u => u.Id == actor).Select(u => u.FullName).SingleOrDefaultAsync()
            ?? throw Rule("StaffRequired", "An authenticated employee is required.", 403);
        var clinician = appointment.DoctorId ?? appointment.SeniorId ?? "";
        var clinicianName = await db.Users.Where(u => u.Id == clinician).Select(u => u.FullName).SingleOrDefaultAsync();
        db.AppointmentScheduleChanges.Add(new AppointmentScheduleChange {
            AppointmentId = id, PatientId = appointment.PatientId, TreatmentPlanId = appointment.TreatmentPlanSession?.TreatmentPlanId,
            OperationKey = request.IdempotencyKey, RequestHash = fingerprint, ActorId = actor, ActorName = actorName, ChangedAt = CurrentUtc,
            Action = action, Reason = request.Reason.Trim(), PreviousStart = appointment.StartTime, PreviousEnd = appointment.EndTime,
            NewStart = appointment.StartTime, NewEnd = appointment.EndTime, PreviousRoomId = appointment.RoomId, NewRoomId = appointment.RoomId ?? 0,
            PreviousRoomName = appointment.Room?.Name ?? "", NewRoomName = appointment.Room?.Name ?? "", PreviousClinicianId = clinician,
            ClinicianId = clinician, PreviousClinicianName = clinicianName, ClinicianName = clinicianName
        });
        appointment.Status = noShow ? AppointmentStatus.NoShow : AppointmentStatus.Cancelled;
        // NoShow retains its course/approval allocation. Money and clinical history are unchanged.
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }
}
