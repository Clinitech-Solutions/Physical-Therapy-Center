using Microsoft.AspNetCore.Mvc;
using PTCenter.Application.Contracts.Billing;
using PTCenter.Application.Interfaces;

namespace PTCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetAllInvoices()
    {
        var result = await _billingService.GetAllInvoicesAsync();

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("invoices/{id:int}")]
    public async Task<InvoiceResponse> GetInvoiceById(int id) // تم تعديلها لتتوافق مع الـ Frontend أو إرجاع IActionResult
    {
        var result = await _billingService.GetInvoiceByIdAsync(id);

        if (result.IsFailure)
            return null!; // أو NotFound

        return result.Value;
    }

    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        var result = await _billingService.CreateInvoiceAsync(request);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("invoices/{id:int}/status")]
    public async Task<IActionResult> UpdateInvoiceStatus(int id, [FromBody] UpdateInvoiceStatusRequest request)
    {
        var result = await _billingService.UpdateInvoiceStatusAsync(id, request);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("invoices/{id:int}")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        var result = await _billingService.DeleteInvoiceAsync(id);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(new { message = "Invoice deleted successfully." });
    }

    [HttpPost("payments")]
    public async Task<IActionResult> AddPayment([FromBody] CreatePaymentRequest request)
    {
        var result = await _billingService.AddPaymentAsync(request);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("invoices/{invoiceId:int}/payments")]
    public async Task<IActionResult> GetPaymentsByInvoiceId(int invoiceId)
    {
        var result = await _billingService.GetPaymentsByInvoiceIdAsync(invoiceId);

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }
}