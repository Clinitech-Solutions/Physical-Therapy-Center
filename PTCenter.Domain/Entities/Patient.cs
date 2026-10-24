
namespace PTCenter.Domain.Entities;

public class Patient : BaseEntity
{
    public int Id { get; set; }
    public string ApplicationUserId { get; set; } = null!;
    public string? PaymentType { get; set; }
    public string? EmergencyContact { get; set; }
    public int? InsuranceCompanyId { get; set; }

    // Insurance Details Object
    public InsuranceDetails? InsuranceDetails { get; set; }

    public ApplicationUser ApplicationUser { get; set; } = null!;
    public InsuranceCompany? InsuranceCompany { get; set; }
    public ICollection<InsuranceDocument> InsuranceDocuments { get; set; } = new List<InsuranceDocument>();
}
