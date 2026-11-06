namespace PTCenter.Application.Contracts.Bookings;

public record AbsenceRequest(string OperationKey, string ClinicianId, DateOnly Date, bool FullDay,
    TimeOnly? StartTime, TimeOnly? EndTime, string? Note, int? ExpectedVersion = null, DateOnly? EndDate = null);
public record AbsenceAudit(string Action, string ActorId, string ActorName, DateTime RecordedAt,
    DateTime? PreviousStart, DateTime? PreviousEnd, DateTime Start, DateTime End, string? Note,
    string? OperationKey = null, string? RequestHash = null);
public record AbsenceView(int Id, string ClinicianId, string ClinicianName, string Role, DateTime Start,
    DateTime End, string? Note, int Version, bool Withdrawn, string? RecordedBy, DateTime RecordedAt,
    List<AbsenceAudit> History, List<RecoveryBooking> Affected);
public record RecoveryBooking(int AppointmentId, int PatientId, string PatientName, string PatientGender,
    string AppointmentType, string ClinicianId, string ClinicianName, int? RoomId, string? RoomName,
    DateTime Start, DateTime End, string Version, List<int> AbsenceIds, string ContactStatus);
public record AbsenceImpact(DateTime Start, DateTime End, string TimeZone, List<RecoveryBooking> Affected);
public record RecoveryCandidate(string ClinicianId, string Name, string Role, string Gender, bool GenderMatch,
    bool CoveringTreatment, DateTime Start, DateTime End, int RoomId, string RoomName);
public record RecoveryOptions(RecoveryBooking Appointment, List<RecoveryCandidate> Matching,
    List<RecoveryCandidate> Exceptions, List<RecoveryCandidate> NearestMatching, int SeniorCapacity, string TimeZone);
public record RecoveryRequest(string OperationKey, string ExpectedVersion, int AbsenceId, string ClinicianId,
    DateTimeOffset StartTime, int RoomId, string Action, string Reason, bool UseException = false,
    bool PhoneConsent = false, string? ExceptionReason = null);
public record RecoveryContactRequest(string OperationKey, string ExpectedVersion, int AbsenceId, string Outcome, string? Reason);
public record RecoveryReceipt(int AppointmentId, string OperationKey, string Action, string Version);
