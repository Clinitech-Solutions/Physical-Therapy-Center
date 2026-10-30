using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;

public class Assessment : BaseEntity
{
    public int Id { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? AppointmentId { get; set; }

    public int PatientId { get; set; }

    public string SeniorId { get; set; } = null!;

    public string Diagnosis { get; set; } = null!;

    public string ConditionType { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;

    public Status Status { get; set; } = Status.Low;

    public Patient Patient { get; set; } = null!;

    public ApplicationUser Senior { get; set; } = null!;

    public TreatmentPlan? TreatmentPlan { get; set; }
}
