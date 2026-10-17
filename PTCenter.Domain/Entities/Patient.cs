
namespace PTCenter.Domain.Entities;
public enum InsuranceStatus
{
    Approved,
    Pending,
    Rejected,
    Expired
}

public class Patient : BaseEntity
{
    public int Id { get; set; }
    public string ApplicationUserId { get; set; } = null!;
    public string? PaymentType { get; set; }
    public string? EmergencyContact { get; set; }
    public InsuranceStatus? InsuranceStatus { get; set; } // Approved / Pending / etc.
    public int? InsuranceCompanyId { get; set; }

    public ApplicationUser ApplicationUser { get; set; } = null!;
    public InsuranceCompany? InsuranceCompany { get; set; }
    public ICollection<InsuranceDocument> InsuranceDocuments { get; set; } = new List<InsuranceDocument>();
}
