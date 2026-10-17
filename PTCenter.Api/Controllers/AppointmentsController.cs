using Microsoft.AspNetCore.Mvc;
using PTCenter.Application.Contracts.Appointments;
using PTCenter.Application.Contracts.Common;
using PTCenter.Application.Interfaces;

namespace PTCenter.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    private readonly IAppointmentService _appointmentService = appointmentService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AppointmentQueryRequest request)
    {
        var result = await _appointmentService.GetAllAsync(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _appointmentService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request)
    {
        var result = await _appointmentService.AddAsync(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAppointmentRequest request)
    {
        var result = await _appointmentService.UpdateAsync(id, request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _appointmentService.DeleteAsync(id);
        return result.IsSuccess ? Ok() : BadRequest(result.Error); // شيلنا .Value هنا تماماً
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var result = await _appointmentService.UpdateStatusAsync(id, status);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}