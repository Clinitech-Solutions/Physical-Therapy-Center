using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Billing;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;

namespace PTCenter.Application.Services;

public class BillingService : IBillingService
{
    private readonly ApplicationDbContext _context;

    public BillingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<InvoiceResponse>>> GetAllInvoicesAsync()
    {
        try
        {
            var invoices = await _context.Invoices
                .Include(x => x.Patient)
                .ThenInclude(x => x.ApplicationUser)
                .Include(x => x.Payments)
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var response = invoices.Select(MapInvoice).ToList();

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            // يمكنك تسجيل الخطأ هنا عبر Logger لو موجود
            return Result.Failure<List<InvoiceResponse>>(new Error("Billing.DatabaseError", ex.Message, 500));
        }
    }

    public async Task<Result<InvoiceResponse>> GetInvoiceByIdAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(x => x.Patient)
            .ThenInclude(x => x.ApplicationUser)
            .Include(x => x.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice is null)
        {
            return Result.Failure<InvoiceResponse>(BillingErrors.InvoiceNotFound);
        }

        return Result.Success(MapInvoice(invoice));
    }

    public async Task<Result<InvoiceResponse>> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        if (request.TotalAmount <= 0)
        {
            return Result.Failure<InvoiceResponse>(BillingErrors.InvalidAmount);
        }

        var appointmentExists = await _context.Appointments
            .AnyAsync(x => x.Id == request.AppointmentId);

        if (!appointmentExists)
        {
            return Result.Failure<InvoiceResponse>(BillingErrors.AppointmentNotFound);
        }

        var patientExists = await _context.Patients
            .AnyAsync(x => x.Id == request.PatientId);

        if (!patientExists)
        {
            return Result.Failure<InvoiceResponse>(BillingErrors.PatientNotFound);
        }

        var invoice = new Invoice
        {
            AppointmentId = request.AppointmentId,
            PatientId = request.PatientId,
            TotalAmount = request.TotalAmount,
            Type = request.Type,
            Status = InvoiceStatus.Pending
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // إعادة جلب الفاتورة مع العلاقات لضمان اكتمال البيانات في الـ Response
        return await GetInvoiceByIdAsync(invoice.Id);
    }

    public async Task<Result<InvoiceResponse>> UpdateInvoiceStatusAsync(int id, UpdateInvoiceStatusRequest request)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice is null)
        {
            return Result.Failure<InvoiceResponse>(BillingErrors.InvoiceNotFound);
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return Result.Failure<InvoiceResponse>(BillingErrors.PaidInvoiceCannotBeUpdated);
        }

        invoice.Status = request.Status;
        await _context.SaveChangesAsync();

        return await GetInvoiceByIdAsync(id);
    }

    public async Task<Result> DeleteInvoiceAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice is null)
        {
            return Result.Failure(BillingErrors.InvoiceNotFound);
        }

        if (invoice.Payments.Any())
        {
            return Result.Failure(BillingErrors.InvoiceHasPayments);
        }

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<PaymentResponse>> AddPaymentAsync(CreatePaymentRequest request)
    {
        if (request.Amount <= 0)
        {
            return Result.Failure<PaymentResponse>(BillingErrors.InvalidAmount);
        }

        var invoice = await _context.Invoices
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == request.InvoiceId);

        if (invoice is null)
        {
            return Result.Failure<PaymentResponse>(BillingErrors.InvoiceNotFound);
        }

        if (invoice.Status == InvoiceStatus.Cancelled ||
            invoice.Status == InvoiceStatus.Waived)
        {
            return Result.Failure<PaymentResponse>(BillingErrors.InvoiceCannotReceivePayment);
        }

        var alreadyPaid = invoice.Payments
            .Where(x => x.Status == PaymentStatus.Paid)
            .Sum(x => x.Amount);

        var remaining = invoice.TotalAmount - alreadyPaid;

        if (request.Amount > remaining)
        {
            return Result.Failure<PaymentResponse>(BillingErrors.PaymentExceedsRemainingAmount);
        }

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = request.Amount,
            PaymentMethod = request.Method,
            Status = PaymentStatus.Paid,
            PaymentDate = DateTime.UtcNow
        };

        _context.Payments.Add(payment);

        var newPaidAmount = alreadyPaid + request.Amount;

        invoice.Status = newPaidAmount >= invoice.TotalAmount
            ? InvoiceStatus.Paid
            : InvoiceStatus.Pending;

        await _context.SaveChangesAsync();

        return Result.Success(MapPayment(payment));
    }

    public async Task<Result<List<PaymentResponse>>> GetPaymentsByInvoiceIdAsync(int invoiceId)
    {
        var invoiceExists = await _context.Invoices
            .AnyAsync(x => x.Id == invoiceId);

        if (!invoiceExists)
        {
            return Result.Failure<List<PaymentResponse>>(BillingErrors.InvoiceNotFound);
        }

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync();

        return Result.Success(payments.Select(MapPayment).ToList());
    }

    private static InvoiceResponse MapInvoice(Invoice invoice)
    {
        return new InvoiceResponse
        {
            Id = invoice.Id,
            AppointmentId = invoice.AppointmentId,
            PatientId = invoice.PatientId,
            PatientName = invoice.Patient?.ApplicationUser?.FullName ?? "Unknown",
            TotalAmount = invoice.TotalAmount,
            Type = invoice.Type,
            Status = invoice.Status
        };
    }

    private static PaymentResponse MapPayment(Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            PaymentDate = payment.PaymentDate
        };
    }
}