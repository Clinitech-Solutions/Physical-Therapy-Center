using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Application.Contracts.Assessments;

public class AssessmentResponse
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string SeniorId { get; set; } = null!;

    public string SeniorName { get; set; } = string.Empty;

    public string Diagnosis { get; set; } = null!;

    public string ConditionType { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime AssessmentDate { get; set; }

    public Status Status { get; set; }

    public bool HasTreatmentPlan { get; set; }

    public int? TreatmentPlanId { get; set; }
}