using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
namespace PTCenter.Infrastructure.Services;

public static class ClinicianAbsenceRules
{
    public static (DateTime Start, DateTime End) Interval(DoctorAbsence a)
        => (a.StartsAt ?? ClinicCalendar.DayStart(a.AbsentDate), a.EndsAt ?? ClinicCalendar.DayStart(a.AbsentDate.AddDays(1)));
    public static bool Overlaps(DoctorAbsence a, DateTime start, DateTime end)
    { var range = Interval(a); return !a.Withdrawn && !a.IsDeleted && range.Start < end && range.End > start; }
    public static bool Unstarted(Appointment a) => a.Status is AppointmentStatus.Pending or AppointmentStatus.InProgress
        && (a.AppointmentType == "Session" || a.ActualCheckIn == null)
        && a.TreatmentStartedAt == null && a.TreatmentEndedAt == null && a.ActualCheckOut == null
        && (a.TreatmentPlanSession == null || a.TreatmentPlanSession.Status is not (SessionStatus.Completed or SessionStatus.InProgress) && a.TreatmentPlanSession.CheckOut == null);
}
