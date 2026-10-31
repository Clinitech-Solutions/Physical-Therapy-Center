using Microsoft.AspNetCore.Mvc;
using PTCenter.Application.Contracts.InsuranceDocument;
using PTCenter.Application.Interfaces;

namespace PTCenter.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.StaffRead)]
public class InsuranceDocumentsController(IInsuranceDocumentService documentService, IWebHostEnvironment environment) : ControllerBase
{
    private readonly IInsuranceDocumentService _documentService = documentService;

    [HttpPost("Add")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public async Task<IActionResult> Add([FromForm] CreateInsuranceDocumentRequest request)
    {
        var result = await _documentService.AddRangeAsync(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? patientId)
    {
        if (patientId <= 0 || patientId == null && User.IsInRole("Receptionist") && !User.IsInRole("Admin") && !User.IsInRole("Ceo") && !User.IsInRole("Owner"))
            return BadRequest(new { code = "Documents.PatientRequired", description = "Select the patient whose documents you need." });
        var result = await _documentService.GetAllAsync(patientId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> Download(int id)
    {
        var result = await _documentService.GetByIdAsync(id);
        if (result.IsFailure) return NotFound();
        var document = result.Value;
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var allowedRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "insurance-documents")) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(webRoot, document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(candidate)) return NotFound();
        return PhysicalFile(candidate, "application/octet-stream", Path.GetFileName(document.FileName));
    }

    [HttpPut("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateInsuranceDocumentRequest request)
    {
        var result = await _documentService.UpdateAsync(id, request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _documentService.DeleteAsync(id);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
