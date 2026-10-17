
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Billing;
using PTCenter.Application.Contracts.Patients;

namespace PTCenter.Application.Interfaces;

public interface IBillingService
{
    Task<Result<List<InvoiceResponse>>> GetAllInvoicesAsync();

    Task<Result<InvoiceResponse>> GetInvoiceByIdAsync(int id);

    Task<Result<InvoiceResponse>> CreateInvoiceAsync(
        CreateInvoiceRequest request);

    Task<Result<InvoiceResponse>> UpdateInvoiceStatusAsync(
        int id,
        UpdateInvoiceStatusRequest request);

    Task<Result> DeleteInvoiceAsync(int id);

    Task<Result<PaymentResponse>> AddPaymentAsync(
        CreatePaymentRequest request);

    Task<Result<List<PaymentResponse>>> GetPaymentsByInvoiceIdAsync(
        int invoiceId);
}
