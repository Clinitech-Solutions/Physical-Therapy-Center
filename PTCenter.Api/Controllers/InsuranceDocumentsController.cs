using Microsoft.AspNetCore.Mvc;
using PTCenter.Application.Contracts.InsuranceDocument;
using PTCenter.Application.Interfaces;

namespace PTCenter.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InsuranceDocumentsController(IInsuranceDocumentService documentService) : ControllerBase
{
    private readonly IInsuranceDocumentService _documentService = documentService;

    [HttpPost("Add")]
    public async Task<IActionResult> Add([FromForm] CreateInsuranceDocumentRequest request)
    {
        var result = await _documentService.AddRangeAsync(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _documentService.GetAllAsync();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateInsuranceDocumentRequest request)
    {
        var result = await _documentService.UpdateAsync(id, request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _documentService.DeleteAsync(id);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}