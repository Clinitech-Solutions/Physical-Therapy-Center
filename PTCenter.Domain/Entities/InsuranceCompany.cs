namespace PTCenter.Domain.Entities;

public class InsuranceCompany : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    
    public string? PortalUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
