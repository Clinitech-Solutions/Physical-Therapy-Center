using Microsoft.AspNetCore.Http;

namespace PTCenter.Application.Contracts.InsuranceDocument;

public class CreateInsuranceDocumentRequest
{
    public int PatientId { get; set; }
    public List<IFormFile> Files { get; set; } = new();
}