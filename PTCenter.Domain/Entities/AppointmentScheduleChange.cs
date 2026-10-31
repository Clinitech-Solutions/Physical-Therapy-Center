namespace PTCenter.Domain.Entities;

/// <summary>Immutable single-appointment schedule audit and retry receipt.</summary>
public class AppointmentScheduleChange
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public int? TreatmentPlanId { get; set; }
    public string OperationKey { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public string ActorId { get; set; } = "";
    public string ActorName { get; set; } = "";
    public DateTime ChangedAt { get; set; }
    public DateTime PreviousStart { get; set; }
    public DateTime PreviousEnd { get; set; }
    public int? PreviousRoomId { get; set; }
    public string PreviousRoomName { get; set; } = "";
    public DateTime NewStart { get; set; }
    public DateTime NewEnd { get; set; }
    public int NewRoomId { get; set; }
    public string NewRoomName { get; set; } = "";
    public string ClinicianId { get; set; } = "";
    public string? Reason { get; set; }
}
