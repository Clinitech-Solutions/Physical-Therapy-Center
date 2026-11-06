using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Contracts.Appointments;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Infrastructure.Services;

public partial class BookingService
{
    public async Task<List<AppointmentResponse>> AssessmentTasksAsync(string actor, bool supervisor)
    {
        var today = ClinicCalendar.UtcDay(DateTimeOffset.UtcNow);
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
        return new(id,appointment.PatientId,appointment.StartTime,appointment.ActualCheckIn,appointment.ActualCheckOut,assessment?.CompletedAt,
            assessment?.Diagnosis ?? "",assessment?.Notes,assessment?.TreatmentPlan?.NumberOfSessions ?? 1,assessment?.TreatmentPlan?.FrequencyPerWeek ?? 2,
            assessment?.TreatmentPlan?.DoctorId,assessment?.TreatmentPlan?.Exercises,assessment?.ConditionType ?? "PhysicalTherapy");
    }

    private async Task MarkAssessmentDeliveredAsync(int appointmentId, DateTime signedAt)
    {
        foreach(var invoice in await db.Invoices.Where(i=>i.AppointmentId==appointmentId).ToListAsync()) invoice.DeliveredAt=signedAt;
    }
}
