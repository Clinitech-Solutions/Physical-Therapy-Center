using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
namespace PTCenter.Infrastructure.Services;

public static class ClinicalCapacity
{
    public static bool Occupied(Appointment a) => a.ActualCheckIn != null && a.ActualCheckOut == null;
    public static bool Running(Appointment a) => a.TreatmentStartedAt != null && a.TreatmentEndedAt == null;
    public static bool Live(Appointment a) => a.Status is not (AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Completed);
    public static List<string> Conflicts(DateTime start, DateTime end, int patient, string clinician, int room, IEnumerable<Appointment> source, bool doctorTreatment)
    {
        var appointments = source.Where(Live).ToList();
        bool Intersects(Appointment a) => a.StartTime < end && a.EndTime > start;
        var conflicts = new List<string>();
        if (appointments.Any(a => a.PatientId == patient && (Intersects(a) || Occupied(a)))) conflicts.Add("PatientConflict");
        if (appointments.Any(a => a.RoomId == room && (Intersects(a) || Occupied(a)))) conflicts.Add("RoomConflict");
        var related = appointments.Where(a => a.DoctorId == clinician || a.SeniorId == clinician).Where(a => a.TreatmentEndedAt == null).ToList();
        var seniors = related.Where(a => a.SeniorId == clinician);
        if (seniors.Any(a => Intersects(a) || Occupied(a))) conflicts.Add("ClinicianConflict");
        var doctors = related.Where(a => a.DoctorId == clinician && (Intersects(a) || Running(a))).ToList();
        if (!doctorTreatment && doctors.Count > 0) conflicts.Add("ClinicianConflict");
        else if (doctorTreatment)
        {
            var events = doctors.SelectMany(a => new[] {
                (At: Running(a) ? start : (a.StartTime < start ? start : a.StartTime), Delta: 1),
                (At: Running(a) ? end : (a.EndTime > end ? end : a.EndTime), Delta: -1)
            }).OrderBy(e => e.At).ThenBy(e => e.Delta);
            var active = 1;
            foreach (var point in events) { active += point.Delta; if (active > 2) { conflicts.Add("DoctorCapacity"); break; } }
        }
        return conflicts.Distinct().ToList();
    }
}
