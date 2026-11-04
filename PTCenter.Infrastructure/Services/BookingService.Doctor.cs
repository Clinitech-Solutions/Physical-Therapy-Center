using Microsoft.EntityFrameworkCore;
using PTCenter.Domain.Entities;
namespace PTCenter.Infrastructure.Services;

public partial class BookingService
{
    public async Task RequireEligibleDoctorAsync(int patientId, string doctorId)
    {
        if (!(await CliniciansForAsync(await PatientAsync(patientId), "Doctor")).Any(c => c.Id == doctorId))
            throw Rule("ClinicianEligibility", "Select an eligible Doctor matching the patient.", 409);
    }
    public async Task ValidateDoctorStartAsync(Appointment appointment, DateTime now)
    {
        var patient = await PatientAsync(appointment.PatientId);
        var clinician = appointment.DoctorId ?? appointment.SeniorId ?? "";
        var identity = await RecoveryClinician(clinician);
        var gender = await db.Users.Where(u=>u.Id==clinician).Select(u=>u.Gender).SingleAsync();
        if (gender != patient.ApplicationUser.Gender && !await HasGenderConsentAsync(appointment,clinician))
            throw Rule("ClinicianEligibility", "The assigned clinician requires saved phone consent for this booking.", 409);
        await RequireNotAbsentAsync(appointment);
        if (appointment.InsuranceFunded && !(await ContextAsync(appointment.PatientId)).InsuranceApproved)
            throw Rule("InsuranceApprovalRequired", "Verified insurance approval is required.", 409);
        var hours = await db.DoctorSchedules.Where(s => s.DoctorId == clinician).ToListAsync();
        var absences = await db.DoctorAbsences.Where(s => s.DoctorId == clinician).ToListAsync();
        var conflicts = WorkingTimeConflicts(new DateTimeOffset(DateTime.SpecifyKind(appointment.StartTime, DateTimeKind.Utc)), hours, absences).Where(c => c != "TimeInPast").ToList();
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(now, DateTimeKind.Utc), ClinicTimeZone);
        if (!hours.Any(s => s.DayOfWeek == (int)local.DayOfWeek && s.StartTime <= TimeOnly.FromDateTime(local) && s.EndTime > TimeOnly.FromDateTime(local))) conflicts.Add("OutsideWorkingHours");
        if (absences.Any(a => ClinicianAbsenceRules.Overlaps(a,now,now.AddMinutes(60)))) conflicts.Add("ClinicianAbsent");
        if (!await db.Rooms.AnyAsync(r => r.Id == appointment.RoomId && r.IsActive && r.Capacity > 0)) conflicts.Add("RoomRequired");
        var others = await db.Appointments.AsNoTracking().Where(a => a.Id != appointment.Id && a.Status != PTCenter.Domain.Entities.Enums.AppointmentStatus.Completed && a.Status != PTCenter.Domain.Entities.Enums.AppointmentStatus.Cancelled && a.Status != PTCenter.Domain.Entities.Enums.AppointmentStatus.NoShow).ToListAsync();
        var capacity = await db.Rooms.Where(r => r.Id == appointment.RoomId && r.IsActive).Select(r => r.Capacity).SingleOrDefaultAsync();
        conflicts.AddRange(ClinicalCapacity.Conflicts(now, now.AddMinutes(60), appointment.PatientId, clinician, appointment.RoomId ?? 0, others, identity.Role == "Doctor", capacity, actual:true));
        if (conflicts.Count > 0) throw new ClinicalRuleException("Doctor.CapacityOrEligibility", "Treatment cannot start. Review the reported room, clinician or schedule conflict.", 409, new { conflicts = conflicts.Distinct().ToList() });
    }
}
