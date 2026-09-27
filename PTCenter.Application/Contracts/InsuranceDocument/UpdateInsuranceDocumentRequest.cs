using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PTCenter.Application.Contracts.InsuranceDocument;

public class UpdateInsuranceDocumentRequest
{
    [Range(0, 100)]
    public decimal CoveragePercent { get; set; }

    public IFormFile? File { get; set; }
}