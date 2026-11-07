using System.Data;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Billing;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;
using PTCenter.Infrastructure.Services;

namespace PTCenter.Application.Services;

public class BillingService(ApplicationDbContext context) : IBillingService
{
    private static Result<T> Reject<T>(string code, string message, int status = 400) => Result.Failure<T>(new("Billing." + code, message, status));
    private static bool Money(decimal n) => n >= 0 && n <= 999999999 && decimal.Round(n, 2) == n;
    private IQueryable<Invoice> Invoices => context.Invoices.Include(i => i.Patient).ThenInclude(p => p.ApplicationUser)
        .Include(i => i.Patient).ThenInclude(p => p.InsuranceCompany).Include(i => i.Appointment).Include(i => i.Payments);
    public async Task<Result<List<InvoiceResponse>>> GetAllInvoicesAsync() => Result.Success(await MapHistoryAsync(await Invoices.AsNoTracking().OrderByDescending(i => i.Id).ToListAsync()));
    public async Task<Result<InvoiceResponse>> GetInvoiceByIdAsync(int id)
    {
        var invoice = await Invoices.AsNoTracking().SingleOrDefaultAsync(i => i.Id == id);
        return invoice == null ? Reject<InvoiceResponse>("InvoiceNotFound", "Invoice was not found.", 404) : Result.Success((await MapHistoryAsync([invoice])).Single());
    }
    public async Task<Result<InvoiceResponse>> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!Money(request.TotalAmount) || request.TotalAmount <= 0 || !Money(request.PatientAmount) || !Money(request.InsuranceAmount) ||
            request.PatientAmount + request.InsuranceAmount != request.TotalAmount || !Enum.IsDefined(request.Type))
            return Reject<InvoiceResponse>("InvalidAmount", "The patient and insurer liabilities must equal the agreed positive invoice total.");
        var appointment = await context.Appointments.Include(a => a.Patient).SingleOrDefaultAsync(a => a.Id == request.AppointmentId);
        if (appointment == null || appointment.PatientId != request.PatientId) return Reject<InvoiceResponse>("AppointmentNotFound", "Select an appointment belonging to this patient.", 404);
        if (await context.Invoices.AnyAsync(i => i.AppointmentId == request.AppointmentId || appointment.TreatmentPlanSessionId != null && i.TreatmentPlanSessionId == appointment.TreatmentPlanSessionId))
            return Reject<InvoiceResponse>("AlreadyInvoiced", "This session already has an invoice. Reuse its existing balance.", 409);
        if (request.InsuranceAmount > 0 && (appointment.Patient.PaymentType != "Insurance" || appointment.Patient.InsuranceDetails?.Status != InsuranceStatus.Approved))
            return Reject<InvoiceResponse>("InsuranceApprovalRequired", "Insurance liability requires verified approval.");
        var invoice = new Invoice { AppointmentId = request.AppointmentId, PatientId = request.PatientId, TotalAmount = request.TotalAmount,
            PatientAmount = request.PatientAmount, InsuranceAmount = request.InsuranceAmount, Type = request.Type, InvoiceNumber = "I-" + Guid.NewGuid().ToString("N"), Status = InvoiceStatus.Pending };
        context.Invoices.Add(invoice); await context.SaveChangesAsync(); await transaction.CommitAsync(); return await GetInvoiceByIdAsync(invoice.Id);
    }
    public async Task<Result<InvoiceResponse>> UpdateInvoiceStatusAsync(int id, UpdateInvoiceStatusRequest request)
    {
        var invoice = await context.Invoices.Include(i => i.Payments).SingleOrDefaultAsync(i => i.Id == id);
        if (invoice == null) return Reject<InvoiceResponse>("InvoiceNotFound", "Invoice was not found.", 404);
        if (!Enum.IsDefined(request.Status)) return Reject<InvoiceResponse>("InvalidStatus", "Invalid invoice status.");
        if (request.Status != invoice.Status && request.Status is InvoiceStatus.Cancelled or InvoiceStatus.Waived &&
            (invoice.CourseAgreementId != null || await context.Appointments.AnyAsync(a => a.Id == invoice.AppointmentId && a.BookingBatchId != null)))
            return Reject<InvoiceResponse>("AgreementAdjustmentRequired", "A booking agreement needs an explicit financial adjustment; changing an invoice flag cannot erase the saved package liability.", 409);
        var payments = invoice.Payments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount);
        if (request.Status == InvoiceStatus.Paid && (invoice.TotalAmount <= 0 || payments < invoice.TotalAmount))
            return Reject<InvoiceResponse>("PaymentRequired", "An invoice cannot be marked paid without actual financial coverage.");
        if (payments > 0 && request.Status != invoice.Status) return Reject<InvoiceResponse>("PaymentReconciliationRequired", "An invoice with payments requires financial reconciliation before its status can change.");
        invoice.Status = request.Status; await context.SaveChangesAsync(); return await GetInvoiceByIdAsync(id);
    }
    public async Task<Result> DeleteInvoiceAsync(int id)
    {
        var invoice = await context.Invoices.Include(i => i.Payments).Include(i => i.Appointment).SingleOrDefaultAsync(i => i.Id == id);
        if (invoice == null) return Result.Failure(new("Billing.InvoiceNotFound", "Invoice was not found.", 404));
        if (invoice.Payments.Count > 0 || invoice.CourseAgreementId != null || invoice.Appointment?.BookingBatchId != null)
            return Result.Failure(new("Billing.InvoiceHasRecords", "Booking and payment audit records cannot be deleted. Reconcile or explicitly void the financial obligation.", 409));
        context.Invoices.Remove(invoice); await context.SaveChangesAsync(); return Result.Success();
    }
    public async Task<Result<PaymentResponse>> AddPaymentAsync(CreatePaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 90 || !Money(request.Amount) || request.Amount <= 0 || request.Method == null || !Enum.IsDefined(request.Method.Value))
            return Reject<PaymentResponse>("InvalidPayment", "A stable payment key, valid method and positive currency amount are required.");
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var key = $"invoice:{request.InvoiceId}:{request.IdempotencyKey}";
        await ClinicMutationLock.AcquireAsync(context);
        var previous = await context.Payments.SingleOrDefaultAsync(p => p.IdempotencyKey == key);
        if (previous != null) return previous.InvoiceId == request.InvoiceId && previous.Amount == request.Amount && previous.PaymentMethod == request.Method
            ? Result.Success(MapPayment(previous)) : Reject<PaymentResponse>("RetryKeyConflict", "This payment key belongs to a different collection.", 409);
        var invoice = await context.Invoices.Include(i => i.Payments).Include(i => i.Appointment).ThenInclude(a => a!.TreatmentPlanSession).SingleOrDefaultAsync(i => i.Id == request.InvoiceId);
        if (invoice == null) return Reject<PaymentResponse>("InvoiceNotFound", "Invoice was not found.", 404);
        if (invoice.Status is InvoiceStatus.Cancelled or InvoiceStatus.Waived) return Reject<PaymentResponse>("CannotReceivePayment", "A waived or cancelled invoice cannot receive payment.");
        var insurerPayment = request.Method == PaymentMethod.Insurance;
        if (insurerPayment) return Reject<PaymentResponse>("ClaimSettlementRequired", "Record insurer receipts against a submitted delivered-service claim.", 409);
        var received = invoice.Payments.Where(p => CourseFinanceService.PatientReceipt(p)).Sum(p => p.Amount);
        if (request.ExpectedRemaining is {} expected && expected != Math.Max(0, invoice.PatientAmount - received))
            return Reject<PaymentResponse>("StaleBalance", "The patient balance changed. Refresh the invoice before collecting.", 409);
        var liability = insurerPayment ? invoice.InsuranceAmount : invoice.PatientAmount;
        if (request.Amount > liability - received) return Reject<PaymentResponse>("PaymentExceedsBalance", "Payment cannot exceed the remaining liability of its payer.");
        var payment = new Payment { InvoiceId = invoice.Id, Amount = request.Amount, PaymentMethod = request.Method.Value, PayerType = "Patient", CollectionMethod = request.Method.Value.ToString(), IdempotencyKey = key, Status = PaymentStatus.Paid, PaymentDate = DateTime.UtcNow };
        invoice.Payments.Add(payment);
        invoice.PaidAmount = invoice.Payments.Where(p => CourseFinanceService.PatientReceipt(p)).Sum(p => p.Amount);
        var allPaid = invoice.Payments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount);
        invoice.Status = allPaid >= invoice.TotalAmount ? InvoiceStatus.Paid : InvoiceStatus.Pending;
        var session = invoice.TreatmentPlanSessionId != null ? await context.TreatmentPlanSessions.FindAsync(invoice.TreatmentPlanSessionId) : invoice.Appointment?.TreatmentPlanSession;
        if (session != null) session.PaymentStatus = allPaid >= invoice.TotalAmount ? PaymentStatus.Paid : PaymentStatus.Pending;
        await context.SaveChangesAsync(); await transaction.CommitAsync(); return Result.Success(MapPayment(payment));
    }
    public async Task<Result<List<PaymentResponse>>> GetPaymentsByInvoiceIdAsync(int invoiceId)
    {
        if (!await context.Invoices.AnyAsync(i => i.Id == invoiceId)) return Reject<List<PaymentResponse>>("InvoiceNotFound", "Invoice was not found.", 404);
        var rows=(await context.Payments.AsNoTracking().Where(p => p.InvoiceId == invoiceId).OrderBy(p => p.Id).ToListAsync()).Select(MapPayment).ToList();
        var actors=rows.Where(p=>p.RecordedBy!=null).Select(p=>p.RecordedBy!).Distinct().ToArray();
        var names=await context.Users.Where(u=>actors.Contains(u.Id)).ToDictionaryAsync(u=>u.Id,u=>u.FullName);
        foreach(var row in rows)row.RecordedByName=row.RecordedBy==null?null:names.GetValueOrDefault(row.RecordedBy);
        return Result.Success(rows);
    }
    private async Task<List<InvoiceResponse>> MapHistoryAsync(List<Invoice> invoices)
    {
        var courseIds=invoices.Where(i=>i.CourseAgreementId!=null).Select(i=>i.CourseAgreementId!.Value).Distinct().ToArray();
        var agreements=await context.TreatmentCourseAgreements.AsNoTracking().Where(a=>courseIds.Contains(a.Id)).ToDictionaryAsync(a=>a.Id);
        var ids=invoices.Select(i=>i.Id).ToArray();
        var claims=await context.InsuranceClaims.AsNoTracking().Where(c=>ids.Contains(c.InvoiceId)).ToDictionaryAsync(c=>c.InvoiceId);
        var companies=await context.InsuranceCompanies.AsNoTracking().ToDictionaryAsync(c=>c.Id,c=>c.Name);
        return invoices.Select(i=>{var response=MapInvoice(i);var companyId=(i.CourseAgreementId is {} aid ? agreements.GetValueOrDefault(aid)?.InsuranceCompanyId : null) ?? claims.GetValueOrDefault(i.Id)?.InsuranceCompanyId;
            response.InsuranceCompany=companyId is {} cid ? companies.GetValueOrDefault(cid) : i.InsuranceAmount>0 ? "Historical company unavailable" : null;return response;}).ToList();
    }
    private static InvoiceResponse MapInvoice(Invoice i)
    {
        var paid = i.Payments.Where(p => CourseFinanceService.PatientReceipt(p)).Sum(p => p.Amount);
        return new() { Id = i.Id, InvoiceNumber = i.InvoiceNumber, AppointmentId = i.AppointmentId, PatientId = i.PatientId,
            PatientName = i.Patient.ApplicationUser.FullName, PatientPhone = i.Patient.ApplicationUser.PhoneNumber,
            TotalAmount = i.TotalAmount, PatientAmount = i.PatientAmount, InsuranceAmount = i.InsuranceAmount, PaidAmount = paid, RemainingAmount = i.Status is InvoiceStatus.Waived or InvoiceStatus.Cancelled ? 0 : Math.Max(0, i.PatientAmount - paid),
            CourseAgreementId = i.CourseAgreementId, TreatmentPlanSessionId = i.TreatmentPlanSessionId, DeliveredAt = i.DeliveredAt,
            Type = i.Type, Status = i.Status, AppointmentDate = i.Appointment?.StartTime, CreatedAt = i.CreatedAt,
            PaymentMethod = i.Payments.LastOrDefault(p => p.Status == PaymentStatus.Paid)?.PaymentMethod.ToString() };
    }
    private static PaymentResponse MapPayment(Payment p) => new() { Id = p.Id, IdempotencyKey = p.IdempotencyKey, InvoiceId = p.InvoiceId, Amount = p.Amount, PaymentMethod = p.PaymentMethod, Status = p.Status, PaymentDate = p.PaymentDate, TransactionId = p.TransactionId,
        PayerType = p.PayerType ?? (p.PaymentMethod == PaymentMethod.Insurance ? "Insurer" : "Patient"), CollectionMethod = p.CollectionMethod, InsuranceClaimId = p.InsuranceClaimId, RecordedBy=p.CreatedBy };
}
