namespace PTCenter.Domain.Entities;

/// <summary>One immutable pricing agreement per prescribed treatment plan, reused by every booking batch.</summary>
public class TreatmentCourseAgreement : BaseEntity
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int TreatmentPlanId { get; set; }
    public string FundingType { get; set; } = "SelfPay";
    public string PricingMode { get; set; } = "PerSession";
    public int PrescribedSessions { get; set; }
    public int CoveredSessions { get; set; }
    public decimal UnitPrice { get; set; }
    public string DiscountType { get; set; } = "None";
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PatientAmount { get; set; }
    public decimal InsuranceAmount { get; set; }
    public decimal CopayPercentage { get; set; }
    public int? InsuranceCompanyId { get; set; }
    public int? ApprovedSessions { get; set; }
    public string? ApprovalReference { get; set; }
    public string? ApprovalSnapshotJson { get; set; }
    public string InstallmentsJson { get; set; } = "[]";
}
