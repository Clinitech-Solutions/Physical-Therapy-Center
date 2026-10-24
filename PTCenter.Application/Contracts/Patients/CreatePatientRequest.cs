
using System.ComponentModel.DataAnnotations;
using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Application.Contracts.Patients;

public class CreatePatientRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public Gender Gender { get; set; } = Gender.Male;

    [Required]
    public DateTime BirthDate { get; set; }

    [Required]
    [Phone]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PaymentType { get; set; }

    [StringLength(20)]
    public string? EmergencyContact { get; set; }

    public InsuranceDetailsResponse? InsuranceDetails { get; set; }

    public int? InsuranceCompanyId { get; set; }
}
