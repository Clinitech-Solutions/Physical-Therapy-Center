using Microsoft.AspNetCore.Http;

namespace PTCenter.Application.Contracts.InsuranceDocument;

public class UpdateInsuranceDocumentRequest
{
    public IFormFile? File { get; set; }
}