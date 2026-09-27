
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PTCenter.Application.Contracts.InsuranceDocument;

public class CreateInsuranceDocumentRequest
{
    [Required]
    public int PatientId { get; set; }

    [Range(0, 100)]
    public decimal CoveragePercent { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}