using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTCenter.Api.Extensions;
using PTCenter.Application.Contracts.InsuranceCompany;
using PTCenter.Application.Interfaces;

namespace PTCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.StaffRead)]
public class InsuranceCompaniesController(
    IInsuranceCompanyService insuranceCompanyService) : ControllerBase
{
    private readonly IInsuranceCompanyService _insuranceCompanyService = insuranceCompanyService;


    [HttpPost]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Administration)]
    [ProducesResponseType(typeof(InsuranceCompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Add(CreateInsuranceCompanyRequest request)
    {
        var result = await _insuranceCompanyService.AddAsync(request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }


    [HttpGet]
    [ProducesResponseType(typeof(List<InsuranceCompanyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _insuranceCompanyService.GetAllAsync();

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }


    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InsuranceCompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _insuranceCompanyService.GetByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }


    [HttpPut("{id:int}")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Administration)]
    [ProducesResponseType(typeof(InsuranceCompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        UpdateInsuranceCompanyRequest request)
    {
        var result = await _insuranceCompanyService.UpdateAsync(id, request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }


    [HttpDelete("{id:int}")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Administration)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _insuranceCompanyService.DeleteAsync(id);

        return result.IsSuccess
            ? Ok()
            : result.ToProblem();
    }
}
