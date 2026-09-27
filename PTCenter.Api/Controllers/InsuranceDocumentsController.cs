using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTCenter.Api.Extensions;
using PTCenter.Application.Contracts.InsuranceDocument;
using PTCenter.Application.Interfaces;

namespace PTCenter.Api.Controllers;


[ApiController]
[Route("api/[controller]/[action]")]
[Authorize(Roles = DefaultRoles.Admin.Name + "," + DefaultRoles.Owner.Name)]
public class InsuranceDocumentsController(
    IInsuranceDocumentService insuranceDocumentService)
    : ControllerBase
{
    private readonly IInsuranceDocumentService _insuranceDocumentService =
        insuranceDocumentService;



    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(InsuranceDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Add(
        [FromForm] CreateInsuranceDocumentRequest request)
    {
        var result = await _insuranceDocumentService.AddAsync(request);


        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }



    [HttpGet]
    [ProducesResponseType(typeof(List<InsuranceDocumentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _insuranceDocumentService.GetAllAsync();


        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }




    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InsuranceDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _insuranceDocumentService.GetByIdAsync(id);


        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }




    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(InsuranceDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] UpdateInsuranceDocumentRequest request)
    {
        var result =
            await _insuranceDocumentService.UpdateAsync(id, request);


        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }





    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await _insuranceDocumentService.DeleteAsync(id);


        return result.IsSuccess
            ? Ok()
            : result.ToProblem();
    }
}