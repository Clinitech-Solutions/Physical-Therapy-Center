using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Application.Contracts.Assessments;

public class UpdateAssessmentRequest
{
    public string SeniorId { get; set; } = null!;

    public string Diagnosis { get; set; } = null!;

    public string ConditionType { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime AssessmentDate { get; set; }

    public Status Status { get; set; }
}