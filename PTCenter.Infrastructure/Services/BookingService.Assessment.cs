using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Contracts.Appointments;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Domain.Entities.Enums;
using System.Data;
using System.Text.Json;

namespace PTCenter.Infrastructure.Services;

public partial class BookingService
{
    public async Task<List<AppointmentResponse>> AssessmentTasksAsync(string actor, bool supervisor)
    {
        var today = ClinicCalendar.UtcDay(CurrentInstant);
        // Due unsigned work survives departure and its planned clinic date. Future arrivals also remain actionable.
        return await db.Appointments.AsNoTracking().Where(a=>a.AppointmentType=="Assessment" &&
            (supervisor || a.SeniorId==actor) && a.Status!=AppointmentStatus.Cancelled && a.Status!=AppointmentStatus.NoShow &&
            (a.StartTime<today.End || a.ActualCheckIn!=null) && !db.Assessments.Any(s=>s.AppointmentId==a.Id && s.CompletedAt!=null))
            .OrderBy(a=>a.StartTime).ThenBy(a=>a.Id).Select(a=>new AppointmentResponse {
                Id=a.Id,PatientId=a.PatientId,PatientName=a.Patient.ApplicationUser.FullName,SeniorId=a.SeniorId,DoctorId=a.DoctorId,
                DoctorName=db.Users.Where(u=>u.Id==a.SeniorId).Select(u=>u.FullName).FirstOrDefault(),RoomId=a.RoomId,RoomName=a.Room==null?null:a.Room.Name,
                AppointmentType=a.AppointmentType,StartTime=a.StartTime,EndTime=a.EndTime,Status=a.Status,ActualCheckIn=a.ActualCheckIn,ActualCheckOut=a.ActualCheckOut,
                AssessmentCompleted=false,CreatedAt=a.CreatedAt
            }).ToListAsync();
    }

    public async Task<AssessmentWorkspace> AssessmentWorkspaceAsync(int id, string actor, bool supervisor)
    {
        var appointment = await db.Appointments.AsNoTracking().SingleOrDefaultAsync(a=>a.Id==id && a.AppointmentType=="Assessment")
            ?? throw Rule("AssessmentNotFound", "The assessment appointment was not found.", 404);
        if (!supervisor && appointment.SeniorId!=actor) throw Rule("AssessmentOwner", "Only the assigned Senior can open this assessment.",403);
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow) throw Rule("AssessmentUnavailable", "This assessment appointment is cancelled or marked NoShow.",409);
        var assessment = await db.Assessments.AsNoTracking().Include(a=>a.TreatmentPlan).SingleOrDefaultAsync(a=>a.AppointmentId==id);
        var draft = assessment?.CompletedAt == null && assessment?.DraftJson != null
            ? JsonSerializer.Deserialize<CompleteAssessmentRequest>(assessment.DraftJson) : null;
        return new(id,appointment.PatientId,appointment.StartTime,appointment.ActualCheckIn,appointment.ActualCheckOut,assessment?.CompletedAt,
            draft?.Diagnosis ?? assessment?.Diagnosis ?? "",assessment?.Notes,draft?.NumberOfSessions ?? assessment?.TreatmentPlan?.NumberOfSessions ?? 1,draft?.FrequencyPerWeek ?? assessment?.TreatmentPlan?.FrequencyPerWeek ?? 2,
            draft?.DoctorId ?? assessment?.TreatmentPlan?.DoctorId,draft?.Exercises ?? assessment?.TreatmentPlan?.Exercises,draft?.TreatmentType ?? assessment?.ConditionType ?? "PhysicalTherapy");
    }

    public async Task<AssessmentWorkspace> SaveAssessmentDraftAsync(CompleteAssessmentRequest request, string actor, bool supervisor)
    {
        if (request.Diagnosis?.Length > 4000 || request.Exercises?.Length > 10000 || request.TreatmentType?.Length > 100 ||
            request.NumberOfSessions is < 0 or > 200 || request.FrequencyPerWeek is < 0 or > 7)
            throw Rule("DraftInvalid", "Review the draft text, session count and weekly frequency.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ClinicMutationLock.AcquireAsync(db);
        var appointment = await db.Appointments.SingleOrDefaultAsync(a => a.Id == request.AppointmentId && a.AppointmentType == "Assessment")
            ?? throw Rule("AssessmentNotFound", "The assessment appointment was not found.", 404);
        if (!supervisor && appointment.SeniorId != actor) throw Rule("AssessmentOwner", "Only the assigned Senior can save this assessment.", 403);
        if (appointment.ActualCheckIn == null || appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
            throw Rule("AssessmentNotStarted", "Record arrival before saving clinical work.", 409);
        var assessment = await db.Assessments.SingleOrDefaultAsync(a => a.AppointmentId == appointment.Id);
        if (assessment?.CompletedAt != null) throw Rule("AssessmentAlreadyCompleted", "The signed assessment is read-only.", 409);
        if (assessment == null)
        {
            assessment = new Assessment { AppointmentId = appointment.Id, PatientId = appointment.PatientId, SeniorId = appointment.SeniorId!,
                Diagnosis = request.Diagnosis ?? "", ConditionType = request.TreatmentType ?? "PhysicalTherapy", Priority = "Routine", CreatedBy = actor };
            db.Assessments.Add(assessment);
        }
        assessment.Diagnosis = request.Diagnosis ?? "";
        assessment.DraftJson = JsonSerializer.Serialize(request);
        assessment.UpdatedBy = actor;
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        return await AssessmentWorkspaceAsync(appointment.Id, actor, supervisor);
    }

    private async Task MarkAssessmentDeliveredAsync(int appointmentId, DateTime signedAt)
    {
        foreach(var invoice in await db.Invoices.Where(i=>i.AppointmentId==appointmentId).ToListAsync()) invoice.DeliveredAt=signedAt;
    }
}
