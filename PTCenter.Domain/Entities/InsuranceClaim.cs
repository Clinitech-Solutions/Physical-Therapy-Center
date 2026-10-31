namespace PTCenter.Domain.Entities;

/// <summary>An earned, submitted insurer receivable. Settlements are separate Payment records.</summary>
public class InsuranceClaim : BaseEntity
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int CourseAgreementId { get; set; }
    public int TreatmentPlanSessionId { get; set; }
    public int PatientId { get; set; }
    public int InsuranceCompanyId { get; set; }
    public string ApprovalReference { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public string SubmissionReference { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string SubmittedBy { get; set; } = "";
}
