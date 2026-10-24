using PTCenter.Domain.Entities;

namespace PTCenter.Application.Contracts.Patients;

public class InsuranceDetailsResponse
{
    public InsuranceStatus? Status { get; set; }
    public decimal? CopayPercentage { get; set; }
    public int? ApprovedSessions { get; set; }
    public string? MemberId { get; set; }
    public string? Employer { get; set; }
}