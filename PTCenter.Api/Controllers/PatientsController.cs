
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTCenter.Api.Extensions;
using PTCenter.Application.Contracts.Common;
using PTCenter.Application.Contracts.Patients;
using PTCenter.Application.Interfaces;

namespace PTCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.StaffRead)]
public class PatientsController(
    IPatientService patientService) : ControllerBase
{
    private readonly IPatientService _patientService = patientService;

    
    
    [HttpPost]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
    [FromBody] CreatePatientRequest request)
    {
        var result = await _patientService.AddAsync(request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<PatientResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PatientQueryRequest request)
    {
        var result = await _patientService.GetAllAsync(request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _patientService.GetByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdatePatientRequest request)
    {
        var result = await _patientService.UpdateAsync(id, request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = PTCenter.Api.Authorization.ReceptionPermissions.Operations)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _patientService.DeleteAsync(id);

        return result.IsSuccess
            ? Ok()
            : result.ToProblem();
    }
}
