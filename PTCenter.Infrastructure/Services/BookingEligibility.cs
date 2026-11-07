using Microsoft.EntityFrameworkCore;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Infrastructure.Services;

public partial class BookingService
{
    private IQueryable<Patient> BookablePatients() => db.Patients.Where(patient =>
        !db.Assessments.Any(a=>a.PatientId==patient.Id && a.CompletedAt!=null) ||
        db.TreatmentPlans.Any(plan=>plan.Assessment.PatientId==patient.Id && plan.Assessment.CompletedAt!=null && plan.Status==StatusPlan.Active &&
            plan.NumberOfSessions > db.TreatmentPlanSessions.Count(s=>s.TreatmentPlanId==plan.Id &&
                (s.Status==SessionStatus.Completed || db.Appointments.Any(a=>a.TreatmentPlanSessionId==s.Id &&
                    (a.ActualCheckIn!=null || a.Status!=AppointmentStatus.Cancelled)))) &&
            (!db.TreatmentCourseAgreements.Any(c=>c.TreatmentPlanId==plan.Id) || db.TreatmentPlanSessions.Count(s=>s.TreatmentPlanId==plan.Id &&
                (s.Status==SessionStatus.Completed || db.Appointments.Any(a=>a.TreatmentPlanSessionId==s.Id &&
                    (a.ActualCheckIn!=null || a.Status!=AppointmentStatus.Cancelled)))) <
                db.TreatmentCourseAgreements.Where(c=>c.TreatmentPlanId==plan.Id).Select(c=>c.CoveredSessions).FirstOrDefault())))
        .Where(patient=>patient.PaymentType!="Insurance" || !db.Assessments.Any(a=>a.PatientId==patient.Id && a.CompletedAt!=null) ||
            patient.InsuranceDetails!=null && patient.InsuranceDetails.ApprovedSessions > db.Appointments.Count(a=>a.PatientId==patient.Id && a.AppointmentType=="Session" &&
                (a.InsuranceFunded || a.BookingBatchId==null) && (a.ActualCheckIn!=null || a.Status!=AppointmentStatus.Cancelled)));

    private async Task<Dictionary<DateOnly,int>> WeeklyCountsAsync(int planId,int? exclude)
    {
        var starts=await db.Appointments.Where(a=>a.Id!=exclude && a.TreatmentPlanSession!=null && a.TreatmentPlanSession.TreatmentPlanId==planId &&
            a.Status!=AppointmentStatus.Cancelled && a.Status!=AppointmentStatus.NoShow).Select(a=>a.StartTime).ToListAsync();
        return starts.GroupBy(s=>ClinicCalendar.Week(new DateTimeOffset(DateTime.SpecifyKind(s,DateTimeKind.Utc)))).ToDictionary(g=>g.Key,g=>g.Count());
    }
    // Recovery and ordinary rescheduling must use the same prescription limit as new bookings.
    private sealed record WeeklyBookingLimit(int? Frequency, Dictionary<DateOnly,int> Counts)
    {
        public string? Conflict(DateTime start) => Frequency is not (>=1 and <=7) ? "FrequencyRequired" :
            Counts.GetValueOrDefault(ClinicCalendar.Week(new DateTimeOffset(DateTime.SpecifyKind(start,DateTimeKind.Utc)))) >= Frequency ? "WeeklyFrequencyLimit" : null;
    }
    private async Task<WeeklyBookingLimit?> WeeklyLimitAsync(Appointment appointment)
    {
        if(appointment.AppointmentType!="Session" || appointment.TreatmentPlanSession==null)return null;
        var planId=appointment.TreatmentPlanSession.TreatmentPlanId;
        var frequency=await db.TreatmentPlans.Where(p=>p.Id==planId).Select(p=>(int?)p.FrequencyPerWeek).SingleOrDefaultAsync();
        return new(frequency,await WeeklyCountsAsync(planId,appointment.Id));
    }
    private async Task<List<PTCenter.Application.Contracts.Bookings.BookedAppointment>> CompleteRowsAsync(List<Appointment> appointments)
    {
        var ids=appointments.Select(a=>a.Id).ToArray();
        var rows=await db.Appointments.AsNoTracking().Include(a=>a.Doctor).Include(a=>a.Room).Include(a=>a.TreatmentPlanSession).Where(a=>ids.Contains(a.Id)).ToListAsync();
        var clinicianIds=rows.Select(a=>a.DoctorId??a.SeniorId).Where(id=>id!=null).Distinct().ToList();
        var names=await db.Users.AsNoTracking().Where(u=>clinicianIds.Contains(u.Id)).ToDictionaryAsync(u=>u.Id,u=>u.FullName);
        var invoices=await db.Invoices.AsNoTracking().Where(i=>i.AppointmentId!=null && ids.Contains(i.AppointmentId.Value)).ToListAsync();
        return appointments.Select(original=>{
            var a=rows.Single(r=>r.Id==original.Id);var bills=invoices.Where(i=>i.AppointmentId==a.Id).ToList();
            return new PTCenter.Application.Contracts.Bookings.BookedAppointment(a.Id,a.TreatmentPlanSessionId,
                new DateTimeOffset(DateTime.SpecifyKind(a.StartTime,DateTimeKind.Utc)),new DateTimeOffset(DateTime.SpecifyKind(a.EndTime,DateTimeKind.Utc))) {
                ClinicianId=a.DoctorId??a.SeniorId,ClinicianName=names.GetValueOrDefault(a.DoctorId??a.SeniorId??""),RoomId=a.RoomId,RoomName=a.Room?.Name,
                TreatmentPlanId=a.TreatmentPlanSession?.TreatmentPlanId,SessionNumber=a.TreatmentPlanSession?.SessionNumber,
                CourseAgreementId=bills.Select(i=>i.CourseAgreementId).FirstOrDefault(),InvoiceIds=bills.Select(i=>i.Id).ToList()
            };
        }).ToList();
    }
}
