namespace PTCenter.Domain.Entities;

/// <summary>An atomic monthly submission of individual existing invoice claims; creates no charges.</summary>
public class InsuranceClaimSubmission
{
    public int Id { get; set; }
    public int InsuranceCompanyId { get; set; }
    public DateOnly ServiceMonth { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public string Reference { get; set; } = "";
    public string RecordedBy { get; set; } = "";
    public DateTime RecordedAt { get; set; }
}
